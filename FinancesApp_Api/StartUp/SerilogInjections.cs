using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Grafana.Loki;

namespace FinancesApp_Api.StartUp;

public static class SerilogInjections
{
    public static LoggerConfiguration ConfigureAppLogging(
        this LoggerConfiguration config,
        IConfiguration configuration)
    {
        return config
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(new CommaDelimitedJsonFormatter())
            .WriteTo.File(
                formatter: new CommaDelimitedJsonFormatter(),
                path: "logs/api-.json",
                rollingInterval: RollingInterval.Hour,
                retainedFileCountLimit: 168,
                shared: false,
                flushToDiskInterval: TimeSpan.FromSeconds(2))
            .WriteToGrafanaLoki(configuration);
    }

    private static LoggerConfiguration WriteToGrafanaLoki(
        this LoggerConfiguration config,
        IConfiguration configuration)
    {
        var lokiUrl = configuration["Loki:Url"];
        if (string.IsNullOrWhiteSpace(lokiUrl))
            return config;

        return config.WriteTo.GrafanaLoki(
            uri: lokiUrl,
            labels:
            [
                new LokiLabel { Key = "app", Value = "financesapp-api" },
                new LokiLabel { Key = "env", Value = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production" }
            ]);
    }
}
