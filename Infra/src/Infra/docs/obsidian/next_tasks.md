# Next Tasks

更新日: 2026-06-25

## 優先順位つきの次タスク

1. `Infra/src/Infra/Infra.csproj` 単体ビルドを必要に応じて再確認する。
2. `InfraStack.cs` のLambda、DynamoDB、API Gateway、Secrets Manager設定を現行仕様として整理する。
3. `Infra/src_1` の扱いを確認し、旧コピーならアーカイブまたは除外方針を決める。

## Codexに次回依頼する指示文

```text
Infraプロジェクトの状態確認をお願いします。
まだコード変更せず、CDK定義、秘匿情報の扱い、Infra/src_1 の扱いを確認して要約してください。
```

## 作業前に確認する注意点

- APIキー、Secret ARN、認証情報をソースへ直書きしない。
- `RemovalPolicy` やDynamoDBテーブル定義は実環境に影響するため慎重に扱う。
