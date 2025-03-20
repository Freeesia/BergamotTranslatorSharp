add_executable(native-sample bergamot.cpp)

# bergamot_translator_dynamicターゲットを直接リンク
# インクルードディレクトリやライブラリ情報が自動的に伝播する
target_link_libraries(native-sample PRIVATE bergamot_translator_dynamic)

add_custom_command(TARGET native-sample POST_BUILD
  COMMAND ${CMAKE_COMMAND} -E copy_if_different
    $<TARGET_RUNTIME_DLLS:native-sample>
    $<TARGET_FILE_DIR:native-sample>
  COMMAND_EXPAND_LISTS
)