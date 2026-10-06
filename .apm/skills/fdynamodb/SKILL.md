---
name: fdynamodb
description: Use whenever generating or reviewing F# code that accesses AWS DynamoDB through Alma.DynamoDB — calls to DynamoDB.connect, DynamoDB.putItem, DynamoDB.getItem, DynamoDB.getItems, DynamoDB.scanAllItems, or that builds a Configuration/Credentials/TableName, defines a DTO with [<Attribute.HashKey>]/[<Attribute.RangeKey>], or wraps keys in HashKey/RangeKey/ItemKey. Trigger also on Kafka consumer checkpoint storage via the Checkpoint module (CheckpointStore.connect, CheckpointStore.storeCheckpoint, CheckpointStore.retrieveCheckpoint), on ConnectionError/PutItemError/GetItemError handling, and on mentions of DynamoDB tables, sidecar table naming, or asyncResult-based DynamoDB persistence.
---

# F-DynamoDB

Library: [alma-oss/fdynamodb](https://github.com/alma-oss/fdynamodb)
NuGet: `Alma.DynamoDB`

## Purpose

`Alma.DynamoDB` is an F# library that provides a typed, traced, ergonomic API over AWS DynamoDB. It maps F# records to DynamoDB items, exposes put/get/query/scan operations as railway-oriented `AsyncResult` workflows, and ships a `Checkpoint` module for persisting Kafka consumer offsets in a DynamoDB-backed store.

## When to Use

- Connecting to a DynamoDB table and putting, getting, querying, or scanning typed items.
- Modeling a table item as an F# record DTO with hash/range key attributes.
- Persisting and retrieving Kafka consumer checkpoints (offsets) in DynamoDB.
- Composing DynamoDB persistence into an `asyncResult` pipeline with traced spans.

## When NOT to Use

- Accessing non-DynamoDB AWS storage (S3, RDS, etc.).
- Running ad-hoc full-table scans as a routine access pattern (scanning is supported but expensive).
- Defining table key schema dynamically — keys are declared statically via record attributes.

## Main Concepts

- `Configuration` — pairs a `TableName` with `Credentials`; input to `DynamoDB.connect`.
- `Credentials` — `ServiceAccount` (resolved from the environment) or `AccessKey` of `AWSAccessKey` (explicit key/secret).
- `TableName` — `Instance` of a service `Instance`, or `InstanceWithSidecar` of an `Instance` and a `SidecarSuffix`; rendered as `domain-context-purpose-version[--suffix]`.
- `DynamoDB` — opaque connected handle returned by `connect`; passed to every operation.
- DTO — an F# record whose fields are tagged `[<Attribute.HashKey>]` / `[<Attribute.RangeKey>]` to declare the key schema.
- `HashKey` / `RangeKey` — single-case DU wrappers around key strings.
- `ItemKey` — `Hash` (hash only) or `Combined` of a `KeyCombined` (hash + range); identifies a single item for `getItem`.
- `ConnectionError` / `PutItemError` / `GetItemError` — error DUs; the latter two distinguish `TableError` (table init) from `RuntimeError` (operation).
- `Checkpoint` module — `CheckpointDTO`, `ConsumerInstance`, `Checkpoint`, and `CheckpointStore` functions for offset storage.

## Related Libraries

- `Feather.ErrorHandling` — provides the `asyncResult` CE and `AsyncResult` combinators that every operation returns.
- `Alma.ServiceIdentification` — `Instance`, `Domain`, `Context`, `Purpose`, `Version`, `Create.Instance` used to build table names.
- `Alma.Tracing` — each operation emits a child trace span with DynamoDB tags.
- `FSharp.AWS.DynamoDB` — underlying typed record-to-item mapping (beta).

## Keywords for Search

DynamoDB, Alma.DynamoDB, fdynamodb, F#, AWS, putItem, getItem, getItems, scanAllItems, connect, Configuration, Credentials, ServiceAccount, AccessKey, TableName, SidecarSuffix, Instance, HashKey, RangeKey, ItemKey, KeyCombined, Attribute.HashKey, Attribute.RangeKey, DTO, ConnectionError, PutItemError, GetItemError, asyncResult, AsyncResult, Checkpoint, CheckpointStore, ConsumerInstance, Kafka, offset, tracing

## Reference Files

- For composition principles and recommended API usage, read `references/preferred-patterns.md`.
- For known pitfalls and incorrect assumptions, read `references/anti-patterns.md`.
- For worked code examples, read `references/examples.md`.
