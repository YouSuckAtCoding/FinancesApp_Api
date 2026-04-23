using System.Threading.RateLimiting;

namespace FinancesApp_Api.StartUp;

public static class RateLimitingInjections
{
    public const string GlobalPolicy = "global";
    public const string AuthPolicy = "auth";
    public const string VerifyTotpPolicy = "verify-totp";
    public const string DeltaPolicy = "delta";

    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Global limiter — chained: sliding window (rate) + concurrency (parallel cap).
            // Applied to every request. Per-endpoint policies layer on top of this.
            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: GetClientIp(context),
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 50,
                            QueueLimit = 0
                        })),
                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetConcurrencyLimiter(
                        partitionKey: GetUserIdFromJwt(context),
                        factory: _ => new ConcurrencyLimiterOptions
                        {
                            PermitLimit = 10,
                            QueueLimit = 0
                        })));

            // Auth (Login) — partitioned by IP + operation type.
            // NOTE: This endpoint is pre-authentication so we cannot use userId claims.
            // IP-based partitioning can be bypassed by clients behind NATs or shared proxies,
            // where multiple distinct users share the same public IP. In those scenarios
            // legitimate users may be rate-limited due to another user's activity.
            // A complementary approach (e.g. login-string hashing via a custom middleware
            // that buffers the body) should be considered if NAT bypass becomes a problem.
            options.AddPolicy(AuthPolicy, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: $"{GetClientIp(context)}:login",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromSeconds(30),
                        SegmentsPerWindow = 50,
                        QueueLimit = 0
                    }));

            // Verify TOTP — partitioned by userId (from JWT) + operation type.
            // Tight limit: 5 attempts per 30s window to prevent TOTP brute-force.
            options.AddPolicy(VerifyTotpPolicy, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: $"{GetUserIdFromJwt(context)}:verify-totp",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromSeconds(30),
                        SegmentsPerWindow = 50,
                        QueueLimit = 0
                    }));

            // ApplyDelta — partitioned by userId (from JWT) + operation type.
            // Financial mutations get a tighter per-user rate than global.
            options.AddPolicy(DeltaPolicy, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: $"{GetUserIdFromJwt(context)}:apply-delta",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromSeconds(30),
                        SegmentsPerWindow = 50,
                        QueueLimit = 0
                    }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/json";

                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry)
                    ? retry
                    : TimeSpan.Zero;

                if (retryAfter > TimeSpan.Zero)
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();

                await context.HttpContext.Response.WriteAsync(
                    """{"error": "Too many requests. Please try again later."}""",
                    cancellationToken);
            };
        });

        return services;
    }

    private static string GetClientIp(HttpContext context)
    {
        return context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";
    }

    private static string GetUserIdFromJwt(HttpContext context)
    {
        return context.User.FindFirst("userid_enc")?.Value
            ?? context.User.FindFirst("sub")?.Value
            ?? GetClientIp(context);
    }
}
