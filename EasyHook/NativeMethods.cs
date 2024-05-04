// EasyHook (File: EasyHook\NativeMethods.cs)
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

using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace EasyHook
{
    [HostProtection(MayLeakOnAbort = true)]
    [SuppressUnmanagedCodeSecurity]
    internal sealed class SafeLocalMemHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeLocalMemHandle() : base(true)
        {
        }

        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        internal SafeLocalMemHandle(IntPtr existingHandle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(existingHandle);
        }

        protected override bool ReleaseHandle()
        {
            return LocalFree(this.handle) == IntPtr.Zero;
        }

        [DllImport(ExternDll.Kernel32)]
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private static extern IntPtr LocalFree(IntPtr hMem);
    }

    [SecurityCritical]
    internal sealed class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeProcessHandle()
            : base(true)
        {
        }

        internal SafeProcessHandle(IntPtr handle)
            : base(true)
        {
            SetHandle(handle);
        }

        internal static SafeProcessHandle InvalidHandle => new SafeProcessHandle(IntPtr.Zero);

        [SecurityCritical]
        protected override bool ReleaseHandle()
        {
            return NativeMethods.CloseHandle(this.handle);
        }
    }

    internal static class ExternDll
    {
        public const string Advapi32 = "advapi32.dll";
        public const string Kernel32 = "kernel32.dll";
    }

    [HostProtection(MayLeakOnAbort = true)]
    internal static class NativeMethods
    {
        public const int CREATE_ALWAYS = 2;

        public const int DUPLICATE_SAME_ACCESS = 2;
        public const int E_NOTIMPL = unchecked((int) 0x80004001);

        public const int GENERIC_READ = unchecked(((int) 0x80000000));

        public const int S_OK = 0x0;
        public const int STD_ERROR_HANDLE = -12;
        public const int STD_INPUT_HANDLE = -10;
        public const int STD_OUTPUT_HANDLE = -11;

        [DllImport(ExternDll.Kernel32, CharSet = CharSet.Auto, SetLastError = true)]
        [ResourceExposure(ResourceScope.Process)]
        public static extern bool CreatePipe(
            out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe,
            SECURITY_ATTRIBUTES lpPipeAttributes, int nSize);

        [DllImport(
            ExternDll.Kernel32, CharSet = CharSet.Ansi, SetLastError = true,
            BestFitMapping = false)]
        [ResourceExposure(ResourceScope.Machine)]
        public static extern bool DuplicateHandle(
            HandleRef hSourceProcessHandle,
            SafeHandle hSourceHandle,
            HandleRef hTargetProcess,
            out SafeFileHandle targetHandle,
            int dwDesiredAccess,
            bool bInheritHandle,
            int dwOptions
        );

        [DllImport(ExternDll.Kernel32, CharSet = CharSet.Ansi, SetLastError = true)]
        [ResourceExposure(ResourceScope.Process)]
        public static extern IntPtr GetCurrentProcess();

        [DllImport(ExternDll.Kernel32, CharSet = CharSet.Ansi, SetLastError = true)]
        [ResourceExposure(ResourceScope.Process)]
        public static extern IntPtr GetStdHandle(int whichHandle);

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(ProcessAccessFlags access, bool inheritHandle, int procId);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class SECURITY_ATTRIBUTES
        {
            public bool bInheritHandle = false;

            public SafeLocalMemHandle lpSecurityDescriptor =
                new SafeLocalMemHandle(IntPtr.Zero, false);

            // We don't support ACL's on Silverlight nor on CoreSystem builds in our API's.  
            // But, we need P/Invokes to occasionally take these as parameters.  We can pass null.
            public int nLength = 12;
        }
    }
}
