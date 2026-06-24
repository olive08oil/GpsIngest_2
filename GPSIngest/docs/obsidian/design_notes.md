# Design Notes

更新日: 2026-06-25

## 設計方針

- API Gateway `/ingest` から受けたGPSデータを正規化し、DynamoDBへ保存するLambda。
- `ID75` / `IDA3` / `NMEA` を扱う。
- `TelemetryHistory` は履歴、`DeviceLatest` は最新状態を保持する想定。

## 変更してはいけない方針

- `ID75` / `IDA3` の `raw` はBase64入力として扱う。
- `NMEA` の `raw` はNMEA文字列として扱う。
- 保存先テーブルやキー設計を変える場合はInfra側と合わせて変更する。

## 重要なクラス、ファイル、設定

- `GPSIngest/Function.cs`
- `GPSIngest/Models/IngestRequest.cs`
- `GPSIngest/Models/NormalizedTelemetry.cs`
- `GPSIngest/Parsers/TelemetryParser.cs`
- `GPSIngest/Models/NmeaParser.cs`
- `Infra/src/Infra/InfraStack.cs`
