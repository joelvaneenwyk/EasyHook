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

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Ipc;
using FileMon;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EasyHook.Tests
{
    [TestClass]
    public class FileHookTests
    {
        /// <summary>
        /// This is assigned automatically.
        /// </summary>
        /// <summary>
        /// This is automatically set by unit testing and must be public.
        /// </summary>
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public TestContext TestContext
        {
            get;
            set;
        }

        private const string _testExecutablePath = "D:\\Havok\\Perforce\\Support\\Data\\Executables\\2020_2_Stable";

        private static readonly string _testDemoExecutablePath = $"{_testExecutablePath}\\Demo\\Demos\\Demos_x64-vs2017_Debug.exe";

        private void ExecuteTest()
        {
            // This test is optional and just skip if the executable does not exist
            if (!File.Exists(_testDemoExecutablePath))
            {
                return;
            }

            RemoteHookProcess remoteProcess = new RemoteHookProcess();
            string output = "";

            remoteProcess.ErrorDataReceived += (sender, argData) =>
            {
                lock (remoteProcess)
                {
                    output += $"{argData.Data}\n";
                    TestContext.WriteLine($"[stderr] {argData.Data}");
                }
            };

            remoteProcess.OutputDataReceived += (sender, argData) =>
            {
                lock (remoteProcess)
                {
                    output += $"{argData.Data}\n";
                    TestContext.WriteLine($"[stdout] {argData.Data}");
                }
            };

            string channelName = null;
            IpcServerChannel server = RemoteHooking.IpcCreateServer<FileMonInterface>(
                ref channelName, WellKnownObjectMode.Singleton);

            string injectionLibrary = Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty,
                "FileMonInject.dll");
            Assert.IsFalse(string.IsNullOrEmpty(injectionLibrary), "Injection library missing.");

            bool launchResult = remoteProcess.Launch(
                _testDemoExecutablePath,
                "-g Physics/Core/Constraints/Ragdoll -i 1 -nows -nowp -rui 0",
                0,
                InjectionOptions.DoNotRequireStrongName,
                injectionLibrary,
                injectionLibrary,
                server.ChannelName);
            Assert.IsTrue(launchResult, "Remote processed failed to launch and inject");

            uint returnCode = remoteProcess.WaitForExit();

            Assert.IsTrue(remoteProcess.IsValid, "Remote process is not valid.");
            Assert.IsTrue(returnCode == 0, "Expected return code of zero.");

            // Keep client alive with a ping and test that it works
            FileMonInterface client = RemoteHooking.IpcConnectClient<FileMonInterface>(channelName);
            client.Ping();

            // Grab array of files accessed
            string[] paths = client.GetPaths(remoteProcess.RemoteProcessId);

            Assert.IsTrue(
                paths.Length > 0,
                "No paths accessed in remote process.");

            Assert.IsTrue(
                output.Contains("Failed to stat arial.ttf"),
                "Missing log message.");
            Assert.IsTrue(
                remoteProcess.StandardOutput.Contains("Failed to stat arial.ttf"),
                "Missing log message.");
        }

        [TestMethod]
        public void HookFileTest()
        {
            ExecuteTest();
        }

        [TestMethod]
        public void HookFileStressTest()
        {
            for (int i = 0; i < 5000; i++)
            {
                ExecuteTest();
            }
        }

        private bool RunHookTest()
        {
            // This test is optional and just skip if the executable does not exist
            if (!File.Exists(_testDemoExecutablePath))
            {
                return true;
            }

            bool success = true;

            RemoteHookProcess remoteProcess = new RemoteHookProcess();
            string output = "";

            remoteProcess.ErrorDataReceived += (sender, argData) =>
            {
                lock (remoteProcess)
                {
                    output += $"{argData.Data}\n";
                    TestContext.WriteLine($"[stderr] {argData.Data}");
                }
            };

            remoteProcess.OutputDataReceived += (sender, argData) =>
            {
                lock (remoteProcess)
                {
                    output += $"{argData.Data}\n";
                    TestContext.WriteLine($"[stdout] {argData.Data}");
                }
            };

            string channelName = null;
            IpcServerChannel server = RemoteHooking.IpcCreateServer<FileMonInterface>(
                ref channelName, WellKnownObjectMode.Singleton);

            string injectionLibrary = Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty,
                "FileMonInject.dll");

            success &= remoteProcess.Launch(
                _testDemoExecutablePath,
                "-g Physics/Core/Constraints/Ragdoll -i 1 -nows -nowp -rui 0",
                0,
                InjectionOptions.DoNotRequireStrongName,
                injectionLibrary,
                injectionLibrary,
                server.ChannelName);

            uint returnCode = remoteProcess.WaitForExit();

            success &= remoteProcess.IsValid;
            success &= returnCode == 0;

            // Keep client alive with a ping and test that it works
            FileMonInterface client = RemoteHooking.IpcConnectClient<FileMonInterface>(channelName);
            client.Ping();

            // Grab array of files accessed
            string[] paths = client.GetPaths(remoteProcess.RemoteProcessId);

            success &= paths.Length > 0;

            success &= output.Contains("Failed to stat arial.ttf");
            success &= remoteProcess.StandardOutput.Contains("Failed to stat arial.ttf");

            return success;
        }
    }
}
