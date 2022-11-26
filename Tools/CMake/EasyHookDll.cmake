
SET(TARGET_NAME EasyHookDll)

add_library(${TARGET_NAME}
    "${CMAKE_SOURCE_DIR}/EasyHookDll/AUX_ULIB.H"
    "${CMAKE_SOURCE_DIR}/EasyHookDll/AUX_ULIB_x64.LIB"
    "${CMAKE_SOURCE_DIR}/EasyHookDll/AUX_ULIB_x86.LIB"
    "${CMAKE_SOURCE_DIR}/EasyHookDll/dllmain.c"
    "${CMAKE_SOURCE_DIR}/EasyHookDll/EasyHook32.def"
    "${CMAKE_SOURCE_DIR}/EasyHookDll/EasyHook64.def"
    "${CMAKE_SOURCE_DIR}/EasyHookDll/EasyHookDll.vcxproj"
    "${CMAKE_SOURCE_DIR}/EasyHookDll/EasyHookDll.vcxproj.filters"
    "${CMAKE_SOURCE_DIR}/EasyHookDll/EasyHookDll.vcxproj.user"
    "${CMAKE_SOURCE_DIR}/EasyHookDll/EasyHookDll_32.rc"
    "${CMAKE_SOURCE_DIR}/EasyHookDll/EasyHookDll_64.rc"
    "${CMAKE_SOURCE_DIR}/EasyHookDll/gacutil.cpp"
    "${CMAKE_SOURCE_DIR}/EasyHookDll/LocalHook"
    "${CMAKE_SOURCE_DIR}/EasyHookDll/ntstatus.h"
    "${CMAKE_SOURCE_DIR}/EasyHookDll/RemoteHook"
    "${CMAKE_SOURCE_DIR}/EasyHookDll/resource.h"
    "${CMAKE_SOURCE_DIR}/EasyHookDll/Rtl"
    "${CMAKE_SOURCE_DIR}/EasyHookDll/stdafx.h"
    "${CMAKE_SOURCE_DIR}/EasyHookDll/LocalHook/acl.c"
    "${CMAKE_SOURCE_DIR}/EasyHookDll/LocalHook/debug.cpp"
    "${CMAKE_SOURCE_DIR}/EasyHookDll/RemoteHook/driver.cpp"
    "${CMAKE_SOURCE_DIR}/EasyHookDll/RemoteHook/entry.cpp"
    "${CMAKE_SOURCE_DIR}/EasyHookDll/RemoteHook/service.c"
    "${CMAKE_SOURCE_DIR}/EasyHookDll/RemoteHook/stealth.c"
    "${CMAKE_SOURCE_DIR}/EasyHookDll/RemoteHook/thread.c"
    "${CMAKE_SOURCE_DIR}/EasyHookDll/Rtl/file.c"
    "${CMAKE_SOURCE_DIR}/EasyHookDll/Rtl/memory.c")

target_include_directories(${TARGET_NAME} AFTER
		PUBLIC "${CMAKE_SOURCE_DIR}/EasyHookDll"
		PUBLIC "${CMAKE_SOURCE_DIR}/Public"
		PUBLIC "${CMAKE_SOURCE_DIR}/DriverShared")
