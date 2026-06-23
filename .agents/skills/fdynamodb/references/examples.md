# Examples

All example code for the skill lives here. Examples are ordered from simplest to most complete; each is self-contained.

## Basic Connection

```fsharp
open Alma.DynamoDB
open Alma.ServiceIdentification

let configuration = {
    Credentials = AccessKey {
        Key = "..."
        Secret = "..."
    }
    TableName = TableName.Instance {
        Domain = Domain "domain"
        Context = Context "context"
        Purpose = Purpose "purpose"
        Version = Version "version"
    }
}

// `connect` returns AsyncResult<DynamoDB, ConnectionError>
let connectExample = DynamoDB.connect configuration
```

## Put And Get

```fsharp
open Alma.DynamoDB
open Feather.ErrorHandling

type ItemDTO = {
    [<Attribute.HashKey>] PrimaryKey: string
    [<Attribute.RangeKey>] SecondaryKey: string
    OtherAttribute: string
}

let putAndGet dynamoDB = asyncResult {
    let! itemKey =
        {
            PrimaryKey = "ExampleApi"
            SecondaryKey = "config-v1"
            OtherAttribute = "enabled"
        }
        |> DynamoDB.putItem dynamoDB

    let! fetched =
        Combined {
            HashKey = HashKey "ExampleApi"
            RangeKey = RangeKey "config-v1"
        }
        |> DynamoDB.getItem<ItemDTO> dynamoDB

    return itemKey, fetched
}
```

## Query By Hash Key

```fsharp
open Alma.DynamoDB
open Feather.ErrorHandling

let queryByHashKey dynamoDB = asyncResult {
    let! items =
        HashKey "ExampleApi"
        |> DynamoDB.getItems<ItemDTO> dynamoDB (fun table (HashKey key) ->
            table.QueryAsync(keyCondition = <@ fun item -> item.PrimaryKey = key @>))

    return items
}
```

## Scan All Items

```fsharp
open Alma.DynamoDB
open Feather.ErrorHandling

// Reserve scans for maintenance/bootstrap paths.
let scanAll dynamoDB = asyncResult {
    let! allItems = DynamoDB.scanAllItems<ItemDTO> dynamoDB
    return allItems
}
```

## Checkpoint Storage

```fsharp
open Alma.DynamoDB
open Alma.DynamoDB.Checkpoint
open Alma.ServiceIdentification
open Feather.ErrorHandling

let connectCheckpointStore () = asyncResult {
    let! instance = Create.Instance "domain-context-purpose-version"
    let table = instance, SidecarSuffix "v1"
    let configuration = CheckpointStore.configuration table ServiceAccount
    return! CheckpointStore.connect configuration
}

let storeAndRetrieve dynamoDB = asyncResult {
    let consumer = ConsumerInstance "worker-instance"
    let checkpoint = Checkpoint "topic-0"

    do! CheckpointStore.storeCheckpoint dynamoDB consumer checkpoint 42L

    let! offset = CheckpointStore.retrieveCheckpoint dynamoDB consumer checkpoint
    return offset // int64 option
}
```

## Full Workflow

```fsharp
open Alma.DynamoDB
open Alma.ServiceIdentification
open Feather.ErrorHandling

type RecordDTO = {
    [<Attribute.HashKey>] Partition: string
    OtherAttribute: string
}

let runWorkflow () = asyncResult {
    let configuration = {
        Credentials = ServiceAccount
        TableName =
            TableName.Instance {
                Domain = Domain "domain"
                Context = Context "context"
                Purpose = Purpose "purpose"
                Version = Version "version"
            }
    }

    let! dynamoDB = DynamoDB.connect configuration

    let! _ =
        { Partition = "CacheInstance"; OtherAttribute = "warm" }
        |> DynamoDB.putItem dynamoDB

    let! fetched =
        Hash (HashKey "CacheInstance")
        |> DynamoDB.getItem<RecordDTO> dynamoDB

    return fetched
}
```
