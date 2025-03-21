# BergamotTranslatorSharp

BergamotTranslatorSharp は Bergamot Translator の C# ラッパーです。オフラインで動作する機械翻訳エンジンを .NET アプリケーションから簡単に利用できるようにします。

## 概要

[Bergamot Translator](https://github.com/browsermt/bergamot-translator) は、オフラインで動作する翻訳エンジンです。公式ウェブサイトは [https://browser.mt/](https://browser.mt/) です。このライブラリはその機能を C# から利用できるようにラップしたものです。

## 特徴

- オフラインでの翻訳が可能
- 多言語対応
- 高速な処理
- .NET アプリケーションとの簡単な統合

## インストール方法

### NuGet からのインストール

```
Install-Package BergamotTranslatorSharp
```

または

```
dotnet add package BergamotTranslatorSharp
```

### 必要条件

- .NET 6.0 以上
- .NET Standard 2.1 以上
- Windows x64

## 使用方法

### モデルのダウンロードとコンフィグファイルの作成

1. [Firefox Translations models](https://github.com/mozilla/firefox-translations-models) の公式ウェブサイトからモデルをダウンロードし、解凍します。
    ```bash
    cd test_page
    git clone --depth 1 --branch main --single-branch https://github.com/mozilla/firefox-translations-models/
    mkdir models
    cp -rf firefox-translations-models/registry.json models
    cp -rf firefox-translations-models/models/prod/* models
    cp -rf firefox-translations-models/models/dev/* models
    gunzip models/*/*
    ```
2. バイナリファイルと同じ位置にコンフィグファイルを作成します。
    ```yml
    relative-paths: true
    models:
    - model.enja.intgemm.alphas.bin // モデルのパス
    vocabs:
    - vocab.enja.spm // ソースボキャブラリのパス
    - vocab.enja.spm // ターゲットボキャブラリのパス
    shortlist:
    - lex.50.50.enja.s2t.bin // ショートリストのパス
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
    gemm-precision: int8shiftAlphaAll // モデル名に`alphas`が含まれる場合は`int8shiftAlphaAll`、それ以外は`int8shiftAll`を指定
    ```

### 翻訳サービスの初期化と翻訳

```cs
using BergamotTranslatorSharp;

// BergamotTranslator のインスタンスを生成
using var translator = new BergamotTranslator(configPath);

// 翻訳の実行
string sourceText = "Hello, world!";
string translatedText = translator.Translate(sourceText);

Console.WriteLine(translatedText); // こんにちは、世界！
```

## ライセンス

このプロジェクトは [MPL-2.0](https://www.mozilla.org/en-US/MPL/2.0/) ライセンスの下で公開されています。

## 貢献

バグ報告や機能リクエストは GitHub の Issue トラッカーにお願いします。プルリクエストも歓迎します。

## 謝辞

このプロジェクトは [browsermt/bergamot-translator](https://github.com/browsermt/bergamot-translator) を基にしています。