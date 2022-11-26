# EasyHook (File: Tools/CMake/EasyHookDll.cmake)
#
# Copyright (c) 2009 Christoph Husse & Copyright (c) 2015 Justin Stenning
#
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
#
# The above copyright notice and this permission notice shall be included in
# all copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
# THE SOFTWARE.
#
# Please visit https://easyhook.github.io for more information
# about the project and latest updates.

set(TARGET_NAME EasyHookDll)
set(TARGET_ARCH 64)

if (CMAKE_SIZEOF_VOID_P EQUAL 8)
	set(TARGET_ARCH 64)
	set(TARGET_LIB_POSTFIX x64)
else ()
	set(TARGET_ARCH 32)
	set(TARGET_LIB_POSTFIX x86)
endif ()

add_library(${TARGET_NAME} SHARED
		"${CMAKE_SOURCE_DIR}/EasyHookDll/EasyHook${TARGET_ARCH}.def"
		"${CMAKE_SOURCE_DIR}/EasyHookDll/dllmain.c"
		"${CMAKE_SOURCE_DIR}/EasyHookDll/EasyHookDll_${TARGET_ARCH}.rc"
		"${CMAKE_SOURCE_DIR}/EasyHookDll/gacutil.cpp"
		"${CMAKE_SOURCE_DIR}/EasyHookDll/ntstatus.h"
		"${CMAKE_SOURCE_DIR}/EasyHookDll/resource.h"
		"${CMAKE_SOURCE_DIR}/EasyHookDll/stdafx.h"
		"${CMAKE_SOURCE_DIR}/EasyHookDll/AUX_ULIB.H"
		"${CMAKE_SOURCE_DIR}/EasyHookDll/LocalHook/acl.c"
		"${CMAKE_SOURCE_DIR}/EasyHookDll/LocalHook/debug.cpp"
		"${CMAKE_SOURCE_DIR}/EasyHookDll/RemoteHook/driver.cpp"
		"${CMAKE_SOURCE_DIR}/EasyHookDll/RemoteHook/entry.cpp"
		"${CMAKE_SOURCE_DIR}/EasyHookDll/RemoteHook/service.c"
		"${CMAKE_SOURCE_DIR}/EasyHookDll/RemoteHook/stealth.c"
		"${CMAKE_SOURCE_DIR}/EasyHookDll/RemoteHook/thread.c"
		"${CMAKE_SOURCE_DIR}/EasyHookDll/Rtl/file.c"
		"${CMAKE_SOURCE_DIR}/EasyHookDll/Rtl/memory.c"
		"${CMAKE_SOURCE_DIR}/DriverShared/DriverShared.h"
		"${CMAKE_SOURCE_DIR}/DriverShared/rtl.h"
		"${CMAKE_SOURCE_DIR}/DriverShared/ASM/HookSpecific_${TARGET_LIB_POSTFIX}.asm"
		"${CMAKE_SOURCE_DIR}/DriverShared/Disassembler/udis86.h"
		"${CMAKE_SOURCE_DIR}/DriverShared/Disassembler/libudis86/decode.c"
		"${CMAKE_SOURCE_DIR}/DriverShared/Disassembler/libudis86/decode.h"
		"${CMAKE_SOURCE_DIR}/DriverShared/Disassembler/libudis86/extern.h"
		"${CMAKE_SOURCE_DIR}/DriverShared/Disassembler/libudis86/itab.c"
		"${CMAKE_SOURCE_DIR}/DriverShared/Disassembler/libudis86/itab.h"
		"${CMAKE_SOURCE_DIR}/DriverShared/Disassembler/libudis86/syn-att.c"
		"${CMAKE_SOURCE_DIR}/DriverShared/Disassembler/libudis86/syn-intel.c"
		"${CMAKE_SOURCE_DIR}/DriverShared/Disassembler/libudis86/syn.c"
		"${CMAKE_SOURCE_DIR}/DriverShared/Disassembler/libudis86/syn.h"
		"${CMAKE_SOURCE_DIR}/DriverShared/Disassembler/libudis86/types.h"
		"${CMAKE_SOURCE_DIR}/DriverShared/Disassembler/libudis86/udint.h"
		"${CMAKE_SOURCE_DIR}/DriverShared/Disassembler/libudis86/udis86.c"
		"${CMAKE_SOURCE_DIR}/DriverShared/LocalHook/alloc.c"
		"${CMAKE_SOURCE_DIR}/DriverShared/LocalHook/barrier.c"
		"${CMAKE_SOURCE_DIR}/DriverShared/LocalHook/caller.c"
		"${CMAKE_SOURCE_DIR}/DriverShared/LocalHook/install.c"
		"${CMAKE_SOURCE_DIR}/DriverShared/LocalHook/reloc.c"
		"${CMAKE_SOURCE_DIR}/DriverShared/LocalHook/uninstall.c"
		"${CMAKE_SOURCE_DIR}/DriverShared/Rtl/error.c"
		"${CMAKE_SOURCE_DIR}/DriverShared/Rtl/string.c"
		)

target_link_libraries(${TARGET_NAME}
		AUX_ULIB_${TARGET_LIB_POSTFIX}
		psapi)

target_link_directories(${TARGET_NAME}
		PUBLIC "${CMAKE_SOURCE_DIR}/EasyHookDll")

target_include_directories(${TARGET_NAME} AFTER
		PUBLIC "${CMAKE_SOURCE_DIR}/EasyHookDll"
		PUBLIC "${CMAKE_SOURCE_DIR}/Public"
		PUBLIC "${CMAKE_SOURCE_DIR}/DriverShared")

set_property(TARGET ${TARGET_NAME} PROPERTY
		MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")

target_compile_definitions(${TARGET_NAME} PRIVATE
		"_UNICODE"
		"UNICODE"
		"WIN32"
		"$<$<CONFIG:Debug>:_DEBUG>"
		"_WINDOWS"
		"_USRDLL"
		"EASYHOOK_EXPORTS"
		"_ALLOW_RTCc_IN_STL"
		)

target_link_options(${TARGET_NAME}
		PRIVATE -SAFESEH:NO)

set_source_files_properties("${CMAKE_SOURCE_DIR}/EasyHookDll/EasyHook${TARGET_ARCH}.def" PROPERTIES
		HEADER_FILE_ONLY TRUE)
