using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using Microsoft.Win32;
using System.Management;
using System.Reflection;
using WischisLauncherCore;
using WischisMinecraftCore;
using System.Text.RegularExpressions;
using Ionic.Zip;
using System.Xml;
using System.Drawing.Text;
using System.Threading;
using WischisMinecraftLauncherCoreDLL;
using WischisMinecraftLauncherCoreDLL.PluginSystem;

namespace ExternalMinecraftLauncher
{
	/// <summary>
	/// Die Graphische Oberfläche des Wischilaunchers
	/// </summary>
	public partial class WischiLauncherMainForm : Form, IPluginHost
	{
		Dictionary<string, MinecraftBinJarLocation> MinecraftLocations = new Dictionary<string, MinecraftBinJarLocation>();

		public void ClearLog()
		{
			textBox5.Text = "";
		}

		public void FireLog(string Text)
		{
			if (LogEvent != null)
				LogEvent(this, Text);
		}

		public delegate void LogEventDelegate(object sender, string Text);
		public event LogEventDelegate LogEvent;

		int PID = 0;

		public LauncherProfile CurrentProfile;
		public LauncherProfile RunningProfile;
		public bool AllowOffline = false;

		[DllImport("user32.dll")]
		static extern int SetWindowText(IntPtr hWnd, string text);

		bool updateVersion = false;

		public bool KillOnExit = false;
		public DateTime MCstart = DateTime.MinValue;

		PrivateFontCollection PrvFontCollection;
		FontFamily MinecraftFont;

		Microsoft.VisualBasic.Devices.ComputerInfo CInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();

		public WischiLauncherMainForm()
		{
			InitializeComponent();
			LoadMinecraftFont();

			SwitchToMainWindow();

			PID = Process.GetCurrentProcess().Id;

			regler_init.Minimum = 32;
			regler_init.Maximum = (int)(CInfo.TotalPhysicalMemory / 1024 / 1024);
			regler_init.TickFrequency = (regler_init.Maximum - regler_init.Minimum) / 20 + 1;

			regler_max.Minimum = 32;
			regler_max.Maximum = (int)(CInfo.TotalPhysicalMemory / 1024 / 1024);
			regler_max.TickFrequency = (regler_max.Maximum - regler_max.Minimum) / 20 + 1;

			System.Net.ServicePointManager.Expect100Continue = false;

			RefreshVersions(true);
			LoadTextsFromRessources();
			BuildPriorityList();

			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

			AssemblyTitelLabel.Text = AssemblyTitle;
			AssemblyBeschreibung.Text = AssemblyDescription;
			AssemblyVersionLabel.Text = "Version " + AssemblyVersion;

			StartProfileCache();
			RebuildProfileList();
			mcserverstatus.Image = imageList3.Images["unknown"];
			StatusRefresh.RunWorkerAsync();

			LoadPlugins();
		}

		//ToDo: irgendwie wird das zum laden der Plugins benötigt!?
		Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			var asm = Assembly.GetAssembly(this.GetType());
			if (asm.FullName == args.Name)
			{
				return asm;
			}
			throw new Exception(args.Name + " gesucht, aber nur folgendes auf Lager: " + asm.FullName);
		}

		private void LoadPlugins()
		{
			string[] pluginfiles = Directory.GetFiles(LauncherFolderStructure.PluginFolder, "*.plugin.dll");

			foreach (string pluginfile in pluginfiles)
			{
				try
				{
					Assembly asm = null;
					#region PluginDatei laden
					try
					{
						asm = Assembly.LoadFrom(pluginfile);
					}
					catch (Exception ex)
					{
						throw new Exception("Die Datei \"" + Path.GetFileName(pluginfile) + "\" ist kein gültiges Plugin: " + ex.Message, ex);
					}
					#endregion

					var PluginTypes = asm.GetTypes();
					foreach (var PluginType in PluginTypes)
					{
						if (typeof(WischiLauncherPlugin).IsAssignableFrom(PluginType))
						{
							//Typ ist ein Plugin
							var plugin = (WischiLauncherPlugin)Activator.CreateInstance(PluginType);
							try
							{
								plugin.Initialize(this, Path.Combine(LauncherFolderStructure.PluginFolder, plugin.ID.ToString()));
								PluginListBox.Items.Add(plugin);
								NoPluginInfoLabel.Visible = false;
							}
							catch (Exception ex)
							{
								throw new Exception("Fehler beim Initialisieren: " + ex.Message, ex);
							}

						}
					}
				}
				catch (Exception ex)
				{
					AddLogLine("Plugin Fehler: " + ex.Message);
				}
			}
		}

		private void BuildPriorityList()
		{
			comboBox1.BeginUpdate();
			comboBox1.Items.Clear();
			Array ar = Enum.GetValues(typeof(ProcessPriorityClass));
			foreach (ProcessPriorityClass pri in ar)
			{
				comboBox1.Items.Add(pri);
			}


			comboBox1.EndUpdate();
		}

		private void StartProfileCache()
		{
			foreach (LauncherProfile profil in LauncherProfile.GetLauncherProfileList())
			{
				if (profil.Setting_LauncherModAnalyzeStatus == 0 || !File.Exists(profil.Settings.MinecraftBinJarCache)) BuildJarInBackground(profil);
			}
		}

		public void RefreshVersions(bool fast = false)
		{
			RebuildLocations();
			BuildMinecraftJarList();
		}

		ToolStripItem Selected = null;
		public bool GUISelectProfile(ToolStripItem Item)
		{
			if (Selected != null)
			{
				Selected.Image = null;
			}

			Selected = Item;
			Selected.Image = imageList3.Images["right"];
			StatusProfilListe.Text = Selected.Text;
			CurrentProfile = LauncherProfile.Load(Selected.Text);
			LoadSettings();
			return true;
		}

		public bool GUISelectProfile(string Item)
		{
			ToolStripItem[] gefunden = new ToolStripItem[0];
			if (StatusProfilListe.DropDownItems.ContainsKey(Item))
			{
				gefunden = StatusProfilListe.DropDownItems.Find(Item, false);
				if (gefunden.Length >= 1)
				{
					return GUISelectProfile(gefunden[0]);
				}
			}

			return false;
		}

		public void GUIDelecteProfile(ToolStripItem Item)
		{
			if (StatusProfilListe.DropDownItems.Count > 1)
			{
				bool selnew = Item == Selected;

				StatusProfilListe.DropDownItems.Remove(Item);
				if (selnew)
				{
					GUISelectProfile(StatusProfilListe.DropDownItems[0]);
				}
			}
			else
			{
				MessageBox.Show("Das ist das letzte Profil und kann nicht gelöscht werden.");
			}
		}

		public ToolStripItem GUINewProfile(string Profilename)
		{
			ToolStripItem it = StatusProfilListe.DropDownItems.Add(Profilename);
			it.Click += new EventHandler(it_Click);
			return it;
		}

		private void RebuildProfileList()
		{
			#region MinecraftProfileList - Old

			/*string oldtext = MinecraftProfileList.Text;
            MinecraftProfileList.BeginUpdate();
            MinecraftProfileList.Items.Clear();

            foreach (string prof in LauncherProfile.GetLauncherProfileList())
            {
                MinecraftProfileList.Items.Add(prof);
            }

            MinecraftProfileList.EndUpdate();
            MinecraftProfileList.Text = oldtext;

            if (MinecraftProfileList.Text == "" && MinecraftProfileList.Items.Count > 0)
            {
                MinecraftProfileList.SelectedIndex = 0;
            }*/

			#endregion

			string oldtext = StatusProfilListe.Text;
			//StatusProfilListe.up
			StatusProfilListe.DropDownItems.Clear();

			foreach (LauncherProfile prof in LauncherProfile.GetLauncherProfileList())
			{
				GUINewProfile(prof.ProfileName);
			}

			if (!GUISelectProfile(oldtext) && StatusProfilListe.DropDownItems.Count > 0)
			{
				GUISelectProfile(StatusProfilListe.DropDownItems[0]);
			}

			List<LauncherProfile> Profile = LauncherProfile.GetLauncherProfileList();
			if (Profile.Count > 0)
			{
				GUISelectProfile(StatusProfilListe.DropDownItems[0]);
			}
			else
			{
				string DefaultName = "Default";
				CurrentProfile = LauncherProfile.Load(DefaultName);
				CurrentProfile.Save();
				GUISelectProfile(GUINewProfile(DefaultName));
			}
		}

		void it_Click(object sender, EventArgs e)
		{
			if (sender is ToolStripItem)
			{
				ToolStripItem s = sender as ToolStripItem;
				GUISelectProfile(s);
			}
		}

		public void RebuildLocations(bool wait = false, bool fast = false)
		{
			if (!LocationLoader.IsBusy)
			{
				LocationLoader.RunWorkerAsync(fast);
			}

			if (wait)
			{
				Thread.Sleep(100);
				while (LocationLoader.IsBusy) Application.DoEvents();
			}
		}

		bool buildmodlist = false;
		private void RebuildModList()
		{
			buildmodlist = true;
			string[] files = new string[0];
			string[] aktivemods = CurrentProfile.Settings.MinecraftModListe.Split('|');

			ModListe.BeginUpdate();
			ModListe.Items.Clear();

			List<string> aktive = new List<string>();

			if (Directory.Exists(LauncherFolderStructure.ModFolder)) files = Directory.GetFiles(LauncherFolderStructure.ModFolder);

			foreach (string aktiv in aktivemods)
			{
				if (!File.Exists(LauncherFolderStructure.ModFolder + Path.DirectorySeparatorChar + aktiv)) continue;
				ListViewItem it = ModListe.Items.Add(aktiv);
				aktive.Add(aktiv);
				it.ImageIndex = 1;
				it.Group = ModListe.Groups["aktive"];
				it.Checked = true;
			}

			foreach (string file in files)
			{
				string ent = Path.GetFileName(file);
				if (aktive.Contains(ent)) continue;
				ListViewItem it = ModListe.Items.Add(ent);
				it.ImageIndex = 1;
				it.Group = ModListe.Groups["inaktive"];
			}
			ModListe.EndUpdate();
			buildmodlist = false;
		}

