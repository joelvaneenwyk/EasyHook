using System;
using EasyHook;
using System.IO;
using System.Reflection;

namespace MultipleHooks
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Enter process id to hook:");
            string input = Console.ReadLine();
            int processId = 0;

            if (int.TryParse(input, out processId))
            {
                //hook now
                string filePath1 = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + "SimpleHook1.dll";
                string filePath2 = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + "SimpleHook2.dll";

                Console.WriteLine("Start hook SimpleHook1.dll");
                RemoteHooking.Inject(processId, InjectionOptions.DoNotRequireStrongName, filePath1, filePath1);
                Console.WriteLine("Hook SimpleHook1.dll success");

                Console.WriteLine("Start hook SimpleHook2.dll");
                RemoteHooking.Inject(processId, InjectionOptions.DoNotRequireStrongName, filePath2, filePath2);
                Console.WriteLine("Hook SimpleHook2.dll success");
            }
            else Console.WriteLine("Invalid process id");

            Console.WriteLine("End");
        }
    }
}
