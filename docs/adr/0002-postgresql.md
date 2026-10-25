# ADR 0002: PostgreSQL as the database

- **Status:** Accepted
- **Date:** 2026-10-25

## Context

Hookline needs:

- Transactions that cover a business change and its outgoing event, for the transactional outbox.
- A delivery queue where several workers pick up pending deliveries without taking the same one twice.
- Unique constraints to reject duplicate messages sent with the same idempotency key.
- Separate schemas, so each module owns its own tables in a shared database (see [ADR 0001](0001-modular-monolith.md)).
- A local setup that anyone can start with a single `docker compose up`, on any machine.

## Decision

Use **PostgreSQL 17** through Entity Framework Core and the Npgsql provider.

- Each module maps to its own schema: `management`, `ingestion` and `delivery`.
- The delivery queue will rely on row locking with `FOR UPDATE SKIP LOCKED`, so competing workers each claim different rows.

### Alternatives considered

- **SQL Server:** a solid option with similar features. Its official container image only runs under emulation on ARM machines, which makes the local setup heavier for contributors.
- **MongoDB:** multi-document transactions require a replica set, and a queue with competing consumers and unique constraints is less natural to model than in a relational database.

## Consequences

**Positive**

- Open source, with an official image that runs natively on any platform.
- `SKIP LOCKED` gives a simple and well-known way to build a queue with competing workers.
- Schemas map directly to module ownership.

**Negative**

- Each `DbContext` must keep its migrations history table in its own schema. Without that setting, every module would write to one shared table in the `public` schema.
- Moving to another database later would require reworking the queue and the migrations.
