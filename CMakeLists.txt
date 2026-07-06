cmake_minimum_required(VERSION 3.5.1)

if (POLICY CMP0074)
  cmake_policy(SET CMP0074 NEW) # CMake 3.12
endif ()

if (POLICY CMP0077)
  cmake_policy(SET CMP0077 NEW)
endif()

# vcpkgのtoolchainファイルを設定（指定されていない場合）
if(NOT DEFINED CMAKE_TOOLCHAIN_FILE)
  if(DEFINED ENV{VCPKG_ROOT})
    set(CMAKE_TOOLCHAIN_FILE "$ENV{VCPKG_ROOT}/scripts/buildsystems/vcpkg.cmake" CACHE STRING "")
  endif()
endif()

project(bergamot CXX C)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

string(TOLOWER "${CMAKE_SYSTEM_PROCESSOR}" BERGAMOT_SYSTEM_PROCESSOR)
set(BERGAMOT_TARGET_ARM64 OFF)
if(BERGAMOT_SYSTEM_PROCESSOR MATCHES "^(arm64|aarch64)$")
  set(BERGAMOT_TARGET_ARM64 ON)
endif()

set(BERGAMOT_TARGET_CLANG_CL OFF)
if(MSVC AND CMAKE_CXX_COMPILER_ID MATCHES "Clang")
  set(BERGAMOT_TARGET_CLANG_CL ON)
endif()

function(bergamot_apply_dependency_patch repository patch_file)
  find_package(Git REQUIRED)
  set(BERGAMOT_GIT_ARGS
    -c "safe.directory=${repository}"
    -C "${repository}"
    apply --ignore-space-change --unidiff-zero)

  execute_process(
    COMMAND "${GIT_EXECUTABLE}" ${BERGAMOT_GIT_ARGS} --reverse --check "${patch_file}"
    RESULT_VARIABLE BERGAMOT_PATCH_ALREADY_APPLIED
    OUTPUT_QUIET ERROR_QUIET)
  if(BERGAMOT_PATCH_ALREADY_APPLIED EQUAL 0)
    return()
  endif()

  execute_process(
    COMMAND "${GIT_EXECUTABLE}" ${BERGAMOT_GIT_ARGS} --check "${patch_file}"
    RESULT_VARIABLE BERGAMOT_PATCH_CHECK_RESULT
    OUTPUT_QUIET ERROR_VARIABLE BERGAMOT_PATCH_CHECK_ERROR)
  if(NOT BERGAMOT_PATCH_CHECK_RESULT EQUAL 0)
    message(FATAL_ERROR "Cannot apply ${patch_file}: ${BERGAMOT_PATCH_CHECK_ERROR}")
  endif()

  execute_process(
    COMMAND "${GIT_EXECUTABLE}" ${BERGAMOT_GIT_ARGS} "${patch_file}"
    RESULT_VARIABLE BERGAMOT_PATCH_RESULT
    OUTPUT_QUIET ERROR_VARIABLE BERGAMOT_PATCH_ERROR)
  if(NOT BERGAMOT_PATCH_RESULT EQUAL 0)
    message(FATAL_ERROR "Failed to apply ${patch_file}: ${BERGAMOT_PATCH_ERROR}")
  endif()
endfunction()

if(WIN32 AND BERGAMOT_TARGET_ARM64)
  set(ARM ON CACHE BOOL "Build for ARM" FORCE)
  set(USE_MKL OFF CACHE BOOL "Compile with MKL support" FORCE)

  if(BERGAMOT_TARGET_CLANG_CL)
    set(BERGAMOT_PATCH_DIR "${CMAKE_CURRENT_SOURCE_DIR}/cmake/patches/windows-arm64-clang-cl")
    set(BERGAMOT_MARIAN_DIR "${CMAKE_CURRENT_SOURCE_DIR}/bergamot-translator/3rd_party/marian-dev")
    bergamot_apply_dependency_patch(
      "${CMAKE_CURRENT_SOURCE_DIR}/bergamot-translator"
      "${BERGAMOT_PATCH_DIR}/bergamot-translator.patch")
    bergamot_apply_dependency_patch(
      "${BERGAMOT_MARIAN_DIR}"
      "${BERGAMOT_PATCH_DIR}/marian-dev.patch")
    bergamot_apply_dependency_patch(
      "${BERGAMOT_MARIAN_DIR}/src/3rd_party/ruy"
      "${BERGAMOT_PATCH_DIR}/ruy.patch")
    bergamot_apply_dependency_patch(
      "${BERGAMOT_MARIAN_DIR}/src/3rd_party/sentencepiece"
      "${BERGAMOT_PATCH_DIR}/sentencepiece.patch")
    bergamot_apply_dependency_patch(
      "${BERGAMOT_MARIAN_DIR}/src/3rd_party/simd_utils"
      "${BERGAMOT_PATCH_DIR}/simd-utils.patch")
  endif()
endif()

function(bergamot_configure_windows_arm64_target target_name)
  if(WIN32 AND BERGAMOT_TARGET_ARM64 AND MSVC AND TARGET ${target_name})
    target_compile_options(${target_name} PUBLIC
      /U__AVX__ /U__AVX2__ /U__SSE__ /U__SSE2__ /UUSE_SSE2)
  endif()
endfunction()