		private void BuildMinecraftJarList()
		{
			MethodInvoker asdf = delegate
			{
				MinecraftVersionList.BeginUpdate();
				updateVersion = true;

				MinecraftVersionList.Sorted = true;
				MinecraftVersionList.Items.Clear();

				MinecraftVersionList.Items.Add("<keine Version gewählt>");
				foreach (KeyValuePair<string, MinecraftBinJarLocation> loc in MinecraftLocations)
				{
					MinecraftVersionList.Items.Add(loc.Value);
				}

				if (CurrentProfile != null && MinecraftLocations.ContainsKey(CurrentProfile.Settings.MinecraftBinJarHash))
				{
					MinecraftVersionList.SelectedItem = MinecraftLocations[CurrentProfile.Settings.MinecraftBinJarHash];
				}
				else MinecraftVersionList.Text = "<keine Version gewählt>";
				updateVersion = false;
				MinecraftVersionList.EndUpdate();
			};
			if (this.InvokeRequired) this.Invoke(asdf);
			else asdf.Invoke();
		}

		private void checkBox2_CheckedChanged(object sender, EventArgs e)
		{
			groupBox5.Visible = checkBox2.Checked;
			CurrentProfile.Settings.UseJavaProxyAuth = checkBox2.Checked;
		}

		private void löschenToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ClearLog();
		}

		private void kopierenToolStripMenuItem_Click(object sender, EventArgs e)
		{
			System.Windows.Forms.Clipboard.SetDataObject(textBox5.Text, true);
		}

		Process minecraft;

		public bool StartMinecraft(WischisLauncherCore.LauncherProfile Profil)
		{
			ClearConsole();
			SwitchToConsoleScreen();
			#region Check running Processes
			foreach (Process proc in Process.GetProcesses())
			{
				if (proc.ProcessName.Contains("java"))
				{
					ManagementObjectSearcher mos = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + proc.Id);
					foreach (ManagementObject mo in mos.Get())
					{
						object abc = mo["CommandLine"];
						if (!(abc is string)) continue;
						string cmd = (string)abc;
						Regex mineEx = new Regex("-cp[ ]+[\"]?([a-zA-Z]:\\\\([^|/:*\\\\?<>]+\\\\)+).minecraft\\\\[^|/:*\\\\?<>]+.jar[\"]? net.minecraft.LauncherFrame");
						Match MineMatch = mineEx.Match(cmd);

						//checke ob der aktuelle Start im gleichen Profil passiert.
						if (MineMatch.Groups.Count > 2)
						{
							string a = MineMatch.Groups[1].Value;
							string b = Path.GetDirectoryName(a);

							//TODO: schau dir das mal an
							/*
							if (b.ToLower() == CurrentAppPath.ToLower())
							{
								 AddLogLine("> Minecraft läuft bereits im Verzeichnis: " + b + ". Bitte beende die offene Instanz.");
								 //proc.
								 this.Show();
								 return false;
							}*/


						}
					}
				}
			}
			#endregion

			#region Check Java Path
			if (Profil.Settings.JavaExecutable == "")
			{
				AddLogLine("$WMCL> JavaPfad nicht eingetragen!");
				this.Show();
				return false;
			}
			#endregion

			Application.DoEvents();

			minecraft = new Process();
			minecraft.StartInfo.FileName = Profil.Settings.JavaExecutable;

			//Pfad verbiegen
			minecraft.StartInfo.EnvironmentVariables["appdata"] = LauncherFolderStructure.GetSpecificProfilePath(Profil);

			if (!MinecraftLocations.ContainsKey(Profil.Settings.MinecraftBinJarHash))
			{
				AddLogLine("$WMCL> FEHLER: Es ist keine gültige Minecraft-Version gewählt");
				return false;
			}

			AddConsoleLine("Login: http://login.minecraft.net");

			Application.DoEvents();

			while (RebuildJarList.Count > 0 || ModCompatibilityChecker.IsBusy) Application.DoEvents();
			if (!File.Exists(Profil.Settings.MinecraftBinJarCache))
			{
				AddModInfo("Keine Jar im Cache gefunden -> neu bilden.", Profil);
				BuildJarInBackground(Profil, true);
				Thread.Sleep(200);
			}
			while (RebuildJarList.Count > 0 || ModCompatibilityChecker.IsBusy) Application.DoEvents();

			if (!File.Exists(Profil.Settings.MinecraftBinJarCache))
			{
				label22.Text = "minecraft.jar nicht gefunden.";
				label22.Visible = true;
				return false;

				#region Weg damit

				/*AddConsoleLine("Erstelle MinecraftBinJar");
                while (ModCompatibilityChecker.IsBusy || RebuildJarList.Count > 0) Application.DoEvents();

                
                CookieContainer cc = null;
                MinecraftBinJarLocation loc = MinecraftLocations[Profil.Settings.MinecraftBinJarHash];
                AddConsoleLine("Lade Jar: " + loc.Name);
                if (loc.NeedAuth)
                {
                    AddConsoleLine("Verbinde zu http://dev.wischenbart.org/minecraft");
                    AddLogLine("$WMCL> Verbinde zu http://dev.wischenbart.org/minecraft");
                    string Hinweis;
                    cc = MinecraftAuth.WischiAuth(sid, Profil.Settings.MinecraftLoginUser,out Hinweis);
                    AddLogLine("$WMCL> AuthResp.: " + Hinweis);
                    AddLogLine("$WMCL> Rückmeldung: " + MinecraftAuth.CheckWischiAuth(cc));
                }

                AddConsoleLine("> " + loc.DownloadLocation);
                Application.DoEvents();
                MinecraftBinJar mjar = null;
                try
                {
                    mjar = loc.GetMinecraftBinJar(cc);
                }
                catch (Exception ex)
                {
                    AddLogLine("$WMCL> MinecraftJar konnte nicht geladen werden: " + ex.Message);
                    return false;
                }

                if (Profil.Settings.MinecraftServerAutoConnect)
                {

                    string WischiPatch = Assembly.GetExecutingAssembly().GetName().Name + ".Resources." + "WMLPatch.class";
                    Stream WischiPatchStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(WischiPatch);
                    mjar.AddModFile(WischiPatchStream, "net/minecraft/client/WMLPatch.class");
                }

                Application.DoEvents();
                AddConsoleLine("Patche Mods in Binjar");
                foreach (ListViewItem it in ModListe.Items)
                {
                    if (!it.Checked) continue;
                    AddConsoleLine("Mod: " + it.Text);
                    string mod = LauncherFolderStructure.ModFolder + Path.DirectorySeparatorChar + it.Text;
                    if (File.Exists(mod) && ZipFile.CheckZip(mod))
                    {
                        ZipFile modfile = new ZipFile(mod);
                        mjar.AddModPack(modfile,Profil);
                    }
                    else
                    {
                        AddLogLine("$WMCL> Der Mod konnte nicht geladen werden: '" + mod + "'");
                    }
                }

                Application.DoEvents();

                string tempString = Guid.NewGuid().ToString().ToUpper();
                Profil.Settings.MinecraftBinJarCache = LauncherFolderStructure.GetSpecificBinFolder(Profil) + Path.DirectorySeparatorChar + tempString;
                mjar.Save(Profil.Settings.MinecraftBinJarCache);*/
				#endregion
			}
			else
			{
				AddConsoleLine("MinecraftBinJar Cache gefunden.");
			}


			string sid = "";
			string mcuser = "";
			if (!AllowOffline && !MinecraftAuth.ProfileAuth(Profil, out sid, out mcuser, textBox11.Text))
			{
				label22.Text = "Can't connect to minecraft.net";
				label22.Visible = true;
				AddLogLine("Error minecraft.net: " + sid);
				return false;
			}

			if (AllowOffline)
			{
				mcuser = FakePlayerName.Text.Replace(" ", "");
				Random rnd = new Random();
				sid = rnd.Next().ToString();
				AddConsoleLine("Offline-Mode:" + mcuser);
			}

			AddConsoleLine("Lade Resourcen");
			MinecraftRessourceDownloader.DownloadBins(LauncherFolderStructure.GetSpecificBinFolder(Profil), this);


			string arguments = "";

			//RAM Einstellungen
			if (checkBox3.Checked) arguments += " -Xmx" + regler_max.Value + "M -Xms" + regler_init.Value + "M";

			//Set Proxy
			if (checkBox4.Checked)
			{
				arguments += " -DsocksproxyHost=" + textBox1.Text + " -DsocksproxyPort=" + textBox2.Text;
				arguments += " -Dhttp.proxyHost=" + textBox1.Text + " -Dhttp.proxyPort=" + textBox2.Text;
				arguments += " -Dhttps.proxyHost=" + textBox1.Text + " -Dhttps.proxyPort=" + textBox2.Text;
				arguments += " -Dhttp.proxyUser=" + textBox4.Text + " -Dhttp.proxyPassword=" + textBox3.Text;
			}

			//Minecraft start Einstellungen

			arguments += " -Djava.library.path=\"" + LauncherFolderStructure.GetSpecificBinFolder(Profil) + Path.DirectorySeparatorChar + "natives\"";



			string Classes = "";
			Classes += "\"" + Profil.Settings.MinecraftBinJarCache + "\";";
			Classes += "\"" + LauncherFolderStructure.GetSpecificBinFolder(Profil) + Path.DirectorySeparatorChar + "lwjgl.jar\";";
			Classes += "\"" + LauncherFolderStructure.GetSpecificBinFolder(Profil) + Path.DirectorySeparatorChar + "lwjgl_util.jar\";";
			Classes += "\"" + LauncherFolderStructure.GetSpecificBinFolder(Profil) + Path.DirectorySeparatorChar + "jinput.jar\";";

			if (Profil.Settings.MinecraftServerAutoConnect) arguments += " -cp " + Classes + " net.minecraft.client.WMLPatch";
			else arguments += " -cp " + Classes + " net.minecraft.client.Minecraft";

			arguments += " \"" + mcuser + "\" \"" + sid + "\"";


			if (Profil.Settings.MinecraftServerAutoConnect) arguments += " " + Profil.Settings.MinecraftServerAutoconnectHost;

			minecraft.StartInfo.Arguments = arguments;
			minecraft.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;


			AddLogLine("$WMCL> Java: " + minecraft.StartInfo.FileName);
			AddLogLine("$WMCL> Attrib: " + arguments);

			minecraft.StartInfo.CreateNoWindow = true;
			minecraft.StartInfo.RedirectStandardOutput = true;
			minecraft.StartInfo.RedirectStandardError = true;
			minecraft.StartInfo.UseShellExecute = false;
			minecraft.EnableRaisingEvents = true;

			minecraft.OutputDataReceived += new DataReceivedEventHandler(minecraft_OutputDataReceived);
			minecraft.ErrorDataReceived += new DataReceivedEventHandler(minecraft_ErrorDataReceived);

			AddLogLine("$WMCL> Starte Minecraft.");
			MCstart = DateTime.Now;
			LastWindowTime = DateTime.Now;
			AddConsoleLine("Starte Minecraft...");

			RunningProfile = Profil;
			minecraft.Exited += new EventHandler(minecraft_Exited);
			minecraft.Start();

			object Prio = Enum.Parse(typeof(ProcessPriorityClass), Profil.Settings.MinecraftPrio);
			if (Prio is ProcessPriorityClass && Profil.Settings.JavaMemoryUse)
			{
				minecraft.PriorityClass = (ProcessPriorityClass)Prio;
			}
			else
			{
				minecraft.PriorityClass = ProcessPriorityClass.Normal;
				Profil.Settings.MinecraftPrio = ProcessPriorityClass.Normal.ToString();
			}



			button10.Enabled = false;
			SetSize = true;
			timer1.Enabled = true;


			minecraft.BeginErrorReadLine();
			minecraft.BeginOutputReadLine();
			return true;
		}

		void mjar_ZipError(object sender, ZipErrorEventArgs e)
		{
			//throw new NotImplementedException();
		}


		void RefreshModIcon()
		{
			if (CurrentProfile == null) return;

			//ModAnalyzeInfo.DropDownItems.Clear();

			bool onque = false;
			foreach (LauncherProfile p in RebuildJarList)
			{
				if (CurrentProfile.ProfileName == p.ProfileName)
				{
					onque = true;
					break;
				}
			}

			if (ProfileInCheck != null && ProfileInCheck.ProfileName == CurrentProfile.ProfileName)
			{
				ModAnalyzeInfo.Image = imageList3.Images["refresh"];
				ModAnalyzeInfo.Text = "Aktualisiere...";
			}
			else if (onque)
			{
				ModAnalyzeInfo.Image = imageList3.Images["clock"];
				ModAnalyzeInfo.Text = "Warte auf Thread...";
			}
			else
			{
				switch (CurrentProfile.Setting_LauncherModAnalyzeStatus)
				{
					case 1:
						ModAnalyzeInfo.Image = imageList3.Images["check"];
						ModAnalyzeInfo.Text = "Keine Modkollisionen gefunden.";
						break;

					case -1:
						ModAnalyzeInfo.Image = imageList3.Images["warning"];
						ModAnalyzeInfo.Text = "Warnung. Mögliche Modkollision gefunden.";
						break;

					default:
						ModAnalyzeInfo.Image = imageList3.Images["unknown"];
						ModAnalyzeInfo.Text = "???. Unbekannter Status. - Guck in's Log für mehr Details";
						break;
				}
			}
		}


		bool LoadSettingsOn = false;
		void LoadSettings()
		{
			#region MinecraftProfileList - Old

			/*if (MinecraftProfileList.Text == "")
            {
                string[] Profile = LauncherProfile.GetLauncherProfileList();
                if (Profile.Length > 0)
                {
                    MinecraftProfileList.Text = Profile[0];
                }
                else
                {
                    string DefaultName = "Default";
                    CurrentProfile = LauncherProfile.Load(DefaultName);
                    CurrentProfile.Save();
                    MinecraftProfileList.Text = DefaultName;
                }
            }

            if (MinecraftProfileList.Text == "")
            {
                LockAll();
                return;
            }
            else
            {
                UnlockAll();
                CurrentProfile = LauncherProfile.Load(MinecraftProfileList.Text);
            }*/

			#endregion

			LoadSettingsOn = true;

			//Tab: HOME
			//User
			textBox10.Text = CurrentProfile.Settings.MinecraftLoginUser;
			//Pass
			textBox11.Text = DeScrambleString(CurrentProfile.Settings.MinecraftLoginPassword);
			//RememberPass
			checkBox6.Checked = CurrentProfile.Settings.MinecraftRememberPassword;

			switch (CurrentProfile.Settings.MinecraftStartState)
			{
				case 1: MaximiertRadio.Checked = true; break;
				case 2: UserRadio.Checked = true; break;
				default: normalRadio.Checked = true; break;
			}
			RefreshModIcon();



			//Tab: Minecraft
			//MC-Einstellungen
			checkBox1.Checked = CurrentProfile.Settings.MinecraftAutoLogin;
			checkBox5.Checked = CurrentProfile.Settings.MinecraftServerAutoConnect;
			checkBox7.Checked = CurrentProfile.Settings.LauncherWaitNonResponding;
			textBox7.Text = CurrentProfile.Settings.MinecraftServerAutoconnectHost;
			MinecraftVersionList.Text = "";

			WindowStateInfo.Text = CurrentProfile.Settings.MinecraftStateInfo;

			comboBox1.Text = CurrentProfile.Settings.MinecraftPrio;

			bool vorher = updateVersion;
			updateVersion = true;
			if (MinecraftLocations.ContainsKey(CurrentProfile.Settings.MinecraftBinJarHash))
			{
				MinecraftVersionList.SelectedItem = MinecraftLocations[CurrentProfile.Settings.MinecraftBinJarHash];
			}
			else
			{
				MinecraftVersionList.Text = "<keine Version gewählt>";
			}
			updateVersion = vorher;

			//Modeinstellungen
			RebuildModList();

			//Java
			//Falls java.exe nicht eingetragen (try auto detect)
			if (CurrentProfile.Settings.JavaExecutable == "" || !File.Exists(CurrentProfile.Settings.JavaExecutable) || Path.GetFileName(CurrentProfile.Settings.JavaExecutable).ToLower() != "java.exe")
			{
				string javapath = System.IO.Path.Combine(GetJavaInstallationPath(), "bin\\Java.exe");
				if (File.Exists(javapath))
				{
					CurrentProfile.Settings.JavaExecutable = javapath;
				}
				else
				{
					MessageBox.Show("Java konnte nicht gefunden werden bitte manuell suchen, bzw. einfach mal Java installieren :-)");
				}
			}
			textBox6.Text = CurrentProfile.Settings.JavaExecutable;

			//Memory
			checkBox3.Checked = CurrentProfile.Settings.JavaMemoryUse;
			regler_init.Value = Math.Min(Math.Max(CurrentProfile.Settings.JavaMemoryInit, regler_init.Minimum), regler_init.Maximum);
			regler_max.Value = Math.Min(Math.Max(CurrentProfile.Settings.JavaMemoryMax, regler_max.Minimum), regler_max.Maximum);

			//Proxy
			textBox1.Text = CurrentProfile.Settings.JavaProxyHost;
			textBox2.Text = CurrentProfile.Settings.JavaProxyPort.ToString();

			//Proxy-AUth
			checkBox2.Checked = CurrentProfile.Settings.UseJavaProxyAuth;
			textBox3.Text = CurrentProfile.Settings.JavaProxyUser;
			textBox4.Text = DeScrambleString(CurrentProfile.Settings.JavaProxyPassword);

			//Logging
			LoggingActiveCheckBox.Checked = CurrentProfile.Settings.EnableLogging;
			LoggingActiveFileTextBox.Text = CurrentProfile.Settings.LoggingFile;

			LoadSettingsOn = false;
		}

		private void UnlockAll()
		{
			tabControl1.Visible = true;
		}

		private void LockAll()
		{
			tabControl1.Visible = false;
		}

		void minecraft_Exited(object sender, EventArgs e)
		{
			RunningProfile = null;
			MethodInvoker RefreshText = delegate
			{
				timer1.Enabled = false;
				if (minecraft.ExitCode != 0)
				{
					SetStatusText("Beendet mit Fehlercode " + minecraft.ExitCode);
				}
				button10.Enabled = true;
				SwitchToMainWindow();
			};
			if (this.InvokeRequired) this.Invoke(RefreshText);
			else RefreshText.Invoke();
			AddLogLine("$WMCL> minecraft beendet. Code: " + minecraft.ExitCode);

			if (KillOnExit)
			{
				Application.Exit();
			}
			else
			{
				MethodInvoker ShowMe = delegate
				{
					this.Show();
					SetForegroundWindow(this.Handle);
				};
				if (this.InvokeRequired) this.Invoke(ShowMe);
				else ShowMe.Invoke();
			}


		}


		private string GetJavaInstallationPath()
		{
			string environmentPath = Environment.GetEnvironmentVariable("JAVA_HOME");
			if (!string.IsNullOrEmpty(environmentPath))
			{
				return environmentPath;
			}

			string javaKey = "SOFTWARE\\JavaSoft\\Java Runtime Environment\\";
			Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(javaKey);
			if (rk != null)
			{
				string currentVersion = rk.GetValue("CurrentVersion").ToString();
				using (Microsoft.Win32.RegistryKey key = rk.OpenSubKey(currentVersion))
				{
					return key.GetValue("JavaHome").ToString();
				}
			}



			string[] Progdirs = { Environment.GetEnvironmentVariable("ProgramW6432"), Environment.GetEnvironmentVariable("ProgramFiles"), Environment.GetEnvironmentVariable("ProgramFiles(x86)") };
			foreach (string path in Progdirs)
			{
				for (int i = 10; i > 1; i--)
				{
					string tryjava = path + Path.DirectorySeparatorChar + "Java\\jre" + i + "\\bin\\java.exe";
					if (File.Exists(tryjava))
						return tryjava;
				}

			}


			return "";
		}

		protected string GetMD5HashFromFile(string fileName, string hashcachefile = null)
		{
			try
			{
				if (File.Exists(hashcachefile)) return File.ReadAllText(hashcachefile);

				FileStream file = new FileStream(fileName, FileMode.Open);
				MD5 md5 = new MD5CryptoServiceProvider();
				byte[] retVal = md5.ComputeHash(file);
				file.Close();

				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < retVal.Length; i++)
				{
					sb.Append(retVal[i].ToString("X2"));
				}
				string hash = sb.ToString();
				if (hashcachefile != null)
				{
					File.WriteAllText(hashcachefile, hash);
				}
				return hash;
			}
			catch
			{
				return "";
			}
		}

		void AddLogLine(string TextZeile)
		{
			MethodInvoker RefreshText = delegate
			{
				bool writelog = false;
				string dir = "";
				string file = "";
				if (!string.IsNullOrEmpty(CurrentProfile.Settings.LoggingFile))
				{
					try
					{
						dir = Path.GetDirectoryName(CurrentProfile.Settings.LoggingFile);
						file = Path.GetFileName(CurrentProfile.Settings.LoggingFile);

						if (Directory.Exists(dir) && Regex.IsMatch(file, "^[^\\\\/*:?|<>\"]+$")) writelog = true;
						else writelog = false;
						if (writelog)
						{
							FileStream fs = File.Open(CurrentProfile.Settings.LoggingFile, FileMode.Append, FileAccess.Write);
							string write = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.ffff") + "(" + PID.ToString() + "): " + TextZeile + Environment.NewLine;
							byte[] bytes = new ASCIIEncoding().GetBytes(write);
							fs.Write(bytes, 0, bytes.Length);
							fs.Close();
						}
					}
					catch
					{
					}
				}

				textBox5.Text += DateTime.Now.ToString() + ": " + TextZeile + Environment.NewLine;
				FireLog(TextZeile);
			};
			if (this.InvokeRequired) this.Invoke(RefreshText);
			else RefreshText.Invoke();
		}

		void minecraft_ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			AddLogLine(e.Data);
		}

		void minecraft_OutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			AddLogLine(e.Data);
		}

		private void checkBox3_CheckedChanged(object sender, EventArgs e)
		{
			groupBox4.Visible = checkBox3.Checked;
			CurrentProfile.Settings.JavaMemoryUse = checkBox3.Checked;
		}

		private void checkBox4_CheckedChanged(object sender, EventArgs e)
		{
			groupBox8.Visible = checkBox4.Checked;
			checkBox2.Visible = checkBox4.Checked;
			checkBox2.Checked &= checkBox4.Checked;
			CurrentProfile.Settings.JavaProxyUse = checkBox4.Checked;
		}


		private void regler_max_ValueChanged(object sender, EventArgs e)
		{
			max_label.Text = regler_max.Value + "MB";
		}

		private void regler_init_ValueChanged(object sender, EventArgs e)
		{
			init_label.Text = regler_init.Value + "MB";
		}

		[DllImport("User32.dll", EntryPoint = "SendMessage")]
		public static extern int SendMessage(int hWnd, int Msg, int wParam, int lParam);

		private const int SW_SHOWNORMAL = 1;
		private const int SW_SHOWMINIMIZED = 2;
		private const int SW_SHOWMAXIMIZED = 3;

		[DllImport("user32.dll")]
		private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

		DateTime LastWindowTime;


		bool SetSize = false;
		private void timer1_Tick(object sender, EventArgs e)
		{
			if (RunningProfile == null) return;
			if (minecraft == null || minecraft.HasExited) return;
			minecraft.Refresh();
			string txt = "WMCL - Profil:\"" + RunningProfile.ProfileName + "\", Threads:" + minecraft.Threads.Count.ToString() + ", Mem: " + Math.Round((double)(minecraft.WorkingSet64 / (1024 * 1024)), 0) + "MB, CPU-Time: " + Math.Round((minecraft.TotalProcessorTime.TotalMilliseconds / 1024), 2) + "µs";

			try
			{
				if (minecraft.MainWindowHandle != IntPtr.Zero)
				{
					this.Hide();
					LastWindowTime = DateTime.Now;
					SetWindowText(minecraft.MainWindowHandle, txt);
					if (SetSize)
					{
						if (RunningProfile.Settings.MinecraftStartState == 1) ShowWindowAsync(minecraft.MainWindowHandle, SW_SHOWMAXIMIZED);
						else if (RunningProfile.Settings.MinecraftStartState == 2)
						{
							string[] parts = RunningProfile.Settings.MinecraftStateInfo.Split(';');
							if (parts.Length == 4)
							{
								int[] ps = new int[4];
								bool success = true;
								for (int i = 0; i < 4; i++) success = success && int.TryParse(parts[i], out ps[i]);
								if (success) APICalls.ClientResize(minecraft.MainWindowHandle, ps[0], ps[1], ps[2], ps[3]);
								else AddLogLine("Mindestens ein Wert war keine Zahl.");
							}
							else
							{
								AddLogLine("Benutzerdefinierte Größe hat falsches Format. Links;Oben;Breite;Höhe");
							}
						}
						SetSize = false;
					}
				}

				if (!minecraft.Responding)
				{
					AddLogLine("Minecraft reagiert nicht. Wurde durch den Launcher gekillt.");
					minecraft.Kill();
				}

				if ((DateTime.Now - LastWindowTime).TotalSeconds > 15 && !RunningProfile.Settings.LauncherWaitNonResponding)
				{
					try
					{
						AddLogLine("Minecraft hat länger als 15 Sekunden kein aktives Fenster erstellt und wurde gekillt. (Sollte dies öfter auftreten, dann aktiviere \"Warten auch wenn Minecraft nicht reagiert\"");
						minecraft.Kill();
					}
					catch (Exception ex)
					{
						AddLogLine("Kill fehlgeschlagen: " + ex.Message);
					}
				}

			}
			catch
			{

			}
		}

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool SetForegroundWindow(IntPtr hWnd);

		private void notifyIcon1_DoubleClick(object sender, EventArgs e)
		{
			this.Show();
			SetForegroundWindow(this.Handle);
		}

		private void button4_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Filter = "java.exe|java.exe";
			ofd.FileOk += new CancelEventHandler(ofd_FileOk);
			ofd.ShowDialog();
		}

		void ofd_FileOk(object sender, CancelEventArgs e)
		{
			if (!(sender is OpenFileDialog)) return;
			OpenFileDialog s = (OpenFileDialog)sender;
			textBox6.Text = s.FileName;

		}


		private void textBox2_TextChanged(object sender, EventArgs e)
		{
			int port = 0;
			try { port = Convert.ToInt32(textBox2.Text); }
			catch { }
		}


		private void comboBox1_SelectedValueChanged_1(object sender, EventArgs e)
		{
			if (updateVersion) return;
			if (MinecraftVersionList.SelectedIndex >= 0 && MinecraftVersionList.SelectedIndex < MinecraftVersionList.Items.Count)
			{
				object element = MinecraftVersionList.Items[MinecraftVersionList.SelectedIndex];
				CurrentProfile.InvalidateJar();
				if (element is MinecraftBinJarLocation)
				{
					MinecraftBinJarLocation loc = element as MinecraftBinJarLocation;
					CurrentProfile.Settings.MinecraftBinJarHash = loc.Hash;
					CurrentProfile.InvalidateJar();
					AddModInfo("Minecraft Version geändert -> Cache neu aufbauen.", CurrentProfile);
					BuildJarInBackground(CurrentProfile);
				}
				else
				{
					try
					{
						updateVersion = true;
						MinecraftVersionList.SelectedItem = MinecraftLocations[CurrentProfile.Settings.MinecraftBinJarHash];
						updateVersion = false;
					}
					catch
					{

					}
					MessageBox.Show("Wähle eine gültige Version aus.");
				}
			}


		}

		private void button2_Click(object sender, EventArgs e)
		{
			minecraft.Kill();
		}


		private void LauncherMainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			foreach (WischiLauncherPlugin plugin in PluginListBox.Items)
			{
				plugin.Unload();
			}

			try
			{
				if (minecraft == null || minecraft.HasExited)
				{
					Application.Exit();
				}
				else
				{
					e.Cancel = true;
					this.Hide();
				}
			}
			catch
			{
				Application.Exit();
			}
		}

		private void textBox6_TextChanged(object sender, EventArgs e)
		{
			CurrentProfile.Settings.JavaExecutable = textBox6.Text.Trim();
			if (CurrentProfile.Settings.JavaExecutable == "" || !File.Exists(CurrentProfile.Settings.JavaExecutable) || Path.GetFileName(CurrentProfile.Settings.JavaExecutable).ToLower() != "java.exe")
			{
				textBox6.BackColor = Color.FromArgb(255, 128, 128);
			}
			else
			{
				textBox6.BackColor = SystemColors.Window;
			}
		}

		private void label1_Click(object sender, EventArgs e)
		{

		}

		private void button5_Click(object sender, EventArgs e)
		{

		}

		void sfd_FileOk(object sender, CancelEventArgs e)
		{
			if (!(sender is SaveFileDialog)) return;

			IconPickerDialog a = new IconPickerDialog();
			a.Filename = LauncherFolderStructure.WischiLauncherFolder + Path.DirectorySeparatorChar + "internal.dat";
			a.ShowDialog();


			SaveFileDialog S = (SaveFileDialog)sender;

			ShellLink link = new ShellLink();
			link.Target = Application.ExecutablePath;
			link.Arguments = "/profil=\"" + CurrentProfile.ProfileName + "\"";
			link.IconPath = a.Filename;
			link.IconIndex = a.IconIndex;


			link.Save(S.FileName);
		}



		private void listView1_SelectedIndexChanged(object sender, EventArgs e)
		{

		}


		private void ProfileRename(string orig, string newname)
		{
			LauncherProfile.Load(orig).Rename(newname);
		}



		void sfds_FileOk(object sender, CancelEventArgs e)
		{
			if (!(sender is SaveFileDialog)) return;

			IconPickerDialog a = new IconPickerDialog();
			a.Filename = Application.ExecutablePath;
			a.ShowDialog();

			SaveFileDialog S = (SaveFileDialog)sender;

			ShellLink link = new ShellLink();
			link.Target = Application.ExecutablePath;
			link.IconPath = a.Filename;
			link.IconIndex = a.IconIndex;


			link.Save(S.FileName);
		}

		private void listView1_Click(object sender, EventArgs e)
		{

		}

		private void button7_Click(object sender, EventArgs e)
		{
			MinecraftVersionen.Show(this.Left + button7.Left, this.Top + button7.Bottom);
		}

		private void button7_MouseDown(object sender, MouseEventArgs e)
		{

		}

		private void checkBox1_CheckedChanged(object sender, EventArgs e)
		{
			LoggingActiveFileTextBox.Visible = LoggingActiveCheckBox.Checked;
			button7.Visible = LoggingActiveCheckBox.Checked;
			CurrentProfile.Settings.EnableLogging = LoggingActiveCheckBox.Checked;
		}

		private void button7_Click_1(object sender, EventArgs e)
		{
			SaveFileDialog savelog = new SaveFileDialog();
			savelog.Filter = "Logfile(*.log)|*.log";
			savelog.FileOk += new CancelEventHandler(savelog_FileOk);
			savelog.ShowDialog();

		}

		void savelog_FileOk(object sender, CancelEventArgs e)
		{
			if (!(sender is SaveFileDialog)) return;
			SaveFileDialog s = sender as SaveFileDialog;

			LoggingActiveFileTextBox.Text = s.FileName;
		}

		private void MinecraftProfileList_SelectedIndexChanged(object sender, EventArgs e)
		{

		}

		private void LoggingActiveFileTextBox_TextChanged(object sender, EventArgs e)
		{
			CurrentProfile.Settings.LoggingFile = LoggingActiveFileTextBox.Text;

			string dir = "";
			string file = "";

			try
			{
				dir = Path.GetDirectoryName(LoggingActiveFileTextBox.Text);
				file = Path.GetFileName(LoggingActiveFileTextBox.Text);
			}
			catch
			{
				LoggingActiveFileTextBox.BackColor = Color.FromArgb(255, 128, 128);
				return;
			}

			if (Directory.Exists(dir) && Regex.IsMatch(file, "^[^\\\\/*:?|<>\"]+$")) LoggingActiveFileTextBox.BackColor = SystemColors.Window;
			else LoggingActiveFileTextBox.BackColor = Color.FromArgb(255, 128, 128);

		}

		private void MinecraftJarDrop_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				ZipFile UnzipMe = null;
				try
				{
					Array a = (Array)e.Data.GetData(DataFormats.FileDrop);

					if (a != null)
					{
						string s = a.GetValue(0).ToString();
						if (Path.GetExtension(s).ToLower() == ".jar")
						{
							UnzipMe = new ZipFile(s);
							this.Activate();        // in the case Explorer overlaps this form
							e.Effect = DragDropEffects.Copy;
						}
					}
				}
				catch
				{
				}
				finally
				{
					//if (UnzipMe != null) UnzipMe.Close();
				}

			}
		}



		private void MinecraftJarDrop_DragDrop(object sender, DragEventArgs e)
		{
			//ZipFile UnzipMe = null;
			try
			{
				Array a = (Array)e.Data.GetData(DataFormats.FileDrop);

				if (a != null)
				{
					string s = a.GetValue(0).ToString();
					/*if (Path.GetExtension(s).ToLower() == ".jar")
					{
						 UnzipMe = new ZipFile(s);
						 int ab = UnzipMe.FindEntry("META-INF/MANIFEST.MF", true);
						 if (ab > 0) MessageBox.Show("OriginalVersion");
						 new MinecraftBinJar();
					}*/
				}
			}
			catch
			{
			}
			finally
			{
				//if (UnzipMe != null) UnzipMe.Close();
			}
		}

		private void checkBox1_CheckedChanged_1(object sender, EventArgs e)
		{
			CurrentProfile.Settings.MinecraftAutoLogin = checkBox1.Checked;
		}

		private void checkBox5_CheckedChanged_1(object sender, EventArgs e)
		{
			textBox7.Visible = checkBox5.Checked;
			CurrentProfile.Settings.MinecraftServerAutoConnect = checkBox5.Checked;
			if (!LoadSettingsOn)
			{
				CurrentProfile.InvalidateJar();
				AddModInfo("AutoConnectMod geändert. -> Cache neu aufbauen.", CurrentProfile);
				BuildJarInBackground(CurrentProfile);
			}
		}

		private void textBox8_TextChanged_1(object sender, EventArgs e)
		{

		}

		private void textBox9_TextChanged(object sender, EventArgs e)
		{

		}


		private string ScrambleString(string pass)
		{
			return Crypter.Encrypt(pass, "\\/\\/15C|-|I L4UNCH3R", "Salzmich", "SHA1", 1, "1234567890abcdef", 128);
		}

		private string DeScrambleString(string crypted)
		{
			if (string.IsNullOrEmpty(crypted)) return "";
			try
			{
				return Crypter.Decrypt(crypted, "\\/\\/15C|-|I L4UNCH3R", "Salzmich", "SHA1", 1, "1234567890abcdef", 128);
			}
			catch
			{
				return "";
			}
		}

		private void textBox7_TextChanged(object sender, EventArgs e)
		{
			CurrentProfile.Settings.MinecraftServerAutoconnectHost = textBox7.Text;
		}

		private void textBox7_Leave(object sender, EventArgs e)
		{

		}

		private void button8_Click(object sender, EventArgs e)
		{
			MessageBox.Show("Alte Ordner auswahlfunktion einbauen.");
			/*if (VistaFolderBrowserDialog.IsVistaFolderDialogSupported)
			{
				 VistaFolderBrowserDialog fbd = new VistaFolderBrowserDialog();
				 if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				 {
					  MinecraftPathTextBox.Text = fbd.SelectedPath;
				 }
			}
			else
			{
				 FolderBrowserDialog fbd = new FolderBrowserDialog();
				 fbd.ShowDialog();
			}*/

		}


		private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start("http://www.minecraft.net/register");
		}

		private void button12_Click(object sender, EventArgs e)
		{
			toolTip1.ToolTipIcon = ToolTipIcon.Info;
			toolTip1.ToolTipTitle = "Optionen...";
			toolTip1.Show("Sind für dich zu wenig Einstellungen hier oben?", this, 20, 60, 6000);
		}

		private void button11_Click(object sender, EventArgs e)
		{
			ToolTipBalloon.ToolTipIcon = ToolTipIcon.Info;
			ToolTipBalloon.ToolTipTitle = "Optionen...";
			ToolTipBalloon.Show("Sind für dich zu wenig Einstellungen hier oben?", this, 20, 60, 6000);
		}

		private void button10_Click(object sender, EventArgs e)
		{
			label22.Visible = false;

			if (CurrentProfile.Settings.MinecraftLoginUser == "")
			{
				label22.Text = "Missing Username";
				label22.Visible = true;
				return;
			}

			bool started = StartMinecraft(CurrentProfile);
			if (!started)
			{
				SwitchToMainWindow();
				this.Activate();
			}
			else
			{
				//this.Hide();
			}
		}

		private void textBox10_TextChanged(object sender, EventArgs e)
		{
			CurrentProfile.Settings.MinecraftLoginUser = textBox10.Text;
		}

		private void checkBox6_CheckedChanged(object sender, EventArgs e)
		{
			CurrentProfile.Settings.MinecraftRememberPassword = checkBox6.Checked;

			checkBox1.Checked &= checkBox6.Checked;
			checkBox1.Enabled = checkBox6.Checked;

			if (!checkBox6.Checked) CurrentProfile.Settings.MinecraftLoginPassword = ScrambleString("");
			else CurrentProfile.Settings.MinecraftLoginPassword = ScrambleString(textBox11.Text);
		}

		private void textBox11_TextChanged(object sender, EventArgs e)
		{
			if (checkBox6.Checked)
				CurrentProfile.Settings.MinecraftLoginPassword = ScrambleString(textBox11.Text);
		}

		void LoadTextsFromRessources()
		{
			string TermsOfUse = Assembly.GetExecutingAssembly().GetName().Name + ".Resources." + "TermsOfUse.txt";

			Stream TermsOfUseStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(TermsOfUse);
			StreamReader reader = new StreamReader(TermsOfUseStream);
			string termstext = reader.ReadToEnd();
			textBox8.Text = termstext;

			string Change = Assembly.GetExecutingAssembly().GetName().Name + ".Resources." + "Changelog.txt";
			Stream ChangeStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(Change);
			textBox9.Text = new StreamReader(ChangeStream).ReadToEnd();

			string Spender = Assembly.GetExecutingAssembly().GetName().Name + ".Resources." + "Spender.txt";
			Stream SpenderStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(Spender);
			textBox13.Text = new StreamReader(SpenderStream).ReadToEnd();


		}

		void LoadMinecraftFont()
		{
			//Font
			string EmbeddedFont = Assembly.GetExecutingAssembly().GetName().Name + ".Resources." + "minecraft.ttf";
			Stream FontStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(EmbeddedFont);
			MinecraftFont = LoadFontFamily(FontStream);
		}

		//Load font family from stream
		public FontFamily LoadFontFamily(Stream stream)
		{
			byte[] buffer = new byte[stream.Length];
			stream.Read(buffer, 0, buffer.Length);
			IntPtr data = Marshal.AllocCoTaskMem((int)stream.Length);

			Marshal.Copy(buffer, 0, data, (int)stream.Length);
			PrvFontCollection = new PrivateFontCollection();
			PrvFontCollection.AddMemoryFont(data, (int)stream.Length);

			Marshal.FreeCoTaskMem(data);
			return PrvFontCollection.Families[0];
		}

		private void button1_Click(object sender, EventArgs e)
		{
			// the URL to download the file from
			string sUrlToReadFileFrom = "http://dev.wischenbart.org/minecraft/jars/minecraft_b1.9+pre4.jar";

			// first, we need to get the exact size (in bytes) of the file we are downloading
			Uri url = new Uri(sUrlToReadFileFrom);
			System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
			System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
			response.Close();
			// gets the size of the file in bytes
			Int64 iSize = response.ContentLength;

			// keeps track of the total bytes downloaded so we can update the progress bar
			Int64 iRunningByteTotal = 0;

			MemoryStream streamLocal = new MemoryStream();

			// use the webclient object to download the file
			using (System.Net.WebClient client = new System.Net.WebClient())
			{
				// open the file at the remote URL for reading
				using (System.IO.Stream streamRemote = client.OpenRead(new Uri(sUrlToReadFileFrom)))
				{
					// loop the stream and get the file into the byte buffer
					int iByteSize = 0;
					byte[] byteBuffer = new byte[iSize];
					while ((iByteSize = streamRemote.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
					{
						// write the bytes to the file system at the file path specified
						streamLocal.Write(byteBuffer, 0, iByteSize);
						iRunningByteTotal += iByteSize;

						// calculate the progress out of a base "100"
						double dIndex = (double)(iRunningByteTotal);
						double dTotal = (double)byteBuffer.Length;
						double dProgressPercentage = (dIndex / dTotal);
						int iProgressPercentage = (int)(dProgressPercentage * 100);


					}

					// close the connection to the remote server
					streamRemote.Close();
				}
			}

			streamLocal.Position = 0;
			ZipFile sd = ZipFile.Read(streamLocal);

		}

		private void tabPage11_Click(object sender, EventArgs e)
		{

		}

		private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=67ED48Z9C7SE8");
		}

		bool checknextcheck = false;
		private void ModListe_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			if (ModListe.Items[e.Index].Checked) ModListe.Items[e.Index].Group = ModListe.Groups["inaktive"];
			else ModListe.Items[e.Index].Group = ModListe.Groups["aktive"];
			checknextcheck = true;
		}

		private void tabPage1_Enter(object sender, EventArgs e)
		{
			//listBox1.SelectedIndex
			if (PluginListBox.SelectedIndex < 0 && PluginListBox.Items.Count > 0)
			{
				PluginListBox.SelectedIndex = 0;
			}
		}

		private void refreshToolStripMenuItem_Click_1(object sender, EventArgs e)
		{
			RebuildProfileList();
		}


		private void tabPage7_Enter(object sender, EventArgs e)
		{
			if (ModListe.Columns.Count > 0)
				ModListe.Columns[ModListe.Columns.Count - 1].Width = -2;
		}


		#region Assembly Attribute Accessors

		public string AssemblyTitle
		{
			get
			{
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
				if (attributes.Length > 0)
				{
					AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
					if (titleAttribute.Title != "")
					{
						return titleAttribute.Title;
					}
				}
				return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
			}
		}

		public string AssemblyVersion
		{
			get
			{
				return Assembly.GetExecutingAssembly().GetName().Version.ToString();
			}
		}

		public string AssemblyDescription
		{
			get
			{
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
				if (attributes.Length == 0)
				{
					return "";
				}
				return ((AssemblyDescriptionAttribute)attributes[0]).Description;
			}
		}

		public string AssemblyProduct
		{
			get
			{
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
				if (attributes.Length == 0)
				{
					return "";
				}
				return ((AssemblyProductAttribute)attributes[0]).Product;
			}
		}

		public string AssemblyCopyright
		{
			get
			{
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
				if (attributes.Length == 0)
				{
					return "";
				}
				return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
			}
		}

		public string AssemblyCompany
		{
			get
			{
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
				if (attributes.Length == 0)
				{
					return "";
				}
				return ((AssemblyCompanyAttribute)attributes[0]).Company;
			}
		}
		#endregion

		private void textBox3_TextChanged(object sender, EventArgs e)
		{
			CurrentProfile.Settings.JavaProxyUser = textBox3.Text;
		}

		private void textBox4_TextChanged(object sender, EventArgs e)
		{
			CurrentProfile.Settings.JavaProxyPassword = ScrambleString(textBox4.Text);
		}

		private void regler_init_Scroll(object sender, EventArgs e)
		{
			CurrentProfile.Settings.JavaMemoryInit = regler_init.Value;
		}

		private void regler_max_Scroll(object sender, EventArgs e)
		{
			CurrentProfile.Settings.JavaMemoryMax = regler_max.Value;
		}



		private void ModListe_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				Array a = (Array)e.Data.GetData(DataFormats.FileDrop);

				if (a != null)
				{
					string s = a.GetValue(0).ToString();
					if (Path.GetExtension(s).ToLower() == ".zip" && ZipFile.CheckZip(s))
					{
						this.Activate();        // in the case Explorer overlaps this form
						e.Effect = DragDropEffects.Copy;
					}
				}


			}
		}

		private void label10_DragDrop(object sender, DragEventArgs e)
		{
			//ZipFile UnzipMe = null;
			try
			{
				Array a = (Array)e.Data.GetData(DataFormats.FileDrop);

				if (a != null)
				{
					string s = a.GetValue(0).ToString();
					File.Copy(s, LauncherFolderStructure.ModFolder + Path.DirectorySeparatorChar + Path.GetFileName(s));
					RebuildModList();
				}
			}
			catch
			{
			}
			finally
			{
				//if (UnzipMe != null) UnzipMe.Close();
			}
		}

		private void MinecraftVersionList_SelectedValueChanged(object sender, EventArgs e)
		{

		}

		public void AddConsoleLine(string Line)
		{
			MethodInvoker asdf = delegate
			{
				string pre = "";
				if (pictureBox3.Tag is string) pre = pictureBox3.Tag as string;

				int start = 0;
				string temp;
				string text = "";
				do
				{
					temp = Line.Substring(start, Math.Min(60, Line.Length - start));
					start += temp.Length;
					text += temp + Environment.NewLine;
				} while ((Line.Length - start) != 0);
				text += Environment.NewLine + pre;

				pictureBox3.Tag = text;
				pictureBox3.Invalidate();
			};
			if (this.InvokeRequired) this.Invoke(asdf);
			else asdf.Invoke();

			Application.DoEvents();
		}

		void ClearConsole()
		{
			pictureBox3.Tag = "";
			pictureBox3.Invalidate();
			Application.DoEvents();
		}

		private void pictureBox3_Paint(object sender, PaintEventArgs e)
		{
			using (Font myFont = new Font(MinecraftFont, 12))
			{
				if (pictureBox3.Tag is string)
					e.Graphics.DrawString((string)pictureBox3.Tag, myFont, Brushes.White, new Point(2, 2));
			}
		}


		public void SwitchToConsoleScreen()
		{
			panel4.Visible = true;
			pictureBox3.Visible = true;

			tabControl1.Visible = false;

			StatusProfilListe.Enabled = false;
			toolStripSplitButton2.Enabled = false;

			panel4.Dock = DockStyle.Fill;
			pictureBox3.Size = panel4.Size;
			Application.DoEvents();
		}


		public void SwitchToMainWindow()
		{
			tabControl1.Visible = true;

			panel4.Visible = false;
			pictureBox3.Visible = false;

			StatusProfilListe.Enabled = true;
			toolStripSplitButton2.Enabled = true;

			tabControl1.Dock = DockStyle.Fill;
			Application.DoEvents();
		}


		private void ModListe_DragDrop(object sender, DragEventArgs e)
		{
			ModlistSave();
		}



		List<LauncherProfile> RebuildJarList = new List<LauncherProfile>();
		private void BuildJarInBackground(LauncherProfile Profil, bool force = false)
		{
			if (!Profil.Settings.MinecraftRememberPassword && !force)
			{
				Profil.Setting_LauncherModAnalyzeStatus = 0;
				RefreshModIcon();
				AddModInfo("Cache kann nur bei Remember Password im Hintergrund gebildet werden.", Profil);
				return;
			}

			if (ModCompatibilityChecker.IsBusy)
			{
				if (ProfileInCheck != null && ProfileInCheck.ProfileName == Profil.ProfileName) ModCompatibilityChecker.CancelAsync();
				if (RebuildJarList.Contains(Profil)) return;
				RebuildJarList.Add(Profil);
			}
			else
			{
				ModCompatibilityChecker.RunWorkerAsync(Profil);
			}
		}



		private void ModlistSave()
		{
			if (buildmodlist) return;
			try
			{
				string Mods = "";
				bool first = true;
				foreach (ListViewItem it in ModListe.Items)
				{
					if (it == null || !it.Checked) continue;
					if (!first) Mods += "|";
					first = false;
					Mods += it.Text;
				}
				CurrentProfile.Settings.MinecraftModListe = Mods;
				CurrentProfile.InvalidateJar();
				AddModInfo("Mods geändert. -> Cache neu aufbauen.", CurrentProfile);
				BuildJarInBackground(CurrentProfile);
			}
			catch
			{
			}
		}

		private void ModListe_ItemChecked(object sender, ItemCheckedEventArgs e)
		{
			if (checknextcheck)
			{
				checknextcheck = false;
				ModlistSave();
			}
		}

		private void ImportFromMinecraft()
		{
			ClearConsole();
			SwitchToConsoleScreen();
			int i = 1;

			string name;
			bool found = true;

			do
			{
				name = "Minecraft Import " + i.ToString();
				found = true;
				foreach (LauncherProfile profil in LauncherProfile.GetLauncherProfileList())
				{
					if (name == profil.ProfileName)
					{
						found = false;
						i++;
						break;
					}
				}
			} while (!found);

			LauncherProfile NewProfile = LauncherProfile.Load(name);
			NewProfile.Save();
			string env = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

			CopyDirectory(new DirectoryInfo(env + Path.DirectorySeparatorChar + ".minecraft"), new DirectoryInfo(LauncherFolderStructure.GetSpecificProfilePath(NewProfile) + Path.DirectorySeparatorChar + ".minecraft"));

			if (File.Exists(LauncherFolderStructure.GetSpecificBinFolder(NewProfile) + Path.DirectorySeparatorChar + "minecraft.jar"))
			{
				string ff = "MinecraftImport";
				while (File.Exists(LauncherFolderStructure.BinjarPath + Path.DirectorySeparatorChar + ff + ".jar")) ff += "_";


				File.Copy(LauncherFolderStructure.GetSpecificBinFolder(NewProfile) + Path.DirectorySeparatorChar + "minecraft.jar", LauncherFolderStructure.BinjarPath + Path.DirectorySeparatorChar + ff + ".jar");
				NewProfile.Settings.MinecraftBinJarHash = GetMD5HashFromFile(LauncherFolderStructure.BinjarPath + Path.DirectorySeparatorChar + ff + ".jar");
			}
			else
			{
				AddLogLine("$WMCL> Beim import wurde keine minecraft.jar im Bin-Ordner gefunden!!!!");
			}

			RebuildLocations();
			RebuildProfileList();

			GUISelectProfile(NewProfile.ProfileName);
			Application.DoEvents();
			SwitchToMainWindow();

		}

		void CopyDirectory(DirectoryInfo source, DirectoryInfo destination, bool WriteToConsole = false, int dist = 0)
		{
			if (!destination.Exists)
			{
				destination.Create();
			}

			// Copy all files.
			FileInfo[] files = source.GetFiles();
			foreach (FileInfo file in files)
			{
				file.CopyTo(Path.Combine(destination.FullName,
					 file.Name));

				string dirout = Path.Combine(source.FullName, file.Name);
				dirout = dirout.Replace(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar + ".minecraft" + Path.DirectorySeparatorChar, "");
				AddConsoleLine(dirout);
				//Application.DoEvents();
			}

			// Process subdirectories.
			DirectoryInfo[] dirs = source.GetDirectories();
			foreach (DirectoryInfo dir in dirs)
			{
				// Get destination directory.
				string destinationDir = Path.Combine(destination.FullName, dir.Name);

				// Call CopyDirectory() recursively.
				CopyDirectory(dir, new DirectoryInfo(destinationDir), WriteToConsole, dist + 1);
			}
		}

		private void toolStripSplitButton2_ButtonClick(object sender, EventArgs e)
		{

		}

		private void dshfdsfhToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("Willst du das Profil '" + CurrentProfile.ProfileName + "' wirklick löschen?", "Profil löschen?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != System.Windows.Forms.DialogResult.Yes) return;

			if (LauncherProfile.GetLauncherProfileList().Count > 1)
			{
				CurrentProfile.Delete();
				RebuildProfileList();
			}
			else
			{
				MessageBox.Show("Das letzte Profil kann nicht gelöscht werden.");
			}
		}

		private void toolStripTextBox2_Click(object sender, EventArgs e)
		{

		}

		private void asfToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("Soll dein aktueller \".minecraft\"-Ordner importiert werden? Das kann je nach Save-Games und Mods 1-2Minuten dauern?", "Importieren?", MessageBoxButtons.YesNo) != System.Windows.Forms.DialogResult.Yes) return;
			ImportFromMinecraft();
		}

		private void verknüpfungToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
			sfd.Filter = "Verknüpfung|*.lnk";
			sfd.FileName = "Minecraft - " + CurrentProfile.ProfileName;
			sfd.FileOk += new CancelEventHandler(sfd_FileOk);
			sfd.ShowDialog();
		}

		private void asfToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			int i = 1;

			string name;
			bool found = true;

			do
			{
				name = "New Profile " + i.ToString();
				found = true;
				foreach (LauncherProfile profil in LauncherProfile.GetLauncherProfileList())
				{
					if (name == profil.ProfileName)
					{
						found = false;
						i++;
						break;
					}
				}
			} while (!found);

			LauncherProfile NewProfile = LauncherProfile.Load("New Profile " + i.ToString());
			NewProfile.Save();
			RebuildProfileList();
			GUISelectProfile(NewProfile.ProfileName);
		}

		private void button1_Click_1(object sender, EventArgs e)
		{
			string[] hashfiles = Directory.GetFiles(LauncherFolderStructure.BinjarPath, "*.md5");
			foreach (string hashfile in hashfiles)
			{
				try
				{//probier zum löschen, aber ist nicht wichtig ^^
					File.Delete(hashfile);
				}
				catch
				{

				}
			}

			RefreshVersions();
		}

		private void LocationLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			MinecraftLocations = (Dictionary<string, MinecraftBinJarLocation>)e.Result;
			BuildMinecraftJarList();
		}

		private void LocationLoader_DoWork(object sender, DoWorkEventArgs e)
		{
			bool fast = (bool)e.Argument;

			Dictionary<string, MinecraftBinJarLocation> Locations = new Dictionary<string, MinecraftBinJarLocation>();
			Locations.Clear();

			#region Laden Lokale Binjars

			string[] binjars = Directory.GetFiles(LauncherFolderStructure.BinjarPath, "*.jar");
			foreach (string file in binjars)
			{
				string hashfile = Path.ChangeExtension(file, "md5");
				MinecraftBinJarLocation nloc = new MinecraftBinJarLocation(Path.GetFileNameWithoutExtension(file), "Lokal", file, GetMD5HashFromFile(file, hashfile));
				if (!Locations.ContainsKey(nloc.Hash)) Locations.Add(nloc.Hash, nloc);
			}

			#endregion




			MethodInvoker asdf = delegate
			{
				MinecraftLocations = Locations;
				BuildMinecraftJarList();
			};
			if (this.InvokeRequired) this.Invoke(asdf);
			else asdf.Invoke();




			#region Laden der InternetListe
			XmlDocument doc = new XmlDocument();
			try
			{
				doc.Load("http://dev.wischenbart.org/minecraft/mcsvc.php");
			}
			catch (Exception ex)
			{
				AddLogLine("$WMCL>" + ex.Message + Environment.NewLine + ex.StackTrace);
			}

			XmlNodeList nodelist = doc.SelectNodes("/WischiLauncherList/MinecraftJars/Minecraft");

			foreach (XmlNode node in nodelist)
			{
				try
				{
					string text = node.Attributes["Type"].InnerText + " " + node.Attributes["Version"].InnerText;
					string post = node.Attributes["Post"].InnerText.Trim();
					if (!string.IsNullOrEmpty(post)) text += " (" + post + ")";

					MinecraftBinJarLocation loc = new MinecraftBinJarLocation(text, "Online", node.Attributes["Location"].InnerText, node.Attributes["MD5"].InnerText.ToUpper(), true);
					if (!Locations.ContainsKey(loc.Hash)) Locations.Add(loc.Hash, loc);
				}
				catch (Exception ex)
				{
					AddLogLine("$WMCL> Fehler Jar-Add: " + ex.Message);
				}
			}
			#endregion

			e.Result = Locations;
		}


		LauncherProfile ProfileInCheck;

		private void MergeList(Dictionary<string, string> A, Dictionary<string, string> B)
		{
			foreach (KeyValuePair<string, string> it in B)
			{
				if (A.ContainsKey(it.Key)) A[it.Key] += "|" + it.Value;
				else A.Add(it.Key, it.Value);
			}
		}


		private void AddModInfo(string Element, LauncherProfile Profil, bool Enabled = true)
		{
			MethodInvoker asdf = delegate
			{
				ToolStripItem it;
				if (Element == "separator")
				{
					it = new ToolStripSeparator();
					ModAnalyzeInfo.DropDownItems.Add(it);
				}
				else
				{
					it = ModAnalyzeInfo.DropDownItems.Add(DateTime.Now.ToString("HH:mm:ss.fff") + " [" + Profil.ProfileName + "] " + Element);
					AddConsoleLine(Element);
				}

				while (ModAnalyzeInfo.DropDownItems.Count > 30) ModAnalyzeInfo.DropDownItems.RemoveAt(0);
				it.Enabled = Enabled;
			};
			if (this.InvokeRequired) this.Invoke(asdf);
			else asdf.Invoke();
		}


		private void ModCompatibilityChecker_DoWork(object sender, DoWorkEventArgs e)
		{
			if (!(e.Argument is LauncherProfile && sender is BackgroundWorker)) return;

			BackgroundWorker s = sender as BackgroundWorker;

			Dictionary<string, string> ModListControl = new Dictionary<string, string>();

			LauncherProfile Profil = e.Argument as LauncherProfile;

			MethodInvoker asdf = delegate
			{
				ProfileInCheck = Profil;
				Profil.Setting_LauncherModAnalyzeStatus = 0;
				RefreshModIcon();
				ModAnalyzeInfo.DropDown.Items.Clear();
			};
			if (this.InvokeRequired) this.Invoke(asdf);
			else asdf.Invoke();

			Profil.Settings.MinecraftBinJarCache = "";


			string sid;

			if (s.CancellationPending) return;

			CookieContainer cc = null;

			if (!MinecraftLocations.ContainsKey(Profil.Settings.MinecraftBinJarHash))
			{
				Profil.Setting_LauncherModAnalyzeStatus = 0;
				AddModInfo("Wähle eine andere Minecraft-Version aus!", Profil);
				return;
			}

			string minecraftuser = "";
			MinecraftBinJarLocation loc = MinecraftLocations[Profil.Settings.MinecraftBinJarHash];
			AddModInfo("Lade Jar: " + loc.Name, Profil);
			if (loc.NeedAuth)
			{
				AddConsoleLine("Verifiziere Account...");
				AddModInfo("Verifiziere Account...", Profil);
				if (!MinecraftAuth.ProfileAuth(Profil, out sid, out minecraftuser, textBox11.Text))
				{
					Profil.Setting_LauncherModAnalyzeStatus = 0;
					AddModInfo("minecraft.net Fehler: " + sid, Profil);
					return;
				}

				AddModInfo("Verbinde zu http://dev.wischenbart.org/minecraft", Profil);
				string Hinweis;
				cc = MinecraftAuth.WischiAuth(sid, minecraftuser, out Hinweis);
			}

			if (s.CancellationPending) return;

			AddModInfo("> " + loc.DownloadLocation, Profil);
			Application.DoEvents();
			MinecraftBinJar mjar = null;
			try
			{
				mjar = loc.GetMinecraftBinJar(cc);
			}
			catch (Exception ex)
			{
				AddLogLine("$WMCL> MinecraftJar konnte nicht geladen werden: " + ex.Message);
				AddModInfo("JarFehler: " + ex.Message, Profil);
				Profil.Setting_LauncherModAnalyzeStatus = 0;
				return;
			}

			if (loc.NeedAuth)
			{
				string location = LauncherFolderStructure.BinjarPath + Path.DirectorySeparatorChar + SanitizeFileName(loc.Name) + ".jar";
				mjar.Save(location);
			}

			if (s.CancellationPending) return;

			if (Profil.Settings.MinecraftServerAutoConnect)
			{
				string WischiPatch = Assembly.GetExecutingAssembly().GetName().Name + ".Resources." + "WMLPatch.class";
				Stream WischiPatchStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(WischiPatch);
				mjar.AddModFile(WischiPatchStream, "net/minecraft/client/WMLPatch.class");
				ModListControl.Add("patch|net/minecraft/client/WMLPatch.class", "WischiPatch");
			}

			Application.DoEvents();
			AddModInfo("Patche Mods in Binjar", Profil);
			string[] mdlist = Profil.Settings.MinecraftModListe.Split('|');
			foreach (string it in mdlist)
			{
				if (s.CancellationPending) return;
				if (it.Trim() == "") continue;
				AddModInfo("Mod: " + it, Profil);
				string mod = LauncherFolderStructure.ModFolder + Path.DirectorySeparatorChar + it;
				if (File.Exists(mod) && ZipFile.CheckZip(mod))
				{
					ZipFile modfile = new ZipFile(mod);
					Dictionary<string, string> mods = mjar.AddModPack(modfile, Profil);
					MergeList(ModListControl, mods);
					modfile.Dispose();
				}
				else
				{
					AddLogLine("$WMCL> Der Mod konnte nicht geladen werden: '" + mod + "'");
					AddModInfo("ModFehler: " + mod, Profil);
				}
			}

			Application.DoEvents();

			string tempString = Guid.NewGuid().ToString().ToUpper();
			Profil.Settings.MinecraftBinJarCache = LauncherFolderStructure.GetSpecificBinFolder(Profil) + Path.DirectorySeparatorChar + tempString;
			mjar.Save(Profil.Settings.MinecraftBinJarCache);

			Dictionary<string, List<string>> Compatibilitylist = new Dictionary<string, List<string>>();

			foreach (KeyValuePair<string, string> it in ModListControl)
			{
				string[] dd = it.Value.Split('|');
				if (dd.Length > 1)
				{
					Compatibilitylist.Add(it.Key, new List<string>(dd));
				}
			}

			MethodInvoker fff = delegate
			{
				foreach (KeyValuePair<string, List<string>> iii in Compatibilitylist)
				{
					ToolStripItem kkk = ModAnalyzeInfo.DropDownItems.Add(iii.Key);
					kkk.Enabled = false;
					ModAnalyzeInfo.DropDownItems.Add(new ToolStripSeparator());
					foreach (string mod in iii.Value)
					{
						ToolStripItem uuu = ModAnalyzeInfo.DropDownItems.Add(mod);
						uuu.Enabled = false;
					}
					ModAnalyzeInfo.DropDownItems.Add("");
				}

				if (Compatibilitylist.Count == 0)
				{
					AddModInfo("keine Inkompatibilitäten gefunden", Profil);
					Profil.Setting_LauncherModAnalyzeStatus = 1;
				}
				else
				{
					Profil.Setting_LauncherModAnalyzeStatus = -1;
				}
			};
			if (this.InvokeRequired) this.Invoke(fff);
			else fff.Invoke();

		}

		private void ModCompatibilityChecker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			ProfileInCheck = null;
			if (RebuildJarList.Count > 0)
			{
				LauncherProfile RenderProfil = RebuildJarList[0];
				ModCompatibilityChecker.RunWorkerAsync(RenderProfil);
				for (int i = RebuildJarList.Count - 1; i >= 0; i--)
				{
					if (RebuildJarList[i].ProfileName == RenderProfil.ProfileName) RebuildJarList.RemoveAt(i);
				}
				AddModInfo("Erstelle minecraft.jar für Profil aus Warteschlange. (noch verbleibend: " + RebuildJarList.Count + ")", RenderProfil);
			}

			RefreshModIcon();

		}

		private string SanitizeFileName(string filename)
		{
			string name = filename;
			foreach (char c in Path.GetInvalidFileNameChars())
				name = name.Replace(c, '_');
			return name;
		}

		private void ModAnalyzeInfo_ButtonClick(object sender, EventArgs e)
		{
			ModAnalyzeInfo.DropDown.Items.Clear();
			AddModInfo("Cache neu aufbauen (manuell ausgelöst)", CurrentProfile);
			BuildJarInBackground(CurrentProfile, true);
			RefreshModIcon();
		}

		private void button2_Click_1(object sender, EventArgs e)
		{
			OpenExplorer(LauncherFolderStructure.GetSpecificProfilePath(CurrentProfile));
		}

		private static void OpenExplorer(string path)
		{
			if (Directory.Exists(path))
				Process.Start("explorer.exe", path);
		}

		private void button3_Click(object sender, EventArgs e)
		{
			OpenExplorer(LauncherFolderStructure.ModFolder);
		}

		private void button5_Click_1(object sender, EventArgs e)
		{
			RebuildModList();
		}

		private void gfdfhToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ProfileName hgh = new ProfileName(CurrentProfile.ProfileName);
			hgh.ShowDialog();
			CurrentProfile.Rename(hgh.ProfilName);
			RebuildProfileList();
			GUISelectProfile(hgh.ProfilName);
		}

		private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			CurrentProfile.Settings.MinecraftPrio = comboBox1.Text;
		}

		private void GarbadgeCollector_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				List<LauncherProfile> Profiles = LauncherProfile.GetLauncherProfileList();
				foreach (LauncherProfile lp in Profiles)
				{
					string folder = Path.GetDirectoryName(lp.Settings.MinecraftBinJarCache);
					string file = Path.GetFileName(lp.Settings.MinecraftBinJarCache);

					string[] files = Directory.GetFiles(folder);
					foreach (string ff in files)
					{
						string fileshort = Path.GetFileName(ff);
						if (!Regex.IsMatch(fileshort, "^[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}$"))
							continue;
						if (fileshort != file) File.Delete(ff);
					}
				}
			}
			catch (Exception ex)
			{
				AddLogLine("Error GC: " + ex.Message);
			}
		}

		private void WischiLauncherMainForm_Load(object sender, EventArgs e)
		{
			Random rnd = new Random();
			CrackTools.Visible = AllowOffline;
			FakePlayerName.Text = "Player" + rnd.Next(1000, 9999);
			crackedToolStripMenuItem.Checked = AllowOffline;
			GarbadgeCollector.RunWorkerAsync();
		}

		private void Size_CheckedChanged(object sender, EventArgs e)
		{
			if (normalRadio.Checked) CurrentProfile.Settings.MinecraftStartState = 0;
			else if (UserRadio.Checked) CurrentProfile.Settings.MinecraftStartState = 2;
			else if (MaximiertRadio.Checked) CurrentProfile.Settings.MinecraftStartState = 1;

			WindowStateInfo.Visible = UserRadio.Checked;

		}

		private void WindowStateInfo_TextChanged(object sender, EventArgs e)
		{
			CurrentProfile.Settings.MinecraftStateInfo = WindowStateInfo.Text;
		}

		private void toolStripMenuItem1_Click(object sender, EventArgs e)
		{

		}

		private void crackedToolStripMenuItem_Click(object sender, EventArgs e)
		{
			crackedToolStripMenuItem.Checked = !crackedToolStripMenuItem.Checked;
			AllowOffline = crackedToolStripMenuItem.Checked;
		}

		private void timer2_Tick(object sender, EventArgs e)
		{
			if (!StatusRefresh.IsBusy) StatusRefresh.RunWorkerAsync();
		}

		private void StatusRefresh_DoWork(object sender, DoWorkEventArgs e)
		{
			StatusAnswer antwort = new StatusAnswer();
			StatusCodes worstcode = StatusCodes.unknown;
			try
			{
				string status = new StreamReader(HttpWebRequest.Create("http://status.mojang.com/check").GetResponse().GetResponseStream()).ReadToEnd();
				MatchCollection mc = Regex.Matches(status, "{\"([^\"]*)\":\"([^\"]*)\"}");
				Dictionary<string, string> IconMapping = new Dictionary<string, string>();

				foreach (Match m in mc)
				{
					if (m.Groups.Count != 3) continue;
					StatusCodes curcode = StatusCodes.unknown;

					switch (m.Groups[2].Value)
					{
						case "green": curcode = StatusCodes.check; break;
						case "red": curcode = StatusCodes.red; break;
						case "yellow": curcode = StatusCodes.warning; break;
					}

					if (worstcode < curcode) worstcode = curcode;

					antwort.Messages.Add(new StatusMessage(curcode, m.Groups[1].Value));
				}
			}
			catch (Exception ex)
			{
				antwort.Messages.Add(new StatusMessage(StatusCodes.red, "Statusdienst nicht erreichbar: " + ex.Message));
			}
			antwort.ErrorCode = worstcode;
			e.Result = antwort;
		}

		private void StatusRefresh_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			StatusAnswer ans = (StatusAnswer)e.Result;
			mcserverstatus.DropDownItems.Clear();
			foreach (StatusMessage a in ans.Messages)
			{
				mcserverstatus.DropDownItems.Add(a.Message, imageList3.Images[a.IconID.ToString()]);
			}
			mcserverstatus.Image = imageList3.Images[ans.ErrorCode.ToString()];
		}

		private void logfensterÖffnenToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ExternalLog log = new ExternalLog(this);
			log.Show();
		}

		private void StatusresetTimer_Tick(object sender, EventArgs e)
		{
			StatusresetTimer.Stop();
			StatusTextLabel.Text = "Bereit.";
		}

		private void SetStatusText(string Text)
		{
			StatusresetTimer.Stop();
			StatusresetTimer.Start();
			StatusTextLabel.Text = Text;
		}

		private void checkBox7_CheckedChanged(object sender, EventArgs e)
		{
			CurrentProfile.Settings.LauncherWaitNonResponding = checkBox7.Checked;
		}

		private void PluginListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (PluginListBox.SelectedItem is WischiLauncherPlugin)
			{
				var plugin = PluginListBox.SelectedItem as WischiLauncherPlugin;
				PluginControlHolder.Controls.Clear();
				PluginControlHolder.Controls.Add(plugin.PluginFrame);
				plugin.PluginFrame.Dock = DockStyle.Fill;

				PluginAutorLabel.Text = "Autor: " + plugin.Autor;
				PluginDescriptionLabel.Text = plugin.Description;
				PluginNameLabel.Text = plugin.Name;
				PluginVersionLabel.Text = "Version: " + plugin.Version.ToString();
			}
		}

		private void label11_Click(object sender, EventArgs e)
		{

		}

		private void label10_Click(object sender, EventArgs e)
		{

		}

	}

	enum StatusCodes
	{
		unknown,
		check,
		warning,
		red
	}

	class StatusAnswer
	{
		public StatusCodes ErrorCode = StatusCodes.unknown;
		public List<StatusMessage> Messages = new List<StatusMessage>();
	}


	class StatusMessage
	{
		public StatusCodes IconID { get; private set; }
		public string Message { get; private set; }
		public StatusMessage(StatusCodes IconID, string Message)
		{
			this.IconID = IconID;
			this.Message = Message;
		}
	}

}
