// EasyHook (File: Examples\UnmanagedHook\UnmanagedHook.cpp)
//
// Copyright (c) 2009 Christoph Husse & Copyright (c) 2015 Justin Stenning
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// Please visit https://easyhook.github.io for more information
// about the project and latest updates.

#include "easyhook.h"

#include <stdio.h>
#include <conio.h>

#ifndef _M_X64
    #pragma comment(lib, "EasyHook32.lib")
#else
    #pragma comment(lib, "EasyHook64.lib")
#endif

#pragma warning(disable: 4100 4127)

#define FORCE(expr)     {if(!SUCCEEDED(NtStatus = (expr))) goto ERROR_ABORT;}

BOOL WINAPI MessageBeepHook(__in UINT uType)
{
    /*
        Test barrier methods...
    */
	PVOID					CallStack[64];
	MODULE_INFORMATION		Mod;
	ULONG					MethodCount;

	LhUpdateModuleInformation();

	LhEnumModules((HMODULE*)CallStack, 64, &MethodCount);

	for(ULONG i = 0; i < MethodCount; i++)
	{
		LhBarrierPointerToModule(CallStack[i], &Mod);
	}

	LhBarrierCallStackTrace(CallStack, 64, &MethodCount);

	LhBarrierGetCallingModule(&Mod);

    return TRUE;
}

DWORD __stdcall HijackEntry(void* InParams)
{
	if(InParams != reinterpret_cast<PVOID>(0x12345678))
		throw;

	printf("\nHello from stealth thread! Thread ID: %lu\n", GetCurrentThreadId());

	return 0;
}

DWORD __stdcall TestThread(void* /*InParams*/)
{
	HANDLE					hRemoteThread;
	
	RhCreateStealthRemoteThread(
        GetCurrentProcessId(), 
        HijackEntry, 
        reinterpret_cast<PVOID>(0x12345678), 
        &hRemoteThread);

	while (TRUE) Sleep(100);

	return 0;
}

extern "C" int main(int argc, wchar_t* argv[])
{
    HMODULE                 hUser32 = LoadLibraryA("user32.dll");
    TRACED_HOOK_HANDLE      hHook = new HOOK_TRACE_INFO();
    NTSTATUS                NtStatus;
    ULONG                   ACLEntries[1] = {0};
    UNICODE_STRING*         NameBuffer = NULL;
	HANDLE					hRemoteThread;

	// test driver...
	/*printf("Installing support driver...\n");

	FORCE(RhInstallSupportDriver());

	printf("Installing test driver...\n");

	if(RhIsX64System())
		FORCE(RhInstallDriver(L"TestDriver64.sys", L"TestDriver64.sys"))
	else
		FORCE(RhInstallDriver(L"TestDriver32.sys", L"TestDriver32.sys"));
*/
	
	printf("Main thread Id: %lu\n", GetCurrentThreadId());

	// test stealth thread creation...
	printf("Testing stealth thread creation...\n");

	// The thread will attempt to install a hook using RhCreateStealthRemoteThread
	hRemoteThread = CreateThread(
        NULL, 
        0, 
        TestThread, 
        NULL, 
        0, 
        NULL);

	Sleep(500);

    /*
        The following shows how to install and remove local hooks...
    */
    FORCE(LhInstallHook(
            GetProcAddress(hUser32, "MessageBeep"),
            MessageBeepHook,
            (PVOID)0x12345678,
            hHook));

    // Won't invoke the hook handler because hooks are inactive after installation
    MessageBeep(123);

    // activate the hook for the current thread
    FORCE(LhSetInclusiveACL(ACLEntries, 1, hHook));

    // will be redirected into the handler...
    MessageBeep(123);

    // this will also invalidate "hHook", because it is a traced handle...
    LhUninstallAllHooks();

    // this will do nothing because the hook is already removed...
    LhUninstallHook(hHook);

    // now we can safely release the traced handle
    delete hHook;

    hHook = NULL;

    // even if the hook is removed, we need to wait for memory release
    LhWaitForPendingRemovals();

    /*
        In many situations you will need the handler utilities.
    */
    HANDLE          Handle = CreateEventA(NULL, TRUE, FALSE, "MyEvent");
    ULONG           RequiredSize;
    ULONG           RealThreadId;
    ULONG           ThreadId;

    // handle to name
    if(!SUCCEEDED(NtStatus = DbgHandleToObjectName(Handle, NULL, 0, &RequiredSize)))
        goto ERROR_ABORT;

    NameBuffer = (UNICODE_STRING*)malloc(RequiredSize);

    FORCE(DbgHandleToObjectName(Handle, NameBuffer, RequiredSize, &RequiredSize));

    printf("\n[Info]: Event name is \"%S\".\n", NameBuffer->Buffer);

    // handle to thread ID
    Handle = CreateThread(NULL, 0, NULL, NULL, CREATE_SUSPENDED, &RealThreadId);

    FORCE(DbgGetThreadIdByHandle(Handle, &ThreadId));

    if(ThreadId != RealThreadId)
        return EXIT_FAILURE;

	_getch();

	return 0;

ERROR_ABORT:

    if(hHook != NULL)
        delete hHook;

    if(NameBuffer != NULL)
        free(NameBuffer );

	printf(
        "\n[Error(0x%x)]: \"%S\" (code: %d {0x%x})\n",
        NtStatus,
        RtlGetLastErrorString(), 
        RtlGetLastError(),
        RtlGetLastError());

    _getch();

    return NtStatus;
}
