# ND Parameter Compressor

VRChat アバター用のパラメータ圧縮ツール（Based on Laura's Param Compressor）

## 概要

ND Parameter Compressor は、VRChat アバターの Expression Parameters のメモリ使用量を最適化するための NDMF プラグインです。複数のパラメータを効率的にバッチ処理することで、メモリコストを削減します。
## インストール

1. [NDMF](https://ndmf.nadena.dev/) や [Modular Avatar](https://modular-avatar.nadena.dev/) 等をインストールしていない場合は、[`https://vpm.nadena.dev/vpm.json`](vcc://vpm/addRepo?url=https://vpm.nadena.dev/vpm.json) を追加してください。
2. [`https://vpm.okitsu.net/index.json`](vcc://vpm/addRepo?url=https://vpm.okitsu.net/index.json) を追加していない場合は追加してください。
3. Parameter Compressor パッケージを追加してください。

## 使用方法

1. アバターの GameObject に `ND Parameter Compressor` コンポーネントを追加
2. **「パラメータを検出」** ボタンをクリック
3. 圧縮したいパラメータを選択
4. ビルド時に自動的にパラメータが圧縮されます

### 設定項目

#### 圧縮設定

- **Sizing Mode**
  - `Auto`: 最適なステートサイズを自動計算（推奨）
  - `Manual`: 手動でステートサイズを調整

- **Max Sync Steps** (Auto モード時): 同期ステップの最大数（2-32）
- **Numbers Per State** (Manual モード時): ステートあたりの数値パラメータ数（1-8）
- **Bools Per State** (Manual モード時): ステートあたりの Bool パラメータ数（1-64）

#### 自動フィルター

- **Exclude Bools**: Bool 型パラメータを自動除外
- **Exclude Ints**: Int 型パラメータを自動除外
- **Exclude Floats**: Float 型パラメータを自動除外

#### 詳細フィルター

- **Prefix フィルター**: 指定したプレフィックスで始まるパラメータを除外
- **Suffix フィルター**: 指定したサフィックスで終わるパラメータを除外

### 自動除外されるパラメータ

以下のパラメータは自動的に除外されます:

- [Face Tracking v4 パラメータ](https://docs.vrcft.io/docs/v4.0/category/parameters)
- [Face Tracking v5 パラメータ](https://docs.vrcft.io/docs/category/parameters)

## クレジット

- **Original**: [Laura's Param Compressor](https://github.com/LauraRozier/LauraParamCompressor)

## 変更履歴

詳細な変更履歴については、Releases ページを参照してください。

