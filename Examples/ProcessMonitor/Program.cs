﻿using System;
using System.Windows.Forms;

namespace ProcessMonitor
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // Default to not using the GAC
            bool noGAC = true;

            //try
            //{
            //    Config.Register(
            //        "A simple ProcessMonitor based on EasyHook!",
            //        "ProcMonInject.dll",
            //        "ProcessMonitor.exe");
            //}
            //catch (ApplicationException)
            //{
            //    MessageBox.Show("This is an administrative task! Attempting without GAC...", "Permission denied...", MessageBoxButtons.OK);

            //    noGAC = true;
            //    //System.Diagnostics.Process.GetCurrentProcess().Kill();
            //}

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(noGAC));
        }
    }
}
