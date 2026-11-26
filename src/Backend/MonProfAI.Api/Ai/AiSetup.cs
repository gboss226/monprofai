using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using MonProfAI.Application.Interfaces;
using MonProfAI.Infrastructure.Ai;

namespace MonProfAI.Api.Ai;

/// <summary>Registers the AI tutor: provider adapter, API key and per-student rate limit.</summary>
public static class AiSetup
{
    public const string RateLimitPolicy = "ai-per-user";

    public static IServiceCollection AddAiTutor(
        this IServiceCollection services, IConfiguration configuration, string contentRootPath)
    {
        var options = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();
        services.AddSingleton(options);

        // The key stays on the server: environment variable first, then the git-ignored .env file.
        var apiKey = configuration[AiOptions.ApiKeyVariable]
                     ?? DotEnvFile.ReadValue(contentRootPath, AiOptions.ApiKeyVariable);

        services.AddSingleton<IAiChatService>(sp => options.Provider switch
        {
            "Anthropic" => new AnthropicChatService(
                apiKey, options, sp.GetRequiredService<ILogger<AnthropicChatService>>()),
            _ => throw new InvalidOperationException($"Unknown AI provider '{options.Provider}'"),
        });

        services.AddRateLimiter(limiter =>
        {
            limiter.AddPolicy(RateLimitPolicy, httpContext =>
            {
                // Routes using this policy require authentication, so the user id is always there.
                var partitionKey = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                                   ?? httpContext.Connection.RemoteIpAddress?.ToString()
                                   ?? "anonymous";
                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.RateLimit.PermitLimit,
                    Window = TimeSpan.FromMinutes(options.RateLimit.WindowMinutes),
                    QueueLimit = 0,
                });
            });

            limiter.OnRejected = async (context, cancellationToken) =>
            {
                var response = context.HttpContext.Response;
                response.StatusCode = StatusCodes.Status429TooManyRequests;
                var minutes = options.RateLimit.WindowMinutes;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                    minutes = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalMinutes));
                }

                await response.WriteAsJsonAsync(new
                {
                    code = "RateLimited",
                    message = $"Tu as posé beaucoup de questions ! Tu pourras en poser de nouvelles dans {minutes} minute(s).",
                }, cancellationToken);
            };
        });

        return services;
    }
}
