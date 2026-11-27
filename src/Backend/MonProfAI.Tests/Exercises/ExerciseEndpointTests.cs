using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MonProfAI.Application.Interfaces;
using MonProfAI.Domain.Entities;
using MonProfAI.Infrastructure.Data;
using MonProfAI.Infrastructure.Services;
using Xunit;

namespace MonProfAI.Tests.Exercises;

/// <summary>The exercise routes on the real API, an in-memory database and a scripted model (no real call).</summary>
public class ExerciseEndpointTests : IDisposable
{
    private const string JwtSecret = "your-super-secret-key-change-this-in-production-at-least-32-characters-long!";

    private readonly Guid _fractionsId = Guid.NewGuid();
    private readonly Guid _pythagoreId = Guid.NewGuid();
    private readonly ScriptedAi _ai = new();
    private readonly WebApplicationFactory<Program> _factory;

    private sealed class ScriptedAi : IAiChatService
    {
        public string Answer { get; set; } = "AUCUNE";
        public int Calls { get; private set; }
        public AiChatRequest? LastRequest { get; private set; }

        public async IAsyncEnumerable<string> StreamReplyAsync(AiChatRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            LastRequest = request;
            await Task.Yield();
            yield return Answer;
        }
    }

    public ExerciseEndpointTests()
    {
        var databaseName = Guid.NewGuid().ToString();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IAiChatService>(_ai);
                services.RemoveAll<DbContextOptions<MonProfAIDbContext>>();
                services.AddDbContext<MonProfAIDbContext>(o => o.UseInMemoryDatabase(databaseName));
            });
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MonProfAIDbContext>();
        var subject = new Subject { Code = "maths", Name = "Mathématiques" };
        var fractions = new Chapter { Subject = subject, Title = "Fractions", SchoolLevel = SchoolLevel.Quatrieme, Order = 2 };
        fractions.Notions.Add(new Notion { Id = _fractionsId, Title = "Additionner des fractions", Order = 1 });
        var pythagore = new Chapter { Subject = subject, Title = "Théorème de Pythagore", SchoolLevel = SchoolLevel.Quatrieme, Order = 7 };
        pythagore.Notions.Add(new Notion { Id = _pythagoreId, Title = "Calculer une longueur avec Pythagore", Order = 1 });
        db.AddRange(subject, fractions, pythagore);
        db.SaveChanges();
    }

    public void Dispose() => _factory.Dispose();

    private async Task<HttpClient> Student(Guid? userId = null)
    {
        var client = _factory.CreateClient();
        var (token, _) = await new AuthService(JwtSecret).GenerateTokensAsync(userId ?? Guid.NewGuid());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<int> ExerciseCount()
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<MonProfAIDbContext>().Exercises.CountAsync();
    }

    private sealed record Created(Guid Id, Guid NotionId, bool NotionDetected);

    private sealed record NotionBody(Guid Id, string Title, string ChapterTitle, string SchoolLevel);

    private sealed record ExerciseBody(Guid Id, string Statement, string Status, bool NotionDetected, NotionBody Notion);

    private sealed record ErrorBody(string Code, string Message);

    [Fact]
    public async Task Create_WithAChosenNotion_SavesItWithoutCallingTheModel()
    {
        var client = await Student();

        var response = await client.PostAsJsonAsync("/api/exercises",
            new { statement = "  Calcule $\\frac{2}{3} + \\frac{5}{4}$.  ", notionId = _fractionsId });
        var created = await response.Content.ReadFromJsonAsync<Created>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(_fractionsId, created!.NotionId);
        Assert.False(created.NotionDetected);
        Assert.Equal(0, _ai.Calls);

        var exercise = await client.GetFromJsonAsync<ExerciseBody>($"/api/exercises/{created.Id}");
        Assert.Equal("Calcule $\\frac{2}{3} + \\frac{5}{4}$.", exercise!.Statement);
        Assert.Equal("ToSolve", exercise.Status);
        Assert.Equal("Additionner des fractions", exercise.Notion.Title);
        Assert.Equal("Fractions", exercise.Notion.ChapterTitle);
        Assert.Equal("Quatrieme", exercise.Notion.SchoolLevel);
    }

    [Fact]
    public async Task Create_WithoutNotion_UsesTheNotionDetectedByTheModel()
    {
        var client = await Student();
        _ai.Answer = "N2";

        var response = await client.PostAsJsonAsync("/api/exercises",
            new { statement = "ABC est rectangle en A, AB = 3 cm et AC = 4 cm. Calcule BC." });
        var created = await response.Content.ReadFromJsonAsync<Created>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(_pythagoreId, created!.NotionId);
        Assert.True(created.NotionDetected);
    }

    [Fact]
    public async Task Create_WhenNoNotionIsDetected_Returns422AndSavesNothing()
    {
        var client = await Student();
        _ai.Answer = "AUCUNE";

        var response = await client.PostAsJsonAsync("/api/exercises", new { statement = "Raconte tes vacances." });
        var error = await response.Content.ReadFromJsonAsync<ErrorBody>();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("NotionNotDetected", error!.Code);
        Assert.Equal(0, await ExerciseCount());
    }

    [Theory]
    [InlineData("   ", false)]
    [InlineData("Calcule 2 + 2", true)]
    public async Task Create_WithAnEmptyStatementOrUnknownNotion_Returns400(string statement, bool unknownNotion)
    {
        var client = await Student();

        var response = await client.PostAsJsonAsync("/api/exercises",
            new { statement, notionId = unknownNotion ? Guid.NewGuid() : _fractionsId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await ExerciseCount());
    }

    [Fact]
    public async Task Create_WithoutLogin_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/exercises",
            new { statement = "Calcule 2 + 2", notionId = _fractionsId });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Smallest valid PNG header: enough for the format check, never decoded by the scripted model.
    private static readonly byte[] PngBytes = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13 };

    private static MultipartFormDataContent Photo(byte[] bytes, string contentType = "image/png")
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { file, "image", "photo.png" } };
    }

    private sealed record TranscriptionBody(string Statement);

    [Fact]
    public async Task Transcribe_ReturnsTheStatementReadFromThePhoto_AndStoresNothing()
    {
        var client = await Student();
        _ai.Answer = "  1. Calcule $\\frac{2}{3} + \\frac{5}{4}$.  ";

        var response = await client.PostAsync("/api/exercises/transcribe", Photo(PngBytes));
        var body = await response.Content.ReadFromJsonAsync<TranscriptionBody>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("1. Calcule $\\frac{2}{3} + \\frac{5}{4}$.", body!.Statement);
        var image = Assert.Single(_ai.LastRequest!.Messages.Single().Images!);
        Assert.Equal("image/png", image.MediaType);
        Assert.Equal(PngBytes, image.Data);
        Assert.Equal(0, await ExerciseCount());
    }

    [Fact]
    public async Task Transcribe_WhenThePhotoIsNotReadable_Returns422()
    {
        var client = await Student();
        _ai.Answer = "ILLISIBLE";

        var response = await client.PostAsync("/api/exercises/transcribe", Photo(PngBytes));
        var error = await response.Content.ReadFromJsonAsync<ErrorBody>();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("NotReadable", error!.Code);
    }

    [Fact]
    public async Task Transcribe_RejectsAFileThatIsNotAnImage_WhateverItClaims()
    {
        var client = await Student();

        var response = await client.PostAsync("/api/exercises/transcribe",
            Photo("%PDF-1.7 not an image"u8.ToArray(), contentType: "image/png"));
        var error = await response.Content.ReadFromJsonAsync<ErrorBody>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("UnsupportedImage", error!.Code);
        Assert.Equal(0, _ai.Calls);
    }

    [Fact]
    public async Task Transcribe_WithoutAPhoto_Returns400()
    {
        var client = await Student();

        var response = await client.PostAsync("/api/exercises/transcribe", new MultipartFormDataContent());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Transcribe_WithoutLogin_Returns401()
    {
        var response = await _factory.CreateClient().PostAsync("/api/exercises/transcribe", Photo(PngBytes));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_AnotherStudentsExercise_Returns404()
    {
        var owner = await Student();
        var created = await (await owner.PostAsJsonAsync("/api/exercises",
            new { statement = "Calcule 2/3 + 1/3", notionId = _fractionsId })).Content.ReadFromJsonAsync<Created>();

        var response = await (await Student()).GetAsync($"/api/exercises/{created!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
