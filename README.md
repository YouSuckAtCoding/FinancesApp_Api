# 💰 FinancesApp API

A **Modular Monolith** REST API for personal finance management, built with **CQRS**, **Event Sourcing**, **Outbox Pattern**, and clean architecture principles in **ASP.NET Core**.

---

## 🏗️ Architecture

This project follows a **Modular Monolith** approach — all modules run in the same process but are fully decoupled from each other. Communication between modules happens through a custom-built **CQRS pipeline** (no MediatR).

The **Account** domain is entirely event-sourced: every state change is captured as an immutable domain event, persisted to an append-only Event Store, and projected into read-optimized tables for querying.

---

## ⚙️ Tech Stack

| Layer | Technology |
|---|---|
| **Framework** | ASP.NET Core (.NET 8) |
| **Database** | SQL Server (raw ADO.NET — no EF Core) |
| **CQRS** | Custom hand-rolled `ICommandDispatcher` / `IQueryDispatcher` |
| **Event Store** | Custom append-only store with optimistic concurrency (`UPDLOCK`, `ROWLOCK`) |
| **Outbox** | Transactional Outbox pattern with background processor |
| **Projections** | Event-driven read-model projections with idempotency checkpoints |
| **Observability** | Prometheus metrics + Grafana dashboards |
| **Auth** | JWT authentication via dedicated Identity API |
| **API Versioning** | Built-in ASP.NET Core API versioning |
| **Caching** | Redis |
| **Containers** | Docker + Docker Compose |
| **Testing** | xUnit, Moq, NSubstitute, FluentAssertions |
| **API Docs** | Swagger / OpenAPI |
| **Health Checks** | SQL Server health checks |

---

## 📐 CQRS Pattern

Commands and queries are fully separated:

- `ICommand` → `ICommandHandler<TCommand>` → `ICommandDispatcher`
- `IQuery<TResult>` → `IQueryHandler<TQuery, TResult>` → `IQueryDispatcher`

---

## 📦 Event Sourcing

The Account aggregate is fully event-sourced. All state mutations produce domain events that are appended to the Event Store.

### Domain Events

| Event | Description |
|---|---|
| `AccountCreatedEvent` | A new account was opened |
| `DepositEvent` | Funds deposited into an account |
| `WithdrawEvent` | Funds withdrawn from an account |
| `CreditUpdatedEvent` | Credit card debt updated |
| `CalculatedCreditLimitEvent` | Credit limit recalculated |
| `CredidCardStatementPaymentEvent` | Credit card statement payment applied |
| `DebtRecalculatedEvent` | Debt and due date recalculated |
| `UpdatedAccountEvent` | Full account state sync |
| `AccountClosedEvent` | Account closed |

## 📤 Outbox Pattern

Guarantees **transactional consistency** between the Event Store and downstream side effects. Events are written to the `[Outbox]` table in the **same transaction** as the Event Store append.

### Concurrency Safety

- Pending entries are fetched with `UPDLOCK, READPAST` hints, ensuring multiple processor instances won't pick up the same batch.
- 

## 📊 Projections & Idempotency

Projections build **read-optimized tables** from domain events, keeping the Event Store as the single source of truth without polluting it with read concerns.


## 🔐 Authentication & Identity

- **JWT Authentication** — The main API validates JWT bearer tokens on protected endpoints.
- **Identity.Api** — A dedicated microservice responsible for token generation (`TokenGenerationRequest`), with its own Dockerfile and configuration.
- **API Versioning** — Endpoints are versioned for backward compatibility.

## 📈 Observability — Prometheus & Grafana

The application exposes Prometheus metrics for critical infrastructure components.

### Docker Compose Services

Prometheus and Grafana are included in the `docker-compose.yaml`:

- **Prometheus** — `http://localhost:9090` — scrapes the API metrics endpoint.
- **Grafana** — `http://localhost:3000` — dashboards for visualizing metrics (default password: `admin`).

---

## 🗄️ Data Access

- Raw SQL via `IDbConnectionFactory` and `ICommandFactory`
- `SqlDataReaderExtensions` for clean result mapping
- Each module has separate **read** and **write** repositories
- Transactions managed via `ExecuteInScopeWithTransactionAsync` for multi-statement consistency

---

## 🚀 Getting Started

```bash
docker-compose up --build
```

### Running locally

1. Update the connection string in `appsettings.Development.json`
2. Run the SQL setup scripts from `FinanceApp_Tests/` (use the latest `FinanceAppDb_V*.sql`)
3. Start the API:

```bash
dotnet run --project FinancesApp_Api
```

### Docker Compose Services

| Service | Port | Description |
|---|---|---|
| `db` | `1470` | SQL Server 2022 |
| `redis` | `1405` | Redis cache |
| `prometheus` | `9090` | Prometheus metrics server |
| `grafana` | `3000` | Grafana dashboards |

---

## 🧪 Tests

