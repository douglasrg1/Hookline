# ADR 0005: API keys, isolation between consumers and encryption of endpoint secrets

- **Status:** Accepted
- **Date:** 2026-11-01

## Context

Each Hookline installation serves one organization. The organization's customers are its consumers, and each consumer has the endpoints where its webhooks are delivered.

Two kinds of callers use the API:

- **The organization**, which manages everything: consumers, their keys and their endpoints.
- **A consumer**, which manages its own endpoints and nothing else. It must not be able to read, change or even detect the data of another consumer.

Every endpoint has a signing secret. Hookline uses it to sign each delivery, and the consumer uses it to check the signature. Hookline therefore has to read the secret back in plain text: storing only a hash, as with passwords, is not an option.

## Decision

### API keys

- Every request sends one key in `Authorization: Bearer <key>`. The API has a single authentication scheme for every module.
- **The organization key** comes from configuration (`Auth:OrganizationKey`), never from the database, and gives access to every consumer. It is compared in constant time, over the SHA-256 hashes of both values, so the comparison takes the same time whatever key is sent and reveals nothing about the key, not even its length.
- **Consumer keys** are `hl_` followed by 32 random bytes in base64url. The organization issues them through the API, and the key is returned only in that response. The database keeps its SHA-256 hash, under a unique index, and its first 12 characters, so it can be recognized in a list. A key can be revoked: it is kept with the time it stopped working, and it no longer authenticates.
- SHA-256 is enough for consumer keys. A key holds 256 random bits, so it cannot be guessed, unlike a password, and a fast hash lets each request find the key through the index.
- Every route requires an authenticated caller unless it is marked as public. Only `/health` is public, and a test fails if any other route becomes public.
- Responses that carry a newly issued key or a secret are sent with `Cache-Control: no-store`.

### Isolation between consumers

- The routes of a consumer live under `/consumers/{consumerId}`. A filter on that route group lets in the organization and the consumer whose id is in the route.
- Any other consumer gets the same 404, with the code `consumer.not_found`, that a consumer that does not exist would get. It cannot tell a consumer it may not see from one that does not exist.
- Routes for the organization only, such as issuing keys or deleting a consumer, answer 403 to every consumer. They give the same answer whatever consumer is in the route, so they reveal nothing either.
- Keys and endpoints are always looked up by the pair of consumer id and their own id. An endpoint id placed under another consumer is not found, even for the organization.
- The isolation tests read every route under `/consumers/{consumerId}` from the API itself. With another consumer's key, each one must answer 404 or 403 and change nothing; with the organization key, each one must succeed. A route added later is covered without changing the tests.

### Endpoint signing secrets

- The secret follows the Standard Webhooks format: `whsec_` followed by 32 random bytes in base64. It is generated when the endpoint is created.
- It is stored encrypted with AES-256-GCM. The master key comes from configuration (`Encryption:Key`, 32 bytes in base64) and never reaches the database.
- The stored value is `[key version (1 byte)][nonce (12 bytes)][ciphertext][tag (16 bytes)]`. Each encryption uses a new random nonce. The version byte is covered by the authentication tag, so it cannot be changed unnoticed, and it leaves room to rotate the master key: values written with version 1 stay readable after a version 2 is added.
- The API returns the secret only through its own route, `GET /consumers/{consumerId}/endpoints/{endpointId}/secret`, to the organization or to the consumer that owns the endpoint.

## Consequences

**Positive**

- A copy of the database exposes no usable key: only hashes of random keys. Endpoint secrets stay unreadable without the master key, which lives outside the database.
- A consumer's key cannot reach another consumer's data, and the tests check it on every route, including routes added later.
- Any change to an encrypted secret, or decryption with the wrong key, fails instead of returning garbage.

**Negative**

- The master key protects every secret at once. If it leaks, every secret can be decrypted. If it is lost, no stored secret can be read again, and every endpoint needs a new secret, which arrives with secret rotation in a later stage. It must be kept in a secret manager.
- Rotating the master key is not implemented yet. The version byte only prepares for it.
- The organization key is a single credential with full access and no scopes. Changing it means changing the configuration and restarting the API.
- Every request with a consumer key reads the database to find it. The unique index keeps this cheap, and there is no cache.

### Alternatives considered

- **OAuth 2.0 or JWT tokens.** Suited to users and to many clients with their own identities, but they need an identity provider or token issuing and expiry. For server-to-server calls by one organization and its consumers, API keys are simpler to operate and to rotate.
- **A hash of the signing secret instead of encryption.** Impossible: the secret must be used as-is to sign every delivery and handed to the consumer.
- **ASP.NET Core Data Protection.** It encrypts and rotates keys on its own, but it keeps its key ring in storage that every instance must share, and its payloads are opaque. A single master key in a secret manager, with a documented layout, is simpler to operate for one installation and to move to another stack if needed.
- **404 for every forbidden route, organization-only routes included.** It would hide that those routes exist, but they are documented anyway. 403 states the real reason without revealing anything about the consumer in the route.
