// ProcessMonitor (File: Examples\ProcessMonitor\DemoInterface.cs)
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
using System.Windows.Forms;

namespace ProcessMonitor
{
    internal class MonitorEntry
    {
        public readonly String Access;
        public readonly DateTime Timestamp = DateTime.Now;
        public readonly Int32 ClientPID;

        public MonitorEntry(
            Int32 InClientPID,
            String InAccess)
        {
            ClientPID = InClientPID;
            Access = InAccess;
        }
    }

    /*
     * This is the class where our clients will connect to!
     * 
     * Please note that setting any breakpoint here will cause the related
     * thread in the client process to block until you continue execution!
     * So don't wonder if your browser (for example) hangs when you set a 
     * breakpoint ;-)... Let's say you can debug a part of the code the client
     * is executing (that's not technically correct)
     * 
     * In Windows 2000 debugging the following seems to cause problems. 
     */
    public class DemoInterface : MarshalByRefObject
    {
        public void ReportError(
            Int32 InClientPID,
            Exception e)
        {
            MessageBox.Show(e.ToString(), "A client process (" + InClientPID + ") has reported an error...", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
        }

        public bool Ping(Int32 InClientPID)
        {
            /*
             * We should just check if the client is still in our list
             * of hooked processes...
             */
            lock (Form1.ProcessList)
            {
                return Form1.HookedProcesses.Contains(InClientPID);
            }
        }

        public void OnCreateFile(
            Int32 InClientPID,
            String[] InFileNames)
        {
            if (Form1.IsMonitoring)
            {
                lock (Form1.MonitorQueue)
                {
                    for (int i = 0; i < InFileNames.Length; i++)
                    {
                        Form1.MonitorQueue.Enqueue(new MonitorEntry(
                                InClientPID,
                                "[FILE]: \"" + InFileNames[i] + "\""
                            ));
                    }
                }
            }
        }
    }
}
