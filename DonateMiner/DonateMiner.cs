using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using WischisMinecraftLauncherCoreDLL.PluginSystem;

namespace DonateMiner
{
	public class DonateMiner : WischiLauncherPlugin
	{
		public override string Name { get { return "Bitcoin Spenden Miner"; } }
		public override string Description { get { return "Unterstütze Wischi beim sammeln von Bitcoins"; } }
		public override string Autor { get { return "Wischi"; } }
		public override Guid ID { get { return new Guid("BA24558E-8B55-11E2-92D9-89A66188709B"); } }
		public override Version Version { get { return new Version(0, 1, 0, 0); } }
		public override Control PluginFrame { get { return (Control)DonateMinerView; } }

		private DonateMinerGUI DonateMinerView;
		private IPluginHost Host;
		private string WorkingPath;
		private CGMinerNET.CGMiner miner;
		private string cgminerdir
		{
			get
			{
				return Path.GetDirectoryName(miner.CGMinerPath);
			}
		}

		public override void Initialize(IPluginHost Host, string WorkingPath)
		{
			this.Host = Host;
			this.WorkingPath = WorkingPath;
			DonateMinerView = new DonateMinerGUI();
			this.miner = new CGMinerNET.CGMiner();

			miner.CGMinerPath = Path.Combine(WorkingPath, "cgminer" + Path.DirectorySeparatorChar + "cgminer.exe");
			miner.MinerStateChanged += miner_MinerStateChanged;

			var altminer = Path.Combine(WorkingPath, "miner.txt");
			if (File.Exists(altminer))
			{
				string[] parts = File.ReadAllText(altminer).Split('|');
				miner.Username = parts[0];
				miner.Password = parts[1];
			}
			else
			{
				miner.Username = "wischi.wischilauncher";
				miner.Password = "w15ch1l4unch3r";
			}

			miner.PoolServerUrl = "stratum.bitcoin.cz:3333";

			ValidateCGMiner();

			DonateMinerView.StartButton.Click += StartButton_Click;
			DonateMinerView.StopButton.Click += StopButton_Click;
			DonateMinerView.DataRefreshTimer.Tick += DataRefreshTimer_Tick;
		}

		void DataRefreshTimer_Tick(object sender, EventArgs e)
		{
			if (miner.RunningState != CGMinerNET.CGMiner.RunningStates.Running) return;
			try
			{
				var cont = miner.SendRequest("devs");
				string val = Regex.Match(cont, "MHS 5s=([^,]*)").Groups[1].Value;
				string shares = Regex.Match(cont, ",Accepted=([^,]*),").Groups[1].Value;
				DonateMinerView.MinerStatusText.Text = "Läuft. - Geschw.: " + val + " MHash/s  -  Shares: " + shares;
			}
			catch
			{
			}
		}

		private void ValidateCGMiner()
		{
			if (!Directory.Exists(cgminerdir)) Directory.CreateDirectory(cgminerdir);
			//ToDo: CGMiner überprüfen/downloaden
		}

		void miner_MinerStateChanged(object sender, EventArgs e)
		{
			var meth = new MethodInvoker(() =>
			{
				switch (miner.RunningState)
				{
					case CGMinerNET.CGMiner.RunningStates.Running:
						DonateMinerView.DataRefreshTimer.Start();
						DonateMinerView.StopButton.Enabled = true;
						DonateMinerView.StartButton.Enabled = false;
						DonateMinerView.MinerStatusText.Text = "Läuft.";
						break;

					case CGMinerNET.CGMiner.RunningStates.Starting:
						DonateMinerView.StartButton.Enabled = false;
						DonateMinerView.StopButton.Enabled = false;
						DonateMinerView.MinerStatusText.Text = "Arbeiter gehen in die Mine...";
						break;

					case CGMinerNET.CGMiner.RunningStates.Stopping:
						DonateMinerView.DataRefreshTimer.Stop();
						DonateMinerView.StartButton.Enabled = false;
						DonateMinerView.StopButton.Enabled = false;
						DonateMinerView.MinerStatusText.Text = "Arbeiter gehen nach Hause...";
						break;

					case CGMinerNET.CGMiner.RunningStates.Stopped:
						DonateMinerView.StopButton.Enabled = false;
						DonateMinerView.StartButton.Enabled = true;
						DonateMinerView.MinerStatusText.Text = "Beendet.";
						break;
				}
			});
			if (DonateMinerView != null)
			{
				if (DonateMinerView.InvokeRequired) DonateMinerView.Invoke(meth);
				else meth.Invoke();
			}
		}

		void StopButton_Click(object sender, EventArgs e)
		{
			miner.StopAsync();
		}

		void StartButton_Click(object sender, EventArgs e)
		{
			miner.CleanUpProcesses();
			if (!DownloadCGMiner()) return;
			miner.StartAsync();
		}

		private bool DownloadCGMiner()
		{
			try
			{
				DonateMinerView.MinerStatusText.Text = "Prüfe Minenarbeiter...";
				Application.DoEvents();
				string xml = new StreamReader(((HttpWebRequest)HttpWebRequest.Create("http://dev.wischenbart.org/minecraft/plugins/bitcoindonate/cgminerfiles.php")).GetResponse().GetResponseStream()).ReadToEnd();
				var doc = new XmlDocument();
				doc.LoadXml(xml);
				foreach (XmlNode n in doc.SelectNodes("/FileDownloadList/File"))
				{
					string target = Path.Combine(cgminerdir, n.Attributes["name"].Value);
					string sourceurl = "http://dev.wischenbart.org/minecraft/plugins/bitcoindonate/cgminer/" + n.Attributes["name"].Value;
					string targethashfile = target + ".md5";

					if (!File.Exists(target) || (GetMD5HashFromFile(target, targethashfile) != n.Attributes["md5"].Value))
					{
						if (File.Exists(target)) File.Delete(target);
						new WebClient().DownloadFile(sourceurl, target);
					}
				}
				return true;
			}
			catch(Exception ex)
			{
				DonateMinerView.MinerStatusText.Text = "Fehler beim Laden von cgminer: " + ex.Message;
				return false;
			}
		}

		private string GetMD5HashFromFile(string fileName, string hashcachefile = null)
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

		public override void Unload()
		{
			if (miner.RunningState == CGMinerNET.CGMiner.RunningStates.Running) miner.StopAsync();
			this.DonateMinerView = null;
			this.Host = null;
			this.WorkingPath = null;
		}


	}
}
