using FinanceAppDatabase.DbConnection;
using FinancesApp_Api.StartUp;
using FinancesApp_CQRS.Dispatchers;
using FinancesApp_CQRS.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddSingleton<ICommandFactory, CommandFactory>();
builder.Services.AddSingleton<IQueryDispatcher, QueryDispatcher>();
builder.Services.AddSingleton<ICommandDispatcher, CommandDispatcher>();
builder.Services.AddAccountModule();
builder.Services.AddUserModule();
builder.Services.AddCredentialsModule();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
