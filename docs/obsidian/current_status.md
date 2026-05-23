# Current Status

更新日: 2026-05-24

## 現在の作業内容

- 中断処理を実行し、Codex作業引き継ぎ用のObsidianメモを現在の作業結果が次回すぐ分かる状態へ更新している。
- 現行の作業対象は `C:\Users\olive\SynologyDrive\code\Visual Studio 2022\Projects\GPSIngest_2`。
- GitHubバックアップは設定済み。`main` ブランチは `origin/main` を追跡している。
- 2026-05-24 01時台に再度中断処理を実行し、Git未管理状態とバックアップzipの存在を再確認した。

## 今回変更したファイル

- `.gitignore`
- `Infra/src/Infra/InfraStack.cs`
- `docs/obsidian/current_status.md`
- `docs/obsidian/next_tasks.md`
- `docs/obsidian/design_notes.md`
- `docs/obsidian/development_log.md`

## Gitバックアップ状況

- GitHubアップロード準備として `GPSIngest_2` をGit初期化済み。
- GitHub remote予定先: `https://github.com/olive08oil/GpsIngest_2`
- `.gitignore` を追加し、`.vs/`, `bin/`, `obj/`, `publish/`, `cdk.out/`, zip, `*.user` を除外している。
- `Infra/.git` と `Infra/_1.git` は `Infra_legacy_git_metadata_20260524.zip` に退避後、GitHubへ通常ソースとして上げるため削除した。
- GitHubへの初回pushは成功。
- GitHub URL: `https://github.com/olive08oil/GpsIngest_2`
- 初回コミット: `5af4b0f` (`chore: initial project import`)
- GitHub状態記録コミット: `0677813` (`docs: record github backup status`)

## 今回解決した問題

- Codex引き継ぎ用メモを作成/更新し、次回再開時の確認先を明確にした。
- 旧候補フォルダの関係を確認した。
  - `GPSIngest_2`: 現行本線。
  - `GPSIngest`: `GPSIngest_2` の前段階に近い旧ソース。
  - `GPSIngest_1`: ほぼ空の初期Class Library。
- `GPSIngest_1` はバックアップzip化して削除済み。
  - バックアップ: `C:\Users\olive\SynologyDrive\code\Visual Studio 2022\Projects\GPSIngest_1_backup_20260524.zip`
  - 元フォルダ: 削除済み。
- 旧 `GPSIngest` のビルド状況を確認した。
  - `GPSIngest\GPSIngest.csproj` 単体: ビルド成功、警告0、エラー0。
  - `GPSIngest.sln` 全体: ビルド失敗。
  - 失敗原因: `SingularForwarderFunction\Function.cs` の未定義変数 `odoKm`。
- 「再開処理」「中断処理」の運用ルールを会話上で確認した。
- GitHubへ上げる前に、`Infra/src/Infra/InfraStack.cs` から固定APIキーとRemote DDB Secrets Manager ARNの直書きを除去した。
  - API Gateway APIキー値はCDK生成に変更。
  - Remote DDB Secret ARNは `RemoteDdbSecretArn` CloudFormationパラメータに変更。
- `Infra/src/Infra/Infra.csproj` 単体ビルドは成功。警告1件、エラー0件。
- `GPSIngest_2.sln` 全体ビルドはRestoreターゲットで失敗扱い。`-v:normal` でも警告0・エラー0で具体原因は未表示。

## まだ残っている問題

- `GPSIngest_2.sln` の全体ビルドは失敗扱い。原因は未特定。
- `GPSIngest_2` はGit初期化済みで、GitHubへの初回pushは完了。
- `GPSIngest_2` には `bin/`, `obj/`, `publish/`, zipファイルなどの生成物/バックアップが残っているが、`.gitignore` でGit管理対象外にしている。
- `GPSIngest` 配下に空または重複に見える `.cs` ファイルがある。
  - `GPSIngest/NormalizedTelemetry.cs`
  - `GPSIngest/NmeaParser.cs`
  - `GPSIngest/IngestRequest.cs`
  - `GPSIngest/IdxxParser.cs`
  - `GPSIngest/FrameUnwrapper.cs`
  - `GPSIngest/CodeFile1.cs`
  - `GPSIngest/CodeFile1 - コピー (2).cs`
  - `GPSIngest/Models/GPSIngest.cs`
  - `GPSIngest/Models/CodeFile1 - コピー (2).cs`
- `Infra/src/Infra/InfraStack.cs` の固定APIキー値とRemote DDB Secrets Manager ARN直書きは除去済み。
- `GPSIngest/Function.cs` は `DeviceLatest` を常に上書きする実装。古い `ts` を弾く条件付き更新はコメントアウトされている。
- `RemoteDdbProxyFunction/Function.cs` など、一部ファイルに日本語コメントの文字化けが見える。
- 旧 `GPSIngest` をアーカイブするか削除するかは未決定。

## 次回Codexに依頼すべき作業

1. `GPSIngest_2.sln` のRestore/ビルド失敗原因を調査する。
2. ビルド結果をもとに、`GPSIngest` 配下の重複/空ファイルを整理する。
3. 次回以降の変更はGitコミットしてGitHubへpushする。

## 次回作業前に確認すべき注意点

- まず `docs/obsidian/current_status.md` を読む。
- コード変更前に `GPSIngest_2.sln` のビルド結果を確認する。
- `GPSIngest_2` ルートはGit初期化済み。生成物を誤って追加しないよう `.gitignore` を確認する。
- 作業後は `git status --short` を確認し、必要に応じてコミットしてGitHubへpushする。
- 旧 `GPSIngest_1` は削除済み。必要なら `GPSIngest_1_backup_20260524.zip` を参照する。
- 旧 `GPSIngest` はまだ存在するが、現行作業対象は `GPSIngest_2` とする。
- `GpsIngestClinetForm` は誤字に見えるが既存プロジェクト名なので、未確認のままリネームしない。
- AWS CDKの `RemovalPolicy`, API Key, Secrets ARN, Lambda asset path は実環境に影響するため慎重に扱う。

## 次回最初に確認すべきファイル

- `docs/obsidian/current_status.md`
- `docs/obsidian/next_tasks.md`
- `GPSIngest_2.sln`
- `GPSIngest/GPSIngest.csproj`
- `GPSIngest/Function.cs`
- `GPSIngest/Parsers/TelemetryParser.cs`
- `GPSIngest/Models/IngestRequest.cs`
- `GPSIngest/Models/NormalizedTelemetry.cs`
- `GpsIngestClinetForm/Form1.cs`
- `GpsIngestClinetForm/ReceiverService.cs`
- `SingularForwarderFunction/Function.cs`
- `RemoteDdbProxyFunction/Function.cs`
- `Infra/src/Infra/InfraStack.cs`
