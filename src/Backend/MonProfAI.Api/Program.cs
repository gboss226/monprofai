using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MonProfAI.Api.Ai;
using MonProfAI.Application.Behaviors;
using MonProfAI.Application.Interfaces;
using MonProfAI.Application.UseCases.Auth;
using MonProfAI.Infrastructure.Data;
using MonProfAI.Infrastructure.Repositories;
using MonProfAI.Infrastructure.Seeding;
using MonProfAI.Infrastructure.Services;

// `dotnet run -- seed` applies migrations, loads the test data and exits (see README).
const string SeedCommand = "seed";
var seedMode = args.Contains(SeedCommand);
// `dotnet run -- tutor-eval` plays test sessions against the real model and grades the tutor (see README).
var tutorEvalMode = args.Contains(TutorEval.Command);

var builder = WebApplication.CreateBuilder(
    args.Where(a => a != SeedCommand && a != TutorEval.Command).ToArray());

// Load environment variables from .env file or system
builder.Configuration.AddEnvironmentVariables();

// Configuration
var jwtSecret = builder.Configuration["JwtSettings:Secret"] ?? throw new InvalidOperationException("JwtSettings:Secret not configured");
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "MonProfAI";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "MonProfAI";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Database
builder.Services.AddDbContext<MonProfAIDbContext>(options =>
    options.UseNpgsql(connectionString));

// Services
builder.Services.AddScoped<IAuthService>(sp =>
    new AuthService(jwtSecret, jwtIssuer, jwtAudience));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ILearnerProfileRepository, LearnerProfileRepository>();
builder.Services.AddScoped<INotionRepository, NotionRepository>();
builder.Services.AddScoped<DevDataSeeder>();
builder.Services.AddAiTutor(builder.Configuration, builder.Environment.ContentRootPath);

// MediatR with FluentValidation
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining(typeof(RegisterUserCommand)));
builder.Services.AddValidatorsFromAssemblyContaining(typeof(RegisterUserCommandValidator));

// MediatR Pipeline Validation
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue("accessToken", out var token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", corsPolicyBuilder =>
    {
        corsPolicyBuilder.WithOrigins("http://localhost:3000", "http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (seedMode)
{
    // The seeded accounts share a password published in the README: never load them outside development.
    if (!app.Environment.IsDevelopment())
    {
        Console.Error.WriteLine(
            $"Seed refusé : environnement '{app.Environment.EnvironmentName}'. Le seed ne tourne qu'en Development.");
        Environment.ExitCode = 1;
        return;
    }

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MonProfAIDbContext>();
    await db.Database.MigrateAsync();
    var result = await scope.ServiceProvider.GetRequiredService<DevDataSeeder>().SeedAsync();
    Console.WriteLine(
        $"Seed terminé : {result.Total} ligne(s) créée(s) — matières {result.SubjectsCreated}, " +
        $"chapitres {result.ChaptersCreated}, notions {result.NotionsCreated}, " +
        $"prérequis {result.PrerequisitesCreated}, élèves {result.UsersCreated}, " +
        $"profils {result.ProfilesCreated}, maîtrises {result.MasteriesCreated}.");
    return;
}

if (tutorEvalMode)
{
    if (!app.Environment.IsDevelopment())
    {
        Console.Error.WriteLine("L'évaluation du tuteur ne tourne qu'en Development (elle utilise les comptes du seed).");
        Environment.ExitCode = 1;
        return;
    }

    Environment.ExitCode = await TutorEval.RunAsync(app);
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowReact");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

app.Run();

// Lets integration tests start the API with WebApplicationFactory<Program>.
public partial class Program;
