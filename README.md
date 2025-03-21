# BergamotTranslatorSharp

BergamotTranslatorSharp is a C# wrapper for Bergamot Translator. It allows easy integration of an offline machine translation engine into .NET applications.

## Overview

[Bergamot Translator](https://github.com/browsermt/bergamot-translator) is an offline translation engine. The official website is [https://browser.mt/](https://browser.mt/). This library wraps its functionality for use from C#.

## Features

- Offline translation capability
- Multi-language support
- Fast processing
- Easy integration with .NET applications

## Installation

### Installation from NuGet

```
Install-Package BergamotTranslatorSharp
```

Or

```
dotnet add package BergamotTranslatorSharp
```

### Requirements

- .NET 6.0 or later
- .NET Standard 2.1 or later
- Windows x64

## Usage

### Downloading Models and Creating Configuration Files

1. Download and extract models from the [Firefox Translations models](https://github.com/mozilla/firefox-translations-models) official website.
    ```bash
    cd test_page
    git clone --depth 1 --branch main --single-branch https://github.com/mozilla/firefox-translations-models/
    mkdir models
    cp -rf firefox-translations-models/registry.json models
    cp -rf firefox-translations-models/models/prod/* models
    cp -rf firefox-translations-models/models/dev/* models
    gunzip models/*/*
    ```
2. Create a configuration file in the same location as the binary file.
    ```yml
    relative-paths: true
    models:
    - model.enja.intgemm.alphas.bin // model path
    vocabs:
    - vocab.enja.spm // source vocabulary path
    - vocab.enja.spm // target vocabulary path
    shortlist:
    - lex.50.50.enja.s2t.bin // shortlist path
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
    gemm-precision: int8shiftAlphaAll // specify 'int8shiftAlphaAll' if the model name contains 'alphas', otherwise specify 'int8shiftAll'
    ```

### Initializing the Translation Service and Translating

```cs
using BergamotTranslatorSharp;

// Create an instance of BergamotTranslator
using var translator = new BergamotTranslator(configPath);

// Execute translation
string sourceText = "Hello, world!";
string translatedText = translator.Translate(sourceText);

Console.WriteLine(translatedText); // こんにちは、世界！
```

## License

This project is released under the [MPL-2.0](https://www.mozilla.org/en-US/MPL/2.0/) license.

## Contribution

Please report bugs and feature requests to the GitHub Issue Tracker. Pull requests are also welcome.

## Acknowledgments

This project is based on [browsermt/bergamot-translator](https://github.com/browsermt/bergamot-translator).
