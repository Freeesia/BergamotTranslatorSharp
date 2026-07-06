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
  set(BERGAMOT_ORIGINAL_FIND_LIBRARY_SUFFIXES ${CMAKE_FIND_LIBRARY_SUFFIXES})
  set(CMAKE_FIND_LIBRARY_SUFFIXES ".a")
  find_library(BERGAMOT_TCMALLOC_LIBRARY
    NAMES tcmalloc
    HINTS "${GPERFTOOLS_ROOT}"
    PATH_SUFFIXES lib
  )
  set(CMAKE_FIND_LIBRARY_SUFFIXES ${BERGAMOT_ORIGINAL_FIND_LIBRARY_SUFFIXES})

  if(NOT BERGAMOT_TCMALLOC_INCLUDE_DIR OR NOT BERGAMOT_TCMALLOC_LIBRARY)
    message(FATAL_ERROR "Could not find static gperftools/tcmalloc. Install gperftools development files.")
  endif()
  add_library(bergamot_tcmalloc STATIC IMPORTED)
  set_target_properties(bergamot_tcmalloc PROPERTIES
    IMPORTED_LOCATION "${BERGAMOT_TCMALLOC_LIBRARY}"
    INTERFACE_INCLUDE_DIRECTORIES "${BERGAMOT_TCMALLOC_INCLUDE_DIR}"
  )
  set(BERGAMOT_TCMALLOC_TARGET bergamot_tcmalloc)
elseif(UNIX)
  include(FetchContent)

  # Distribution-provided tcmalloc archives are not consistently built as
  # PIC, so build a pinned static archive that can be embedded in libbergamot.
  # tcmalloc's initial-exec TLS model is incompatible with libraries loaded
  # lazily through .NET P/Invoke; use its pthread-key fallback instead.
  set(BUILD_TESTING OFF CACHE BOOL "" FORCE)
  set(GPERFTOOLS_BUILD_STATIC ON CACHE BOOL "" FORCE)
  set(GPERFTOOLS_BUILD_CPU_PROFILER OFF CACHE BOOL "" FORCE)
  set(GPERFTOOLS_BUILD_HEAP_PROFILER ON CACHE BOOL "" FORCE)
  set(GPERFTOOLS_BUILD_HEAP_CHECKER OFF CACHE BOOL "" FORCE)
  set(GPERFTOOLS_BUILD_DEBUGALLOC OFF CACHE BOOL "" FORCE)
  set(gperftools_build_benchmark OFF CACHE BOOL "" FORCE)
  set(gperftools_enable_libunwind OFF CACHE BOOL "" FORCE)
  set(HAVE_TLS OFF CACHE INTERNAL "Disable initial-exec TLS for P/Invoke loading" FORCE)

  FetchContent_Declare(bergamot_gperftools
    GIT_REPOSITORY https://github.com/gperftools/gperftools.git
    GIT_TAG 365060c4213a48adb27f63d5dfad41b3dfbdd62e
    GIT_SHALLOW TRUE
  )
  FetchContent_MakeAvailable(bergamot_gperftools)
  set(BERGAMOT_TCMALLOC_TARGET tcmalloc_static)
endif()

# 必要な依存ライブラリのリンク
target_link_libraries(bergamot_translator_dynamic
  PUBLIC
    bergamot-translator
    $<$<NOT:$<PLATFORM_ID:Windows>>:${BERGAMOT_TCMALLOC_TARGET}>
)

# コンパイルオプションの設定
if(MSVC)
  target_compile_options(bergamot_translator_dynamic PRIVATE /W4)
else()
  target_compile_options(bergamot_translator_dynamic PRIVATE -Wall -Wextra)
endif()

# DLLエクスポートマクロの定義
target_compile_definitions(bergamot_translator_dynamic
  PRIVATE
    BERGAMOT_TRANSLATOR_DYNAMIC_EXPORTS
)

# プラットフォーム別のインストール設定
if(WIN32)
  set(BERGAMOT_WINDOWS_INSTALL_DIR "win")
  if(BERGAMOT_TARGET_ARM64)
    set(BERGAMOT_WINDOWS_INSTALL_DIR "win-arm64")
  endif()

  install(TARGETS bergamot_translator_dynamic
    RUNTIME DESTINATION bin/${BERGAMOT_WINDOWS_INSTALL_DIR} COMPONENT bergamot_translator_dynamic
    LIBRARY DESTINATION bin/${BERGAMOT_WINDOWS_INSTALL_DIR} COMPONENT bergamot_translator_dynamic
    ARCHIVE DESTINATION lib/${BERGAMOT_WINDOWS_INSTALL_DIR} COMPONENT bergamot_translator_dynamic
  )
elseif(APPLE)
  install(TARGETS bergamot_translator_dynamic
    LIBRARY DESTINATION lib/macos COMPONENT bergamot_translator_dynamic
    RUNTIME DESTINATION bin/macos COMPONENT bergamot_translator_dynamic
    ARCHIVE DESTINATION lib/macos COMPONENT bergamot_translator_dynamic
  )
else() # Linux その他のUnix系
  set(BERGAMOT_LINUX_INSTALL_DIR "linux")
  if(BERGAMOT_TARGET_ARM64)
    set(BERGAMOT_LINUX_INSTALL_DIR "linux-arm64")
  endif()

  install(TARGETS bergamot_translator_dynamic
    LIBRARY DESTINATION lib/${BERGAMOT_LINUX_INSTALL_DIR} COMPONENT bergamot_translator_dynamic
    RUNTIME DESTINATION bin/${BERGAMOT_LINUX_INSTALL_DIR} COMPONENT bergamot_translator_dynamic
    ARCHIVE DESTINATION lib/${BERGAMOT_LINUX_INSTALL_DIR} COMPONENT bergamot_translator_dynamic
  )
endif()

install(FILES include/bergamot.h
  DESTINATION include
  COMPONENT bergamot_translator_dynamic
)
