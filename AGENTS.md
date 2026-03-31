# AGENTS.md — Alma.DynamoDB (fdynamodb)

## Project Purpose

F# library (`Alma.DynamoDB`) for accessing AWS DynamoDB storage. Provides a typed, traced, and ergonomic API for connecting to DynamoDB tables, putting/getting items, querying by hash key, scanning, and managing Kafka consumer checkpoints backed by DynamoDB. Published as a NuGet package.

## Tech Stack

| Component | Detail |
|---|---|
| Language | F# on .NET 10.0 |
| SDK | `global.json` pins .NET SDK 10.0.x (`rollForward: latestMinor`) |
| AWS SDK | `FSharp.AWS.DynamoDB 0.13.0-beta`, `AWSSDK.SecurityToken ~> 4.0` |
| Error handling | `Feather.ErrorHandling` (`asyncResult` CE, `AsyncResult` combinators) |
| Service identification | `Alma.ServiceIdentification` — `Instance`, `Domain`, `Context`, `Purpose`, `Version` |
| Tracing | `Alma.Tracing` — OpenTracing-style spans via `Trace.ChildOf`, `Trace.addTags` |
| Test framework | Expecto |
| Build system | FAKE (F# Make) v1.3.0 via `build/` project |
| Package manager | Paket (`paket.dependencies` / `paket.references`) |
| Lint | fsharplint (`fsharplint.json` — only disables `genericTypesNames`) |

## Commands

```bash
# Restore dependencies
dotnet paket install

# Build
./build.sh build

# Run tests
./build.sh -t tests

# Publish to NuGet (CI only — requires NUGET_API_KEY)
./build.sh -t publish

# Lint (runs automatically as part of build pipeline)
# fsharplint is invoked by the FAKE build; no separate CLI command needed
```

Build options (passed as args to `build.sh`):
- `no-clean` — skip cleaning output dirs (required on CI)
- `no-lint` — run lint but ignore failures

## Project Structure

```
fdynamodb/
├── DynamoDB.fsproj          # Library project (PackageId: Alma.DynamoDB, v8.0.0)
├── AssemblyInfo.fs           # Auto-generated assembly metadata
├── src/
│   ├── DynamoDB.fs           # Core module: connect, putItem, getItem, getItems, scanAllItems
│   └── Checkpoint.fs         # CheckpointStore module for Kafka consumer offset storage
├── tests/
│   ├── tests.fsproj          # Test project
│   └── Tests.fs              # Expecto test runner entry point
├── build/
│   ├── Build.fs              # FAKE build entry point (project config)
│   ├── Targets.fs            # FAKE target definitions (Clean, Build, Lint, Tests, Publish)
│   ├── SafeBuildHelpers.fs   # SAFE Stack helpers (not used by this library)
│   └── Utils.fs              # Build utilities
├── paket.dependencies        # Dependency definitions
├── paket.references          # Package references for the main project
├── fsharplint.json           # Lint configuration
├── global.json               # .NET SDK version pin
├── CHANGELOG.md              # Release history
└── .github/workflows/
    ├── tests.yaml            # Runs tests on PRs and nightly schedule
    ├── pr-check.yaml         # Blocks fixup commits, runs ShellCheck
    └── publish.yaml          # Publishes to NuGet on version tags
```

## Architecture & Key Concepts

### Module: `Alma.DynamoDB.DynamoDB`

- `connect`: Creates a `DynamoDB` handle from `Configuration` (credentials + table name). Uses EU-West-1 region by default.
- `putItem<'Dto>`: Puts a typed record into the table. DTO fields marked with `[<Attribute.HashKey>]` and `[<Attribute.RangeKey>]` define the key schema.
- `getItem<'Dto>`: Gets a single item by `ItemKey` (hash-only or combined hash+range).
- `getItems<'Dto>`: Queries items by `HashKey` using a caller-provided query function.
- `scanAllItems<'Dto>`: Full table scan (use sparingly).

### Module: `Alma.DynamoDB.Checkpoint`

- `CheckpointStore.connect` / `storeCheckpoint` / `retrieveCheckpoint` — DynamoDB-backed checkpoint storage for Kafka consumer offsets.
- Uses `InstanceWithSidecar` table naming with a `SidecarSuffix`.

### Table Naming Convention

Tables are named using `Alma.ServiceIdentification.Instance` — a 4-part identifier: `{domain}-{context}-{purpose}-{version}`. Sidecar tables append `--{suffix}`.

### Error Types

All operations return `AsyncResult<'T, 'Error>` where errors are discriminated unions: `ConnectionError`, `PutItemError`, `GetItemError`. Each error case wraps either a table-level or runtime exception.

### Tracing

Every DynamoDB operation creates a child trace span with tags: `component`, `peer.service`, `db.instance`, `db.type`, `span.kind`. Errors are added to the active trace via `Trace.addError`.

## Key Dependencies

| Package | Role |
|---|---|
| `FSharp.AWS.DynamoDB` | High-level typed DynamoDB access (record-to-item mapping) |
| `AWSSDK.SecurityToken` | AWS STS for service-account credential resolution |
| `Feather.ErrorHandling` | `asyncResult` CE and `AsyncResult` combinators (railway-oriented programming) |
| `Alma.ServiceIdentification` | `Instance`, `Domain`, `Context`, `Purpose`, `Version` types for naming |
| `Alma.Tracing` | Distributed tracing (OpenTracing/Jaeger compatible) |

## Conventions

- **Single-case DU wrappers**: `HashKey of string`, `RangeKey of string`, `TableName`, `SidecarSuffix` — never use raw strings for domain identifiers.
- **Module-per-type pattern**: Each DU has a companion `[<RequireQualifiedAccess>] module` with `value`, `parse`, etc.
- **Railway-oriented error handling**: All public functions return `AsyncResult<'T, 'Error>`. Use `asyncResult { }` CE and `|>` pipelines with `AsyncResult.map`, `AsyncResult.teeError`, etc.
- **Tracing via `use`**: Trace spans are `IDisposable` — use `use trace = ...` to auto-finish.
- **No mutable state**: All types are immutable records or DUs.
- **Namespace**: `Alma.DynamoDB` for all modules.

## CI/CD

| Workflow | Trigger | What it does |
|---|---|---|
| `tests.yaml` | PRs + nightly cron (03:00 UTC) | Runs `./build.sh -t tests` on ubuntu-latest with .NET 10.x |
| `pr-check.yaml` | PRs | Blocks fixup commits + ShellCheck on shell scripts |
| `publish.yaml` | Git tags matching `[0-9]+.[0-9]+.[0-9]+` | Runs `./build.sh -t publish` with `NUGET_API_KEY` secret |

Environment variables for CI:
- `PRIVATE_FEED_USER` / `PRIVATE_FEED_PASS` — GitHub Packages auth (auto-provided)
- `NUGET_API_KEY` — NuGet.org publish key (secret)
- `DOTNET_ROLL_FORWARD=latestMajor` — allows running on newer SDK versions

## Release Process

1. Increment `<Version>` in `DynamoDB.fsproj`
2. Update `CHANGELOG.md` (move items from Unreleased to new version section)
3. Commit and create a git tag matching the version (e.g., `8.0.0`)
4. Push tag — CI publishes to NuGet.org automatically

## Pitfalls

- **Test coverage is minimal**: `tests/Tests.fs` is just an Expecto entry point with no test cases. Any changes should be manually verified.
- **No docker-compose**: This is a library, not a service. No local environment setup beyond `dotnet` SDK.
- **No `.env` or `configuration/`**: No environment variables needed for the library itself. Tests don't require AWS credentials (no integration tests).
- **`FSharp.AWS.DynamoDB` is beta**: Using `0.13.0-beta` — API may have breaking changes.
- **Region hardcoded**: `EU-West-1` is the default region in `DynamoDB.connect`. Override requires modifying code.
- **`build/` is shared boilerplate**: The FAKE build system files (`Targets.fs`, `SafeBuildHelpers.fs`, `Utils.fs`) are shared across multiple Alma libraries. Do not modify `Targets.fs` or `SafeBuildHelpers.fs` without understanding the cross-project impact.
