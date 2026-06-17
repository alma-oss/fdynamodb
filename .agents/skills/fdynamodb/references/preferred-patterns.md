# Preferred Patterns

## Core Principles

- **Railway-oriented flow.** Every public operation returns `AsyncResult<'T, 'Error>`. Compose calls inside an `asyncResult { }` computation expression and bind with `let!` / `do!`; let errors short-circuit rather than throwing.
- **Wrap key strings in their DUs.** Pass `HashKey`, `RangeKey`, and `ItemKey` values rather than raw strings, and build table names from typed `Instance` values — never hand-format the underlying string.
- **Connect once, reuse the handle.** `DynamoDB.connect` returns a handle that holds the underlying client and table name; create it once and thread it through every operation instead of reconnecting per call.
- **Let the DTO declare the schema.** Tag exactly the key fields with `[<Attribute.HashKey>]` and (optionally) `[<Attribute.RangeKey>]`; the same record type parameter `<'Dto>` must be supplied to put and get operations against that table.

## Recommended API Usage

- `connect` takes a `Configuration` (table name + credentials) and yields the `DynamoDB` handle. See `examples.md` → Basic Connection.
- `putItem` writes a typed record and returns the resulting `ItemKey`. See `examples.md` → Put And Get.
- `getItem` fetches a single item by `ItemKey` (`Hash` for hash-only tables, `Combined` for hash+range). See `examples.md` → Put And Get.
- `getItems` queries all items for a `HashKey`; you supply the query function that expresses the key condition over the table context. See `examples.md` → Query By Hash Key.
- `scanAllItems` reads the whole table; reserve it for maintenance/bootstrap paths, not hot paths.
- The `Checkpoint` module composes `CheckpointStore.configuration` + `connect` once, then `storeCheckpoint` / `retrieveCheckpoint`. See `examples.md` → Checkpoint Storage.

## Error Handling

- Distinguish the two failure phases: `TableError` means the table context could not be initialized (configuration/permissions), while `RuntimeError` means the request itself failed. Match on these cases when deciding whether to retry or surface the error.
- Prefer `AsyncResult.map`, `AsyncResult.ignore`, and `AsyncResult.teeError` over manual unwrapping; map sub-errors into your own error type at the boundary of your module.
- `getItem` returns the item wrapped in an option-like result — a missing item is a successful `None`, not an error.

## Composition

- Build larger persistence functions by chaining the primitives inside a single `asyncResult` block so a failure at any step aborts the rest.
- Convert a `putItem` that you only care about for its side effect with `AsyncResult.ignore` to yield `AsyncResult<unit, _>`.

## Integration with Other Libraries

- Build `TableName.Instance` from `Alma.ServiceIdentification` values (`Domain`, `Context`, `Purpose`, `Version`, or `Create.Instance`). For sidecar tables append a `SidecarSuffix`, which renders as `…--suffix`.
- Tracing is automatic: each operation opens a child span (via `Alma.Tracing`) tagged with the component, peer service, table instance, and statement, and attaches errors to the active trace. Keep an active trace context in scope so spans nest correctly.
- Credentials default to the service-account resolution path (`ServiceAccount`); use `AccessKey` only for explicit, non-ambient credentials.

## Naming Conventions

- Domain identifiers are single-case DUs (`HashKey of string`, `TableName`, `SidecarSuffix`, `ConsumerInstance`, `Checkpoint`); construct and deconstruct via pattern matching.
- DU types ship with a companion `[<RequireQualifiedAccess>] module` exposing `value` / `parse`; use `TableName.parse` to turn a stored table-name string back into a typed `TableName`.
- Records and DUs are immutable — derive new values instead of mutating.

## Testing Recommendations

- The library has no integration test suite and the AWS region is fixed at construction time, so unit-test your own mapping/composition logic around the typed wrappers rather than hitting a live table.
- Test DTO round-tripping by asserting on the `ItemKey` returned from a put and the option returned from a get against an in-memory or local DynamoDB endpoint when one is available.
