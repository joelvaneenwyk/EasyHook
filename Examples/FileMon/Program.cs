using System;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.IO;
using EasyHook;

namespace FileMon
{
    public class FileMonInterface : MarshalByRefObject
    {
        private readonly Dictionary<int, List<string>> _filenames = new Dictionary<int, List<string>>();

        public void IsInstalled(int InClientPID)
        {
            Console.WriteLine("FileMon has been installed in target {0}.\r\n", InClientPID);
        }

        public void OnCreateFile(int InClientPID, string[] InFileNames)
        {
            List<string> outFilenames;

            lock (this._filenames)
            {
                if (!this._filenames.TryGetValue(InClientPID, out outFilenames))
                {
                    outFilenames = new List<string>();
                    this._filenames.Add(InClientPID, outFilenames);
                }
            }

            foreach (string fileName in InFileNames)
            {
                lock (this._filenames)
                {
                    outFilenames?.Add(fileName);
                }

                Console.WriteLine(fileName);
            }
        }

        public string[] GetFilenames(int InClientPID)
        {
            lock (this._filenames)
            {
                this._filenames.TryGetValue(InClientPID, out List<string> outFilenames);
                return outFilenames?.ToArray();
            }
        }

        public void ReportException(Exception InInfo)
        {
            Console.WriteLine("The target process has reported an error:\r\n" + InInfo.ToString());
        }

        public void Ping()
        {
        }
    }

    internal class Program
    {
        private static string ChannelName;

        private static void Main(string[] args)
        {
            int TargetPID = 0;
            string targetExe = null;

            // Load the parameter
            while (args.Length != 1 ||
                   !int.TryParse(args[0], out TargetPID) ||
                   !File.Exists(args[0]))
            {
                if (TargetPID > 0)
                {
                    break;
                }

                if (args.Length != 1 || !File.Exists(args[0]))
                {
                    Console.WriteLine();
                    Console.WriteLine("Usage: FileMon %PID%");
                    Console.WriteLine("   or: FileMon PathToExecutable");
                    Console.WriteLine();
                    Console.Write("Please enter a process Id or path to executable: ");

                    args = new string[] { Console.ReadLine() };

                    if (string.IsNullOrEmpty(args[0])) return;
                }
                else
                {
                    targetExe = args[0];
                    break;
                }
            }

            try
            {
                RemoteHooking.IpcCreateServer<FileMonInterface>(ref ChannelName, WellKnownObjectMode.SingleCall);

                string injectionLibrary = Path.Combine(
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    "FileMonInject.dll");

                if (string.IsNullOrEmpty(targetExe))
                {
                    RemoteHooking.Inject(
                        TargetPID,
                        injectionLibrary,
                        injectionLibrary,
                        ChannelName);

                    Console.WriteLine("Injected to process {0}", TargetPID);
                }
                else
                {
                    var remoteProcess = new RemoteHookProcess();

                    remoteProcess.ErrorDataReceived += (sender, argData) =>
                    {
                        Console.WriteLine($"[stderr] {argData.Data}");
                    };

                    remoteProcess.OutputDataReceived += (sender, argData) =>
                    {
                        Console.WriteLine($"[stdout] {argData.Data}");
                    };

                    for (int i = 1; i < 20; i++)
                    {
                        try
                        {
                            remoteProcess.CreateAndInject(
                                targetExe, "",
                                0, InjectionOptions.DoNotRequireStrongName,
                                injectionLibrary, injectionLibrary,
                                ChannelName);

                            Console.WriteLine("Created and injected process {0}", remoteProcess.RemotePID);

                            break;
                        }
                        catch
                        {
                            Console.WriteLine($"[Attempt #{i}] Failed to create and inject.");
                        }
                    }

                    remoteProcess.WaitForExit();
                }
                Console.WriteLine("<Press any key to exit>");
                Console.ReadKey();
            }
            catch (Exception ExtInfo)
            {
                Console.WriteLine("There was an error while connecting to target:\r\n{0}", ExtInfo.ToString());
                Console.WriteLine("<Press any key to exit>");
                Console.ReadKey();
            }
        }
    }
}
