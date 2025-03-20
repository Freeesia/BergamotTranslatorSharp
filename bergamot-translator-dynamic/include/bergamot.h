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
   * @brief Bergamot翻訳エンジンを初期化
   * @param configPath 設定ファイルのパス
   * @return 初期化されたトランスレーターのポインタ、失敗した場合はNULL
   */
  BERGAMOT_API void *translator_initialize(const char *configPath);

  /**
   * @brief テキストを翻訳
   * @param translator 初期化済みトランスレーターのポインタ
   * @param text 翻訳するテキスト
   * @return 翻訳結果（テキストは呼び出し側が解放する必要がある）
   */
  BERGAMOT_API char *translator_translate(void *translator, const char *text);

  /**
   * @brief トランスレーターを解放
   * @param translator 解放するトランスレーターのポインタ
   */
  BERGAMOT_API void translator_free(void *translator);

#ifdef __cplusplus
}
#endif

#endif // BERGAMOT_TRANSLATOR_DYNAMIC_H
