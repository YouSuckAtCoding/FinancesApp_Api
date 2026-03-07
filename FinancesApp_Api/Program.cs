using Asp.Versioning;
using FinanceAppDatabase.DbConnection;
using FinancesApp_Api.Jwt;
using FinancesApp_Api.StartUp;
using FinancesApp_Api.SwaggerValues;
using FinancesApp_CQRS.Dispatchers;
using FinancesApp_CQRS.Interfaces;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddSqlServer
    (
        connectionString: builder.Configuration.GetConnectionString("DbConnection")!,
        name: "SQL Database Check",
        failureStatus: HealthStatus.Unhealthy,
        tags: [ "database", "critical" ]
    );

builder.Services.AddJwtServices(config);

builder.Services.AddScoped<ApiAuthKeyFilter>();
builder.Services.AddApiVersioning(x =>
{
    x.DefaultApiVersion = new ApiVersion(1, 0);
    x.AssumeDefaultVersionWhenUnspecified = true;
    x.ReportApiVersions = true;
    x.ApiVersionReader = new MediaTypeApiVersionReader("api-version");

}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

builder.Services.AddSwaggerGen(x =>
{
    x.OperationFilter<SwaggerDefaultValues>();
});

builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddSingleton<ICommandFactory, CommandFactory>();
builder.Services.AddSingleton<IQueryDispatcher, QueryDispatcher>();
builder.Services.AddSingleton<ICommandDispatcher, CommandDispatcher>();
builder.Services.AddAccountModule();
builder.Services.AddUserModule();
builder.Services.AddCredentialsModule();

var app = builder.Build();

app.UseHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration,
                tags = e.Value.Tags
            })
        });

        await context.Response.WriteAsync(result);
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
