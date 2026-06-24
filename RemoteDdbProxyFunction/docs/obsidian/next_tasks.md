# Next Tasks

更新日: 2026-06-25

## 優先順位つきの次タスク

1. `RemoteDdbProxyFunction` 単体ビルドの可否を確認する。
2. Secrets Manager参照と外部DynamoDBアクセス設定を確認する。
3. `/remote-ddb/{pk}` のAPI Gateway定義とLambda実装の整合を確認する。

## Codexに次回依頼する指示文

```text
RemoteDdbProxyFunctionプロジェクトの状態確認をお願いします。
まだコード変更せず、単体ビルド可否、Secrets Manager参照、API Gateway経路との整合を確認して要約してください。
```

## 作業前に確認する注意点

- Secret ARNや認証情報をソースへ直書きしない。
- 外部DynamoDBの実環境情報は未確認として扱う。
