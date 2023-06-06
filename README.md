F-DynamoDB
==========

Library for accessing a DynamoDB storage.

## Install

Add following into `paket.dependencies`
```
git ssh://git@bitbucket.lmc.cz:7999/archi/nuget-server.git master Packages: /nuget/
# LMC Nuget dependencies:
nuget Lmc.DynamoDB
```

Add following into `paket.references`
```
Lmc.DynamoDB
```

## Use

### Connect to a table
> TableName is part of the configuration since it should be paired with specific credentials (and its policies)

```fs
open Lmc.DynamoDB

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
open Lmc.DynamoDB
open Lmc.ErrorHandling

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
open Lmc.DynamoDB

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
open Lmc.DynamoDB

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
4. Run `$ fake build target release`
5. Go to `nuget-server` repo, run `fake build target copyAll` and push new versions

## Development
### Requirements
- [dotnet core](https://dotnet.microsoft.com/learn/dotnet/hello-world-tutorial)
- [FAKE](https://fake.build/fake-gettingstarted.html)

### Build
```bash
./build.sh
```

### Watch
```bash
./build.sh -t watch
```
