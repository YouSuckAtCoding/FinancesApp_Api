# 💰 FinancesApp API

A **Modular Monolith** REST API for personal finance management, built with **CQRS**, **Event Sourcing**, and clean architecture principles in **ASP.NET Core**.

---

## 🏗️ Architecture

This project follows a **Modular Monolith** approach — all modules run in the same process but are fully decoupled from each other. Communication between modules happens through a custom-built **CQRS pipeline** (no MediatR).


## ⚙️ Tech Stack

- **ASP.NET Core** (.NET 8)
- **SQL Server** (raw ADO.NET — no EF Core)
- **Custom CQRS** — hand-rolled `ICommandDispatcher` / `IQueryDispatcher`
- **Docker** + **Docker Compose**
- **xUnit** for testing
- **Swagger / OpenAPI**
- **Health Checks** (SQL Server)

---

## 📐 CQRS Pattern

Commands and queries are fully separated:

- `ICommand` → `ICommandHandler<TCommand>` → `ICommandDispatcher`
- `IQuery<TResult>` → `IQueryHandler<TQuery, TResult>` → `IQueryDispatcher`

Each module registers its own handlers via DI extension methods (e.g. `AddAccountModule()`).

---

## 🗄️ Data Access

- Raw SQL via `IDbConnectionFactory` and `ICommandFactory`
- `SqlDataReaderExtensions` for clean result mapping
- Each module has separate **read** and **write** repositories

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

## 🧪 Tests

Integration tests run against a real SQL Server instance using `SqlFixture` and `DatabaseInitializer`.
The Initializer requires a SQL script, that can be generated in MSSQL. Make sure to remove any 'USE' statements before running the tests.

---

## 📁 Project Structure

```
├── FinancesApp_Api/
│   ├── Controllers/
│   ├── Endpoints/
│   ├── Contracts/
│   ├── StartUp/           ← Module DI registrations
│   └── Program.cs
├── FinancesApp_CQRS/
│   ├── Interfaces/
│   └── Dispatchers/
├── FinancesApp_Module/
│   ├── Application/       ← Commands, Queries, Handlers
│   ├── Domain/
│   └── Infrastructure/    ← Repositories
├── FinanceAppDatabase/
│   ├── DbConnection/
│   └── Utils/
└── FinanceApp_Tests/
```

---

## 📄 License

MIT


# 💰 FinancesApp API

Uma API REST em **Monólito Modular** para gerenciamento de finanças pessoais, construída com **CQRS**, **Event Sourcing** e princípios de arquitetura limpa em **ASP.NET Core**.

---

## 🏗️ Arquitetura

O projeto segue a abordagem de **Monólito Modular** — todos os módulos rodam no mesmo processo, mas são completamente desacoplados entre si. A comunicação entre módulos acontece por meio de um **pipeline CQRS customizado** (sem MediatR).


## ⚙️ Stack

- **ASP.NET Core** (.NET 8)
- **SQL Server** (ADO.NET puro — sem EF Core)
- **CQRS Customizado** — `ICommandDispatcher` / `IQueryDispatcher` feitos à mão
- **Docker** + **Docker Compose**
- **xUnit** para testes
- **Swagger / OpenAPI**
- **Health Checks** (SQL Server)

---

## 📐 Padrão CQRS

Commands e queries são completamente separados:

- `ICommand` → `ICommandHandler<TCommand>` → `ICommandDispatcher`
- `IQuery<TResult>` → `IQueryHandler<TQuery, TResult>` → `IQueryDispatcher`

Cada módulo registra seus próprios handlers via métodos de extensão de DI (ex: `AddAccountModule()`).

---

## 🗄️ Acesso a Dados

- SQL puro via `IDbConnectionFactory` e `ICommandFactory`
- `SqlDataReaderExtensions` para mapeamento limpo dos resultados
- Cada módulo possui repositórios de **leitura** e **escrita** separados

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
É necessária a criação de scripts SQl que podem ser feito pelo MSSQL. Lembre-se de remover qualquer 'USE' statement do script antes de rodar.

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
│   ├── StartUp/           ← Registro de DI dos módulos
│   └── Program.cs
├── FinancesApp_CQRS/
│   ├── Interfaces/
│   └── Dispatchers/
├── FinancesApp_Module/
│   ├── Application/       ← Commands, Queries, Handlers
│   ├── Domain/
│   └── Infrastructure/    ← Repositórios
├── FinanceAppDatabase/
│   ├── DbConnection/
│   └── Utils/
└── FinanceApp_Tests/
```

---

## 📄 Licença

MIT

