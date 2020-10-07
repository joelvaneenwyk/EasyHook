using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;

namespace EasyHook
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Runtime.InteropServices;

    #region WIN32 API
    class Win32
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SECURITY_ATTRIBUTES
        {
            public Int32 nLength;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public Int32 dwProcessId;
            public Int32 dwThreadId;
        }

        [Flags]
        public enum DuplicateOptions : uint
        {
            DUPLICATE_CLOSE_SOURCE = (0x00000001),// Closes the source handle. This occurs regardless of any error status returned.
            DUPLICATE_SAME_ACCESS = (0x00000002), //Ignores the dwDesiredAccess parameter. The duplicate handle has the same access as the source handle.
        }

        [DllImport("kernel32.dll")]
        public static extern Boolean CreatePipe(
            out IntPtr hReadPipe,
            out IntPtr hWritePipe,
            ref SECURITY_ATTRIBUTES lpPipeAttributes,
            uint nSize);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean DuplicateHandle(
            IntPtr hSourceProcessHandle,
            IntPtr hSourceHandle,
            IntPtr hTargetProcessHandle,
            out IntPtr lpTargetHandle,
            UInt32 dwDesiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
            UInt32 dwOptions);

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto)]
        public static extern IntPtr CreateEvent(IntPtr lpEventAttributes,
            [In, MarshalAs(UnmanagedType.Bool)] Boolean bManualReset,
            [In, MarshalAs(UnmanagedType.Bool)] Boolean bIntialState,
            [In, MarshalAs(UnmanagedType.BStr)] string lpName);

        [DllImport("kernel32.dll")]
        public static extern void SetLastError(Int32 dwErrCode);

        [DllImport("kernel32.dll")]
        public static extern bool SetEvent(IntPtr hEvent);

        [DllImport("kernel32.dll")]
        public static extern Boolean WriteFile(
            IntPtr hFile,
            Byte[] lpBuffer,
            UInt32 nNumberOfBytesToWrite,
            out UInt32 lpNumberOfBytesWritten,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadFile(
            IntPtr hFile,
            [Out] Byte[] lpBuffer,
            UInt32 nNumberOfBytesToRead,
            out UInt32 lpNumberOfBytesRead,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll")]
        public static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            Boolean bInheritHandles,
            UInt32 dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool TerminateProcess(IntPtr hProcess, UInt32 uExitCode);

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", EntryPoint = "PeekNamedPipe", SetLastError = true)]
        public static extern bool PeekNamedPipe(
            IntPtr handle,
            Byte[] buffer,
            UInt32 nBufferSize,
            ref UInt32 bytesRead,
            ref UInt32 bytesAvail,
            ref UInt32 BytesLeftThisMessage);

        [DllImport("kernel32.dll", EntryPoint = "WaitForMultipleObjects", SetLastError = true)]
        public static extern int WaitForMultipleObjects(
            UInt32 nCount,
            IntPtr[] lpHandles,
            Boolean fWaitAll,
            UInt32 dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean GetExitCodeProcess(IntPtr hProcess, out Int32 lpExitCode);
    }
    #endregion

    public class ProcessRedirector : IDisposable
    {
        #region Internal Members
        private System.Threading.Thread m_hThread; // thread to receive the output of the child process
        private IntPtr m_hEvtStop; // event to notify the redir thread to exit
        private Int32 m_dwThreadId; // id of the redir thread
        private Int32 m_resultCode; // returned result code of the process
        private UInt32 m_dwWaitTime; // wait time to check the status of the child process

        protected IntPtr m_hStdinWrite; // write end of child's stdin pipe
        protected IntPtr m_hStdoutRead; // read end of child's stdout pipe
        protected IntPtr m_hStderrRead; // read end of child's stderr pipe
        protected IntPtr m_hChildProcess;
        #endregion

        public int ProcessId => this.m_hChildProcess.ToInt32();

        #region Constructor
        public ProcessRedirector()
        {
            this.m_hStdinWrite = IntPtr.Zero;
            this.m_hStdoutRead = IntPtr.Zero;
            this.m_hStderrRead = IntPtr.Zero;
            this.m_hChildProcess = IntPtr.Zero;
            this.m_hThread = null;
            this.m_hEvtStop = IntPtr.Zero;
            this.m_dwThreadId = 0;
            this.m_dwWaitTime = 100;
            this.m_resultCode = -1;
        }
        #endregion

        #region Finalizer
        /// <summary>
        /// NOTE: Leave out the finalizer altogether if this class doesn't 
        /// own unmanaged resources itself, but leave the other methods
        /// exactly as they are. 
        /// </summary>
        ~ProcessRedirector()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }
        #endregion

        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // The bulk of the clean-up code is implemented in Dispose(bool)
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
            }

            // free native resources if there are any.
            Close(true);
        }
        #endregion

        #region Property: ResultCode
        /// <summary>
        /// Access the return code from the spawned process
        /// </summary>
        public Int32 ResultCode
        {
            get { return this.m_resultCode; }
        }
        #endregion

        #region protected LaunchChild
        protected bool LaunchChild(string pathToApp, string workingDirectory, string args, IntPtr hStdOut, IntPtr hStdIn, IntPtr hStdErr)
        {
            const Int16 SW_HIDE = 0;
            const Int32 STARTF_USESTDHANDLES = 0x100;
            const Int32 STARTF_USESHOWWINDOW = 0x1;
            const uint CREATE_NEW_CONSOLE = 0x00000010;

            // Set up the start up info struct.
            Win32.STARTUPINFO si = new Win32.STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            si.hStdOutput = hStdOut;
            si.hStdInput = hStdIn;
            si.hStdError = hStdErr;
            si.wShowWindow = SW_HIDE;
            si.dwFlags = STARTF_USESTDHANDLES | STARTF_USESHOWWINDOW;

            // Note that dwFlags must include STARTF_USESHOWWINDOW if we
            // use the wShowWindow flags. This also assumes that the
            // CreateProcess() call will use CREATE_NEW_CONSOLE.
            Win32.PROCESS_INFORMATION pi = new Win32.PROCESS_INFORMATION();

            // Launch the child process.
            if (!Win32.CreateProcess(pathToApp, args, IntPtr.Zero, IntPtr.Zero, true, CREATE_NEW_CONSOLE, IntPtr.Zero, workingDirectory, ref si, out pi))
                return false;

            this.m_hChildProcess = pi.hProcess;

            // Close any non-useful handles
            Win32.CloseHandle(pi.hThread);
            return true;
        }
        #endregion

        #region protected DestroyHandle
        protected void DestroyHandle(ref IntPtr rhObject)
        {
            if (rhObject == IntPtr.Zero)
                return;

            Win32.CloseHandle(rhObject);
            rhObject = IntPtr.Zero;
        }
        #endregion

        #region private InternalClose
        private void InternalClose(bool getExitCode)
        {
            if (getExitCode && this.m_hChildProcess != IntPtr.Zero)
            {
                // Snag the exit code before it's gone
                if (!Win32.GetExitCodeProcess(this.m_hChildProcess, out this.m_resultCode))
                {
                    this.m_resultCode = -1;
                }
            }

            DestroyHandle(ref this.m_hEvtStop);
            DestroyHandle(ref this.m_hChildProcess);
            DestroyHandle(ref this.m_hStdinWrite);
            DestroyHandle(ref this.m_hStdoutRead);
            DestroyHandle(ref this.m_hStderrRead);

            this.m_dwThreadId = 0;
        }
        #endregion

        #region Stdout/Stderr redirection processing
        delegate void RedirectDelegate(string msg);

        private int InternalRedirect(IntPtr hPipeRead, RedirectDelegate del)
        {
            const int ERROR_BROKEN_PIPE = 109;
            const int ERROR_NO_DATA = 232;

            for (; ; )
            {
                uint bytesRead = 0;
                uint dwAvail = 0;
                uint bytesLeft = 0;
                if (!Win32.PeekNamedPipe(hPipeRead, null, 0, ref bytesRead, ref dwAvail, ref bytesLeft))
                    break; // error

                if (dwAvail == 0)
                {
                    // no data available
                    return 1;
                }

                byte[] szOutput = new byte[dwAvail];
                if (!Win32.ReadFile(hPipeRead, szOutput, dwAvail, out bytesRead, IntPtr.Zero) || bytesRead == 0)
                    break; // error, the child might have ended

                del(Encoding.ASCII.GetString(szOutput, 0, (int)bytesRead));
            }

            int dwError = Marshal.GetLastWin32Error();
            if (dwError == ERROR_BROKEN_PIPE || // pipe has been ended
                dwError == ERROR_NO_DATA)       // pipe closing in progress
            {
                return 0;   // child process ended
            }

            WriteStdError("Read stdout pipe error\r\n");
            return -1;      // os error
        }

        /// <summary>
        /// redirect the child process's stdout:
        /// return: 1: no more data, 0: child terminated, -1: os error
        /// </summary>
        /// <returns></returns>
        protected virtual int RedirectStdout()
        {
            return InternalRedirect(this.m_hStdoutRead, WriteStdOut);
        }

        /// <summary>
        /// redirect the child process's stderr:
        /// return: 1: no more data, 0: child terminated, -1: os error
        /// </summary>
        /// <returns></returns>
        protected virtual int RedirectStderr()
        {
            return InternalRedirect(this.m_hStderrRead, WriteStdError);
        }

        /// <summary>
        /// Combined standard output
        /// </summary>
        public string StandardOutput;


        /// <summary>
        /// Combined error output
        /// </summary>
        public string StandardError;

        /// <summary>
        /// Callback for data received
        /// </summary>
        public event RemoteHookProcess.OutputReceivedEventHandler ErrorDataReceived;

        /// <summary>
        /// Support for asynchronously reading streams
        /// </summary>
        public event RemoteHookProcess.OutputReceivedEventHandler OutputDataReceived;

        /// <summary>
        /// Main thread function
        /// </summary>
        protected void OutputThread()
        {
            const int WAIT_OBJECT_0 = 0;

            IntPtr[] aHandles = new IntPtr[2];
            aHandles[0] = this.m_hChildProcess;
            aHandles[1] = this.m_hEvtStop;

            bool exitNormally = false;
            for (; ; )
            {
                // redirect stdout till there's no more data.
                int nRet;

                nRet = RedirectStdout();
                if (nRet < 0)
                    break;

                nRet = RedirectStderr();
                if (nRet < 0)
                    break;

                // check if the child process has terminated.
                int dwRc = Win32.WaitForMultipleObjects(2, aHandles, false, this.m_dwWaitTime);
                if (WAIT_OBJECT_0 == dwRc)      // the child process ended
                {
                    RedirectStdout();
                    RedirectStderr();
                    exitNormally = true;
                    break;
                }

                if (WAIT_OBJECT_0 + 1 == dwRc)  // m_hEvtStop was signalled
                {
                    Win32.TerminateProcess(this.m_hChildProcess, 0xFFFFFFFF);
                    break;
                }
            }

            // close handles
            InternalClose(exitNormally);
        }
        #endregion

        #region virtual WriteStdOut
        /// <summary>
        /// Override to handle processing data written to stdout by the process
        /// </summary>
        /// <param name="outputStr"></param>
        protected virtual void WriteStdOut(string outputStr)
        {
            this.StandardOutput += outputStr;

            // To avoid ---- between remove handler and raising the event
            RemoteHookProcess.OutputReceivedEventHandler outputDataReceived = OutputDataReceived;
            if (outputDataReceived != null)
            {
                OutputReceivedEventArgs e = new OutputReceivedEventArgs(outputStr);
                outputDataReceived(this, e); // Call back to user informing data is available.
            }
            else
            {
                Console.Out.Write($"[REDIRECTOR] {outputStr}");
            }
        }
        #endregion

        #region virtual WriteStdError
        /// <summary>
        /// Override to handle processing data written to stderr by the process
        /// </summary>
        /// <param name="errorStr"></param>
        protected virtual void WriteStdError(string errorStr)
        {
            this.StandardError += errorStr;

            // To avoid ---- between remove handler and raising the event
            RemoteHookProcess.OutputReceivedEventHandler outputDataReceived = ErrorDataReceived;
            if (outputDataReceived != null)
            {
                OutputReceivedEventArgs e = new OutputReceivedEventArgs(errorStr);
                outputDataReceived(this, e); // Call back to user informing data is available.
            }
            else
            {
                Console.Error.Write($"[REDIRECTOR] {errorStr}");
            }

        }
        #endregion

        #region Open
        private IntPtr _hStdoutReadTmp = IntPtr.Zero; // parent stdout read handle
        private IntPtr _hStderrReadTmp = IntPtr.Zero; // parent stderr read handle
        private IntPtr _hStdinWriteTmp = IntPtr.Zero; // parent stdin write handle

        public IntPtr StandardWriteHandle = IntPtr.Zero; // child stdout write handle
        public IntPtr StandardErrorWriteHandle = IntPtr.Zero;
        public IntPtr StandardInputReadHandle = IntPtr.Zero; // child stdin read handle

        /// <summary>
        /// Initialize and spawn a process
        /// </summary>
        public bool PreLaunch()
        {
            Close(true);

            this._hStdoutReadTmp = IntPtr.Zero; // parent stdout read handle
            this._hStderrReadTmp = IntPtr.Zero; // parent stderr read handle
            this.StandardWriteHandle = IntPtr.Zero; // child stdout write handle
            this.StandardErrorWriteHandle = IntPtr.Zero;
            this._hStdinWriteTmp = IntPtr.Zero; // parent stdin write handle
            this.StandardInputReadHandle = IntPtr.Zero; // child stdin read handle

            // Set up the security attributes struct.
            Win32.SECURITY_ATTRIBUTES sa = new Win32.SECURITY_ATTRIBUTES();
            sa.nLength = Marshal.SizeOf(sa);
            sa.lpSecurityDescriptor = IntPtr.Zero;
            sa.bInheritHandle = true;
            this.m_resultCode = -1;

            bool setupOk = false;
            try
            {
                // Create a child stdout pipe.
                if (!Win32.CreatePipe(out this._hStdoutReadTmp, out this.StandardWriteHandle, ref sa, 0))
                    return false;

                // Create a child stderr pipe.
                if (!Win32.CreatePipe(out this._hStderrReadTmp, out this.StandardErrorWriteHandle, ref sa, 0))
                    return false;

                // Create a child stdin pipe.
                if (!Win32.CreatePipe(out this.StandardInputReadHandle, out this._hStdinWriteTmp, ref sa, 0))
                    return false;

                // Create new stdout read handle, stderr read handle and the stdin write handle.
                // Set the inheritance properties to FALSE. Otherwise, the child
                // inherits the these handles; resulting in non-closeable
                // handles to the pipes being created.
                if (!Win32.DuplicateHandle(
                    Win32.GetCurrentProcess(),
                    this._hStdoutReadTmp,
                    Win32.GetCurrentProcess(),
                    out this.m_hStdoutRead, 0,
                    false,
                    (uint)Win32.DuplicateOptions.DUPLICATE_SAME_ACCESS))
                    return false;

                if (!Win32.DuplicateHandle(
                    Win32.GetCurrentProcess(),
                    this._hStderrReadTmp,
                    Win32.GetCurrentProcess(),
                    out this.m_hStderrRead, 0,
                    false,
                    (uint)Win32.DuplicateOptions.DUPLICATE_SAME_ACCESS))
                    return false;

                if (!Win32.DuplicateHandle(
                    Win32.GetCurrentProcess(),
                    this._hStdinWriteTmp,
                    Win32.GetCurrentProcess(),
                    out this.m_hStdinWrite, 0,
                    false,
                    (uint)Win32.DuplicateOptions.DUPLICATE_SAME_ACCESS))
                    return false;

                // Close inheritable copies of the handles we do not want to be inherited.
                DestroyHandle(ref this._hStdoutReadTmp);
                DestroyHandle(ref this._hStderrReadTmp);
                DestroyHandle(ref this._hStdinWriteTmp);
            }
            finally
            {
                // ignore
            }

            return true;
        }

        /// <summary>
        /// Initialize and spawn a process
        /// </summary>
        public bool PostLaunch(IntPtr processId)
        {
            bool setupOk = false;
            try
            {
                this.m_hChildProcess = processId;

                // Child is launched. Close the parents copy of those pipe
                // handles that only the child should have open.
                // Make sure that no handles to the write end of the stdout pipe
                // are maintained in this process or else the pipe will not
                // close when the child process exits and ReadFile will hang.
                DestroyHandle(ref this.StandardWriteHandle);
                DestroyHandle(ref this.StandardInputReadHandle);
                DestroyHandle(ref this.StandardErrorWriteHandle);

                // Launch a thread to receive output from the child process.
                this.m_hEvtStop = Win32.CreateEvent(IntPtr.Zero, true, false, null);

                try
                {
                    this.m_hThread = new System.Threading.Thread(OutputThread)
                    {
                        Name = "StdOutErr Processor"
                    };
                }
                catch
                {
                    return false;
                }

                this.m_hThread.Start();
                this.m_dwThreadId = this.m_hThread.ManagedThreadId;
                setupOk = true;
            }
            finally
            {
                if (!setupOk)
                {
                    Int32 dwOsErr = Marshal.GetLastWin32Error();
                    if (dwOsErr != 0)
                    {
                        WriteStdError("Redirect console error: " + dwOsErr.ToString("x8") + "\r\n");
                    }

                    DestroyHandle(ref this._hStdoutReadTmp);
                    DestroyHandle(ref this._hStderrReadTmp);
                    DestroyHandle(ref this.StandardWriteHandle);
                    DestroyHandle(ref this.StandardErrorWriteHandle);
                    DestroyHandle(ref this._hStdinWriteTmp);
                    DestroyHandle(ref this.StandardInputReadHandle);
                    Close(true);

                    Win32.SetLastError(dwOsErr);
                }
            }

            return true;
        }

        /// <summary>
        /// Initialize and spawn a process
        /// </summary>
        /// <param name="pathToApp">Absolute path to the executable to spawn</param>
        /// <param name="workingDirectory">The working directory for the process</param>
        /// <param name="args">The full set of command line arguments</param>
        /// <returns>True if everything went okay</returns>
        public bool Open(string pathToApp, string workingDirectory, string args)
        {
            Close(true);

            IntPtr hStdoutReadTmp = IntPtr.Zero; // parent stdout read handle
            IntPtr hStderrReadTmp = IntPtr.Zero; // parent stderr read handle
            IntPtr hStdoutWrite = IntPtr.Zero; // child stdout write handle
            IntPtr hStderrWrite = IntPtr.Zero;
            IntPtr hStdinWriteTmp = IntPtr.Zero; // parent stdin write handle
            IntPtr hStdinRead = IntPtr.Zero; // child stdin read handle

            // Set up the security attributes struct.
            Win32.SECURITY_ATTRIBUTES sa = new Win32.SECURITY_ATTRIBUTES();
            sa.nLength = Marshal.SizeOf(sa);
            sa.lpSecurityDescriptor = IntPtr.Zero;
            sa.bInheritHandle = true;
            this.m_resultCode = -1;

            bool setupOk = false;
            try
            {
                // Create a child stdout pipe.
                if (!Win32.CreatePipe(out hStdoutReadTmp, out hStdoutWrite, ref sa, 0))
                    return false;

                // Create a child stderr pipe.
                if (!Win32.CreatePipe(out hStderrReadTmp, out hStderrWrite, ref sa, 0))
                    return false;

                // Create a child stdin pipe.
                if (!Win32.CreatePipe(out hStdinRead, out hStdinWriteTmp, ref sa, 0))
                    return false;

                // Create new stdout read handle, stderr read handle and the stdin write handle.
                // Set the inheritance properties to FALSE. Otherwise, the child
                // inherits the these handles; resulting in non-closeable
                // handles to the pipes being created.
                if (!Win32.DuplicateHandle(Win32.GetCurrentProcess(), hStdoutReadTmp, Win32.GetCurrentProcess(), out this.m_hStdoutRead, 0, false, (uint)Win32.DuplicateOptions.DUPLICATE_SAME_ACCESS))
                    return false;

                if (!Win32.DuplicateHandle(Win32.GetCurrentProcess(), hStderrReadTmp, Win32.GetCurrentProcess(), out this.m_hStderrRead, 0, false, (uint)Win32.DuplicateOptions.DUPLICATE_SAME_ACCESS))
                    return false;

                if (!Win32.DuplicateHandle(Win32.GetCurrentProcess(), hStdinWriteTmp, Win32.GetCurrentProcess(), out this.m_hStdinWrite, 0, false, (uint)Win32.DuplicateOptions.DUPLICATE_SAME_ACCESS))
                    return false;

                // Close inheritable copies of the handles we do not want to be inherited.
                DestroyHandle(ref hStdoutReadTmp);
                DestroyHandle(ref hStderrReadTmp);
                DestroyHandle(ref hStdinWriteTmp);

                // launch the child process
                string appFilename = System.IO.Path.GetFileName(pathToApp);
                string cmdArgs = "\"" + appFilename + "\" " + args;
                if (!LaunchChild(pathToApp, workingDirectory, cmdArgs, hStdoutWrite, hStdinRead, hStderrWrite))
                    return false;

                // Child is launched. Close the parents copy of those pipe
                // handles that only the child should have open.
                // Make sure that no handles to the write end of the stdout pipe
                // are maintained in this process or else the pipe will not
                // close when the child process exits and ReadFile will hang.
                DestroyHandle(ref hStdoutWrite);
                DestroyHandle(ref hStdinRead);
                DestroyHandle(ref hStderrWrite);

                // Launch a thread to receive output from the child process.
                this.m_hEvtStop = Win32.CreateEvent(IntPtr.Zero, true, false, null);

                try
                {
                    this.m_hThread = new System.Threading.Thread(OutputThread);
                    this.m_hThread.Name = "StdOutErr Processor " + appFilename;
                }
                catch
                {
                    return false;
                }

                this.m_hThread.Start();
                this.m_dwThreadId = this.m_hThread.ManagedThreadId;
                setupOk = true;
            }
            finally
            {
                if (!setupOk)
                {
                    Int32 dwOsErr = Marshal.GetLastWin32Error();
                    if (dwOsErr != 0)
                    {
                        WriteStdError("Redirect console error: " + dwOsErr.ToString("x8") + "\r\n");
                    }

                    DestroyHandle(ref hStdoutReadTmp);
                    DestroyHandle(ref hStderrReadTmp);
                    DestroyHandle(ref hStdoutWrite);
                    DestroyHandle(ref hStderrWrite);
                    DestroyHandle(ref hStdinWriteTmp);
                    DestroyHandle(ref hStdinRead);
                    Close(true);

                    Win32.SetLastError(dwOsErr);
                }
            }

            return true;
        }
        #endregion

        #region Close
        /// <summary>
        /// Close a process
        /// </summary>
        /// <param name="abort">If true, abort the processing (waiting up to
        /// 5 seconds before terminating), otherwise just wait until it exits</param>
        public virtual void Close(bool abort)
        {
            if (this.m_hThread != null)
            {
                if (abort)
                {
                    // tell the thread to bail
                    Win32.SetEvent(this.m_hEvtStop);
                    if (!this.m_hThread.Join(5000))
                    {
                        try
                        {
                            this.m_hThread.Abort();
                        }
                        catch
                        {
                        }
                    }
                }
                else
                {
                    // wait until the thread exits
                    this.m_hThread.Join();
                }

                this.m_hThread = null;
            }

            InternalClose(false);
        }
        #endregion

        #region SendToStdIn
        /// <summary>
        /// Send the given string of data to the stdin of the spawned process
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public bool SendToStdIn(string str)
        {
            if (this.m_hStdinWrite == IntPtr.Zero)
                return false;

            byte[] strData = Encoding.ASCII.GetBytes(str);

            uint dwWritten;
            return Win32.WriteFile(this.m_hStdinWrite, strData, (uint)strData.Length, out dwWritten, IntPtr.Zero);
        }
        #endregion

        #region SetWaitTime
        /// <summary>
        /// Adjust the waiting time between checks to see if the process
        /// has exited, this is also the time between checks to stdout and
        /// stderr processing.
        /// </summary>
        /// <param name="waitTime"></param>
        public void SetWaitTime(UInt32 waitTime)
        {
            this.m_dwWaitTime = waitTime;
        }
        #endregion
    }
}
