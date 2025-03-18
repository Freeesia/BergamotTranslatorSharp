#ifndef BERGAMOT_TRANSLATOR_DYNAMIC_H
#define BERGAMOT_TRANSLATOR_DYNAMIC_H

#include <string>
#include <vector>
#include <memory>

// DLLエクスポート/インポートマクロの定義
#if defined(_WIN32) || defined(__CYGWIN__)
  #ifdef BERGAMOT_TRANSLATOR_DYNAMIC_EXPORTS
    #define BERGAMOT_API __declspec(dllexport)
  #else
    #define BERGAMOT_API __declspec(dllimport)
  #endif
#else
  #define BERGAMOT_API __attribute__((visibility("default")))
#endif

namespace bergamot {

/**
 * @brief 翻訳設定クラス
 */
struct BERGAMOT_API TranslationOptions {
    bool batchTranslation = false;
    int maxLengthBreak = 128;
    int beamSize = 1;
};

/**
 * @brief 翻訳結果クラス
 */
struct BERGAMOT_API TranslationResult {
    std::string text;
    float quality = 0.0f;
    float time = 0.0f;
};

/**
 * @brief Bergamot翻訳エンジンの動的ライブラリインターフェース
 */
class BERGAMOT_API BergamotTranslatorDynamic {
public:
    /**
     * @brief コンストラクタ
     */
    BergamotTranslatorDynamic();
    
    /**
     * @brief デストラクタ
     */
    ~BergamotTranslatorDynamic();
    
    /**
     * @brief 翻訳モデルを初期化
     * @param modelPath モデルファイルのパス
     * @return 初期化成功の場合true
     */
    bool Initialize(const std::string& modelPath);
    
    /**
     * @brief テキストを翻訳
     * @param text 翻訳するテキスト
     * @param options 翻訳オプション
     * @return 翻訳結果
     */
    TranslationResult Translate(const std::string& text, const TranslationOptions& options = TranslationOptions());
    
    /**
     * @brief 複数テキストを一括翻訳
     * @param texts 翻訳するテキスト配列
     * @param options 翻訳オプション
     * @return 翻訳結果配列
     */
    std::vector<TranslationResult> TranslateBatch(const std::vector<std::string>& texts, 
                                                  const TranslationOptions& options = TranslationOptions());
    
    /**
     * @brief 利用可能な言語ペアの取得
     * @return 言語ペア（例："en-ja"）の配列
     */
    std::vector<std::string> GetAvailableLanguagePairs() const;
    
    /**
     * @brief 現在の言語ペアを設定
     * @param from ソース言語コード（例："en"）
     * @param to ターゲット言語コード（例："ja"）
     * @return 設定成功の場合true
     */
    bool SetLanguagePair(const std::string& from, const std::string& to);

private:
    // Pimplイディオムで実装の詳細を隠蔽
    class Impl;
    std::unique_ptr<Impl> pImpl;
    
    // コピー禁止
    BergamotTranslatorDynamic(const BergamotTranslatorDynamic&) = delete;
    BergamotTranslatorDynamic& operator=(const BergamotTranslatorDynamic&) = delete;
};

} // namespace bergamot

#endif // BERGAMOT_TRANSLATOR_DYNAMIC_H
