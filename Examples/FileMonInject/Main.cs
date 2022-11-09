// ProcessMonitor (File: Examples\FileMonInject\Main.cs)
//
// Copyright (c) 2015 Justin Stenning
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using EasyHook;

namespace FileMonInject
{
    public class Main : IEntryPoint
    {
        private readonly FileMon.FileMonInterface _interface;
        private LocalHook _createFileHookA;
        private LocalHook _createFileHookW;
        private readonly Stack<string> _queue = new Stack<string>();

        public Main(
            RemoteHooking.IContext inContext,
            string inChannelName)
        {
            // Connect to host...
            this._interface = RemoteHooking.IpcConnectClient<FileMon.FileMonInterface>(inChannelName);

            this._interface.Ping();
        }

        public void Run(
            RemoteHooking.IContext inContext,
            string inChannelName)
        {
            // Install hook...
            try
            {
                this._createFileHookA = LocalHook.Create(
                    LocalHook.GetProcAddress("kernel32.dll", "CreateFileA"),
                    new CreateFileAsciiDelegate(HookCreateFileA),
                    this);
                this._createFileHookA.ThreadACL.SetExclusiveACL(null);

                this._createFileHookW = LocalHook.Create(
                    LocalHook.GetProcAddress("kernel32.dll", "CreateFileW"),
                    new CreateFileWideDelegate(HookCreateFileW),
                    this);
                this._createFileHookW.ThreadACL.SetExclusiveACL(null);
            }
            catch (Exception exception)
            {
                this._interface.ReportException(exception);
                return;
            }

            this._interface.IsInstalled(RemoteHooking.GetCurrentProcessId());

            RemoteHooking.WakeUpProcess();

            // Wait for host process termination...
            try
            {
                while (true)
                {
                    Thread.Sleep(500);

                    List<string> package = new List<string>();

                    lock (this._queue)
                    {
                        package.AddRange(this._queue.ToArray());
                        this._queue.Clear();
                    }

                    // Transmit newly monitored file accesses...
                    if (package.Count > 0)
                    {
                        this._interface.OnCreateFile(
                            RemoteHooking.GetCurrentProcessId(),
                            package.ToArray());
                    }
                    else
                    {
                        this._interface.Ping();
                    }
                }
            }
            catch
            {
                // Ping() will raise an exception if host is unreachable
            }
        }

        [UnmanagedFunctionPointer(
            CallingConvention.StdCall,
            CharSet = CharSet.Ansi,
            SetLastError = true)]
        private delegate IntPtr CreateFileAsciiDelegate(
            string inFileName,
            uint inDesiredAccess,
            uint inShareMode,
            IntPtr inSecurityAttributes,
            uint inCreationDisposition,
            uint inFlagsAndAttributes,
            IntPtr inTemplateFile);

        [UnmanagedFunctionPointer(
            CallingConvention.StdCall,
            CharSet = CharSet.Unicode,
            SetLastError = true)]
        private delegate IntPtr CreateFileWideDelegate(
            string inFileName,
            uint inDesiredAccess,
            uint inShareMode,
            IntPtr inSecurityAttributes,
            uint inCreationDisposition,
            uint inFlagsAndAttributes,
            IntPtr inTemplateFile);

        [DllImport(
            "kernel32.dll",
            CharSet = CharSet.Ansi,
            SetLastError = true,
            CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr CreateFileA(
            [MarshalAs(UnmanagedType.LPWStr)] string inFileName,
            uint inDesiredAccess,
            uint inShareMode,
            IntPtr inSecurityAttributes,
            uint inCreationDisposition,
            uint inFlagsAndAttributes,
            IntPtr inTemplateFile);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateFileW(
            [MarshalAs(UnmanagedType.LPWStr)] string filename,
            [MarshalAs(UnmanagedType.U4)] FileAccess access,
            [MarshalAs(UnmanagedType.U4)] FileShare share,
            IntPtr securityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
            [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
            IntPtr templateFile);

        private static IntPtr HookCreateFileA(
            string inFileName,
            uint inDesiredAccess,
            uint inShareMode,
            IntPtr inSecurityAttributes,
            uint inCreationDisposition,
            uint inFlagsAndAttributes,
            IntPtr inTemplateFile)
        {
            try
            {
                Main This = (Main)HookRuntimeInfo.Callback;

                lock (This._queue)
                {
                    This._queue.Push("[" + RemoteHooking.GetCurrentProcessId() + ":" +
                                     RemoteHooking.GetCurrentThreadId() + "]: \"" + inFileName + "\"");
                }
            }
            catch
            {
                // Ignored
            }

            return CreateFileA(
                inFileName,
                inDesiredAccess,
                inShareMode,
                inSecurityAttributes,
                inCreationDisposition,
                inFlagsAndAttributes,
                inTemplateFile);
        }

        private static IntPtr HookCreateFileW(
            string inFileName,
            uint inDesiredAccess,
            uint inShareMode,
            IntPtr inSecurityAttributes,
            uint inCreationDisposition,
            uint inFlagsAndAttributes,
            IntPtr inTemplateFile)
        {
            try
            {
                Main This = (Main)HookRuntimeInfo.Callback;

                lock (This._queue)
                {
                    This._queue.Push("[" + RemoteHooking.GetCurrentProcessId() + ":" +
                                     RemoteHooking.GetCurrentThreadId() + "]: \"" + inFileName + "\"");
                }
            }
            catch
            {
                // Ignored
            }

            return CreateFileW(
                inFileName,
                (FileAccess)inDesiredAccess,
                (FileShare)inShareMode,
                inSecurityAttributes,
                (FileMode)inCreationDisposition,
                (FileAttributes)inFlagsAndAttributes,
                inTemplateFile);
        }
    }
}
