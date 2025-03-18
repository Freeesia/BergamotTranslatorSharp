# 動的ライブラリのターゲットを追加
add_library(bergamot_translator_dynamic SHARED
  src/bergamot_translator_dynamic.cpp
)

# インクルードディレクトリの設定
target_include_directories(bergamot_translator_dynamic
  PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}/include
)

# 必要な依存ライブラリのリンク
target_link_libraries(bergamot_translator_dynamic
  PRIVATE
    bergamot-translator
)

# コンパイルオプションの設定
target_compile_options(bergamot_translator_dynamic
  PRIVATE
    $<$<CXX_COMPILER_ID:MSVC>:/W4>
    $<$<NOT:$<CXX_COMPILER_ID:MSVC>>:-Wall -Wextra>
)

# DLLエクスポートマクロの定義
target_compile_definitions(bergamot_translator_dynamic
  PRIVATE
    BERGAMOT_TRANSLATOR_DYNAMIC_EXPORTS
)

# インストール設定
install(TARGETS bergamot_translator_dynamic
  LIBRARY DESTINATION lib
  RUNTIME DESTINATION bin
  ARCHIVE DESTINATION lib
)

install(FILES include/bergamot_translator_dynamic.h
  DESTINATION include
)
