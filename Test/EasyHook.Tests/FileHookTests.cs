using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.Threading;
using EasyHook;
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

            try
            {
                var server = RemoteHooking.IpcCreateServer<FileMonInterface>(ref channelName, WellKnownObjectMode.SingleCall);

                var client = RemoteHooking.IpcConnectClient<FileMonInterface>(channelName);

                string injectionLibrary = Path.Combine(
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty,
                    "FileMonInject.dll");

                Assert.IsFalse(string.IsNullOrEmpty(injectionLibrary));

                var remoteProcess = new RemoteHookProcess();
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

                client.Ping();
                Assert.Equals(remoteProcess.WaitForExit(), 0);
                Assert.IsTrue(client.GetFilenames(remoteProcess.RemoteProcessId).Length > 0);
                Assert.IsTrue(output.Contains("Failed to stat arial.ttf"));
            }
            catch (Exception ExtInfo)
            {
                Console.WriteLine("There was an error while connecting to target:\r\n{0}", ExtInfo.ToString());
            }
        }
    }
}
