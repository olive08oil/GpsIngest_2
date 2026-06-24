# Design Notes

更新日: 2026-06-25

## 設計方針

- Windows FormsでGPSデータを受信し、取り込みLambdaへ送信するクライアント。
- シリアルまたはUDP受信を扱う想定だが、詳細は未確認。

## 変更してはいけない方針

- 既存のフォーム構成、プロジェクト名、設定名は根拠なく変更しない。
- 実機接続や外部API送信に関わる変更は、設定と送信先を確認してから行う。

## 重要なクラス、ファイル、設定

- `GpsIngestClinetForm/Form1.cs`
- `GpsIngestClinetForm/ReceiverService.cs`
- `GpsIngestClinetForm/GpsIngestClinetForm.csproj`
