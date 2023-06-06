namespace Lmc.DynamoDB

open System
open Amazon.DynamoDBv2
open FSharp.AWS.DynamoDB
open FSharp.AWS.DynamoDB.Scripting // Expose non-Async methods, e.g. PutItem/GetItem
open Lmc.ServiceIdentification
open Lmc.Tracing

//
// Errors
//

[<RequireQualifiedAccess>]
type ConnectionError =
    | RuntimeError of exn

[<RequireQualifiedAccess>]
type PutItemError =
    | TableError of exn
    | RuntimeError of exn

[<RequireQualifiedAccess>]
type GetItemError =
    | TableError of exn
    | RuntimeError of exn

//
// Types
//

[<RequireQualifiedAccess>]
module Attribute =
    type HashKey = HashKeyAttribute
    type RangeKey = RangeKeyAttribute

type AWSAccessKey = {
    Key: string
    Secret: string
}

type Credentials =
    | AccessKey of AWSAccessKey

type TableName =
    | Instance of Instance
    | InstanceWithSidecar of Instance * SidecarSuffix

and SidecarSuffix = SidecarSuffix of string

type Configuration = {
    TableName: TableName
    Credentials: Credentials
}

type DynamoDB = internal {
    DynamoDB: IAmazonDynamoDB
    TableName: TableName
}

[<RequireQualifiedAccess>]
module TableName =
    let value = function
        | Instance instance -> instance |> Instance.concat "-"
        | InstanceWithSidecar (instance, SidecarSuffix sideCarSuffix) -> sprintf "%s--%s" (instance |> Instance.concat "-") sideCarSuffix

type ItemKey = {
    HashKey: HashKey
    RangeKey: RangeKey
}

and HashKey = HashKey of string
and RangeKey = RangeKey of string

[<RequireQualifiedAccess>]
module HashKey =
    let value (HashKey key) = key

[<RequireQualifiedAccess>]
module RangeKey =
    let value (RangeKey key) = key

[<RequireQualifiedAccess>]
module ItemKey =
    let internal toTableKey (key: ItemKey): TableKey =
        TableKey.Combined(key.HashKey, key.RangeKey)

[<RequireQualifiedAccess>]
module DynamoDB =
    open Lmc.ErrorHandling

    let tableName ({ TableName = table }: DynamoDB) = table

    let private trace name tableName =
        sprintf "[DynamoDB] %s" name
        |> Trace.ChildOf.continueOrStart Trace.Active.current
        |> Trace.addTags [
            "component", (sprintf "fDynamoDB (%s)" AssemblyVersionInformation.AssemblyVersion)
            "peer.service", "DynamoDB"
            "db.instance", tableName |> TableName.value
            "db.type", "DynamoDB"
            "span.kind", "client"
        ]

    let private traceError trace error =
        trace
        |> Trace.addError (TracedError.ofError (sprintf "%A") error)
        |> ignore

    let connect (configuration: Configuration) = asyncResult {
        use trace = trace "Connect" configuration.TableName

        try
            let client: IAmazonDynamoDB =
                match configuration.Credentials with
                | AccessKey { Key = key; Secret = secret } -> new AmazonDynamoDBClient(key, secret)

            return {
                DynamoDB = client
                TableName = configuration.TableName
            }
        with e ->
            trace
            |> Trace.addError (TracedError.ofExn e)
            |> ignore

            return! Error (ConnectionError.RuntimeError e)
    }

    let private table<'Dto> { DynamoDB = client; TableName = tableName } =
        try TableContext.Initialize<'Dto>(client, tableName = (tableName |> TableName.value)) |> Ok
        with e -> Error e

    let putItem<'Dto> dynamoDB (item: 'Dto): AsyncResult<ItemKey, PutItemError> = asyncResult {
        use trace = trace "Put Item" dynamoDB.TableName
        let traceError = traceError trace

        let! table =
            dynamoDB
            |> table<'Dto>
            |> Result.mapError PutItemError.TableError
            |> Result.teeError traceError

        let! (key: TableKey) =
            table.PutItemAsync(item)
            |> AsyncResult.ofAsyncCatch PutItemError.RuntimeError
            |> AsyncResult.teeError traceError

        return {
            HashKey = key.HashKey |> string |> HashKey
            RangeKey = key.RangeKey |> string |> RangeKey
        }
    }

    let getItem<'Dto> dynamoDB key = asyncResult {
        use trace =
            trace "Get Item" dynamoDB.TableName
            |> Trace.addTags [
                "db.statement", sprintf "HashKey = %s AND RangeKey = %s" (key.HashKey |> HashKey.value) (key.RangeKey |> RangeKey.value)
            ]
        let traceError = traceError trace

        let! table =
            dynamoDB
            |> table<'Dto>
            |> Result.mapError GetItemError.TableError
            |> Result.teeError traceError

        return!
            table.TryGetItemAsync(key |> ItemKey.toTableKey, true)
            |> AsyncResult.ofAsyncCatch GetItemError.RuntimeError
            |> AsyncResult.teeError traceError
    }

    let getItems<'Dto> dynamoDB itemId (hashKey: HashKey) = asyncResult {
        use trace =
            trace "Get Items" dynamoDB.TableName
            |> Trace.addTags [
                "db.statement", sprintf "HashKey = %s" (hashKey |> HashKey.value)
            ]
        let traceError = traceError trace

        let! table =
            dynamoDB
            |> table<'Dto>
            |> Result.mapError GetItemError.TableError
            |> Result.teeError traceError

        let! items =
            table.QueryAsync(keyCondition = <@ fun item -> item |> itemId = hashKey @>)
            |> AsyncResult.ofAsyncCatch GetItemError.RuntimeError
            |> AsyncResult.teeError traceError

        trace
        |> Trace.addTags [
            "query.totalItems", items |> Seq.length |> string
        ]
        |> ignore

        return items |> List.ofArray
    }
