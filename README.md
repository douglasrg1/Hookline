# Hookline

Reliable webhook delivery platform in .NET: idempotent ingestion, transactional outbox, retries with backoff and Standard Webhooks signing.

> **Status: work in progress.** The foundation is in place. Features are added in small, reviewed pull requests, and each milestone is published as a release.

## What is in place today

- Modular monolith with three modules: **Management**, **Ingestion** and **Delivery**.
- One PostgreSQL database with one schema and one `DbContext` per module.
- Health check endpoint backed by the database connection.
- Architecture tests that enforce module and layer boundaries on every build.
- Docker Compose environment and a CI pipeline that builds, tests and builds the Docker image.

## Architecture

| Module | Responsibility |
|---|---|
| Management | Applications, API keys and subscriber endpoints |
| Ingestion | Accepting messages from applications |
| Delivery | Delivering messages to subscriber endpoints |

Each module is a single project with its layers as folders (`Domain`, `Application`, `Infrastructure`) plus a `Contracts` project that holds everything other modules are allowed to see. Module internals are `internal`. Modules never read or write another module's tables.

The reasoning behind these choices is recorded in the architecture decision records:

- [ADR 0001: Modular monolith with a schema per module](docs/adr/0001-modular-monolith.md)
- [ADR 0002: PostgreSQL as the database](docs/adr/0002-postgresql.md)

## Tech stack

.NET 10, ASP.NET Core, Entity Framework Core with Npgsql, PostgreSQL 17, xUnit, Shouldly, NetArchTest, Docker Compose and GitHub Actions.

## Getting started

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download) and Docker.

### Run everything with Docker

1. Create your local environment file and set a password for the local database:

   ```bash
   cp .env.example .env
   ```

2. Start the database and the API:

   ```bash
   docker compose up --build
   ```

3. Check that the API is healthy:

   ```bash
   curl http://localhost:8080/health
   ```

### Run the API from your IDE

Start only the database:

```bash
docker compose up -d postgres
```

Then give the API its connection string, for example with user secrets:

```bash
dotnet user-secrets set "ConnectionStrings:Hookline" "Host=localhost;Port=5432;Database=hookline;Username=hookline;Password=<your-local-password>" --project src/Hookline.Api
```

### Run the tests

```bash
dotnet test
```

## Project structure

```
src/
  Hookline.Api/              ASP.NET Core host that composes the modules
  Modules/
    Management/              Module project and its Contracts project
    Ingestion/
    Delivery/
tests/
  Hookline.ArchitectureTests/
docs/
  adr/                       Architecture decision records
```

## License

[MIT](LICENSE)
