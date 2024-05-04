// EasyHook (File: EasyHook\RemoteHookProcess.cs)
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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace EasyHook
{
    /// <summary>
    /// Provides data for the process callback.
    /// </summary>
    public class OutputReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Basic constructor to copy over the string data.
        /// </summary>
        /// <param name="data"></param>
        public OutputReceivedEventArgs(string data)
        {
            Data = data;
        }

        /// <summary>
        /// Gets the line of characters that was written to a redirected <see cref="RemoteHookProcess" />
        /// output stream.
        /// </summary>
        public string Data { get; }
    }

    /// <summary>
    /// Wrapper around a process ID to handle redirected output.
    /// </summary>
    public class RemoteHookProcess
    {
        /// <summary>
        /// Handler for receiving standard output or error streams from remote process.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void OutputReceivedEventHandler(object sender, OutputReceivedEventArgs e);

        /// <summary>
        /// Handle to the standard error stream.
        /// </summary>
        private SafeFileHandle _standardErrorHandle;

        /// <summary>
        /// Handle to the standard output stream.
        /// </summary>
        private SafeFileHandle _standardOutputHandle;

        /// <summary>
        /// Remote process ID.
        /// </summary>
        public int RemoteProcessId;

        /// <summary>
        /// Remote thread ID.
        /// </summary>
        public int RemoteThreadId;

        /// <summary>
        /// Accumulated standard error output from remote process.
        /// </summary>
        public string StandardError;

        /// <summary>
        /// Accumulated standard output from remote process.
        /// </summary>
        public string StandardOutput;

        private RemoteAsyncStreamReader _errorReader;

        private RemoteAsyncStreamReader _outputReader;

        private StreamReader _standardError;

        private SafeFileHandle _standardErrorReadPipeHandle;

        private StreamReader _standardOutput;

        private SafeFileHandle _standardOutputReadPipeHandle;

        /// <summary>
        /// Start reading error output asynchronously.
        /// </summary>
        public void BeginErrorReadLine()
        {
            if (this._standardError != null)
            {
                Stream s = this._standardError.BaseStream;
                this._errorReader = new RemoteAsyncStreamReader(
                    s, ErrorReadNotifyUser, this._standardError.CurrentEncoding);
                this._errorReader.BeginReadLine();
            }
        }

        /// <summary>
        /// Start reading standard output asynchronously.
        /// </summary>
        public void BeginOutputReadLine()
        {
            if (this._standardOutput != null)
            {
                Stream s = this._standardOutput.BaseStream;
                this._outputReader = new RemoteAsyncStreamReader(
                    s, OutputReadNotifyUser, this._standardOutput.CurrentEncoding);
                this._outputReader.BeginReadLine();
            }
        }

        /// <summary>
        /// Validate that the remote process ID is valid.
        /// </summary>
        public bool IsValid => this.RemoteProcessId != 0;

        private void Reset()
        {
            Kill();

            this.RemoteProcessId = 0;
            this.RemoteThreadId = 0;

            this._standardOutputHandle?.Dispose();
            this._standardOutputHandle = null;

            this._standardErrorHandle?.Dispose();
            this._standardErrorHandle = null;

            this._standardOutputReadPipeHandle?.Dispose();
            this._standardOutputReadPipeHandle = null;

            this._standardErrorReadPipeHandle?.Dispose();
            this._standardErrorReadPipeHandle = null;

            this._standardOutput?.Close();
            this._standardOutput = null;

            this._standardError?.Close();
            this._standardError = null;

            this._outputReader?.CancelOperation();
            this._outputReader?.WaitUtilEOF();
            this._outputReader?.Close();
            this._outputReader = null;

            this._errorReader?.CancelOperation();
            this._errorReader?.WaitUtilEOF();
            this._errorReader?.Close();
            this._errorReader = null;
        }

        /// <summary>
        /// Creates a new process which is started suspended until you call <see cref="RemoteHooking.WakeUpProcess"/>
        /// from within your injected library <c>Run()</c> method. This allows you to hook the target
        /// BEFORE any of its usual code is executed. In situations where a target has debugging and
        /// hook preventions, you will get a chance to block those mechanisms for example...
        /// </summary>
        /// <remarks>
        /// <para>
        /// Please note that this method might fail when injecting into managed processes, especially
        /// when the target is using the CLR hosting API and takes advantage of AppDomains. For example,
        /// the Internet Explorer won't be hookable with this method. In such a case your only options
        /// are either to hook the target with the unmanaged API or to hook it after (non-suspended) creation 
        /// with the usual <see cref="RemoteHooking.Inject(int, InjectionOptions, string, string, object[])"/> method.
        /// </para>
        /// <para>
        /// See <see cref="RemoteHooking.Inject(int, InjectionOptions, string, string, object[])"/> for more information. The exceptions listed here are additional
        /// to the ones listed for <see cref="RemoteHooking.Inject(int, InjectionOptions, string, string, object[])"/>.
        /// </para>
        /// </remarks>
        /// <param name="InEXEPath">
        /// A relative or absolute path to the desired executable.
        /// </param>
        /// <param name="InCommandLine">
        /// Optional command line parameters for process creation.
        /// </param>
        /// <param name="InProcessCreationFlags">
        /// Internally CREATE_SUSPENDED is already passed to CreateProcess(). With this
        /// parameter you can add more flags like DETACHED_PROCESS, CREATE_NEW_CONSOLE or
        /// whatever!
        /// </param>
        /// <param name="InOptions">
        /// A valid combination of options.
        /// </param>
        /// <param name="InLibraryPath_x86">
        /// A partially qualified assembly name or a relative/absolute file path of the 32-bit version of your library. 
        /// For example "MyAssembly, PublicKeyToken=248973975895496" or ".\Assemblies\\MyAssembly.dll". 
        /// </param>
        /// <param name="InLibraryPath_x64">
        /// A partially qualified assembly name or a relative/absolute file path of the 64-bit version of your library. 
        /// For example "MyAssembly, PublicKeyToken=248973975895496" or ".\Assemblies\\MyAssembly.dll". 
        /// </param>
        /// <param name="InPassThruArgs">
        /// A serializable list of parameters being passed to your library entry points <c>Run()</c> and
        /// constructor (see <see cref="IEntryPoint"/>).
        /// </param>
        /// <exception cref="ArgumentException">
        /// The given EXE path could not be found.
        /// </exception>
        private void CreateAndInject(
            string InEXEPath,
            string InCommandLine,
            int InProcessCreationFlags,
            InjectionOptions InOptions,
            string InLibraryPath_x86,
            string InLibraryPath_x64,
            params object[] InPassThruArgs)
        {
            try
            {
                // Make sure there isn't an existing hook in place
                Reset();

                // Standard output pipe
                CreatePipe(
                    out this._standardOutputReadPipeHandle,
                    out this._standardOutputHandle,
                    false);

                // Standard error pipe
                CreatePipe(
                    out this._standardErrorReadPipeHandle,
                    out this._standardErrorHandle,
                    false);

                // Create suspended process
                NativeAPI.RtlCreateSuspendedProcess(
                    InEXEPath,
                    InCommandLine,
                    InProcessCreationFlags,
                    new SafeFileHandle(
                        NativeMethods.GetStdHandle(NativeMethods.STD_INPUT_HANDLE),
                        false),
                    this._standardOutputHandle,
                    this._standardErrorHandle,
                    out this.RemoteProcessId,
                    out this.RemoteThreadId);

                Encoding enc = Console.OutputEncoding;

                this._standardOutput = new StreamReader(
                    new FileStream(
                        this._standardOutputReadPipeHandle, FileAccess.Read,
                        4096, false), 
                    enc, true, 4096);

                this._standardError = new StreamReader(
                    new FileStream(
                        this._standardErrorReadPipeHandle, FileAccess.Read,
                        4096, false), 
                    enc, true, 4096);

                // Start reading before we inject because this will almost certainly wake the processes which
                // may result in missing some output from the target.
                BeginOutputReadLine();
                BeginErrorReadLine();
            }
            catch (Exception)
            {
                // Once we are done trying to kill process go ahead and throw the exception again.
                Reset();
                throw;
            }

            try
            {
                RemoteHooking.InjectEx(
                    NativeAPI.GetCurrentProcessId(),
                    this.RemoteProcessId, this.RemoteThreadId,
                    InjectionNativeOptions.NetDefibrillator,
                    InLibraryPath_x86,
                    InLibraryPath_x64,
                    ((InOptions & InjectionOptions.NoWOW64Bypass) == 0),
                    ((InOptions & InjectionOptions.NoService) == 0),
                    ((InOptions & InjectionOptions.DoNotRequireStrongName) == 0),
                    InPassThruArgs);
            }
            catch (Exception exception)
            {
                // Once we are done trying to kill process go ahead and throw the exception again.
                Reset();
                ErrorReadNotifyUser($"Inject failed: '{exception.Message}'");
                throw;
            }
        }

        /// <summary>
        /// Creates a new process which is started suspended until you call <see cref="RemoteHooking.WakeUpProcess"/>
        /// from within your injected library <c>Run()</c> method. This allows you to hook the target
        /// BEFORE any of its usual code is executed. In situations where a target has debugging and
        /// hook preventions, you will get a chance to block those mechanisms for example...
        /// </summary>
        /// <remarks>
        /// <para>
        /// Please note that this method might fail when injecting into managed processes, especially
        /// when the target is using the CLR hosting API and takes advantage of AppDomains. For example,
        /// the Internet Explorer won't be hookable with this method. In such a case your only options
        /// are either to hook the target with the unmanaged API or to hook it after (non-suspended) creation 
        /// with the usual <see cref="RemoteHooking.Inject(int, InjectionOptions, string, string, object[])">method</see>.
        /// </para>
        /// <para>
        /// See <see cref="RemoteHooking.Inject(int, InjectionOptions, string, string, object[])"/> for more information. The exceptions listed here are additional
        /// to the ones listed for <see cref="RemoteHooking.Inject(int, InjectionOptions, string, string, object[])"/>.
        /// </para>
        /// </remarks>
        /// <param name="InEXEPath">
        /// A relative or absolute path to the desired executable.
        /// </param>
        /// <param name="InCommandLine">
        /// Optional command line parameters for process creation.
        /// </param>
        /// <param name="InProcessCreationFlags">
        /// Internally CREATE_SUSPENDED is already passed to CreateProcess(). With this
        /// parameter you can add more flags like DETACHED_PROCESS, CREATE_NEW_CONSOLE or
        /// whatever!
        /// </param>
        /// <param name="InOptions">
        /// A valid combination of options.
        /// </param>
        /// <param name="InLibraryPath_x86">
        /// A partially qualified assembly name or a relative/absolute file path of the 32-bit version of your library. 
        /// For example "MyAssembly, PublicKeyToken=248973975895496" or ".\Assemblies\\MyAssembly.dll". 
        /// </param>
        /// <param name="InLibraryPath_x64">
        /// A partially qualified assembly name or a relative/absolute file path of the 64-bit version of your library. 
        /// For example "MyAssembly, PublicKeyToken=248973975895496" or ".\Assemblies\\MyAssembly.dll". 
        /// </param>
        /// <param name="InChannelName"></param>
        /// <param name="InAttempts"></param>
        /// <exception cref="ArgumentException">
        /// The given EXE path could not be found.
        /// </exception>
        public bool Launch(
            string InEXEPath,
            string InCommandLine,
            int InProcessCreationFlags,
            InjectionOptions InOptions,
            string InLibraryPath_x86,
            string InLibraryPath_x64,
            string InChannelName,
            int InAttempts = 50)
        {
            bool succeeded = false;

            for (int attemptIndex = 1; attemptIndex < InAttempts && !succeeded; attemptIndex++)
            {
                try
                {
                    CreateAndInject(
                        InEXEPath, InCommandLine,
                        InProcessCreationFlags, InOptions,
                        InLibraryPath_x86, InLibraryPath_x64,
                        InChannelName);

                    OutputReadNotifyUser($"Created and injected process '{this.RemoteProcessId}'");

                    succeeded = true;
                }
                catch
                {
                    OutputReadNotifyUser($"[Attempt #{attemptIndex}] Failed to create and inject.");
                    Reset();
                }
            }

            return succeeded;
        }


        /// <summary>
        /// Support for asynchronously reading streams
        /// </summary>
        public event OutputReceivedEventHandler ErrorDataReceived;

        /// <summary>
        /// Support for asynchronously reading streams
        /// </summary>
        public event OutputReceivedEventHandler OutputDataReceived;

        /// <summary>
        /// Finish getting data from standard output and error.
        /// </summary>
        public uint WaitForExit()
        {
            uint exitCode = 0;

            while (IsValid && IsProcessAlive)
            {
                Thread.Sleep(50);
            }

            IntPtr h = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.QueryInformation, true, this.RemoteProcessId);

            if (h != IntPtr.Zero)
            {
                NativeMethods.GetExitCodeProcess(h, out exitCode);
                NativeMethods.CloseHandle(h);
            }

            return exitCode;
        }

        /// <summary>
        /// Checks if given process is still alive
        /// </summary>
        /// <returns>true if process is alive, false if not</returns>
        public bool IsProcessAlive
        {
            get
            {
                bool isAlive = false;

                if (IsValid)
                {
                    IntPtr h = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.QueryInformation, true, this.RemoteProcessId);

                    if (h == IntPtr.Zero)
                        return false;

                    uint code;
                    bool b = NativeMethods.GetExitCodeProcess(h, out code);
                    NativeMethods.CloseHandle(h);

                    if (b)
                    {
                        /* STILL_ACTIVE  */
                        isAlive = (code == 259);
                    }
                }

                return isAlive;

            }
        }

        /// <summary>
        /// Kill the remote process if it's alive.
        /// </summary>
        public void Kill()
        {
            if (IsProcessAlive)
            {
                IntPtr h = NativeMethods.OpenProcess(
                    NativeMethods.ProcessAccessFlags.QueryInformation, true, this.RemoteProcessId);

                if (h != IntPtr.Zero)
                {
                    NativeMethods.TerminateProcess(h, 666);
                }

                NativeMethods.CloseHandle(h);
            }

            if (IsProcessAlive)
            {
                try
                {
                    Process.GetProcessById(this.RemoteProcessId).Kill();
                }
                catch (Exception)
                {
                    // Many reasons this can fail so we are just trying our best to kill the process if
                    // it fails along the way during injection.
                }
            }
        }

        internal void ErrorReadNotifyUser(string data)
        {
            lock (this)
            {
                this.StandardError += $"{data}{Environment.NewLine}";
            }

            OutputReceivedEventHandler outputDataReceived = ErrorDataReceived;
            if (outputDataReceived != null)
            {
                outputDataReceived(
                    this,
                    new OutputReceivedEventArgs(data));
            }
        }

        internal void OutputReadNotifyUser(string data)
        {
            lock (this)
            {
                this.StandardOutput += $"{data}{Environment.NewLine}";
            }

            OutputReceivedEventHandler outputDataReceived = OutputDataReceived;
            if (outputDataReceived != null)
            {
                outputDataReceived(
                    this,
                    new OutputReceivedEventArgs(data));
            }
        }

        /// <summary>
        /// Using synchronous Anonymous pipes for process input/output redirection means we would end up
        /// wasting a worker threadpool thread per pipe instance. Overlapped pipe IO is desirable, since
        /// it will take advantage of the NT IO completion port infrastructure. But we can't really use
        /// Overlapped I/O for process input/output as it would break Console apps (managed Console class
        /// methods such as WriteLine as well as native CRT functions like printf) which are making an
        /// assumption that the console standard handles (obtained via GetStdHandle()) are opened
        /// for synchronous I/O and hence they can work fine with ReadFile/WriteFile synchronously!
        /// </summary>
        /// <param name="parentHandle"></param>
        /// <param name="childHandle"></param>
        /// <param name="parentInputs"></param>
        private void CreatePipe(
            out SafeFileHandle parentHandle, out SafeFileHandle childHandle, bool parentInputs)
        {
            NativeMethods.SECURITY_ATTRIBUTES securityAttributesParent =
                new NativeMethods.SECURITY_ATTRIBUTES
                {
                    bInheritHandle = true
                };

            SafeFileHandle hTmp = null;
            try
            {
                if (parentInputs)
                {
                    CreatePipeWithSecurityAttributes(
                        out childHandle, out hTmp, securityAttributesParent, 0);
                }
                else
                {
                    CreatePipeWithSecurityAttributes(
                        out hTmp,
                        out childHandle,
                        securityAttributesParent,
                        0);
                }

                // Duplicate the parent handle to be non-inheritable so that the child process 
                // doesn't have access. This is done for correctness sake, exact reason is unclear.
                // One potential theory is that child process can do something brain dead like 
                // closing the parent end of the pipe and there by getting into a blocking situation
                // as parent will not be draining the pipe at the other end anymore. 
                if (!NativeMethods.DuplicateHandle(
                    new HandleRef(this, NativeMethods.GetCurrentProcess()),
                    hTmp,
                    new HandleRef(this, NativeMethods.GetCurrentProcess()),
                    out parentHandle,
                    0,
                    false,
                    NativeMethods.DUPLICATE_SAME_ACCESS))
                {
                    throw new Win32Exception();
                }
            }
            finally
            {
                if (hTmp != null && !hTmp.IsInvalid)
                {
                    hTmp.Close();
                }
            }
        }

        private static void CreatePipeWithSecurityAttributes(
            out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe,
            NativeMethods.SECURITY_ATTRIBUTES lpPipeAttributes, int nSize)
        {
            bool ret = NativeMethods.CreatePipe(
                out hReadPipe, out hWritePipe, lpPipeAttributes, nSize);
            if (!ret || hReadPipe.IsInvalid || hWritePipe.IsInvalid)
            {
                throw new Win32Exception();
            }
        }
    }
}
