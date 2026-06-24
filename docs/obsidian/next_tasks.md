# Next Tasks

更新日: 2026-06-25

## 優先順位つきの次タスク

### P0: プロジェクトルールに沿って再開・中断処理を運用する

1. 再開処理では、ルート `docs/obsidian/` と作業対象プロジェクトの `docs/obsidian/` を読む。
2. `プロジェクトルール.md` と `agent_review_queue.md` を確認してから作業する。
3. Codex以外のAI作業がある場合は、採用前に `agent_review_queue.md` へ記録し、Codexまたはユーザー確認を行う。

### P1: `GPSIngest_2` のRestore/ビルド失敗原因を確認する

1. `dotnet build GPSIngest_2.sln -v:normal` はRestoreターゲットで失敗扱いになるが、警告0・エラー0で具体原因が表示されない。
2. 次回は `dotnet restore GPSIngest_2.sln -v:diag` や個別プロジェクトビルドで原因を切り分ける。
3. 原因を以下のどれに該当するか分類する。
   - 重複 `.cs` ファイル
   - 空ファイル/コピー残骸
   - 文字化けや名前空間不整合
   - Lambda/WinForms/CDKの参照不整合
4. ビルド確認後、結果を `development_log.md` に追記する。

### P2: 重複・残骸ファイルを整理する

1. `GPSIngest` 直下と `GPSIngest/Models` / `GPSIngest/Parsers` の重複ファイルを確認する。
2. `GPSIngest.csproj` の暗黙Compile対象に入っているファイルを確認する。
3. 不要ファイルの削除またはプロジェクト除外は、ビルド結果と根拠を確認してから実施する。

### P3: GitHubバックアップ運用を継続する

1. GitHub remote `https://github.com/olive08oil/GpsIngest_2` への初回pushは完了済み。
2. 今後の作業はコミット単位で保存し、必要に応じてGitHubへpushする。
3. `git status --short --ignored` で生成物やzipが除外されているか確認する。

### P4: 旧 `GPSIngest` の扱いを決める

1. 旧 `GPSIngest` は `GPSIngest_2` の前段階に近いが、現時点では本線ではない。
2. 旧 `GPSIngest.sln` は `SingularForwarderFunction` の `odoKm` 未定義でビルド失敗する。
3. 必要なら旧 `GPSIngest` もzip化してアーカイブし、今後の作業対象を `GPSIngest_2` に一本化する。

## Codexに次回依頼する指示文

```text
再開処理をお願いします。
docs/obsidian/current_status.md, next_tasks.md, design_notes.md, development_log.md を読んで現状を要約してください。
まだコード変更はしないでください。
その後、次に着手すべき作業を3つ以内で提案してください。
```

ビルド確認まで進める場合:

```text
docs/obsidian の引き継ぎメモを読んだうえで、GPSIngest_2.sln のビルド可否を確認してください。
失敗した場合は、コード変更前に原因を分類して報告してください。
生成物、zip、旧フォルダ由来のファイルは勝手に削除しないでください。
```

## 作業前に確認する注意点

- `GPSIngest_2` はGit初期化済み。
- `.gitignore` で `.vs/`, `bin/`, `obj/`, `publish/`, `cdk.out/`, zip, `*.user` を除外している。
- GitHub remoteは `origin https://github.com/olive08oil/GpsIngest_2.git`。
- `GPSIngest_1` は `GPSIngest_1_backup_20260524.zip` に退避済みで、元フォルダは削除済み。
- 直近の中断処理時点で、上記バックアップzipは存在確認済み。
- 旧 `GPSIngest` はまだ存在するが、現行作業対象は `GPSIngest_2` とする。
- `rg --files` では `bin/`, `obj/`, `publish/`, zipが混ざるため、検索時は除外条件を付ける。
- 例: `rg --files -g '!bin/**' -g '!obj/**' -g '!publish/**' -g '!.vs/**'`
- `GpsIngestClinetForm` は誤字に見えるが既存プロジェクト名なので、未確認のままリネームしない。
- AWS CDKの `RemovalPolicy`, API Key, Secrets ARN, Lambda asset path は実環境に影響するため慎重に扱う。
- 日本語コメントに文字化けが見えるファイルがある。文字コード変換や一括整形は慎重に行う。
