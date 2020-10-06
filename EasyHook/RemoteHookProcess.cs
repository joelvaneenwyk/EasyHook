// EasyHook (File: EasyHook\RemoteHook.cs)
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
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace EasyHook
{
    /// <summary>
    /// Provides data for the <see cref="E:System.Diagnostics.Process.OutputDataReceived" /> and
    /// <see cref="E:System.Diagnostics.Process.ErrorDataReceived" /> events.
    /// </summary>
    public class OutputReceivedEventArgs : EventArgs
    {
        public OutputReceivedEventArgs(string data)
        {
            Data = data;
        }

        /// <summary>
        /// Gets the line of characters that was written to a redirected <see cref="T:System.Diagnostics.Process" />
        /// output stream.
        /// </summary>
        /// <returns>
        /// The line that was written by an associated <see cref="T:System.Diagnostics.Process" /> to its redirected
        /// <see cref="P:System.Diagnostics.Process.StandardOutput" /> or <see cref="P:System.Diagnostics.Process.StandardError" />
        /// stream.
        /// </returns>
        public string Data { get; }
    }

    public class RemoteHookProcess
    {
        public delegate void OutputReceivedEventHandler(object sender, OutputReceivedEventArgs e);

        public SafeFileHandle hStdError;
        public SafeFileHandle hStdOutput;

        public int RemotePID;
        public int RemoteTID;

        public string StandardError;

        public string StandardOutput;
        private AsyncStreamReader _errorReader;

        private AsyncStreamReader _outputReader;
        private StreamReader _standardError;
        private readonly SafeFileHandle _standardErrorReadPipeHandle;

        private StreamReader _standardOutput;

        private readonly SafeFileHandle _standardOutputReadPipeHandle;

        public RemoteHookProcess()
        {
            CreatePipe(out this._standardOutputReadPipeHandle, out this.hStdOutput, false);
            CreatePipe(out this._standardErrorReadPipeHandle, out this.hStdError, false);
        }

        /// <summary>
        /// Start reading error output asynchronously.
        /// </summary>
        public void BeginErrorReadLine()
        {
            Stream s = this._standardError.BaseStream;
            this._errorReader = new AsyncStreamReader(
                this, s, ErrorReadNotifyUser, this._standardError.CurrentEncoding);
            this._errorReader.BeginReadLine();
        }

        /// <summary>
        /// Start reading standard output asynchronously.
        /// </summary>
        public void BeginOutputReadLine()
        {
            Stream s = this._standardOutput.BaseStream;
            this._outputReader = new AsyncStreamReader(
                this, s, OutputReadNotifyUser, this._standardOutput.CurrentEncoding);
            this._outputReader.BeginReadLine();
        }

        public void Create()
        {
            Encoding enc = Console.OutputEncoding;
            this._standardOutput = new StreamReader(
                new FileStream(
                    this._standardOutputReadPipeHandle, FileAccess.Read,
                    4096, false), enc, true, 4096);
            this._standardError = new StreamReader(
                new FileStream(
                    this._standardErrorReadPipeHandle, FileAccess.Read,
                    4096, false), enc, true, 4096);
        }

        public event OutputReceivedEventHandler ErrorDataReceived;

        // Support for asynchronously reading streams  
        public event OutputReceivedEventHandler OutputDataReceived;

        public void Start()
        {
            BeginOutputReadLine();
            BeginErrorReadLine();
        }

        /// <summary>
        /// Finish getting data from standard output and error.
        /// </summary>
        public void WaitForExit()
        {
            this._outputReader.WaitUtilEOF();
            this._errorReader.WaitUtilEOF();
        }

        internal void ErrorReadNotifyUser(string data)
        {
            this.StandardError += data;

            // To avoid ---- between remove handler and raising the event
            OutputReceivedEventHandler outputDataReceived = ErrorDataReceived;
            if (outputDataReceived != null)
            {
                OutputReceivedEventArgs e = new OutputReceivedEventArgs(data);
                outputDataReceived(this, e); // Call back to user informing data is available.
            }
        }

        internal void OutputReadNotifyUser(string data)
        {
            this.StandardOutput += data;

            // To avoid ---- between remove handler and raising the event
            OutputReceivedEventHandler outputDataReceived = OutputDataReceived;
            if (outputDataReceived != null)
            {
                OutputReceivedEventArgs e = new OutputReceivedEventArgs(data);
                outputDataReceived(this, e); // Call back to user informing data is available.
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
