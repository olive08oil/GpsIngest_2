# Next Tasks

更新日: 2026-06-25

## 優先順位つきの次タスク

1. `SingularForwarderFunction` 単体ビルドの可否を確認する。
2. `Function.cs` でDynamoDB StreamイベントからSingular送信用データを作る処理を確認する。
3. APIキーやURLなどの秘匿情報が直書きされていないか確認する。

## Codexに次回依頼する指示文

```text
SingularForwarderFunctionプロジェクトの状態確認をお願いします。
まだコード変更せず、単体ビルド可否、転送処理、秘匿情報の有無を確認して要約してください。
```

## 作業前に確認する注意点

- 転送先APIの仕様は未確認。推測でフィールド名を変更しない。
- `odoKm` 関連の過去エラーが現行にも残るか確認してから修正する。
