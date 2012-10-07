using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Win32;
using WischisMinecraftLauncher;
using System.Threading;
using WischisLauncherCore;

namespace ExternalMinecraftLauncher
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (!Application.MessageLoop)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }

            Dictionary<string, string> ArgMap = new Dictionary<string, string>();
            string[] args = Environment.GetCommandLineArgs();

            foreach (string arg in args)
            {
                if (!arg.StartsWith("/")) continue;
                string[] sd = arg.Split('=');
                if (sd.Length != 2) continue;
                string key = sd[0].Substring(1);
                string val = sd[1];
                if (!(key.Length > 0 && val.Length > 0)) continue;
                ArgMap.Add(key, val);
            }

            WischiLauncherMainForm MainFrm = new WischiLauncherMainForm();

            if (ArgMap.ContainsKey("offline"))
                if (ArgMap["offline"] == "true")
                    MainFrm.AllowOffline = true;

            if (ArgMap.ContainsKey("profil"))
            {
                List<LauncherProfile> fff = LauncherProfile.GetLauncherProfileList();
                LauncherProfile p = null;
                foreach (LauncherProfile lp in fff)
                {
                    if (lp.ProfileName == ArgMap["profil"])
                    {
                        p = lp;
                        break;
                    }
                }
                
                if (p != null && p.Settings.MinecraftAutoLogin)
                {
                    MainFrm.SwitchToConsoleScreen();
                    MainFrm.Show();
                    MainFrm.AddConsoleLine("Lade MinecraftJar-Infos");
                    MainFrm.RebuildLocations(true);
                    MainFrm.KillOnExit = true;
                    MainFrm.StartMinecraft(WischisLauncherCore.LauncherProfile.Load(ArgMap["profil"]));
                    MainFrm.Hide();
                }
                else
                {
                    MessageBox.Show("Konnte Profil nicht direkt starten. Mögliche Gründe:" + Environment.NewLine + "  - AutoLogin wurde nicht aktiviert." + Environment.NewLine + "  - Das Profil konnte nicht gefunden werden (umbenannte, gelöscht usw.)");
                    MainFrm.Show();
                }
                if (!Application.MessageLoop) Application.Run();
            }
            else
            {
                if (!Application.MessageLoop) Application.Run(MainFrm);
                MainFrm.Show();
            }
        }


    }
}