if(WIN32)
  set(BERGAMOT_UTF8_PATH_PATCH_DIR "${CMAKE_CURRENT_SOURCE_DIR}/cmake/patches/windows-utf8-paths")
  bergamot_apply_dependency_patch(
    "${CMAKE_CURRENT_SOURCE_DIR}/bergamot-translator"
    "${BERGAMOT_UTF8_PATH_PATCH_DIR}/bergamot-translator.patch")
  bergamot_apply_dependency_patch(
    "${CMAKE_CURRENT_SOURCE_DIR}/bergamot-translator/3rd_party/marian-dev"
    "${BERGAMOT_UTF8_PATH_PATCH_DIR}/marian-dev.patch")
endif()

# Setting BUILD_ARCH to native invokes CPU intrinsic detection logic below.
# Prevent invoking that logic for WASM builds.
set(BUILD_ARCH native CACHE STRING "Compile for this CPU architecture.")

# Unfortunately MSVC supports a limited subset of BUILD_ARCH flags. Instead try to guess
# what architecture we can compile to reading BUILD_ARCH and mapping it to MSVC values
# references: https://clang.llvm.org/docs/UsersManual.html https://gcc.gnu.org/onlinedocs/gcc/x86-Options.html https://gcc.gnu.org/onlinedocs/gcc-4.8.5/gcc/i386-and-x86-64-Options.html
# https://docs.microsoft.com/en-us/cpp/build/reference/arch-x86?redirectedfrom=MSDN&amp;amp;view=vs-2019&view=msvc-170 https://devblogs.microsoft.com/oldnewthing/20201026-00/?p=104397
# This is by no means an exhaustive list but should match the most common flags Linux programmers expect to parse to MSVC
if(MSVC AND NOT BERGAMOT_TARGET_ARM64)
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
elseif(MSVC)
  set(MSVC_BUILD_ARCH "")
endif()

#MSVC can't seem to pick up correct flags otherwise:
if(MSVC)
  if(NOT BERGAMOT_TARGET_ARM64)
    add_definitions(-DUSE_SSE2=1) # Supposed to fix something in the sse_mathfun.h but not sure it does
  endif()
  set(INTRINSICS ${MSVC_BUILD_ARCH}) # ARCH we're targetting on win32. @TODO variable
  
  set(CMAKE_CXX_FLAGS           "/EHsc /DWIN32 /D_WINDOWS /DUNICODE /D_UNICODE /D_CRT_NONSTDC_NO_WARNINGS /D_CRT_SECURE_NO_WARNINGS /bigobj")
  if(BERGAMOT_TARGET_CLANG_CL)
    set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS} /MT /O2 ${INTRINSICS} /DNDEBUG")
  else()
    set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS} /MT /O2 ${INTRINSICS} /MP /GL /DNDEBUG")
  endif()
  set(CMAKE_CXX_FLAGS_DEBUG     "${CMAKE_CXX_FLAGS} /MTd /Od /Ob0 ${INTRINSICS} /RTC1 /Zi /D_DEBUG")

  # ignores warning LNK4049: locally defined symbol free imported - this comes from zlib
  if(BERGAMOT_TARGET_CLANG_CL)
    set(CMAKE_EXE_LINKER_FLAGS       "${CMAKE_EXE_LINKER_FLAGS} /DEBUG /INCREMENTAL:NO /ignore:4049")
  else()
    set(CMAKE_EXE_LINKER_FLAGS       "${CMAKE_EXE_LINKER_FLAGS} /DEBUG /LTCG:incremental /INCREMENTAL:NO /ignore:4049")
  endif()
  set(CMAKE_EXE_LINKER_FLAGS_RELEASE "${CMAKE_EXE_LINKER_FLAGS} /NODEFAULTLIB:MSVCRT")
  set(CMAKE_EXE_LINKER_FLAGS_DEBUG   "${CMAKE_EXE_LINKER_FLAGS} /NODEFAULTLIB:MSVCRTD")
  if(NOT BERGAMOT_TARGET_CLANG_CL)
    set(CMAKE_STATIC_LINKER_FLAGS    "${CMAKE_STATIC_LINKER_FLAGS} /LTCG:incremental")
  endif()
  add_compile_options(/utf-8)
endif(MSVC)

# The P/Invoke shared library embeds its own PIC build of tcmalloc. Avoid
# pulling a second, distribution-provided archive into Marian or SentencePiece.
if(UNIX AND NOT APPLE)
  set(CMAKE_DISABLE_FIND_PACKAGE_Tcmalloc TRUE)
  set(SPM_ENABLE_TCMALLOC OFF CACHE BOOL "Disable the system tcmalloc archive" FORCE)
endif()

add_subdirectory(bergamot-translator)
bergamot_configure_windows_arm64_target(marian)
bergamot_configure_windows_arm64_target(bergamot-translator)
if(WIN32 AND BERGAMOT_TARGET_ARM64 AND BERGAMOT_TARGET_CLANG_CL AND TARGET marian)
  target_compile_options(marian PRIVATE /WX-)
endif()

if(APPLE AND TARGET zlib)
  target_compile_options(zlib PRIVATE -UTARGET_OS_MAC)
endif()

if(APPLE AND CMAKE_CXX_COMPILER_ID MATCHES "Clang" AND TARGET marian)
  target_compile_options(marian PRIVATE -Wno-error=vla-cxx-extension)
endif()

add_subdirectory(bergamot-translator-dynamic)
bergamot_configure_windows_arm64_target(bergamot_translator_dynamic)
add_subdirectory(native-sample)
bergamot_configure_windows_arm64_target(native-sample)
