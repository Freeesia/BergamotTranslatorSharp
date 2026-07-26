# BergamotTranslatorSharp

[![App Build](https://github.com/Freeesia/BergamotTranslatorSharp/actions/workflows/package.yml/badge.svg)](https://github.com/Freeesia/BergamotTranslatorSharp/actions/workflows/package.yml)
[![GitHub Release](https://img.shields.io/github/v/release/Freeesia/BergamotTranslatorSharp)](https://github.com/Freeesia/BergamotTranslatorSharp/releases/latest)
[![NuGet Version](https://img.shields.io/nuget/v/BergamotTranslatorSharp)](https://www.nuget.org/packages/BergamotTranslatorSharp)
[![NuGet Downloads](https://img.shields.io/nuget/dt/BergamotTranslatorSharp)](https://www.nuget.org/packages/BergamotTranslatorSharp)

BergamotTranslatorSharp is a C# wrapper for Bergamot Translator. It allows .NET applications to use an offline machine translation engine.

| [EN](README.md) | [JA](README.ja.md) |

## Overview

[Bergamot Translator](https://github.com/browsermt/bergamot-translator) is an offline translation engine. The official website is [https://browser.mt/](https://browser.mt/). This library wraps its functionality for use from C#.

## Features

- Offline translation capability
- Multi-language support
- Fast processing
- HTML markup preservation
- Easy integration with .NET applications

## Installation

### Install from NuGet

```powershell
Install-Package BergamotTranslatorSharp
```

Or:

```bash
dotnet add package BergamotTranslatorSharp
```

### Requirements

- .NET 8.0 or later
- Windows x86, Windows x64, Windows ARM64, Linux x64, macOS x64, or macOS ARM64

### Build the native library from source

Windows x86 builds use vcpkg, the Visual Studio Win32 toolchain, and OpenBLAS:

```bat
cmake -S . -B out\build\windows-x86-release -A Win32 -DBUILD_ARCH=core2 -DUSE_STATIC_LIBS=ON -DUSE_MKL=OFF -DGIT_SUBMODULE=OFF -DVCPKG_TARGET_TRIPLET=x86-windows-static -DCMAKE_TOOLCHAIN_FILE=C:\vcpkg\scripts\buildsystems\vcpkg.cmake
cmake --build out\build\windows-x86-release --config Release --target bergamot_translator_dynamic
cmake --install out\build\windows-x86-release --prefix libs --component bergamot_translator_dynamic
```

Windows ARM64 builds use vcpkg and Visual Studio's ARM64 clang-cl toolchain.
From an ARM64 developer prompt, set `VCPKG_ROOT` and run:

```bat
set VCPKG_ROOT=C:\vcpkg
cmake --preset windows-arm64-clangcl-release
cmake --build --preset windows-arm64-clangcl-release
cmake --install out\build\windows-arm64-clangcl-release --prefix libs --component bergamot_translator_dynamic
```

## Usage

### 1. Download models

The old `mozilla/firefox-translations-models` repository is no longer maintained. Models are now published through Mozilla's [official model registry](https://storage.googleapis.com/moz-fx-translations-data--303e-prod-translations-data/db/models.json), with the model files hosted under the `baseUrl` in that registry.

The following example requires `curl`, `jq`, and `gzip`. First, download the registry and inspect the available language directions:

```bash
REGISTRY_URL=https://storage.googleapis.com/moz-fx-translations-data--303e-prod-translations-data/db/models.json
curl --fail --location --output models.json "$REGISTRY_URL"
jq -r '.models | keys[]' models.json
```

Choose a direction and inspect its model candidates. Registry directions use a hyphen, such as `de-en` for German to English and `en-ja` for English to Japanese.

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

Set `MODEL_INDEX` to the candidate selected from that output. The example below selects index `1`, the current `Release` candidate for `en-ja`, and stores it in `models/enja`. Change `DIRECTION`, `MODEL_INDEX`, and `MODEL_DIR` together when using another language direction or candidate.

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

This downloads the model, its single vocabulary or separate source and target vocabularies, and the lexical shortlist. After decompression, create the configuration file in `MODEL_DIR` and use the exact decompressed file names shown by `ls "$MODEL_DIR"`.

### 2. Create a configuration file

Create a `config.yml` or `config.txt` file in the same directory as the model files.

Example for an English to Japanese model:

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

Notes:

- The file names in `models`, `vocabs`, and `shortlist` must match the files in the selected model directory.
- If the model file name contains `alphas`, use `gemm-precision: int8shiftAlphaAll`.
- Otherwise, use `gemm-precision: int8shiftAll`.
- When `relative-paths: true` is used, keep the configuration file and the model files together, or update the paths accordingly.

### 3. Translate text from C#

The current API uses `BlockingService`. Pass one or two configuration file paths to the constructor.

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

To translate text content while preserving HTML markup, pass `true` as the second argument:

```cs
var translatedHtml = service.Translate("<p>Hello, <strong>world</strong>!</p>", html: true);
```

If you pass one configuration file path, `BlockingService` uses that model directly.
If you pass two configuration file paths, the native service uses them as a pivot translation chain.

### 4. Run the managed sample

`ManagedSample` follows this argument format:

```text
ManagedSample [--html] <config-paths>[..] <text>
```

All arguments except the last one are treated as configuration file paths. The last argument is treated as the source text.

Example:

```bash
dotnet run --project ManagedSample -- ./models/enja/config.txt "Hello, world!"
```

For HTML input, add `--html` before the configuration paths:

```bash
dotnet run --project ManagedSample -- --html ./models/enja/config.txt "<p>Hello, <strong>world</strong>!</p>"
```

For pivot translation, pass two configuration files before the text:

```bash
dotnet run --project ManagedSample -- ./models/source-pivot/config.txt ./models/pivot-target/config.txt "Hello, world!"
```

## Troubleshooting

### `Failed to create translator instance`

This usually means the model could not be loaded. Check the following:

- The configuration file path is correct.
- The model, vocabulary, and shortlist files exist.
- The file names in the configuration file match the actual files.
- `gemm-precision` matches the model type.
- The native `bergamot` library can be loaded on your platform.

### No translation or unexpected output

Check that the model direction matches the input language. For example, a `deen` model is intended for German to English input, while an `enja` model is intended for English to Japanese input.

## License

This project is released under the [MPL-2.0](https://www.mozilla.org/en-US/MPL/2.0/) license.

## Contribution

Please report bugs and feature requests to the GitHub Issue Tracker. Pull requests are also welcome.

## Acknowledgments

This project is based on [browsermt/bergamot-translator](https://github.com/browsermt/bergamot-translator).
