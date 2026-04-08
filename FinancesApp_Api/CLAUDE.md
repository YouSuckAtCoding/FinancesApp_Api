# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Build entire solution
dotnet build FinancesApp_Api.sln

# Run the API
dotnet run --project FinancesApp_Api.csproj

# Run all tests
dotnet test ../FinanceApp_Tests/FinancesApp_Tests.csproj

# Run a single test class
dotnet test ../FinanceApp_Tests/FinancesApp_Tests.csproj --filter "FullyQualifiedName~ApplyDeltaHandlerTests"

# Run a single test method
dotnet test ../FinanceApp_Tests/FinancesApp_Tests.csproj --filter "FullyQualifiedName~ApplyDeltaHandlerTests.TestMethodName"

# Docker (SQL Server, Redis, Prometheus, Grafana)
docker-compose up --build
```

## Architecture

**Modular Monolith** with hand-rolled **CQRS + Event Sourcing** (no MediatR, no EF Core). .NET 8.

### Solution Projects

| Project | Role |
|---|---|
| `FinancesApp_Api` | ASP.NET Core host — controllers, DI wiring (`StartUp/`), JWT config, Swagger |
| `FinancesApp_CQRS` | Framework layer — dispatchers, event store, outbox processor, projection checkpoint |
| `FinancesApp_Module_Account` | Account domain module (event-sourced aggregate, commands, queries, projections) |
| `FinancesApp_Module_User` | User domain module |
| `FinancesApp_Module_Credentials` | Credentials domain module |
| `FinanceAppDatabase` | Raw ADO.NET data access — `IDbConnectionFactory`, `ICommandFactory`, `SqlDataReaderExtensions` |
| `Identity.Api` | Separate microservice for JWT token generation |
| `FinanceApp_Tests` | Integration tests (xUnit, Moq, NSubstitute, FluentAssertions, Testcontainers) |
| `FinancesAppDb` | SQL Server Database Project (.sqlproj) |

### CQRS Flow

Commands: `ICommand` -> `ICommandHandler<TCommand, TResult>` -> `ICommandDispatcher`
Queries: `IQuery<TResult>` -> `IQueryHandler<TQuery, TResult>` -> `IQueryDispatcher`
Events: `IDomainEvent` -> `IEventHandler` -> `IEventDispatcher`

Each module registers its own handlers in `StartUp/*ModuleInjections.cs` as extension methods on `IServiceCollection`. Projection event handlers are registered on `WebApplication` in the same file.

### Event Sourcing (Account Module)

The Account aggregate is fully event-sourced. State changes produce domain events appended to the Event Store with optimistic concurrency (`UPDLOCK`, `ROWLOCK`). The Outbox pattern ensures transactional consistency — events go to the `[Outbox]` table in the same transaction. `OutboxProcessor` (a `BackgroundService`) dispatches them to projections that build read-optimized tables. Projections use `IProjectionCheckpoint` for idempotency.

### Domain Patterns

**AggregateRoot** (`Domain/AggregateRoot.cs` in each module): base class tracking `CurrentVersion`/`NextVersion` for optimistic concurrency. `Raise(IDomainEvent)` records events internally, calls the abstract `Apply()`, and increments `NextVersion`. Each aggregate implements `RebuildFromEvents(List<IDomainEvent>)` for event replay. The event store consumes `GetUncommittedEvents()`/`ClearUncommittedEvents()`.

**DomainResult\<T\>**: static generic result envelope returned by command handlers and domain operations. Use this instead of throwing exceptions for domain validation failures.

**Money** (`Domain/ValueObjects/Money.cs` in Account module): `readonly record struct` enforcing 3-letter ISO currency codes, rounding to 2 decimal places (`MidpointRounding.AwayFromZero`), and same-currency arithmetic. All financial amounts must use this type.

### Data Access

All database access uses raw SQL via `IDbConnectionFactory` and `ICommandFactory` (ADO.NET, no EF Core). Each module has separate read and write repositories. Multi-statement consistency uses `ExecuteInScopeWithTransactionAsync`.

## Testing

Integration tests run against a real SQL Server via **Testcontainers**. Test fixtures (`SqlFixture`, `DatabaseInitializer`) set up the DB from SQL scripts in `FinanceApp_Tests/` (latest is `FinanceAppDb_V*.sql`). Remove any `USE` statements from scripts before running tests.

## Key Conventions

- **Do not edit SQL migration/test scripts** — the user handles these manually. Only create C# structures and source table `.sql` files.
- Connection strings are in `appsettings.Development.json` (local) or Docker Compose environment vars.
- JWT auth configured in `Jwt/` and `StartUp/JwtInjections.cs`. An additional `ApiAuthKeyFilter` is registered as a scoped service for API key authentication.
- API versioning via `Asp.Versioning` (media type version reader).
- Docker Compose exposes: SQL Server `:1470`, Redis `:1405`, Prometheus `:9090`, Grafana `:3000`.
