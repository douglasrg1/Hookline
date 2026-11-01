# ADR 0003: Result pattern for expected errors and Problem Details for every error response

- **Status:** Accepted
- **Date:** 2026-10-31

## Context

Hookline is consumed by other systems, not by people. Those systems need two things from every error: a status code they can act on, and a body in a format they can parse without guessing.

Inside the code, failures come in two very different kinds:

- **Expected failures** are part of the business: an invalid URL, a consumer that does not exist, an endpoint that is already registered. The caller can fix them.
- **Unexpected failures** are bugs or infrastructure problems: a null reference, a database that is down. The caller cannot fix them, and the response must not reveal internal details.

Using exceptions for both makes expected failures invisible in method signatures and turns ordinary business outcomes into control flow by exception.

## Decision

### Expected failures: Result pattern

- Use cases and domain operations return `Result` or `Result<T>` from `Hookline.SharedKernel`.
- A failure carries an `Error` with a stable `Code`, a `Message` and an `ErrorType`.
- `Result<T>` converts implicitly from a value and from an `Error`, so a method can simply `return error;` or `return dto;`.
- The type is written by hand, with no third-party library, because it is small and its behavior is fully covered by tests.

### Turning errors into HTTP responses

All error responses use the Problem Details format (RFC 9457), with the content type `application/problem+json`.

| Source | Status | Produced by |
|---|---|---|
| `ErrorType.Validation` | 400 | `Error.ToProblem()` in `Hookline.SharedKernel.AspNetCore`, called by the `Presentation` layer |
| `ErrorType.NotFound` | 404 | Same |
| `ErrorType.Conflict` | 409 | Same |
| `BadHttpRequestException`, such as a malformed JSON body | The status carried by the exception, usually 400 | `BadHttpRequestExceptionHandler` |
| Any other unhandled exception | 500 | `UnhandledExceptionHandler` |
| Empty error responses, such as an unknown route | The original status | `UseStatusCodePages` |

- Every response written by our code carries a `code` field with the error code, such as `endpoint.duplicate_url`, `bad_request` or `internal_error`, so callers can branch on it without parsing text.
- ASP.NET Core adds a `traceId` to every Problem Details response, which links the response to the server logs.
- The two exception handlers are registered in that order: handlers are asked in registration order and the first one that handles the exception wins. A malformed request must keep its 4xx status instead of becoming a 500.
- Exception messages and stack traces go to the logs only. They are never written to a response.
- Requests aborted by the client are handled by ASP.NET Core itself, before any handler runs: they are logged at debug level and get the status 499.

## Consequences

**Positive**

- Expected failures are explicit in method signatures, and tests assert the returned error instead of catching exceptions.
- Every error the API returns has the same shape, and the ones produced by our code carry a stable `code`.
- Internal details never leak in responses. Tests run the API in production mode to check this.
- The exception handlers stay few and generic, because business errors never reach them.

**Negative**

- Returning `Result` takes more code than throwing an exception.
- Responses produced by `UseStatusCodePages`, such as a 404 for an unknown route or a 400 for an invalid body in production, carry no `code` field. This is accepted for now.
- An exception type that should map to a status other than 500 needs its own handler. This is expected to be rare, because business rules never throw.

### Alternatives considered

- **Exceptions for business errors**, mapped to status codes by a global handler. Simpler to write, but expected failures disappear from signatures, and every new business error needs a new exception type and mapping.
- **A third-party result library.** The behavior needed here fits in a few dozen lines, so an extra dependency adds more than it saves.
