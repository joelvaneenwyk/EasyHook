using System;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.IO;
using EasyHook;

namespace FileMon
{
    public class FileMonInterface : MarshalByRefObject
    {
        private readonly Dictionary<int, List<string>> _processIdToPaths = new Dictionary<int, List<string>>();

        public void IsInstalled(int inputClientProcessId)
        {
            Console.WriteLine("FileMon has been installed in target {0}.\r\n", inputClientProcessId);
        }

        public void OnCreateFile(int inputClientProcessId, string[] inputPaths)
        {
            List<string> outputPaths;

            lock (this._processIdToPaths)
            {
                if (!this._processIdToPaths.TryGetValue(inputClientProcessId, out outputPaths))
                {
                    outputPaths = new List<string>();
                    this._processIdToPaths.Add(inputClientProcessId, outputPaths);
                }
            }

            lock (this._processIdToPaths)
            {
                outputPaths?.AddRange(inputPaths);
            }

            foreach (string fileName in inputPaths)
            {
                Console.WriteLine(fileName);
            }
        }

        public string[] GetPaths(int inputClientProcessId)
        {
            lock (this._processIdToPaths)
            {
                List<string> outputPaths;
                if (!this._processIdToPaths.TryGetValue(inputClientProcessId, out outputPaths))
                {
                    outputPaths = new List<string>();
                }
                return outputPaths.ToArray();
            }
        }

        public void ReportException(Exception exception)
        {
            Console.WriteLine("The target process has reported an error:\r\n" + exception);
        }

        public void Ping()
        {
            // No operation needed here just here to keep connection alive.
        }
    }

    internal class Program
    {
        private static string _channelName;

        private static void Main(string[] args)
        {
            int targetProcessId = 0;
            string targetExe = null;

            // Load the parameter
            while (args.Length != 1 ||
                   !int.TryParse(args[0], out targetProcessId) ||
                   !File.Exists(args[0]))
            {
                if (targetProcessId > 0)
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
                RemoteHooking.IpcCreateServer<FileMonInterface>(ref _channelName, WellKnownObjectMode.SingleCall);

                string injectionLibrary = Path.Combine(
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty,
                    "FileMonInject.dll");

                if (string.IsNullOrEmpty(targetExe))
                {
                    RemoteHooking.Inject(
                        targetProcessId,
                        injectionLibrary,
                        injectionLibrary,
                        _channelName);

                    Console.WriteLine("Injected to process {0}", targetProcessId);
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

                    bool launchResult = remoteProcess.Launch(
                        targetExe, "",
                        0, InjectionOptions.DoNotRequireStrongName,
                        injectionLibrary, injectionLibrary,
                        _channelName);

                    if (launchResult && remoteProcess.WaitForExit() == 0)
                    {
                        Console.WriteLine($"Created and injected '{remoteProcess.RemoteProcessId}' process: '{targetExe}'");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to create and inject: '{targetExe}'");
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("There was an error while connecting to target:\r\n{0}", exception);
            }
            finally
            {
                Console.WriteLine("<Press any key to exit>");
                Console.ReadKey();
            }
        }
    }
}
