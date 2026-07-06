#include "bergamot.h"
#include <cstdlib>
#include <iostream>
#include <string>
#ifdef _WIN32
#include <combaseapi.h>
#endif

int main(int argc, char *argv[]) {
  if (argc < 3) {
    std::cout << "Usage: " << argv[0] << " <config_path> <text_to_translate>" << std::endl;
    return 1;
  }

  // モデル設定ファイルのパス
  const char* configPath = argv[1];

  // configPathのアドレスを渡すためのポインタ
  const char* configPaths[] = { configPath };

  // 必要な引数をtranslator_initializeに渡す
  // 例: translator_initialize(const char** configPaths, int numPaths)
  void* translator = translator_initialize(configPaths, 1);
  if (!translator) {
    std::cout << "Failed to initialize translator";
    return 2;
  }

  // 翻訳するテキストを取得
  std::string input = argv[2];

  // 翻訳を実行
  char* translatedText = translator_translate(translator, input.c_str(), false);
  if (translatedText) {
    // 翻訳結果を出力
    std::cout << translatedText;
    
    // translator_translate と同じアロケーターでメモリを解放
#ifdef _WIN32
    CoTaskMemFree(translatedText);
#else
    std::free(translatedText);
#endif
  } else {
    std::cout << "Translation failed";
    translator_free(translator);
    return 3;
  }

  // 翻訳エンジンの解放
  translator_free(translator);

  return 0;
}
