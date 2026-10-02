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
- 用語辞書による指定訳の適用
- JSON・YAML・TOML・JSON5・INI・CBOR・MessagePack の文字列値の翻訳
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

- .NET 8.0 以上（ライブラリのターゲットフレームワークは .NET 8.0 と .NET 10.0）
- Windows x86、Windows x64、Windows ARM64、Linux x64、macOS x64、macOS ARM64

### ネイティブライブラリをソースからビルドする

Windows x86 ビルドでは vcpkg、Visual Studio の Win32 ツールチェーン、OpenBLAS を使用します。

```bat
cmake -S . -B out\build\windows-x86-release -A Win32 -DBUILD_ARCH=core2 -DUSE_STATIC_LIBS=ON -DUSE_MKL=OFF -DGIT_SUBMODULE=OFF -DVCPKG_TARGET_TRIPLET=x86-windows-static -DCMAKE_TOOLCHAIN_FILE=C:\vcpkg\scripts\buildsystems\vcpkg.cmake
cmake --build out\build\windows-x86-release --config Release --target bergamot_translator_dynamic
cmake --install out\build\windows-x86-release --prefix libs --component bergamot_translator_dynamic
```

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

従来の `mozilla/firefox-translations-models` リポジトリは保守を終了しています。現在、モデルは Mozilla の[公式モデルレジストリ](https://storage.googleapis.com/moz-fx-translations-data--303e-prod-translations-data/db/models.json)で公開され、モデルファイルはレジストリ内の `baseUrl` 配下に配置されています。

次の例では `curl`、`jq`、`gzip` を使用します。最初にレジストリをダウンロードし、利用できる翻訳方向を確認します。

```bash
REGISTRY_URL=https://storage.googleapis.com/moz-fx-translations-data--303e-prod-translations-data/db/models.json
curl --fail --location --output models.json "$REGISTRY_URL"
jq -r '.models | keys[]' models.json
```

翻訳方向を選び、そのモデル候補を確認します。レジストリの翻訳方向には、ドイツ語から英語の `de-en`、英語から日本語の `en-ja` のようにハイフンが入ります。

```bash
DIRECTION=en-ja
jq --arg direction "$DIRECTION" \
  '.models[$direction] | to_entries | map({
    index: .key,
    architecture: .value.architecture,
    releaseStatus: .value.releaseStatus,
    files: .value.files
  })' models.json
```

この出力から選んだ候補のインデックスを `MODEL_INDEX` に指定します。次の例では、現在の `en-ja` の `Release` 候補であるインデックス `1` を選び、`models/enja` に保存します。別の翻訳方向や候補を使う場合は、`DIRECTION`、`MODEL_INDEX`、`MODEL_DIR` をまとめて変更してください。

```bash
DIRECTION=en-ja
MODEL_INDEX=1
MODEL_DIR=models/enja
set -euo pipefail
BASE_URL=$(jq -r '.baseUrl' models.json)

mkdir -p "$MODEL_DIR"
jq -r --arg direction "$DIRECTION" --argjson index "$MODEL_INDEX" '
  .models[$direction][$index].files
  | [
      .model.path,
      .vocab.path,
      .srcVocab.path,
      .trgVocab.path,
      .lexicalShortlist.path
    ]
  | .[]
  | select(type == "string")
' models.json |
while IFS= read -r path; do
  curl --fail --location \
    --output "$MODEL_DIR/$(basename "$path")" \
    "$BASE_URL/$path"
done

gzip --decompress "$MODEL_DIR"/*.gz
```

この手順で、モデル、単一の語彙または翻訳元・翻訳先に分かれた語彙、および lexical shortlist を取得します。展開後、`MODEL_DIR` にコンフィグファイルを作成し、`ls "$MODEL_DIR"` で表示される展開済みファイルの正確な名前を使用してください。

### 2. コンフィグファイルを作成する

モデルファイルと同じディレクトリに `config.yml` または `config.txt` を作成します。

英語から日本語へ翻訳するモデルの例:

```yml
relative-paths: true
models:
- model.enja.intgemm.alphas.bin
vocabs:
- srcvocab.enja.spm
- trgvocab.enja.spm
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

var translatedBatch = service.Translate(["Hello, world!", "How are you?"]);
foreach (var translatedText in translatedBatch)
{
    Console.WriteLine(translatedText);
}
```

`Translate(IEnumerable<string>)` は各テキストを個別の入力としてまとめて翻訳し、入力と同じ順序で結果を返します。

HTML マークアップを保持したままテキスト部分を翻訳するには、第 2 引数に `true` を指定します。

```cs
var translatedHtml = service.Translate("<p>Hello, <strong>world</strong>!</p>", html: true);

var translatedHtmlBatch = service.Translate(
    ["<p>Hello, <strong>world</strong>!</p>", "<p>How are you?</p>"],
    html: true);
```

プレーンテキスト内の用語を指定した訳語で出力するには、辞書を渡します。単体翻訳とバッチ翻訳の両方に対応し、2 モデルのピボット翻訳でも最終結果に適用されます。

```cs
var dictionary = new Dictionary<string, string>
{
    ["Mana Reactor"] = "マナリアクター",
    ["Shinra"] = "神羅",
};

var translatedWithDictionary = service.Translate(
    "The Mana Reactor was built by Shinra.", dictionary);
var translatedBatchWithDictionary = service.Translate(
    ["The Mana Reactor was built by Shinra.", "Shinra owns it."], dictionary);
```

辞書キーは大文字小文字を区別する完全な文字列として検索し、複数回の出現をすべて置換します。キーが重なる場合は長いものを優先します。空文字のキーは指定できません。辞書付き翻訳の入力はプレーンテキストで、HTML 入力との併用には対応していません。

JSON・YAML・TOML の値を翻訳する場合は、必要な形式のパッケージを追加します。各パッケージは本体だけに依存し、YAML と TOML は JSON パッケージに依存しません。

```bash
dotnet add package BergamotTranslatorSharp.Json
dotnet add package BergamotTranslatorSharp.Yaml
dotnet add package BergamotTranslatorSharp.Toml
```

```cs
using BergamotTranslatorSharp.Json;
using BergamotTranslatorSharp.Yaml;
using BergamotTranslatorSharp.Toml;

var json = service.TranslateJson("""{"title":"Hello, world!","count":1}""", dictionary);
var yaml = service.TranslateYaml("title: Hello, world!\ncount: 1\n", dictionary);
var toml = service.TranslateToml("title = 'Hello, world!'\ncount = 1\n", dictionary);
```

辞書は省略できます。キー、数値、真偽値、null、空文字列、空白だけの文字列は翻訳しません。各文字列値はプレーンテキストとして個別に翻訳します。独自のデータ構造を扱う場合は、本体の `ITranslatableDocument<T>` を実装し、`service.Translate(document, dictionary)` を呼び出せます。

JSON5・INI・CBOR・MessagePack を扱う場合は `BergamotTranslatorSharp.REDox` を追加してください（.NET 10 専用）。依存する `CAPCOM.REDox`、`CAPCOM.REDox.Cbor`、`CAPCOM.REDox.Ini`、`CAPCOM.REDox.MessagePack` は Apache-2.0 ライセンスです（[REDox](https://github.com/CAPCOM-TD-OSS/REDox)）。

```bash
dotnet add package BergamotTranslatorSharp.REDox
```

```cs
using BergamotTranslatorSharp.REDox;

var json5 = service.TranslateJson5("{ title: 'Hello, world!' }", dictionary);
var ini = service.TranslateIni("title=Hello, world!", dictionary);
var translatedCbor = service.TranslateCbor(cborBytes, dictionary);
var translatedMessagePack = service.TranslateMessagePack(messagePackBytes, dictionary);
```

JSON5 のコメントや trivia を保持し、INI も REDox が扱う trivia を保持します。REDox のドキュメントハンドラーは、空白でない文字列値を翻訳し、map のキーや文字列以外の値は翻訳しません。

コンフィグファイルパスを 1 個渡した場合、`BlockingService` はそのモデルを直接使用します。
コンフィグファイルパスを 2 個渡した場合、ネイティブサービスはそれらをピボット翻訳チェーンとして使用します。

### 4. .NET ツールを実行する

`BergamotTranslatorSharp.Tool` はコマンド名 `bergamot` の .NET 10 ツールです。ツールのパッケージ公開後は、.NET 10 SDK の `dnx` で取得して実行できます。

```bash
dnx BergamotTranslatorSharp.Tool -- en ja "Hello, world!"
```

以降の例で `bergamot` コマンドを直接使う場合は、ツールをグローバルにインストールします。

```bash
dotnet tool install --global BergamotTranslatorSharp.Tool
```

このリポジトリから直接実行する場合:

```bash
dotnet run --project BergamotTranslatorSharp.Tool -- en ja "Hello, world!"
```

引数は翻訳元言語、翻訳先言語、テキストの順です。Mozilla のレジストリでは `Release` モデルを優先してダウンロードし、ユーザーのアプリケーションデータディレクトリ内の `BergamotTranslatorSharp/Tool` に保存して再利用します。直接翻訳できるモデルがない場合は英語経由を試します。HTML マークアップを保持するには `--html` を付けます。

```bash
bergamot en ja --html "<p>Hello, <strong>world</strong>!</p>"
```

辞書を使う場合は、原語と指定訳の2列からなる UTF-8 CSV を用意し、`--dictionary` にパスを渡します。先頭の `source,target` ヘッダーは省略できます。用語にカンマ、改行、二重引用符を含める場合は CSV の引用符で囲み、引用符自体は `""` と記載します。

```csv
source,target
Mana Reactor,マナリアクター
Shinra,神羅
```

```bash
bergamot en ja --dictionary terms.csv "The Mana Reactor was built by Shinra."
```

辞書指定はプレーンテキスト専用です。`--html` と同時には指定できません。重複した原語、空の原語、2列以外の行はエラーになります。

使い方は `bergamot --help` で確認できます。モデルファイルがない場合や検証に失敗した場合のみ再取得します。レジストリ情報は 1 日キャッシュし、接続できないときはキャッシュを使用します。

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
