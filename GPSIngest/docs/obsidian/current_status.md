# Current Status

更新日: 2026-06-25

## プロジェクト

- 名前: `GPSIngest`
- 種別: .NET 8 AWS Lambda
- csproj: `GPSIngest/GPSIngest.csproj`
- Gitルート: `C:\Users\olive\SynologyDrive\code\Visual Studio 2022\Projects\GPSIngest_2`
- ブランチ: `main`

## 現在の作業内容

- GPS取り込みLambdaの状態把握と、プロジェクト単位のCodex引き継ぎメモを整備している。
- ルートの `docs/obsidian/` が全体統括メモで、このファイルは `GPSIngest` プロジェクト専用メモ。

## 完了済みの作業

- `GPSIngest_2` が現行本線であることを確認済み。
- GitHub remote `https://github.com/olive08oil/GpsIngest_2` への初回pushは完了済み。
- 固定APIキーなどInfra側の直書き値は過去作業で除去済み。

## 未完了の作業

- `GPSIngest_2.sln` 全体ビルドがRestoreターゲットで失敗扱いになる原因調査。
- `GPSIngest` 配下の重複・空ファイル・コピー残骸の整理。

## 現在の問題点

- ソリューション全体ビルド失敗の具体原因は未確認。
- `GPSIngest` 配下に重複または残骸に見える `.cs` ファイルがある。

## 次回最初に確認すべきファイル

- `docs/obsidian/current_status.md`
- `GPSIngest/docs/obsidian/current_status.md`
- `GPSIngest/GPSIngest.csproj`
- `GPSIngest/Function.cs`
- `GPSIngest/Parsers/TelemetryParser.cs`
- `GPSIngest/Models/IngestRequest.cs`
- `GPSIngest/Models/NormalizedTelemetry.cs`
