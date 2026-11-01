# ADR 0004: Migrations applied on startup only where configuration allows it

- **Status:** Accepted
- **Date:** 2026-11-01

## Context

Each module owns its schema and its migrations (see [ADR 0001](0001-modular-monolith.md)). Before the API serves a request, the database must match the code.

The environments have different needs:

- **Local development and Docker Compose:** anyone should get a working API from an empty database with a single `docker compose up`, with no extra command.
- **Production:** applying migrations from the API on every startup brings problems:
  - Every instance tries to migrate when it starts. EF Core 9 and later serialize them with a database lock, but each startup waits for the others.
  - The API's database user would need permission to change the schema, which it never needs to serve requests.
  - A failed migration shows up as an API that does not start, instead of a deploy step that fails before the new version goes live.
  - The SQL that runs is never reviewed before it reaches the database.

## Decision

### On startup, controlled by a setting

- The setting `Database:ApplyMigrationsOnStartup` turns it on. It is `false` in `appsettings.json` and `true` in the local `appsettings.Development.json` and in `docker-compose.yml`.
- When it is on, the API runs these steps after building the host and before serving requests:
  1. It validates the settings, with the same checks the host runs later when it starts. A misconfigured API stops before it changes the database.
  2. It applies each module's migrations, through a public method of the module's registration class, such as `ApplyManagementMigrationsAsync`. The `DbContext` stays internal to the module. Today only Management has migrations. Ingestion and Delivery add their method when they get tables.

### In production, a deploy step

- Migrations run as a separate step, before the new version of the API starts, with one of the two outputs of `dotnet-ef`:
  - a migrations bundle (`dotnet ef migrations bundle`), a single executable that applies the pending migrations;
  - an idempotent SQL script (`dotnet ef migrations script --idempotent`), which can be reviewed in the pipeline before it runs.
- That step uses a database user that can change the schema. The API's user only reads and writes data.
- The choice between the bundle and the script is made together with the production environment.

### Creating migrations

- `dotnet-ef` is a local tool of the repository, so everyone uses the same version.
- The design-time package lives only in each module project, and each module is its own startup project for the tool. This keeps the tool's dependencies, about 16 MB, out of the API image.
- A test fails when the model changes and no migration was added for it.

## Consequences

**Positive**

- `docker compose up` gives a working API from an empty database.
- In production, schema changes are an explicit step that can be reviewed. API instances never compete to change the schema, and the API's database user cannot change it.
- A misconfigured API never touches the database.

**Negative**

- Two paths apply migrations: the startup, for development and Compose, and the deploy step, for production. Both have to keep working.
- On an empty database, the first migration logs one error-level entry from EF Core: it reads the migrations history table before it creates it. The migration then runs normally. The entry does not appear on later startups.
- A failed migration, like any other startup failure, ends the process with an unhandled exception. Inside a container, the .NET process running as PID 1 does not stop on that exception: the kernel ignores the abort signal for PID 1, and the process keeps running at full CPU while the container looks healthy. Compose runs an init process (`init: true`) so the container stops. Production needs the same protection, either with `tini` in the image or with the orchestrator's option, such as `initProcessEnabled` on Amazon ECS. That choice is made together with the production environment.

### Alternatives considered

- **Always apply migrations on startup, in every environment.** The simplest setup, but it brings every production problem listed above.
- **Never apply them on startup.** Every local setup and Compose would need an extra command before the API works.
- **A separate migration service in Compose that runs the bundle.** Closer to production, but it adds a second image to build before there is a production environment to mirror. It can be revisited then.
- **`EnsureCreated`.** It creates the schema without a migrations history, so the schema cannot evolve afterwards.
