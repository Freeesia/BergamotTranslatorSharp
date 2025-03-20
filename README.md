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

```csharp
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