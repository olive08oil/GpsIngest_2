# Design Notes

更新日: 2026-05-24

## このプロジェクトの設計方針

### 全体構成

- `GPSIngest_2` はGPSデータの受信、正規化、DynamoDB保存、Singular転送、外部DynamoDB参照を扱う .NET 8 / AWS Lambda / Windows Forms / CDK 構成のプロジェクト。
- データの基本経路:
  - `GpsIngestClinetForm` がシリアル/UDPからデータを受信。
  - API Gateway `/ingest` にJSON POST。
  - `GPSIngest` Lambdaが `ID75` / `IDA3` / `NMEA` を正規化。
  - `TelemetryHistory` と `DeviceLatest` に保存。
  - `DeviceLatest` Streamsから `SingularForwarderFunction` がSingularへ転送。
- 追加経路:
  - API Gateway `/remote-ddb/{pk}` から `RemoteDdbProxyFunction` が外部DynamoDBの最新データを取得。

### 取り込みLambda

- 主ファイル: `GPSIngest/Function.cs`
- 入力モデル: `GPSIngest/Models/IngestRequest.cs`
- 正規化モデル: `GPSIngest/Models/NormalizedTelemetry.cs`
- 対応 `payloadType`:
  - `ID75`: 位置、速度、方位を扱う。
  - `IDA3`: センサー速度、方位、積算距離を扱う。
  - `NMEA`: RMC/VTGなどを解析する。
- `ID75` / `IDA3` の `raw` はBase64。
- `NMEA` の `raw` はNMEA文字列。
- `GPSIngest/Parsers/TelemetryParser.cs` がID75/IDA3の主要パーサ。
- `GPSIngest/Models/NmeaParser.cs` がNMEAの主要パーサ。

### DynamoDB設計

- `TelemetryHistory`
  - Partition key: `pk`
  - Sort key: `sk`
  - GSI: `LatestByDevice`
  - GSI: `ByMsgType`
  - PITR有効。
  - 現状 `RemovalPolicy.DESTROY`。
- `DeviceLatest`
  - Partition key: `deviceId`
  - Stream: `NEW_IMAGE`
  - PITR有効。
  - 現状 `RemovalPolicy.DESTROY`。
- 現在の `GPSIngest/Function.cs` は `DeviceLatest` を常に上書きする。
- 古い `ts` を弾く条件付き更新コードはコメントアウトされている。運用上どちらが正しいかは未確認。

### クライアント

- 主フォルダ: `GpsIngestClinetForm`
- 主ファイル:
  - `GpsIngestClinetForm/Form1.cs`
  - `GpsIngestClinetForm/Form1.Designer.cs`
  - `GpsIngestClinetForm/ReceiverService.cs`
- 受信モード:
  - `SerialNmea`
  - `SerialBinaryFixed`
  - `SerialPioneerAuto`
  - `UdpNmea`
  - `UdpBinaryDatagram`
  - `UdpPioneerAuto`
- APIキーは `x-api-key` ヘッダーで送る。

### Singular転送

- 主ファイル: `SingularForwarderFunction/Function.cs`
- トリガ: `DeviceLatest` のDynamoDB Streams。
- Secrets ManagerからSingular Private Tokenを取得する。
- Singular PUT先:
  - `https://datastream.singular.live/datastreams/{privateToken}`
- `speed` が無い場合は `speed_A3` を `speedKmh` として送る。

### Remote DDB Proxy

- 主ファイル: `RemoteDdbProxyFunction/Function.cs`
- Secrets Managerから外部DynamoDB用の認証情報、リージョン、テーブル名を取得する。
- GSI `pk-receivedAt-index` を `pk` で降順Queryし、最新1件を返す。
- `pk` / `deviceId` / `DeviceId` の揺れに対応している。

### インフラ

- 主ファイル: `Infra/src/Infra/InfraStack.cs`
- 定義される主なAWSリソース:
  - `TelemetryHistory`
  - `DeviceLatest`
  - `GpsIngestFn`
  - API Gateway `/ingest`
  - API Key / Usage Plan
  - Singular用Secrets Manager Secret
  - `SingularForwarderFn`
  - `RemoteDdbProxyFn`
  - API Gateway `/remote-ddb/{pk}`

## 変更してはいけない方針

- 未確認のままテーブル名、キー名、GSI名を変更しない。
- 未確認のまま `GpsIngestClinetForm` を `GpsIngestClientForm` などにリネームしない。
- 未確認のまま `speedKmh`, `speed_A3`, `odo_total_m` などの外部連携フィールド名を変更しない。
- 未確認のまま `ID75` / `IDA3` のバイナリオフセット、エンディアン、スケールを変更しない。
- 未確認のままCDKの `RemovalPolicy.DESTROY` を本番向けとして扱わない。
- 未確認のまま固定APIキーやSecrets ARNを変更しない。
- 生成物、zip、旧フォルダ由来に見えるファイルを削除するときは、先に退避または根拠確認を行う。
- `GPSIngest_2` をGit管理する場合は、未確認のまま生成物やzipを大量にコミットしない。
- GitHubへpushする前に、固定APIキーやSecrets ARNなどの直書きを残さない。

## 重要なクラス、ファイル、設定

- `GPSIngest_2.sln`
- `GPSIngest/GPSIngest.csproj`
- `GPSIngest/Function.cs`
- `GPSIngest/Models/IngestRequest.cs`
- `GPSIngest/Models/NormalizedTelemetry.cs`
- `GPSIngest/Models/NmeaParser.cs`
- `GPSIngest/Parsers/TelemetryParser.cs`
- `GpsIngestClinetForm/GpsIngestClinetForm.csproj`
- `GpsIngestClinetForm/Form1.cs`
- `GpsIngestClinetForm/Form1.Designer.cs`
- `GpsIngestClinetForm/ReceiverService.cs`
- `SingularForwarderFunction/Function.cs`
- `RemoteDdbProxyFunction/Function.cs`
- `Infra/src/Infra/InfraStack.cs`
- `GPSIngest/publish`
- `SingularForwarderFunction/bin/Release/net8.0/publish`
- `RemoteDdbProxyFunction/bin/Release/net8.0`

## 未確認事項

- `GPSIngest_2.sln` の現在のビルド可否。
- 実AWS環境のアカウント、リージョン、デプロイ済みスタック名。
- 現在有効なAPI Gateway URLとAPIキー。
- `TelemetryHistory` / `DeviceLatest` が本番データを持つか。
- `RemovalPolicy.DESTROY` がテスト用途として許容されているか。
- `GPSIngest` 配下の重複/空ファイルが意図的なバックアップか、誤ってCompile対象に残っているか。
- `RemoteDdbProxyFunction/Function.cs` の文字化けコメントの修正要否。
- 旧 `GPSIngest` をアーカイブ/削除するか。
- GitHub push後のリポジトリ公開範囲。private推奨。