Integration tests run against a real SQL Server instance using `SqlFixture` and `DatabaseInitializer`.
The Initializer requires a SQL script that can be generated in MSSQL. Make sure to remove any `USE` statements before running the tests.

### Test Coverage

- **Account Handlers** — `ApplyDeltaHandlerTests`, `GetAccountByIdHandlerTests`
- **Outbox** — `EventDispatcherTests`, `EventStoreOutboxWorkflowTests`, `OutboxProcessorTests`
- **Frameworks** — xUnit, Moq, NSubstitute, FluentAssertions

```bash
dotnet test
```

---

## 📁 Project Structure

```
├── FinancesApp_Api/
│   ├── Controllers/
│   ├── Endpoints/
│   ├── Contracts/
│   ├── StartUp/               ← Module DI registrations
│   ├── docker-compose.yaml    ← SQL Server, Redis, Prometheus, Grafana
│   ├── prometheus.yaml        ← Prometheus scrape config
│   └── Program.cs
├── FinancesApp_CQRS/
│   ├── Interfaces/            ← IEventStore, IEventDispatcher, IEventHandler, IProjectionCheckpoint
│   ├── Dispatchers/           ← CommandDispatcher, QueryDispatcher, EventDispatcher
│   ├── EventStore/            ← EventStore (append, load, outbox insert, concurrency control)
│   ├── Outbox/                ← OutboxEntry, OutboxProcessor (BackgroundService)
│   └── Projections/           ← ProjectionCheckpoint (idempotency)
├── FinancesApp_Module_Account/
│   ├── Application/
│   │   ├── AccountProjection.cs   ← Event → read-model projection
│   │   ├── Commands/Handlers/     ← CreateAccount, ApplyDelta, DeleteAccount
│   │   ├── Queries/Handlers/      ← GetAccountById
│   │   └── Repositories/
│   └── Domain/
│       ├── Account.cs
│       ├── AggregateRoot.cs
│       └── Events/            ← 9 domain events
├── FinancesApp_Module_Credentials/
├── FinancesApp_Module_User/
├── FinanceAppDatabase/
│   ├── DbConnection/          ← IDbConnectionFactory, ICommandFactory
│   └── Utils/                 ← SqlDataReaderExtensions
├── Identity.Api/              ← JWT token generation microservice
├── HostedChannelTests/
└── FinanceApp_Tests/
    ├── AccountTests/
    ├── OutboxTests/
    └── Fixtures/
```

---

## 📄 License

MIT

---

# 💰 FinancesApp API

Uma API REST em **Monólito Modular** para gerenciamento de finanças pessoais, construída com **CQRS**, **Event Sourcing**, **Outbox Pattern** e princípios de arquitetura limpa em **ASP.NET Core**.

---

## 🏗️ Arquitetura

O projeto segue a abordagem de **Monólito Modular** — todos os módulos rodam no mesmo processo, mas são completamente desacoplados entre si. A comunicação entre módulos acontece por meio de um **pipeline CQRS customizado** (sem MediatR).

O domínio de **Account** é inteiramente baseado em Event Sourcing: toda mudança de estado é capturada como um evento de domínio imutável, persistido em um Event Store append-only, e projetado em tabelas otimizadas para leitura.

---

## ⚙️ Stack

| Camada | Tecnologia |
|---|---|
| **Framework** | ASP.NET Core (.NET 8) |
| **Banco de Dados** | SQL Server (ADO.NET puro — sem EF Core) |
| **CQRS** | `ICommandDispatcher` / `IQueryDispatcher` feitos à mão |
| **Event Store** | Store append-only com concorrência otimista (`UPDLOCK`, `ROWLOCK`) |
| **Outbox** | Padrão Transactional Outbox com processador em background |
| **Projeções** | Projeções de read-model orientadas a eventos com checkpoints de idempotência |
| **Observabilidade** | Métricas Prometheus + Dashboards Grafana |
| **Autenticação** | JWT via Identity API dedicada |
| **Versionamento de API** | Versionamento nativo do ASP.NET Core |
| **Cache** | Redis |
| **Containers** | Docker + Docker Compose |
| **Testes** | xUnit, Moq, NSubstitute, FluentAssertions |
| **Documentação** | Swagger / OpenAPI |
| **Health Checks** | SQL Server |

---

## 📐 Padrão CQRS

Commands e queries são completamente separados:

- `ICommand` → `ICommandHandler<TCommand>` → `ICommandDispatcher`
- `IQuery<TResult>` → `IQueryHandler<TQuery, TResult>` → `IQueryDispatcher`

## 📦 Event Sourcing

O agregado Account é inteiramente baseado em Event Sourcing. Todas as mutações de estado produzem eventos de domínio que são adicionados ao Event Store.

### Eventos de Domínio

