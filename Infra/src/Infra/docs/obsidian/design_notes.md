# Design Notes

更新日: 2026-06-25

## 設計方針

- AWS CDKでLambda、DynamoDB、API Gateway、Secrets Manager関連設定を定義する。
- `GPSIngest`、`SingularForwarderFunction`、`RemoteDdbProxyFunction` の実行基盤をまとめて扱う。

## 変更してはいけない方針

- 秘匿情報をソースへ直書きしない。
- 実環境へ影響する削除ポリシー、テーブル名、API経路は根拠なく変更しない。
- Lambda asset pathを変える場合は、ソリューション構成とデプロイ成果物を確認する。

## 重要なクラス、ファイル、設定

- `Infra/src/Infra/InfraStack.cs`
- `Infra/src/Infra/Infra.csproj`
- `Infra/README.md`
- `Infra/README_1.md`
