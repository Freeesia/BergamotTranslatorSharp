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

if(APPLE)
  set(GPERFTOOLS_ROOT "" CACHE PATH "gperftools installation prefix")
  find_path(BERGAMOT_TCMALLOC_INCLUDE_DIR
    NAMES gperftools/tcmalloc.h
    HINTS "${GPERFTOOLS_ROOT}"
    PATH_SUFFIXES include
  )
  find_library(BERGAMOT_TCMALLOC_LIBRARY
    NAMES libtcmalloc.a tcmalloc
    HINTS "${GPERFTOOLS_ROOT}"
    PATH_SUFFIXES lib
  )
  if(NOT BERGAMOT_TCMALLOC_INCLUDE_DIR OR NOT BERGAMOT_TCMALLOC_LIBRARY)
    message(FATAL_ERROR "Could not find gperftools/tcmalloc. Install gperftools.")
  endif()
  add_library(bergamot_tcmalloc UNKNOWN IMPORTED)
  set_target_properties(bergamot_tcmalloc PROPERTIES
    IMPORTED_LOCATION "${BERGAMOT_TCMALLOC_LIBRARY}"
    INTERFACE_INCLUDE_DIRECTORIES "${BERGAMOT_TCMALLOC_INCLUDE_DIR}"
  )
  set(BERGAMOT_TCMALLOC_TARGET bergamot_tcmalloc)
else()
  set(BERGAMOT_TCMALLOC_TARGET tcmalloc)
endif()

# 必要な依存ライブラリのリンク
target_link_libraries(bergamot_translator_dynamic
  PUBLIC
    bergamot-translator
    $<$<NOT:$<PLATFORM_ID:Windows>>:${BERGAMOT_TCMALLOC_TARGET}>
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
