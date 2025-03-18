#ifndef BERGAMOT_TRANSLATOR_DYNAMIC_H
#define BERGAMOT_TRANSLATOR_DYNAMIC_H

#ifdef __cplusplus
extern "C"
{
#endif

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

  /**
   * @brief 翻訳設定構造体
   */
  typedef struct
  {
    int beamSize;
    int maxLengthBreak;
    int cacheSize;
    int normalizeScore;
  } BergamotTranslationOptions;

  /**
   * @brief Bergamot翻訳エンジンを初期化
   * @param modelPath モデルファイルのパス
   * @param configPath 設定ファイルのパス
   * @param from ソース言語コード（例："en"）
   * @param to ターゲット言語コード（例："ja"）
   * @param options 翻訳オプション
   * @return 初期化されたトランスレーターのポインタ、失敗した場合はNULL
   */
  BERGAMOT_API void *BergamotTranslator_Initialize(
      const char *modelPath,
      const char *shortlistPath,
      const char *vocabPath,
      const char *configYaml,
      const BergamotTranslationOptions *options);

  /**
   * @brief テキストを翻訳
   * @param translator 初期化済みトランスレーターのポインタ
   * @param text 翻訳するテキスト
   * @return 翻訳結果（テキストは呼び出し側が解放する必要がある）
   */
  BERGAMOT_API char *BergamotTranslator_Translate(
      void *translator,
      const char *text);

  /**
   * @brief トランスレーターを解放
   * @param translator 解放するトランスレーターのポインタ
   */
  BERGAMOT_API void BergamotTranslator_Free(void *translator);

#ifdef __cplusplus
}
#endif

#endif // BERGAMOT_TRANSLATOR_DYNAMIC_H
