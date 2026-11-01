# ADR 0001: Modular monolith with a schema per module

- **Status:** Accepted
- **Date:** 2026-10-25
- **Updated:** 2026-10-31, adding the `Presentation` layer, the full dependency rules between layers and the shared kernel projects.

## Context

Hookline has three clear areas of responsibility: managing consumers and their endpoints, accepting messages, and delivering them. Delivery has different scaling needs from the rest, so it may need to run as a separate service in the future.

Starting with microservices would mean building infrastructure (a message broker, several deployables, distributed tracing) before the first feature works. The project is developed in small increments, so that cost would delay every early milestone.

## Decision

Build a **modular monolith**: one deployable ASP.NET Core host (`Hookline.Api`) that composes three modules, `Management`, `Ingestion` and `Delivery`.

- Each module is one project plus a `Contracts` project. The `Contracts` project holds the only types other modules may use, such as integration events and query interfaces.
- All module types are `internal`, except the `<Module>Module` class. It has two methods: `Add<Module>Module` registers the module's services, and `Map<Module>Endpoints` hands the module's routes to the host. The host never touches anything else inside a module.
- All modules share one PostgreSQL database, but each module has its **own schema** and its **own `DbContext`**. The migrations history table also lives in the module's schema.
- A module never reads or writes another module's tables. There are no joins or foreign keys across modules. Modules talk only through `Contracts`.

### Layers inside a module

Inside a module, the layers are folders:

| Layer | Responsibility | May depend on |
|---|---|---|
| `Domain` | Entities and business rules | `Hookline.SharedKernel` only |
| `Application` | Use cases. They receive commands and return DTOs, mapped by hand. Entities never leave this layer. | `Domain` and `Hookline.SharedKernel` |
| `Infrastructure` | Persistence and other technical details | `Application`, `Domain` and `Hookline.SharedKernel` |
| `Presentation` | HTTP routes. They turn a request into a command and a result into a response. | `Application`, `Hookline.SharedKernel`, `Hookline.SharedKernel.AspNetCore` and ASP.NET Core |

The `<Module>Module` registration class sits at the root of the module, outside the layers, because it wires all of them together. It only delegates route mapping to the `Presentation` layer.

### Shared code

Two projects outside the modules hold code that every module uses:

- `Hookline.SharedKernel` has no dependency on web or persistence. It holds primitives such as `Result` and `Error`, and can be used by any host, including a future worker process without HTTP.
- `Hookline.SharedKernel.AspNetCore` holds HTTP helpers, such as turning an `Error` into a Problem Details response. It exposes no public constants: constants are copied into the caller at compile time, which would hide a forbidden dependency from the architecture tests.

### How the boundaries are enforced

| Boundary | Enforced by |
|---|---|
| Between modules | The compiler: module types are `internal`, and modules only reference `Contracts` projects. An architecture test also checks that no module assembly references another module. |
| Public surface of a module | An architecture test: only the `<Module>Module` class may be public. |
| Between layers inside a module | Architecture tests (NetArchTest), one per layer, following the table above. `Domain` and `Application` may not use ASP.NET Core, EF Core or Npgsql. `Infrastructure` may not use ASP.NET Core. `Presentation` may not use `Domain`, `Infrastructure`, EF Core or Npgsql. |
| Use of `Hookline.SharedKernel.AspNetCore` | An architecture test: in every module, only types in the `Presentation` folder may depend on it. |
| Purity of `Hookline.SharedKernel` | The compiler, because the project does not reference ASP.NET Core, plus an architecture test that also forbids modules, EF Core and Npgsql. |

The architecture tests discover the modules from the `src/Modules` folders, so a new module is covered automatically.

Layers are folders, not projects, on purpose. In a modular monolith the boundary that matters most is the one between modules, because it is the one that becomes a service boundary later. That boundary gets compiler enforcement. The layer boundaries are still checked on every build, by tests that run in CI.

## Consequences

**Positive**

- One process to run, debug and deploy.
- Fewer projects to maintain: one per module plus its contracts, and two shared projects.
- Extracting a module later is cheap: move its schema to its own database and replace the in-process events with a message broker. Code that uses the module's contracts does not change.
- The HTTP contract of the API is independent of the domain model, because only DTOs reach the `Presentation` layer.

**Negative**

- The layer rules depend on the architecture tests. A rule that has no test is not enforced.
- Each module project references ASP.NET Core because of its `Presentation` folder, so the compiler alone does not stop an inner layer from using web types. The tests do.
- Modules share one database server, so a heavy workload in one module can affect the others.
- Data from another module cannot be joined in SQL. It has to be requested through that module's contracts, which takes more code for cross-module reads.
- DTOs and their mapping are written by hand, which is more code than returning entities.
