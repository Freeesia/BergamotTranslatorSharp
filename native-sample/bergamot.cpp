#include "bergamot.h"
#include <iostream>
#include <string>
#include <fstream>
#include <sstream>
#include <combaseapi.h>

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
  char* translatedText = translator_translate(translator, input.c_str());
  if (translatedText) {
    // 翻訳結果を出力
    std::cout << translatedText;
    
    // translatedTextのメモリを解放
    // 注: ここでfree()を使わず、実装に合わせた解放方法を使用する必要があるかもしれません
    CoTaskMemFree(translatedText);  // WindowsのCoTaskMemAllocを使用していると仮定
  } else {
    std::cout << "Translation failed";
    return 3;
  }

  // 翻訳エンジンの解放
  translator_free(translator);

  return 0;
}
