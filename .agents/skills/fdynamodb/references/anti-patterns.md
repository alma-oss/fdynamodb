# Anti-Patterns

Each entry is **mistake → why → fix**.

## Keys and Table Names

- **Passing raw strings as keys** → Operations expect typed `HashKey` / `RangeKey` / `ItemKey`, so raw strings will not compile and bypass the intended domain modeling. → Wrap values: `HashKey "…"`, and select `ItemKey.Hash` or `ItemKey.Combined` to match the table's key schema.
- **Hand-formatting the table-name string** (e.g. concatenating `domain-context-…` yourself) → The rendering rules — including the `--suffix` sidecar form — are owned by `TableName`, and a manual string can drift from them. → Build a `TableName.Instance` / `InstanceWithSidecar` from typed `Instance` values, and use `TableName.parse` to go back from a string.
- **Using `ItemKey.Combined` on a hash-only table (or `Hash` on a hash+range table)** → The key shape must match the DTO's declared attributes, or the request fails at runtime. → Use `Combined` only when the DTO declares both `[<Attribute.HashKey>]` and `[<Attribute.RangeKey>]`; otherwise use `Hash`.

## DTO Definition

- **Forgetting the key attributes on the DTO** → Without `[<Attribute.HashKey>]` (and `[<Attribute.RangeKey>]` where applicable) the record cannot be mapped to the table's key schema. → Tag exactly the key fields; leave non-key fields plain.
- **Using a different record type for put vs. get on the same table** → The `<'Dto>` type parameter drives the mapping, and mismatched types produce inconsistent items. → Use one DTO type per table for both `putItem<'Dto>` and `getItem<'Dto>`.

## Error and Result Handling

- **Ignoring the `AsyncResult` result or unwrapping it with exceptions** → The library models failures as values (`ConnectionError`, `PutItemError`, `GetItemError`), so throwing/ignoring discards that information and breaks the railway flow. → Bind with `let!` / `do!` inside `asyncResult` and handle the error DU cases.
- **Treating a missing item as an error** → `getItem` returns success with no value when the item is absent. → Match the `None` case as a normal outcome, not a failure.
- **Collapsing `TableError` and `RuntimeError` into one branch** → They signal different phases (table init vs. request execution) and often warrant different responses. → Match each case explicitly where the distinction matters.

## Access Patterns

- **Using `scanAllItems` as a routine read path** → A full table scan reads every item and is costly and slow at scale. → Model access around hash-key queries (`getItems`) and reserve scans for one-off maintenance.

## Configuration Assumptions

- **Assuming the AWS region is configurable** → The region is fixed when the client is constructed and is not exposed in `Configuration`. → Do not write code that tries to override it through configuration; treat the region as a property of the deployment.
- **Assuming a stable underlying API** → The underlying typed DynamoDB mapping dependency is a beta release and may change. → Pin versions deliberately and re-verify behavior after dependency upgrades.
- **Reconnecting per operation** → `connect` builds a client/handle that is meant to be reused; reconnecting repeatedly is wasteful. → Connect once and pass the handle to each call.
