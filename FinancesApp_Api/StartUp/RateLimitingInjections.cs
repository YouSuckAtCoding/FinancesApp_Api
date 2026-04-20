using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace FinancesApp_Api.StartUp;

public static class RateLimitingInjections
{
    public const string GlobalPolicy = "global";
    public const string AuthPolicy = "auth";

    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter(GlobalPolicy, limiter =>
            {
                limiter.PermitLimit = 100;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter(AuthPolicy, limiter =>
            {
                limiter.PermitLimit = 10;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueLimit = 0;
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync(
                    """{"error": "Too many requests. Please try again later."}""",
                    cancellationToken);
            };
        });

        return services;
    }
}
