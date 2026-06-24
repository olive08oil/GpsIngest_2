# Next Tasks

更新日: 2026-06-25

## 優先順位つきの次タスク

1. `dotnet restore GPSIngest_2.sln -v:diag` または個別ビルドでRestore失敗原因を切り分ける。
2. `GPSIngest` 配下の重複 `.cs`、空ファイル、コピー残骸を確認する。
3. `GPSIngest` 単体ビルド結果を記録し、必要なら不要ファイルの削除またはプロジェクト除外を行う。

## Codexに次回依頼する指示文

```text
GPSIngestプロジェクトの再開処理をお願いします。
ルート docs/obsidian と GPSIngest/docs/obsidian を読み、まだコード変更せず、Restore/ビルド失敗の切り分け手順を3つ以内で提案してください。
```

## 作業前に確認する注意点

- 生成物、zip、`.vs/`, `bin/`, `obj/`, `publish/` はGitに入れない。
- 不要そうなファイルでも、ビルド対象か確認するまで削除しない。
- Codex以外で変更した差分があれば `agent_review_queue.md` に記録する。
