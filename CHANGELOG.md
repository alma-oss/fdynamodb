# Changelog

<!-- There is always Unreleased section on the top. Subsections (Add, Changed, Fix, Removed) should be Add as needed. -->
## Unreleased

## 5.1.1 - 2024-05-29
- Fix `DynamoDB.getItems` to use a key directly in keyCondition expression

## 5.1.0 - 2024-01-11
- Update dependencies

## 5.0.0 - 2024-01-09
- [**BC**] Use net8.0
- Fix package metadata

## 4.2.0 - 2023-11-02
- Add `AWSSDK.SecurityToken` as it is required for service account connection

## 4.1.0 - 2023-11-02
- Add `Credentials.ServiceAccount` case

## 4.0.0 - 2023-09-11
- [**BC**] Use `Alma` namespace

## 3.1.0 - 2023-08-25
- Add `DynamoDB.scanAllItems` function

## 3.0.0 - 2023-08-11
- [**BC**] Use net7.0

## 2.0.0 - 2023-06-29
- Fix key casting for `DynamoDB`
- [**BC**] Change `ItemKey` type

## 1.2.0 - 2023-06-29
- Always use `Amazon.RegionEndpoint.EUWest1` in connection

## 1.1.0 - 2023-06-06
- Add `DynamoDB.tableName` function
- Add `TableName.parse` function

## 1.0.0 - 2023-06-06
- Initial implementation