| Evento | Descrição |
|---|---|
| `AccountCreatedEvent` | Uma nova conta foi aberta |
| `DepositEvent` | Fundos depositados em uma conta |
| `WithdrawEvent` | Fundos retirados de uma conta |
| `CreditUpdatedEvent` | Dívida do cartão de crédito atualizada |
| `CalculatedCreditLimitEvent` | Limite de crédito recalculado |
| `CredidCardStatementPaymentEvent` | Pagamento de fatura do cartão aplicado |
| `DebtRecalculatedEvent` | Dívida e data de vencimento recalculadas |
| `UpdatedAccountEvent` | Sincronização completa do estado da conta |
| `AccountClosedEvent` | Conta encerrada |

## 📤 Padrão Outbox

Garante **consistência transacional** entre o Event Store e efeitos colaterais downstream. Eventos são escritos na tabela `[Outbox]` na **mesma transação** do append no Event Store.

### Segurança de Concorrência

- Entradas pendentes são buscadas com hints `UPDLOCK, READPAST`, garantindo que múltiplas instâncias do processador não peguem o mesmo lote.

---

## 📊 Projeções & Idempotência

Projeções constroem **tabelas otimizadas para leitura** a partir de eventos de domínio, mantendo o Event Store como fonte única da verdade.

## 🔐 Autenticação & Identidade

- **Autenticação JWT** — A API principal valida tokens JWT bearer em endpoints protegidos.
- **Identity.Api** — Um microsserviço dedicado responsável pela geração de tokens, com seu próprio Dockerfile e configuração.
- **Versionamento de API** — Endpoints são versionados para compatibilidade retroativa.

---

## 📈 Observabilidade — Prometheus & Grafana

A aplicação expõe métricas Prometheus para componentes críticos de infraestrutura.

### Serviços Docker Compose

- **Prometheus** — `http://localhost:9090` — coleta métricas do endpoint da API.
- **Grafana** — `http://localhost:3000` — dashboards para visualização de métricas (senha padrão: `admin`).

---

## 🚀 Como Executar

```bash
docker-compose up --build
```

### Rodando localmente

1. Atualize a connection string em `appsettings.Development.json`
2. Execute os scripts SQL de `FinanceApp_Tests/` (use o mais recente `FinanceAppDb_V*.sql`)
3. Inicie a API:

```bash
dotnet run --project FinancesApp_Api
```

---

## 🧪 Testes

Os testes de integração rodam contra uma instância real do SQL Server usando `SqlFixture` e `DatabaseInitializer`.
É necessária a criação de scripts SQL que podem ser feitos pelo MSSQL. Lembre-se de remover qualquer `USE` statement do script antes de rodar.

### Cobertura de Testes

- **Account Handlers** — `ApplyDeltaHandlerTests`, `GetAccountByIdHandlerTests`
- **Outbox** — `EventDispatcherTests`, `EventStoreOutboxWorkflowTests`, `OutboxProcessorTests`
- **Frameworks** — xUnit, Moq, NSubstitute, FluentAssertions

```bash
dotnet test
```

---

## 📁 Estrutura do Projeto

```
├── FinancesApp_Api/
│   ├── Controllers/
│   ├── Endpoints/
│   ├── Contracts/
│   ├── StartUp/               ← Registro de DI dos módulos
│   ├── docker-compose.yaml    ← SQL Server, Redis, Prometheus, Grafana
│   ├── prometheus.yaml        ← Configuração de scrape do Prometheus
│   └── Program.cs
├── FinancesApp_CQRS/
│   ├── Interfaces/            ← IEventStore, IEventDispatcher, IEventHandler, IProjectionCheckpoint
│   ├── Dispatchers/           ← CommandDispatcher, QueryDispatcher, EventDispatcher
│   ├── EventStore/            ← EventStore (append, load, insert outbox, controle de concorrência)
│   ├── Outbox/                ← OutboxEntry, OutboxProcessor (BackgroundService)
│   └── Projections/           ← ProjectionCheckpoint (idempotência)
├── FinancesApp_Module_Account/
│   ├── Application/
│   │   ├── AccountProjection.cs   ← Evento → projeção de read-model
│   │   ├── Commands/Handlers/     ← CreateAccount, ApplyDelta, DeleteAccount
│   │   ├── Queries/Handlers/      ← GetAccountById
│   │   └── Repositories/
│   └── Domain/
│       ├── Account.cs
│       ├── AggregateRoot.cs
│       └── Events/            ← 9 eventos de domínio
├── FinancesApp_Module_Credentials/
├── FinancesApp_Module_User/
├── FinanceAppDatabase/
│   ├── DbConnection/          ← IDbConnectionFactory, ICommandFactory
│   └── Utils/                 ← SqlDataReaderExtensions
├── Identity.Api/              ← Microsserviço de geração de tokens JWT
├── HostedChannelTests/
└── FinanceApp_Tests/
    ├── AccountTests/
    ├── OutboxTests/
    └── Fixtures/
```

---

## 📄 Licença

MIT
