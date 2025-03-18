cmake_minimum_required(VERSION 3.5.1)

project(bergamot CXX C)

add_compile_options(/utf-8)
add_subdirectory(bergamot-translator)
add_subdirectory(bergamot-translator-dynamic)
