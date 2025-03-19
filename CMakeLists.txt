cmake_minimum_required(VERSION 3.5.1)

if (POLICY CMP0074)
  cmake_policy(SET CMP0074 NEW) # CMake 3.12
endif ()

if (POLICY CMP0077)
  cmake_policy(SET CMP0077 NEW)
endif()

project(bergamot CXX C)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Setting BUILD_ARCH to native invokes CPU intrinsic detection logic below.
# Prevent invoking that logic for WASM builds.
set(BUILD_ARCH native CACHE STRING "Compile for this CPU architecture.")

# Unfortunately MSVC supports a limited subset of BUILD_ARCH flags. Instead try to guess
# what architecture we can compile to reading BUILD_ARCH and mapping it to MSVC values
# references: https://clang.llvm.org/docs/UsersManual.html https://gcc.gnu.org/onlinedocs/gcc/x86-Options.html https://gcc.gnu.org/onlinedocs/gcc-4.8.5/gcc/i386-and-x86-64-Options.html
# https://docs.microsoft.com/en-us/cpp/build/reference/arch-x86?redirectedfrom=MSDN&amp;amp;view=vs-2019&view=msvc-170 https://devblogs.microsoft.com/oldnewthing/20201026-00/?p=104397
# This is by no means an exhaustive list but should match the most common flags Linux programmers expect to parse to MSVC
if(MSVC)
  if(BUILD_ARCH STREQUAL "native") # avx2 is good default for native. Very few desktop systems support avx512
    set(MSVC_BUILD_ARCH "/arch:AVX2")
  elseif(BUILD_ARCH STREQUAL "skylake-avx512" OR BUILD_ARCH STREQUAL "cannonlake" OR BUILD_ARCH STREQUAL "x86-64-v4" OR BUILD_ARCH STREQUAL "tigerlake" OR BUILD_ARCH STREQUAL "cooperlake" OR BUILD_ARCH STREQUAL "cascadelake")
    set(MSVC_BUILD_ARCH "/arch:AVX512")
  elseif(BUILD_ARCH STREQUAL "core-avx2" OR BUILD_ARCH STREQUAL "haswell" OR BUILD_ARCH STREQUAL "x86-64-v3" OR BUILD_ARCH STREQUAL "broadwell" OR BUILD_ARCH STREQUAL "skylake")
    set(MSVC_BUILD_ARCH "/arch:AVX2")
  elseif(BUILD_ARCH STREQUAL "sandybridge" OR BUILD_ARCH STREQUAL "corei7-avx" OR BUILD_ARCH STREQUAL "core-avx-i" OR BUILD_ARCH STREQUAL "ivybridge")
    set(MSVC_BUILD_ARCH "/arch:AVX")
  elseif(BUILD_ARCH STREQUAL "nehalem" OR BUILD_ARCH STREQUAL "westmere" OR BUILD_ARCH STREQUAL "x86-64-v2" OR BUILD_ARCH STREQUAL "corei7" OR BUILD_ARCH STREQUAL "core2")
    set(MSVC_BUILD_ARCH "/arch:SSE2") # This is MSVC default. We won't go down to SSE because we don't support that hardware at all with intgemm. Marian recommends to only go down to SSE4.1 at most
  else()
    message(WARNING "Unknown BUILD_ARCH ${BUILD_ARCH} provided. Default to SSE2 for Windows build")
    set(MSVC_BUILD_ARCH "/arch:SSE2")
  endif()
endif(MSVC)

#MSVC can't seem to pick up correct flags otherwise:
if(MSVC)
  add_definitions(-DUSE_SSE2=1) # Supposed to fix something in the sse_mathfun.h but not sure it does
  set(INTRINSICS ${MSVC_BUILD_ARCH}) # ARCH we're targetting on win32. @TODO variable
  
  set(CMAKE_CXX_FLAGS           "/EHsc /DWIN32 /D_WINDOWS /DUNICODE /D_UNICODE /D_CRT_NONSTDC_NO_WARNINGS /D_CRT_SECURE_NO_WARNINGS /bigobj")
  set(CMAKE_CXX_FLAGS_RELEASE   "${CMAKE_CXX_FLAGS} /MT /O2 ${INTRINSICS} /MP /GL /DNDEBUG")
  set(CMAKE_CXX_FLAGS_DEBUG     "${CMAKE_CXX_FLAGS} /MTd /Od /Ob0 ${INTRINSICS} /RTC1 /Zi /D_DEBUG")

  # ignores warning LNK4049: locally defined symbol free imported - this comes from zlib
  set(CMAKE_EXE_LINKER_FLAGS         "${CMAKE_EXE_LINKER_FLAGS} /DEBUG /LTCG:incremental /INCREMENTAL:NO /ignore:4049")
  set(CMAKE_EXE_LINKER_FLAGS_RELEASE "${CMAKE_EXE_LINKER_FLAGS} /NODEFAULTLIB:MSVCRT")
  set(CMAKE_EXE_LINKER_FLAGS_DEBUG   "${CMAKE_EXE_LINKER_FLAGS} /NODEFAULTLIB:MSVCRTD")
  set(CMAKE_STATIC_LINKER_FLAGS      "${CMAKE_STATIC_LINKER_FLAGS} /LTCG:incremental")
endif(MSVC)

add_compile_options(/utf-8)
add_subdirectory(bergamot-translator)
add_subdirectory(bergamot-translator-dynamic)
