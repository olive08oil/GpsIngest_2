# Design Notes

更新日: 2026-06-25

## 設計方針

- `DeviceLatest` のDynamoDB Streamsを受け、Singular向けに最新GPS状態を転送する。
- 取り込みLambdaとは独立した後段処理として扱う。

## 変更してはいけない方針

- Streamイベントの入力構造を確認せずにフィールド名を変更しない。
- APIキー、URL、Secret ARNなどの秘匿情報をソースへ直書きしない。

## 重要なクラス、ファイル、設定

- `SingularForwarderFunction/Function.cs`
- `SingularForwarderFunction/SingularForwarderFunction.csproj`
- `Infra/src/Infra/InfraStack.cs`
