using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace EasyHook
{

    [HostProtection(MayLeakOnAbort = true)]
    [SuppressUnmanagedCodeSecurity]
    internal sealed class SafeLocalMemHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeLocalMemHandle() : base(true) { }

        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        internal SafeLocalMemHandle(IntPtr existingHandle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(existingHandle);
        }


        [DllImport(ExternDll.Advapi32, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true, BestFitMapping = false)]
        [ResourceExposure(ResourceScope.None)]
        internal static extern unsafe bool ConvertStringSecurityDescriptorToSecurityDescriptor(string StringSecurityDescriptor, int StringSDRevision, out SafeLocalMemHandle pSecurityDescriptor, IntPtr SecurityDescriptorSize);

        [DllImport(ExternDll.Kernel32)]
        [ResourceExposure(ResourceScope.None)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private static extern IntPtr LocalFree(IntPtr hMem);

        override protected bool ReleaseHandle()
        {
            return LocalFree(handle) == IntPtr.Zero;
        }
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
            => this.SetHandle(handle);

        internal static SafeProcessHandle InvalidHandle => new SafeProcessHandle(IntPtr.Zero);

        [SecurityCritical]
        protected override bool ReleaseHandle() => NativeMethods.CloseHandle(this.handle);
    }

    internal static class ExternDll
    {
        public const string Kernel32 = "kernel32.dll";
        public const string Advapi32 = "advapi32.dll";
    }

    [HostProtection(MayLeakOnAbort = true)]
    internal static class NativeMethods
    {
        [DllImport(ExternDll.Kernel32, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        [ResourceExposure(ResourceScope.Process)]
        public static extern int GetCurrentProcessId();
        [DllImport(ExternDll.Kernel32, CharSet = System.Runtime.InteropServices.CharSet.Ansi, SetLastError = true)]
        [ResourceExposure(ResourceScope.Process)]
        public static extern IntPtr GetCurrentProcess();

        [DllImport(ExternDll.Kernel32, CharSet = System.Runtime.InteropServices.CharSet.Ansi, SetLastError = true, BestFitMapping = false)]
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

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool CloseHandle(IntPtr handle);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal class TEXTMETRIC
        {
            public int tmHeight = 0;
            public int tmAscent = 0;
            public int tmDescent = 0;
            public int tmInternalLeading = 0;
            public int tmExternalLeading = 0;
            public int tmAveCharWidth = 0;
            public int tmMaxCharWidth = 0;
            public int tmWeight = 0;
            public int tmOverhang = 0;
            public int tmDigitizedAspectX = 0;
            public int tmDigitizedAspectY = 0;
            public char tmFirstChar = '\0';
            public char tmLastChar = '\0';
            public char tmDefaultChar = '\0';
            public char tmBreakChar = '\0';
            public byte tmItalic = 0;
            public byte tmUnderlined = 0;
            public byte tmStruckOut = 0;
            public byte tmPitchAndFamily = 0;
            public byte tmCharSet = 0;
        }

        public const int DUPLICATE_CLOSE_SOURCE = 1;
        public const int DUPLICATE_SAME_ACCESS = 2;

        public const int DEFAULT_GUI_FONT = 17;
        public const int SM_CYSCREEN = 1;

        public readonly static HandleRef NullHandleRef = new HandleRef(null, IntPtr.Zero);

        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        public const int GENERIC_READ = unchecked(((int)0x80000000));
        public const int GENERIC_WRITE = (0x40000000);

        public const int FILE_SHARE_READ = 0x00000001;
        public const int FILE_SHARE_WRITE = 0x00000002;
        public const int FILE_SHARE_DELETE = 0x00000004;

        public const int S_OK = 0x0;
        public const int E_ABORT = unchecked((int)0x80004004);
        public const int E_NOTIMPL = unchecked((int)0x80004001);

        public const int CREATE_ALWAYS = 2;

        public const int FILE_ATTRIBUTE_NORMAL = 0x00000080;

        public const int STARTF_USESTDHANDLES = 0x00000100;

        public const int STD_INPUT_HANDLE = -10;
        public const int STD_OUTPUT_HANDLE = -11;
        public const int STD_ERROR_HANDLE = -12;

        public const int STILL_ACTIVE = 0x00000103;
        public const int SW_HIDE = 0;

        public const int WAIT_OBJECT_0 = 0x00000000;
        public const int WAIT_FAILED = unchecked((int)0xFFFFFFFF);
        public const int WAIT_TIMEOUT = 0x00000102;
        public const int WAIT_ABANDONED = 0x00000080;
        public const int WAIT_ABANDONED_0 = WAIT_ABANDONED;

        // MoveFile Parameter
        public const int MOVEFILE_REPLACE_EXISTING = 0x00000001;

        // copied from winerror.h
        public const int ERROR_CLASS_ALREADY_EXISTS = 1410;
        public const int ERROR_NONE_MAPPED = 1332;
        public const int ERROR_INSUFFICIENT_BUFFER = 122;

        public const int ERROR_INVALID_NAME = 0x7B; //123

        public const int ERROR_PROC_NOT_FOUND = 127;
        public const int ERROR_BAD_EXE_FORMAT = 193;
        public const int ERROR_EXE_MACHINE_TYPE_MISMATCH = 216;
        public const int MAX_PATH = 260;

        [StructLayout(LayoutKind.Sequential)]
        internal class STARTUPINFO
        {
            public int cb;
            public IntPtr lpReserved = IntPtr.Zero;
            public IntPtr lpDesktop = IntPtr.Zero;
            public IntPtr lpTitle = IntPtr.Zero;
            public int dwX = 0;
            public int dwY = 0;
            public int dwXSize = 0;
            public int dwYSize = 0;
            public int dwXCountChars = 0;
            public int dwYCountChars = 0;
            public int dwFillAttribute = 0;
            public int dwFlags = 0;
            public short wShowWindow = 0;
            public short cbReserved2 = 0;
            public IntPtr lpReserved2 = IntPtr.Zero;
            public SafeFileHandle hStdInput = new SafeFileHandle(IntPtr.Zero, false);
            public SafeFileHandle hStdOutput = new SafeFileHandle(IntPtr.Zero, false);
            public SafeFileHandle hStdError = new SafeFileHandle(IntPtr.Zero, false);

            public STARTUPINFO()
            {
                cb = Marshal.SizeOf(this);
            }

            public void Dispose()
            {
                // close the handles created for child process
                if (hStdInput != null && !hStdInput.IsInvalid)
                {
                    hStdInput.Close();
                    hStdInput = null;
                }

                if (hStdOutput != null && !hStdOutput.IsInvalid)
                {
                    hStdOutput.Close();
                    hStdOutput = null;
                }

                if (hStdError != null && !hStdError.IsInvalid)
                {
                    hStdError.Close();
                    hStdError = null;
                }
            }
        }

        //
        // DACL related stuff
        //
        [StructLayout(LayoutKind.Sequential)]
        internal class SECURITY_ATTRIBUTES
        {
            // We don't support ACL's on Silverlight nor on CoreSystem builds in our API's.  
            // But, we need P/Invokes to occasionally take these as parameters.  We can pass null.
            public int nLength = 12;
            public SafeLocalMemHandle lpSecurityDescriptor = new SafeLocalMemHandle(IntPtr.Zero, false);
            public bool bInheritHandle = false;
        }

        [DllImport(ExternDll.Kernel32, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        [ResourceExposure(ResourceScope.None)]
        public static extern bool GetExitCodeProcess(SafeProcessHandle processHandle, out int exitCode);

        [DllImport(ExternDll.Kernel32, CharSet = System.Runtime.InteropServices.CharSet.Ansi, SetLastError = true)]
        [ResourceExposure(ResourceScope.Process)]
        public static extern IntPtr GetStdHandle(int whichHandle);

        [DllImport(ExternDll.Kernel32, CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        [ResourceExposure(ResourceScope.Process)]
        public static extern bool CreatePipe(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe,
            SECURITY_ATTRIBUTES lpPipeAttributes, int nSize);
    }
}
