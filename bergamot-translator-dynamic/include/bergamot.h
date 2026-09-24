#ifndef BERGAMOT_TRANSLATOR_DYNAMIC_H
#define BERGAMOT_TRANSLATOR_DYNAMIC_H

#include <stddef.h>

#ifndef __cplusplus
#include <stdbool.h>
#endif

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
   * @param configPaths 設定ファイルパスの配列
   * @param numPaths 配列内の設定ファイル数
   * @return 初期化されたトランスレーターのポインタ、失敗した場合はNULL
   */
  BERGAMOT_API void *translator_initialize(const char **configPaths, int numPaths);

  /**
   * @brief テキストを翻訳
   * @param translator 初期化済みトランスレーターのポインタ
   * @param text 翻訳するテキスト
   * @param html HTMLマークアップを保持するかどうか
   * @return 翻訳結果（テキストは呼び出し側が解放する必要がある）
   */
  BERGAMOT_API char *translator_translate(void *translator, const char *text, bool html);

  /**
   * @brief 複数のテキストをまとめて翻訳
   * @param translator 初期化済みトランスレーターのポインタ
   * @param texts 翻訳するテキストの配列
   * @param count 配列内のテキスト数（1以上）
   * @return 入力と同じ順序の翻訳結果配列。失敗した場合はNULL
   * @note 結果配列と各テキストは translator_free_translations で解放する
   */
  BERGAMOT_API char **translator_translate_multiple(void *translator, const char **texts, size_t count);

  /**
   * @brief translator_translate_multiple が返した翻訳結果と配列を解放
   * @param translations 解放する翻訳結果配列
   * @param count 配列内のテキスト数
   */
  BERGAMOT_API void translator_free_translations(char **translations, size_t count);

  /**
   * @brief トランスレーターを解放
   * @param translator 解放するトランスレーターのポインタ
   */
  BERGAMOT_API void translator_free(void *translator);

#ifdef __cplusplus
}
#endif

#endif // BERGAMOT_TRANSLATOR_DYNAMIC_H
