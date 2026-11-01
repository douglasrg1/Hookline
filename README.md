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
| Management | Consumers, their API keys and their endpoints |
| Ingestion | Accepting messages from the organization |
| Delivery | Delivering messages to consumer endpoints |

Each module is a single project with its layers as folders (`Domain`, `Application`, `Infrastructure`, `Presentation`) plus a `Contracts` project that holds everything other modules are allowed to see. Module internals are `internal`. Modules never read or write another module's tables.

The reasoning behind these choices is recorded in the architecture decision records:

- [ADR 0001: Modular monolith with a schema per module](docs/adr/0001-modular-monolith.md)
- [ADR 0002: PostgreSQL as the database](docs/adr/0002-postgresql.md)
- [ADR 0003: Result pattern for expected errors and Problem Details for every error response](docs/adr/0003-result-pattern-and-problem-details.md)
- [ADR 0004: Migrations applied on startup only where configuration allows it](docs/adr/0004-migrations-on-startup-by-configuration.md)

## Tech stack

.NET 10, ASP.NET Core, Entity Framework Core with Npgsql, PostgreSQL 17, xUnit, Shouldly, NetArchTest, Testcontainers, Docker Compose and GitHub Actions.

## Getting started

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download) and Docker.

### Run everything with Docker

1. Create your local environment file:

   ```bash
   cp .env.example .env
   ```

   Set a password for the local database and fill in the two keys, generating each one with:

   ```bash
   openssl rand -base64 32
   ```

   `AUTH_ORGANIZATION_KEY` is the organization's API key. `ENCRYPTION_KEY` is the master key that encrypts the endpoint signing secrets.

2. Start the database and the API. The API applies the database migrations when it starts:

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

Then create `src/Hookline.Api/appsettings.Development.json` with your local values. `appsettings.json` lists every setting the API needs, with empty values; the API refuses to start while a required one is empty or invalid. Use the same password you set in `.env`, and generate each key with `openssl rand -base64 32`:

```json
{
  "ConnectionStrings": {
    "Hookline": "Host=localhost;Port=5433;Database=hookline;Username=hookline;Password=<your-local-password>"
  },
  "Database": {
    "ApplyMigrationsOnStartup": true
  },
  "Auth": {
    "OrganizationKey": "<generated-key>"
  },
  "Encryption": {
    "Key": "<generated-key>"
  }
}
```

`appsettings.Development.json` is ignored by git and by the Docker build, so local secrets never leave your machine.

Run the API. In Development, the interactive API reference opens at `/scalar`:

```bash
dotnet run --project src/Hookline.Api
```

### Run the tests

The persistence tests start a PostgreSQL container with Testcontainers, so Docker must be running:

```bash
dotnet test
```

With Rancher Desktop, point Testcontainers at its Docker socket first:

```bash
export DOCKER_HOST=unix://$HOME/.rd/docker.sock
export TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE=/var/run/docker.sock
export TESTCONTAINERS_HOST_OVERRIDE=localhost
```

### Add a migration

Each module keeps its own migrations, and `dotnet-ef` is a local tool of the repository. For the Management module:

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project src/Modules/Management/Hookline.Modules.Management --output-dir Infrastructure/Migrations
```

A test fails when the model changes and no migration was added for it.

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
