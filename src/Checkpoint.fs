namespace Alma.DynamoDB

module Checkpoint =
    open Feather.ErrorHandling

    //
    // Types
    //

    type CheckpointDTO = {
        [<Attribute.HashKey>] Instance: string
        [<Attribute.RangeKey>] Checkpoint: string
        Value: int64
    }

    type ConsumerInstance = ConsumerInstance of string
    type Checkpoint = Checkpoint of string

    [<RequireQualifiedAccess>]
    module CheckpointStore =
        let configuration table credentials =
            {
                TableName = InstanceWithSidecar table
                Credentials = credentials
            }

        let connect = DynamoDB.connect

        let storeCheckpoint (dynamoDB: DynamoDB) (ConsumerInstance consumer) (Checkpoint checkpoint) (value: int64): AsyncResult<unit, PutItemError> =
            DynamoDB.putItem<CheckpointDTO> dynamoDB {
                Instance = consumer
                Checkpoint = checkpoint
                Value = value
            }
            |> AsyncResult.ignore

        let retrieveCheckpoint (dynamoDB: DynamoDB) (ConsumerInstance consumer) (Checkpoint checkpoint): AsyncResult<int64 option, GetItemError> =
            Combined {
                HashKey = HashKey consumer
                RangeKey = RangeKey checkpoint
            }
            |> DynamoDB.getItem<CheckpointDTO> dynamoDB
            |> AsyncResult.map (Option.map (fun dto -> dto.Value))
