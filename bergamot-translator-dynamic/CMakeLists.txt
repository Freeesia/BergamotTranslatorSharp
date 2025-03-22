# 動的ライブラリのターゲットを追加
add_library(bergamot_translator_dynamic SHARED src/bergamot.cpp)

# 出力ファイル名を設定
set_target_properties(bergamot_translator_dynamic
  PROPERTIES
    OUTPUT_NAME "bergamot"
)

# インクルードディレクトリの設定
target_include_directories(bergamot_translator_dynamic
  PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}/include
)

# 必要な依存ライブラリのリンク
target_link_libraries(bergamot_translator_dynamic
  PUBLIC
    bergamot-translator
    $<$<NOT:$<PLATFORM_ID:Windows>>:tcmalloc>
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
  LIBRARY DESTINATION lib COMPONENT bergamot_translator_dynamic
  RUNTIME DESTINATION bin COMPONENT bergamot_translator_dynamic
  ARCHIVE DESTINATION lib COMPONENT bergamot_translator_dynamic
)

install(FILES include/bergamot.h
  DESTINATION include
  COMPONENT bergamot_translator_dynamic
)