# Development Log

## 2026-06-25

### 変更内容

- 外部ルールファイル `C:\Users\olive\SynologyDrive\code\Visual Studio 2026\プロジェクトルール_AI併用版.md` を確認。
- このリポジトリ用に `プロジェクトルール.md` を追加。
- ルート `docs/obsidian/agent_review_queue.md` を追加。
- ソリューションに含まれる5プロジェクトへ、プロジェクト単位のObsidian引き継ぎメモを追加。
  - `GPSIngest/docs/obsidian/`
  - `GpsIngestClinetForm/docs/obsidian/`
  - `SingularForwarderFunction/docs/obsidian/`
  - `RemoteDdbProxyFunction/docs/obsidian/`
  - `Infra/src/Infra/docs/obsidian/`

### なぜ変更したか

- プロジェクトルールでは、Visual Studioの複数 `.csproj` ソリューションは各プロジェクトフォルダ配下に引き継ぎメモを置く方針のため。
- Codex以外のAI作業を採用前に確認できるよう、`agent_review_queue.md` を追加するため。

### 未解決の課題

- `GPSIngest_2.sln` 全体ビルド失敗原因は未確認。
- `Infra/src_1/Infra/Infra.csproj` はソリューション外に存在するため、旧コピーかどうか未確認。
- 今回はドキュメント整備のみで、ビルド確認は実施していない。

## 2026-05-24

### 変更内容

- `docs/obsidian/` を作成。
- Codex引き継ぎ用Markdownを作成/更新。
  - `current_status.md`
  - `next_tasks.md`
  - `design_notes.md`
  - `development_log.md`
- 現在の `GPSIngest_2` のリポジトリ構成を確認。
- 旧候補フォルダ `GPSIngest` と `GPSIngest_1` を確認。
- `GPSIngest_1` を `GPSIngest_1_backup_20260524.zip` に圧縮し、元フォルダを削除。
- 旧 `GPSIngest` のビルド可否を確認。
- 「再開処理」「中断処理」の運用ルールを確認。
- 中断処理として、今回の作業内容、解決済み事項、残課題、次回依頼内容をObsidianメモへ反映。
- 2026-05-24 01時台に再度中断処理を実行し、引き継ぎメモ、Git状態、`GPSIngest_1` バックアップzipの存在を再確認。
- GitHubアップロード準備として `GPSIngest_2` をGit初期化。
- `.gitignore` を追加し、生成物、zip、`.vs/`, `*.user` などを除外。
- `Infra/src/Infra/InfraStack.cs` から固定APIキーとRemote DDB Secret ARNの直書きを除去。
- `Infra/src/Infra/Infra.csproj` 単体ビルドを実行し、成功を確認。
- `Infra/.git` と `Infra/_1.git` を `Infra_legacy_git_metadata_20260524.zip` に退避して削除し、`Infra` を通常ソースとしてGit管理できる状態にした。
- `GPSIngest_2.sln` 全体ビルドを実行。Restoreターゲットで失敗扱いだが、警告0・エラー0で具体原因は未表示。
- 初回コミット `5af4b0f` (`chore: initial project import`) を作成し、GitHub `https://github.com/olive08oil/GpsIngest_2` の `main` へpush。

### 今回変更したファイル

- `docs/obsidian/current_status.md`
- `docs/obsidian/next_tasks.md`
- `docs/obsidian/design_notes.md`
- `docs/obsidian/development_log.md`
- `.gitignore`
- `Infra/src/Infra/InfraStack.cs`

### なぜ変更したか

- Codexでの開発を途中で中断しても、次回このMarkdownを読めば作業を再開できるようにするため。
- `GPSIngest_2`、旧 `GPSIngest`、旧 `GPSIngest_1` の関係が不明になっていたため、現時点の判断を記録するため。
- 不要と判断できる `GPSIngest_1` を退避して作業対象を減らすため。
- 次回の作業開始時に、コード変更前の確認事項を明確にするため。

### 確認したこと

- `GPSIngest_2.sln` には以下の5プロジェクトが含まれる。
  - `GPSIngest`
  - `Infra`
  - `GpsIngestClinetForm`
  - `SingularForwarderFunction`
  - `RemoteDdbProxyFunction`
- `GPSIngest_2` ルートには当初 `.git` が存在しなかったが、GitHubアップロード作業でGit初期化済み。
- `GPSIngest_1` はほぼ空の初期Class Libraryで、現行作業には不要と判断。
- `GPSIngest_1` のバックアップzipは存在し、元フォルダは削除済み。
- 旧 `GPSIngest` は `GPSIngest_2` の前段階に近い構成。
- 旧 `GPSIngest\GPSIngest.csproj` 単体ビルドは成功。
- 旧 `GPSIngest.sln` 全体ビルドは失敗。
  - 原因: `SingularForwarderFunction\Function.cs` の `odoKm` 未定義エラー。
- `Infra/src/Infra/Infra.csproj` 単体ビルドは成功。
  - 警告: `InfraStack.cs(22,66)` のnullable注釈コンテキスト警告。
  - エラー: 0件。
- `GPSIngest_2.sln` 全体ビルドは失敗扱い。
  - `dotnet build GPSIngest_2.sln -v:normal` でもRestoreターゲットで失敗。
  - 警告0・エラー0で具体原因は未表示。

### 未解決の課題

- `GPSIngest_2.sln` のRestore/ビルド失敗原因調査。
- `GPSIngest_2` 内の重複/空ソースファイルの整理は未実施。
- AWS CDKスタックの実デプロイ状態は未確認。
- 固定APIキー、Secrets Manager ARN、`RemovalPolicy.DESTROY` の運用妥当性は未確認。
- `RemoteDdbProxyFunction/Function.cs` の文字化けコメントの修正要否は未確認。
- 旧 `GPSIngest` をアーカイブ/削除するかは未決定。
- 今後の変更ごとのコミット/push運用。

### Gitバックアップ

- `GPSIngest_2` をGit初期化済み。
- GitHub remote予定先: `https://github.com/olive08oil/GpsIngest_2`
- 生成物を除外する `.gitignore` を追加済み。
- `Infra_legacy_git_metadata_20260524.zip` は `.gitignore` によりGit管理対象外。
- 初回push完了。`main` は `origin/main` を追跡中。
- `0677813` (`docs: record github backup status`) までGitHubへpush済み。

## 過去履歴

- Git初期化前の履歴は未確認。GitHubアップロード後の履歴は `git log` で確認可能。
