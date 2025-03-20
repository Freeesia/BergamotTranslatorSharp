#include "bergamot.h"
#include <iostream>
#include <string>
#include <fstream>
#include <sstream>
#include <combaseapi.h>

// 標準入力からテキストを読み込む関数
std::string readFromStdin() {
  std::stringstream buffer;
  buffer << std::cin.rdbuf();
  return buffer.str();
}

int main(int argc, char *argv[]) {
  if (argc < 2) {
    std::cout << "Usage: " << argv[0] << " <config_path>" << std::endl;
    return 1;
  }

  // モデル設定ファイルのパス
  const char* configPath = argv[1];

  // 翻訳エンジンの初期化
  void* translator = translator_initialize(configPath);
  if (!translator) {
    std::cout << "Failed to initialize translator";
    return 1;
  }

  // 標準入力からテキストを読み込む
  std::string input = readFromStdin();

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
  }

  // 翻訳エンジンの解放
  translator_free(translator);

  return 0;
}
