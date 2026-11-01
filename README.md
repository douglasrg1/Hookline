# Hookline

Reliable webhook delivery platform in .NET: idempotent ingestion, transactional outbox, retries with backoff and Standard Webhooks signing.

> **Status: work in progress.** The management API is in place: consumers, their API keys and their endpoints. Receiving and delivering messages come next. Features are added in small, reviewed pull requests, and each milestone is published as a release.

## What is in place today

- **Management API:** the organization registers its consumers and issues API keys for them. The organization, or each consumer itself, registers the endpoints that will receive the consumer's webhooks, each with a signing secret in the Standard Webhooks format.
- **API keys and isolation between consumers:** a consumer's key never reaches another consumer's data. Tests check this on every route, including routes added later.
- **Signing secrets encrypted at rest** with AES-256-GCM, under a master key that never reaches the database.
- Modular monolith with three modules: **Management**, **Ingestion** and **Delivery**, with one PostgreSQL schema and one `DbContext` per module.
- Problem Details for every error, with a stable `code` for the errors the API produces.
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
- [ADR 0005: API keys, isolation between consumers and encryption of endpoint secrets](docs/adr/0005-api-keys-consumer-isolation-and-secret-encryption.md)

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

Run the API. In Development, the interactive API reference opens at `/scalar`. Paste a key in its authentication field to try the routes:

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

## Using the API

### Keys

Every request sends a key in the `Authorization` header:

```
Authorization: Bearer <key>
```

- **The organization key** is set in the configuration (`Auth:OrganizationKey`) and gives access to every consumer.
- **A consumer key** is issued by the organization through the API and gives access to that consumer only. It starts with `hl_` and is shown once, when it is issued: Hookline stores only its hash. A revoked key stops working at once.

Only `/health` answers without a key.

### A first walkthrough

With the API running through Docker Compose, keep the address and the organization key, the value of `AUTH_ORGANIZATION_KEY` in `.env`, at hand:

```bash
export HOOKLINE=http://localhost:8080
export ORGANIZATION_KEY=<your organization key>
```

1. As the organization, register a consumer. `externalId` is optional: it is the consumer's id in your own system.

   ```bash
   curl -s -X POST $HOOKLINE/consumers \
     -H "Authorization: Bearer $ORGANIZATION_KEY" \
     -H "Content-Type: application/json" \
     -d '{"name": "Acme Store", "externalId": "customer-42"}'
   ```

   Keep the `id` of the response as `CONSUMER_ID`.

2. Issue a key for the consumer and keep the `key` of the response as `CONSUMER_KEY`. It is not shown again.

   ```bash
   curl -s -X POST $HOOKLINE/consumers/$CONSUMER_ID/keys \
     -H "Authorization: Bearer $ORGANIZATION_KEY"
   ```

3. As the consumer, register an endpoint. Without `eventTypes`, the endpoint receives every event type.

   ```bash
   curl -s -X POST $HOOKLINE/consumers/$CONSUMER_ID/endpoints \
     -H "Authorization: Bearer $CONSUMER_KEY" \
     -H "Content-Type: application/json" \
     -d '{"url": "https://example.com/webhooks", "eventTypes": ["invoice.paid"]}'
   ```

   Keep the `id` of the response as `ENDPOINT_ID`.

4. Read the endpoint's signing secret, used to check the signature of every webhook it receives:

   ```bash
   curl -s $HOOKLINE/consumers/$CONSUMER_ID/endpoints/$ENDPOINT_ID/secret \
     -H "Authorization: Bearer $CONSUMER_KEY"
   ```

### Routes

| Route | Organization | Consumer |
|---|---|---|
| `POST /consumers`, `GET /consumers` | ✓ | |
| `GET /consumers/{consumerId}` | ✓ | Itself |
| `DELETE /consumers/{consumerId}`, which also deletes its keys and endpoints | ✓ | |
| `POST`, `GET /consumers/{consumerId}/keys` | ✓ | |
| `DELETE /consumers/{consumerId}/keys/{keyId}`, which revokes the key | ✓ | |
| `POST`, `GET /consumers/{consumerId}/endpoints` | ✓ | Its own |
| `GET`, `PUT`, `DELETE /consumers/{consumerId}/endpoints/{endpointId}` | ✓ | Its own |
| `POST /consumers/{consumerId}/endpoints/{endpointId}/enable` and `/disable` | ✓ | Its own |
| `GET /consumers/{consumerId}/endpoints/{endpointId}/secret` | ✓ | Its own |

Endpoint URLs must use `https`, except for `localhost` and `127.0.0.1`. A consumer cannot register the same URL twice.

### Lists

Lists return one page at a time, oldest first:

```json
{ "items": [ ... ], "next": "0199a3c2-..." }
```

`limit` sets the page size, from 1 to 100, 50 by default. To get the next page, send the value of `next` as `after`. On the last page, `next` is `null`.

```
GET /consumers?limit=20&after=0199a3c2-...
```

### Errors

Every error is a Problem Details response (`application/problem+json`). The errors produced by Hookline carry a `code`, such as `endpoint.https_required`, that clients can rely on.

| Status | When |
|---|---|
| 400 | The request breaks a rule, such as an invalid URL, or cannot be read |
| 401 | The key is missing, unknown or revoked |
| 403 | A consumer calls a route that only the organization may call |
| 404 | The resource does not exist. A consumer gets the same answer for another consumer's data |
| 409 | The resource conflicts with an existing one, such as an external id or a URL already in use |

## Configuration

`src/Hookline.Api/appsettings.json` lists every setting, with empty values. The API refuses to start while a required one is empty or invalid. Locally, the values come from `appsettings.Development.json`; in Docker and in production, from environment variables.

| Setting | Environment variable | Required | Description |
|---|---|---|---|
| `ConnectionStrings:Hookline` | `ConnectionStrings__Hookline` | Yes | PostgreSQL connection string |
| `Auth:OrganizationKey` | `Auth__OrganizationKey` | Yes | The organization's API key, with access to every consumer |
| `Encryption:Key` | `Encryption__Key` | Yes | Master key that encrypts the endpoint signing secrets: 32 random bytes in base64 |
| `Database:ApplyMigrationsOnStartup` | `Database__ApplyMigrationsOnStartup` | No, `false` by default | Applies the database migrations when the API starts. Production applies them in its deploy step instead ([ADR 0004](docs/adr/0004-migrations-on-startup-by-configuration.md)) |

Docker Compose fills the two keys from `AUTH_ORGANIZATION_KEY` and `ENCRYPTION_KEY` in `.env`.

> **Keep both keys in a secret manager, such as AWS Secrets Manager or Azure Key Vault, and never commit them.** Anyone with the organization key can do everything the API allows. If the master key leaks, every endpoint secret can be decrypted. If it is lost, no stored secret can be read again, and every endpoint needs a new secret.

## Project structure

```
src/
  Hookline.Api/                        ASP.NET Core host that composes the modules
  Hookline.SharedKernel/               Result and Error, shared by every module
  Hookline.SharedKernel.AspNetCore/    Turns errors into Problem Details responses
  Modules/
    Management/                        Module project and its Contracts project
    Ingestion/
    Delivery/
tests/
  Hookline.Api.Tests/                  The host: startup, error handling, authentication
  Hookline.ArchitectureTests/          Module and layer boundaries
  Hookline.SharedKernel.Tests/
  Modules/Management/                  Domain, persistence and API tests of the module
docs/
  adr/                                 Architecture decision records
```

## License

[MIT](LICENSE)
