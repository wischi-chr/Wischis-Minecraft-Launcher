using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace WischisMinecraftLauncher
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            #if !DEBUG  
            AppDomain mCurrentDomain = AppDomain.CurrentDomain;
            mCurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(mCurrentDomain_UnhandledException);
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            #endif

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Updater());
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            ExceptionForm asf = new ExceptionForm(e.Exception);
            asf.Show();
        }

        static void mCurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ExceptionForm asf = new ExceptionForm(e.ExceptionObject as Exception);
            asf.Show();
        }

    }
}
