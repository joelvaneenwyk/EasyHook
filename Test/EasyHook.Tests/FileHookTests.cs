// ProcessMonitor (File: Test\EasyHook.Tests\FileHookTests.cs)
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
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Ipc;
using System.Threading;
using FileMon;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EasyHook.Tests
{
    [TestClass]
    public class FileHookTests
    {
        [TestMethod]
        public void HookFileTest()
        {
            string channelName = null;

            const string executableRoot =
                "D:\\Havok\\Perforce\\Support\\Data\\Executables\\2020_2_Stable";

            string targetExe = $"{executableRoot}\\Demo\\Demos\\Demos_x64-vs2017_Debug.exe";

            // This test is optional and just skip if the executable does not exist
            if (!File.Exists(targetExe))
            {
                return;
            }

            IpcServerChannel server = RemoteHooking.IpcCreateServer<FileMonInterface>(
                ref channelName, WellKnownObjectMode.SingleCall);
            FileMonInterface client = RemoteHooking.IpcConnectClient<FileMonInterface>(channelName);

            string injectionLibrary = Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty,
                "FileMonInject.dll");

            Assert.IsFalse(string.IsNullOrEmpty(injectionLibrary));

            RemoteHookProcess remoteProcess = new RemoteHookProcess();
            string output = "";

            remoteProcess.ErrorDataReceived += (sender, argData) =>
            {
                output += argData.Data;
                Console.WriteLine($"[stderr] {argData.Data}");
            };

            remoteProcess.OutputDataReceived += (sender, argData) =>
            {
                output += argData.Data;
                Console.WriteLine($"[stdout] {argData.Data}");
            };

            Assert.IsTrue(
                remoteProcess.Launch(
                    targetExe, "-g Physics/Core/Constraints/Ragdoll -i 1 -nows -nowp -rui 0",
                    0, InjectionOptions.DoNotRequireStrongName,
                    injectionLibrary, injectionLibrary,
                    server.ChannelName),
                "Remote processed failed to launch and inject");

            Thread.Sleep(10000);

            Assert.IsTrue(remoteProcess.IsValid);
            Assert.IsTrue(remoteProcess.WaitForExit() == 0);

            client.Ping();
            Assert.IsTrue(client.GetPaths(remoteProcess.RemoteProcessId).Length > 0);

            Assert.IsTrue(output.Contains("Failed to stat arial.ttf"));
        }
    }
}
