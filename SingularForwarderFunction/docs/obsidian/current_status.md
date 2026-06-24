# Current Status

更新日: 2026-06-25

## プロジェクト

- 名前: `SingularForwarderFunction`
- 種別: AWS Lambda
- csproj: `SingularForwarderFunction/SingularForwarderFunction.csproj`
- Gitルート: `C:\Users\olive\SynologyDrive\code\Visual Studio 2022\Projects\GPSIngest_2`
- ブランチ: `main`

## 現在の作業内容

- DynamoDB StreamsからSingularへ転送するLambdaの引き継ぎメモを整備している。

## 完了済みの作業

- ソリューションに含まれるプロジェクトであることを確認済み。
- `DeviceLatest` StreamsからSingularへ転送する役割として記録済み。

## 未完了の作業

- 単体ビルド可否の確認。
- 転送先設定、APIキー、失敗時処理の確認。

## 現在の問題点

- 旧 `GPSIngest` 側のビルドでは `odoKm` 未定義が出ていた記録がある。現行 `GPSIngest_2` で同じ問題が残るか未確認。

## 次回最初に確認すべきファイル

- `SingularForwarderFunction/Function.cs`
- `SingularForwarderFunction/SingularForwarderFunction.csproj`
- `Infra/src/Infra/InfraStack.cs`
