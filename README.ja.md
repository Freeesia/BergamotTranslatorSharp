# BergamotTranslatorSharp

[![App Build](https://github.com/Freeesia/BergamotTranslatorSharp/actions/workflows/package.yml/badge.svg)](https://github.com/Freeesia/BergamotTranslatorSharp/actions/workflows/package.yml)
[![GitHub Release](https://img.shields.io/github/v/release/Freeesia/BergamotTranslatorSharp)](https://github.com/Freeesia/BergamotTranslatorSharp/releases/latest)
[![NuGet Version](https://img.shields.io/nuget/v/BergamotTranslatorSharp)](https://www.nuget.org/packages/BergamotTranslatorSharp)
[![NuGet Downloads](https://img.shields.io/nuget/dt/BergamotTranslatorSharp)](https://www.nuget.org/packages/BergamotTranslatorSharp)

BergamotTranslatorSharp は Bergamot Translator の C# ラッパーです。オフラインで動作する機械翻訳エンジンを .NET アプリケーションから利用できます。

| [EN](README.md) | [JA](README.ja.md) |

## 概要

[Bergamot Translator](https://github.com/browsermt/bergamot-translator) は、オフラインで動作する翻訳エンジンです。公式ウェブサイトは [https://browser.mt/](https://browser.mt/) です。このライブラリはその機能を C# から利用できるようにラップしたものです。

## 特徴

- オフライン翻訳
- 複数言語対応
- 高速な処理
- HTML マークアップの保持
- .NET アプリケーションへの組み込み

## インストール方法

### NuGet からインストール

```powershell
Install-Package BergamotTranslatorSharp
```

または:

```bash
dotnet add package BergamotTranslatorSharp
```

### 必要条件

- .NET 8.0 以上
- Windows x64、Windows ARM64、Linux x64、macOS ARM64

### ネイティブライブラリをソースからビルドする

Windows ARM64 ビルドでは vcpkg と Visual Studio の ARM64 clang-cl ツールチェーンを使用します。
ARM64 Developer Prompt で `VCPKG_ROOT` を設定し、次を実行してください。

```bat
set VCPKG_ROOT=C:\vcpkg
cmake --preset windows-arm64-clangcl-release
cmake --build --preset windows-arm64-clangcl-release
cmake --install out\build\windows-arm64-clangcl-release --prefix libs --component bergamot_translator_dynamic
```

## 使用方法

### 1. モデルをダウンロードする

[Firefox Translations models](https://github.com/mozilla/firefox-translations-models) からモデルをダウンロードして展開します。

```bash
git clone --depth 1 --branch main --single-branch https://github.com/mozilla/firefox-translations-models/
mkdir models
cp -rf firefox-translations-models/registry.json models
cp -rf firefox-translations-models/models/prod/* models
cp -rf firefox-translations-models/models/dev/* models
gunzip models/*/*
```

展開後、使用したい翻訳方向のモデルディレクトリを選びます。たとえば、ドイツ語から英語に翻訳する場合は `deen`、英語から日本語に翻訳する場合は `enja` のモデルディレクトリを使用します。

### 2. コンフィグファイルを作成する

モデルファイルと同じディレクトリに `config.yml` または `config.txt` を作成します。

英語から日本語へ翻訳するモデルの例:

```yml
relative-paths: true
models:
- model.enja.intgemm.alphas.bin
vocabs:
- vocab.enja.spm
- vocab.enja.spm
shortlist:
- lex.50.50.enja.s2t.bin
- false
beam-size: 1
normalize: 1.0
word-penalty: 0
max-length-break: 128
mini-batch-words: 1024
workspace: 128
max-length-factor: 2.0
skip-cost: true
cpu-threads: 0
quiet: true
quiet-translation: true
gemm-precision: int8shiftAlphaAll
```

注意点:

- `models`、`vocabs`、`shortlist` に指定するファイル名は、選択したモデルディレクトリ内の実ファイル名と一致させてください。
- モデルファイル名に `alphas` が含まれる場合は `gemm-precision: int8shiftAlphaAll` を指定してください。
- それ以外の場合は `gemm-precision: int8shiftAll` を指定してください。
- `relative-paths: true` を使う場合は、コンフィグファイルとモデルファイルを同じディレクトリに置くか、実際の配置に合わせてパスを修正してください。

### 3. C# から翻訳する

現在の API では `BlockingService` を使用します。コンストラクタには 1 個または 2 個のコンフィグファイルパスを渡します。

```cs
using BergamotTranslatorSharp;

var configPath = Path.Combine(
    AppDomain.CurrentDomain.BaseDirectory,
    "models",
    "enja",
    "config.txt");

using var service = new BlockingService(configPath);

var translated = service.Translate("Hello, world!");

Console.WriteLine(translated);
```

HTML マークアップを保持したままテキスト部分を翻訳するには、第 2 引数に `true` を指定します。

```cs
var translatedHtml = service.Translate("<p>Hello, <strong>world</strong>!</p>", html: true);
```

コンフィグファイルパスを 1 個渡した場合、`BlockingService` はそのモデルを直接使用します。
コンフィグファイルパスを 2 個渡した場合、ネイティブサービスはそれらをピボット翻訳チェーンとして使用します。

### 4. ManagedSample を実行する

`ManagedSample` は次の引数形式です。

```text
ManagedSample [--html] <config-paths>[..] <text>
```

最後の引数以外はすべてコンフィグファイルパスとして扱われます。最後の引数は翻訳元テキストとして扱われます。

実行例:

```bash
dotnet run --project ManagedSample -- ./models/enja/config.txt "Hello, world!"
```

HTML 入力では、設定ファイルの前に `--html` を追加します。

```bash
dotnet run --project ManagedSample -- --html ./models/enja/config.txt "<p>Hello, <strong>world</strong>!</p>"
```

ピボット翻訳を行う場合は、翻訳テキストの前に 2 個のコンフィグファイルを渡します。

```bash
dotnet run --project ManagedSample -- ./models/source-pivot/config.txt ./models/pivot-target/config.txt "Hello, world!"
```

## トラブルシューティング

### `Failed to create translator instance` が発生する場合

多くの場合、モデルのロードに失敗しています。次を確認してください。

- コンフィグファイルのパスが正しいか。
- モデル、語彙、ショートリストの各ファイルが存在するか。
- コンフィグファイル内のファイル名が実ファイル名と一致しているか。
- `gemm-precision` がモデルの種類と一致しているか。
- 使用している環境でネイティブ `bergamot` ライブラリをロードできるか。

### 翻訳されない、または想定外の出力になる場合

モデルの翻訳方向と入力言語が一致しているか確認してください。たとえば、`deen` モデルはドイツ語から英語への入力を想定し、`enja` モデルは英語から日本語への入力を想定します。

## ライセンス

このプロジェクトは [MPL-2.0](https://www.mozilla.org/en-US/MPL/2.0/) ライセンスの下で公開されています。

## 貢献

バグ報告や機能リクエストは GitHub の Issue Tracker にお願いします。Pull Request も歓迎します。

## 謝辞

このプロジェクトは [browsermt/bergamot-translator](https://github.com/browsermt/bergamot-translator) を基にしています。
