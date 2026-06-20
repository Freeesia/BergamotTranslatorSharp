# 動的ライブラリのターゲットを追加
add_library(bergamot_translator_dynamic SHARED src/bergamot.cpp)

if(NOT WIN32)
  find_path(GPERFTOOLS_INCLUDE_DIR gperftools/tcmalloc.h)
  set(BERGAMOT_TCMALLOC_NAMES tcmalloc_minimal tcmalloc)
  if(USE_STATIC_LIBS)
    list(INSERT BERGAMOT_TCMALLOC_NAMES 0 libtcmalloc_minimal.a libtcmalloc.a)
  endif()
  find_library(BERGAMOT_TCMALLOC_LIBRARY NAMES ${BERGAMOT_TCMALLOC_NAMES})
  if(NOT GPERFTOOLS_INCLUDE_DIR OR NOT BERGAMOT_TCMALLOC_LIBRARY)
    message(FATAL_ERROR "Could not find gperftools/tcmalloc. Install gperftools.")
  endif()
endif()

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
)

if(NOT WIN32)
  target_include_directories(bergamot_translator_dynamic
    PRIVATE
      ${GPERFTOOLS_INCLUDE_DIR}
  )
  target_link_libraries(bergamot_translator_dynamic
    PRIVATE
      ${BERGAMOT_TCMALLOC_LIBRARY}
  )
endif()

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

# プラットフォーム別のインストール設定
if(WIN32)
  install(TARGETS bergamot_translator_dynamic
    RUNTIME DESTINATION bin/win COMPONENT bergamot_translator_dynamic
    LIBRARY DESTINATION bin/win COMPONENT bergamot_translator_dynamic
    ARCHIVE DESTINATION lib/win COMPONENT bergamot_translator_dynamic
  )
elseif(APPLE)
  install(TARGETS bergamot_translator_dynamic
    LIBRARY DESTINATION lib/macos COMPONENT bergamot_translator_dynamic
    RUNTIME DESTINATION bin/macos COMPONENT bergamot_translator_dynamic
    ARCHIVE DESTINATION lib/macos COMPONENT bergamot_translator_dynamic
  )
else() # Linux その他のUnix系
  install(TARGETS bergamot_translator_dynamic
    LIBRARY DESTINATION lib/linux COMPONENT bergamot_translator_dynamic
    RUNTIME DESTINATION bin/linux COMPONENT bergamot_translator_dynamic
    ARCHIVE DESTINATION lib/linux COMPONENT bergamot_translator_dynamic
  )
endif()

install(FILES include/bergamot.h
  DESTINATION include
  COMPONENT bergamot_translator_dynamic
)
