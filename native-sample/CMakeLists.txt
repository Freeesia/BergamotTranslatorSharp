add_executable(native-sample bergamot.cpp)
target_link_libraries(native-sample PRIVATE bergamot-translator)
# target_link_libraries(native-sample PRIVATE bergamot-translator-dynamic)
