# Development Log

## 2026-06-25

### 変更内容

- `SingularForwarderFunction` プロジェクト専用のObsidian引き継ぎメモを作成。

### なぜ変更したか

- DynamoDB StreamsからSingular転送までの後段処理を、取り込みLambdaと分けて再開できるようにするため。

### 未解決の課題

- 単体ビルド可否、転送先設定、`odoKm` 関連の現行状態は未確認。
