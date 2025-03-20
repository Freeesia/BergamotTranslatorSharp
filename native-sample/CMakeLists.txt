add_executable(native-sample bergamot.cpp)

# インクルードディレクトリを追加
target_include_directories(native-sample PRIVATE
  ${CMAKE_SOURCE_DIR}/bergamot-translator-dynamic/include
)

# 動的ライブラリのディレクトリを追加
target_link_directories(native-sample PRIVATE
  ${CMAKE_BINARY_DIR}/bergamot-translator-dynamic/$<CONFIG>
)

# ターゲット名ではなく、実際のライブラリファイル名を指定
# これによりAdditionalDependenciesにbergamot.libが追加される
target_link_libraries(native-sample PRIVATE bergamot.lib)

# Windowsの場合、DLLをコピー
if(WIN32)
  add_custom_command(TARGET native-sample POST_BUILD
    # bergamot.dllをコピー
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
      ${CMAKE_BINARY_DIR}/bergamot-translator-dynamic/$<CONFIG>/bergamot.dll
      $<TARGET_FILE_DIR:native-sample>
    # pcre2-8.dllをコピー
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
      ${CMAKE_BINARY_DIR}/bergamot-translator-dynamic/$<CONFIG>/pcre2-8.dll
      $<TARGET_FILE_DIR:native-sample>
  )
endif()
