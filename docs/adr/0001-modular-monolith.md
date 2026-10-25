# ADR 0001: Modular monolith with a schema per module

- **Status:** Accepted
- **Date:** 2026-10-25

## Context

Hookline has three clear areas of responsibility: managing applications and endpoints, accepting messages, and delivering them. Delivery has different scaling needs from the rest, so it may need to run as a separate service in the future.

Starting with microservices would mean building infrastructure (a message broker, several deployables, distributed tracing) before the first feature works. The project is developed in small increments, so that cost would delay every early milestone.

## Decision

Build a **modular monolith**: one deployable ASP.NET Core host (`Hookline.Api`) that composes three modules, `Management`, `Ingestion` and `Delivery`.

- Each module is one project plus a `Contracts` project. The `Contracts` project holds the only types other modules may use, such as integration events and query interfaces.
- Inside a module, the layers are folders: `Domain`, `Application` and `Infrastructure`. All module types are `internal`, except the `<Module>Module` class that registers the module in the host.
- All modules share one PostgreSQL database, but each module has its **own schema** and its **own `DbContext`**. The migrations history table also lives in the module's schema.
- A module never reads or writes another module's tables. There are no joins or foreign keys across modules. Modules talk only through `Contracts`.

### How the boundaries are enforced

| Boundary | Enforced by |
|---|---|
| Between modules | The compiler: module types are `internal`, and modules only reference `Contracts` projects. An architecture test also checks project references. |
| Between layers inside a module | Architecture tests (NetArchTest): `Domain` must not depend on `Application`, `Infrastructure`, EF Core or Npgsql, and `Application` must not depend on `Infrastructure`, EF Core or Npgsql. |

Layers are folders, not projects, on purpose. In a modular monolith the boundary that matters most is the one between modules, because it is the one that becomes a service boundary later. That boundary gets compiler enforcement. The layer boundary is still checked on every build, by tests that run in CI.

## Consequences

**Positive**

- One process to run, debug and deploy.
- Fewer projects to maintain: one per module plus its contracts.
- Extracting a module later is cheap: move its schema to its own database and replace the in-process events with a message broker. Code that uses the module's contracts does not change.

**Negative**

- The layer rules depend on the architecture tests. A rule that has no test is not enforced.
- Modules share one database server, so a heavy workload in one module can affect the others.
- Data from another module cannot be joined in SQL. It has to be requested through that module's contracts, which takes more code for cross-module reads.
