namespace FinancesApp_Api.StartUp;

public static class CorsInjections
{
    public const string FrontendPolicy = "FrontendDev";

    public static IServiceCollection AddCorsPolicies(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(FrontendPolicy, policy =>
                policy.SetIsOriginAllowed(IsLocalhost)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials()
                      .WithExposedHeaders("Retry-After"));
        });

        return services;
    }

    // Allow any localhost / 127.0.0.1 origin over http or https on any port.
    // Intentionally permissive for local dev/test only — never enable this in production.
    private static bool IsLocalhost(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            return false;

        var host = uri.Host;
        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host == "127.0.0.1"
            || host == "[::1]";
    }
}
