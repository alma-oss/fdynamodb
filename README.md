F-DynamoDB
==========

Library for accessing a DynamoDB storage.

## Install

Add following into `paket.dependencies`
```
source https://nuget.pkg.github.com/almacareer/index.json username: "%PRIVATE_FEED_USER%" password: "%PRIVATE_FEED_PASS%"
# LMC Nuget dependencies:
nuget Alma.DynamoDB
```

NOTE: For local development, you have to create ENV variables with your github personal access token.
```sh
export PRIVATE_FEED_USER='{GITHUB USERNANME}'
export PRIVATE_FEED_PASS='{TOKEN}'	# with permissions: read:packages
```

Add following into `paket.references`
```
Alma.DynamoDB
```

## Use

### Connect to a table
> TableName is part of the configuration since it should be paired with specific credentials (and its policies)

```fs
open Alma.DynamoDB

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

let dynamoDB = DynamoDB.connect configuration
```

### Put item to DynamoDB
```fs
open Alma.DynamoDB
open Alma.ErrorHandling

type ItemDTO = {
    [<Attribute.HashKey>] PrimaryKey: string
    [<Attribute.RangeKey>] SecondaryKey: string

    OtherAttribute: string
}

asyncResult {
    let! itemKey =
        {
            PrimaryKey = "Movie"
            SecondaryKey = "Lord of the Rings"
            OtherAttribute = "Trilogy"
        }
        |> DynamoDB.putItem dynamoDB

    return itemKey
}
```

### Get item from DynamoDB
```fs
open Alma.DynamoDB

asyncResult {
    let! lotrMovie =
        {
            HashKey = HashKey "Movie"
            RangeKey = RangeKey "Lord of the Rings"
        }
        |> DynamoDB.getItem dynamoDB

    return lotrMovie
}
```

### Get items by a hashKey from DynamoDB
```fs
open Alma.DynamoDB

asyncResult {
    let! movies =
        HashKey "Movie"
        |> DynamoDB.getItems dynamoDB (fun item -> item.PrimaryKey)

    return movies
}
```

## Release
1. Increment version in `DynamoDB.fsproj`
2. Update `CHANGELOG.md`
3. Commit new version and tag it

## Development
### Requirements
- [dotnet core](https://dotnet.microsoft.com/learn/dotnet/hello-world-tutorial)

### Build
```bash
./build.sh build
```

### Tests
```bash
./build.sh -t tests
```
