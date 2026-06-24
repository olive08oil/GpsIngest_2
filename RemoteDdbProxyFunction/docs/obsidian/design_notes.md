# Design Notes

更新日: 2026-06-25

## 設計方針

- API Gateway `/remote-ddb/{pk}` から外部DynamoDBの最新データを取得するProxy Lambda。
- 外部DynamoDBの認証情報はSecrets Manager経由で扱う方針。

## 変更してはいけない方針

- Secret ARN、アクセスキー、APIキーなどをソースへ直書きしない。
- 外部DynamoDBのスキーマは未確認のため、推測でキー構造を変えない。

## 重要なクラス、ファイル、設定

- `RemoteDdbProxyFunction/Function.cs`
- `RemoteDdbProxyFunction/RemoteDdbProxyFunction.csproj`
- `Infra/src/Infra/InfraStack.cs`
