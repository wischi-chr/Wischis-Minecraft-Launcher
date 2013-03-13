using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;

namespace CGMinerNET
{
	public class CGMiner : INotifyPropertyChanged
	{
		#region Properties

		/// <summary>
		/// Liefert den aktuellen Status des Miners
		/// </summary>
		public RunningStates RunningState
		{
			get
			{
				lock (runningStateLocker)
				{
					return this.runningState;
				}
			}
			private set
			{
				bool changed = false;
				RunningStates newstate;

				lock (runningStateLocker)
				{
					newstate = value;
					if (this.runningState != value)
					{
						changed = true;
						this.runningState = value;
					}
				}

				if (changed)
				{
					this.OnPropertyChanged("RunningState");
					if (MinerStateChanged != null) MinerStateChanged(this, EventArgs.Empty);
				}
			}
		}
		private readonly object runningStateLocker = new object();
		private RunningStates runningState = RunningStates.Stopped;

		/// <summary>
		/// Gibt die Adresse des Pool-Servers an
		/// </summary>
		public string PoolServerUrl
		{
			get
			{
				lock (poolServerUrlLocker)
				{
					return poolServerUrl;
				}
			}
			set
			{
				lock (poolServerUrlLocker)
				{
					if (RunningState != RunningStates.Stopped) throw new Exception("Kann nicht im Betrieb umgestellt werden");
					poolServerUrl = value;
					this.OnPropertyChanged("PoolServerUrl");
				}
			}
		}
		private readonly object poolServerUrlLocker = new object();
		private string poolServerUrl;

		/// <summary>
		/// Gibt den Worker Usernamen an
		/// </summary>
		public string Username
		{
			get
			{
				lock (usernameLocker)
				{
					return username;
				}
			}
			set
			{
				lock (usernameLocker)
				{
					if (RunningState != RunningStates.Stopped) throw new Exception("Kann nicht im Betrieb umgestellt werden");
					username = value;
					this.OnPropertyChanged("Username");
				}
			}
		}
		private readonly object usernameLocker = new object();
		private string username;

		/// <summary>
		/// Legt das Passwort für den Worker User fest
		/// </summary>
		public string Password
		{
			get
			{
				lock (passwordLocker)
				{
					return password;
				}
			}
			set
			{
				lock (passwordLocker)
				{
					if (RunningState != RunningStates.Stopped) throw new Exception("Kann nicht im Betrieb umgestellt werden");
					password = value;
					this.OnPropertyChanged("Password");
				}
			}
		}
		private readonly object passwordLocker = new object();
		private string password;

		/// <summary>
		/// Legt den Pfad für den MinerProzess fest
		/// </summary>
		public string CGMinerPath
		{
			get
			{
				lock (cgminerpathLocker)
				{
					return this.cgminerpath;
				}
			}
			set
			{
				lock (cgminerpathLocker)
				{
					if (this.cgminerpath != value)
					{
						this.cgminerpath = value;
						this.OnPropertyChanged("CGMinerPath");
					}
				}
			}
		}
		private readonly object cgminerpathLocker = new object();
		private string cgminerpath = "cgminer.exe";

		/// <summary>
		/// Liefert Informationen zu geänderten Eigenschaften (für Bindings & Co.)
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged(string Prop) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(Prop)); }


		#endregion

		#region Events

		//ToDo: HandlerTypen anpassen
		public event EventHandler MinerStateChanged;

		#endregion

		#region InnerTypes

		/// <summary>
		/// Stellt die Laufzustände des Miners dar
		/// </summary>
		public enum RunningStates
		{
			Starting,
			Running,
			Stopping,
			Stopped
		}

		#endregion

		#region Private Variablen

		private Process minerProcess = null;
		private object startStopLocker = new object();
		private int apiport;

		#endregion

		#region Konstruktoren

		public CGMiner()
		{
		}

		#endregion

		#region Miner Start Routinen

		/// <summary>
		/// Startet den CGMiner und wartet bis dieser Betriebsbereit ist.
		/// </summary>
		private void Start()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Starten des CGMiner Prozesses asynchron (Starten wird eingeleitet). Wenn Miner Einsatzbereit ist, wird MinerStarted ausgelöst.
		/// </summary>
		public void StartAsync()
		{
			new Thread(StartThread).Start();
		}

		/// <summary>
		/// Der Starter-Thread für den Asynchronen Start
		/// </summary>
		private void StartThread()
		{
			lock (startStopLocker)
			{
				if (RunningState != RunningStates.Stopped) return;

				try
				{
					RunningState = RunningStates.Starting;

					#region Vorbereiten der Daten

					apiport = new Random().Next(4000, 4999);

					#endregion

					#region Vorbereiten der Argumente

					List<string> Args = new List<string>();
					Args.Add("--api-listen");
					Args.Add("-u"); Args.Add(Username);
					Args.Add("-p"); Args.Add(Password);
					Args.Add("-o"); Args.Add(PoolServerUrl);
					Args.Add("--api-port"); Args.Add(apiport.ToString());
					Args.Add("--api-allow"); Args.Add("W:0/0");

					#endregion

					#region Prozess aufsetzen und starten

					minerProcess = new Process();
					minerProcess.StartInfo.CreateNoWindow = true;
					minerProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
					minerProcess.StartInfo.FileName = cgminerpath;
					minerProcess.StartInfo.Arguments = CreateArgs(Args);
					minerProcess.EnableRaisingEvents = true;
					minerProcess.Exited += MinerProcess_Exited;
					minerProcess.Start();

					#endregion

					#region Warten auf den API Port

					int tries = 20;
					while (!minerProcess.HasExited)
					{
						if (tries <= 0) throw new Exception("Miner API antwortet nicht!");

						try { SendInternalRequest("version"); break; }
						catch { }

						Thread.Sleep(250);
						tries--;
					}

					#endregion

					RunningState = RunningStates.Running;
				}
				catch (Exception)
				{
					//Falls es zu Fehlern kommt den Prozess eventuell beenden und dann den Status setzen.
					try { if (minerProcess != null && !minerProcess.HasExited) minerProcess.Kill(); }
					catch { }

					//ToDo: Eventuell benötigtes noch auslesen bevor verworfen wird.
					if (minerProcess != null) minerProcess = null;

					//Running-Status wieder auf Stopped setzen
					RunningState = RunningStates.Stopped;
				}
			}
		}

		#endregion

		#region Miner Stop Routinen

		/// <summary>
		/// Stoppt den CGMiner und wartet bis der Prozess beendet ist
		/// </summary>
		private void Stop()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Stoppt den CGMiner Prozess asynchron (Beenden wird durch API "quit" eingeleitet". Wenn Prozess beendet wurde, wird MinerStopped ausgelöst.
		/// </summary>
		public void StopAsync()
		{
			new Thread(StopThread).Start();
		}

		/// <summary>
		/// Stellt den Stop-Thread dar der für die Asynchrone Beendung benötigt wird
		/// </summary>
		private void StopThread()
		{
			lock (startStopLocker)
			{
				if (RunningState != RunningStates.Running) return;

				RunningState = RunningStates.Stopping;

				//verhindern, dass durch das Beenden der Stopp-Mechanismus erneut ausgelöst wird
				minerProcess.Exited -= MinerProcess_Exited;

				//Senden des "quit" - Signals
				try { var resp = SendInternalRequest("quit"); }
				catch { }

				int tries = 4;
				while (!minerProcess.HasExited)
				{
					Thread.Sleep(1000);
					tries--;
					if (tries <= 0) minerProcess.Kill();
				}

				//ToDo: eventuelle Informtionen noch auslesen vor dem Freigeben
				minerProcess = null;

				RunningState = RunningStates.Stopped;
			}
		}

		#endregion

		//TODO: Finde einen Autoshutdown für waise miner
		/// <summary>
		/// Beendet alle cgminer ausser diesen hier um überbleibsel bei dirty close zu verhindern
		/// </summary>
		public void CleanUpProcesses()
		{
			int currentid = minerProcess == null ? -1 : minerProcess.Id;
			foreach (var p in Process.GetProcessesByName("cgminer"))
				if (p.ProcessName == "cgminer" && p.Id != currentid) p.Kill();
		}

		/// <summary>
		/// Setzt eine API Anfrage beim Miner ab
		/// </summary>
		/// <param name="Command"></param>
		/// <returns></returns>
		public string SendRequest(string Command)
		{
			if (RunningState != RunningStates.Running) throw new Exception("Um ein Commando abzusetzen muss der Miner aktiv sein");
			return SendInternalRequest(Command);
		}

		/// <summary>
		/// Sollte der Prozess unerwartet beendet werden, dann wird diese Methode ausgelöst und korrigiert den Status des Wrappers + Liefert eventuelle Fehlermeldungen
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void MinerProcess_Exited(object sender, EventArgs e)
		{
			StopAsync();
		}

		private string CreateArgs(IEnumerable<string> Args)
		{
			StringBuilder sb = new StringBuilder();
			List<string> newargs = new List<string>();
			foreach (string arg in Args) newargs.Add("\"" + arg + "\"");
			return string.Join(" ", newargs.ToArray());
		}

		/// <summary>
		/// API Anfrage ohne Prüfung des Aktuellen RunningStates
		/// </summary>
		/// <param name="Command"></param>
		/// <returns></returns>
		private string SendInternalRequest(string Command)
		{
			var c = new TcpClient();
			c.Connect(new IPEndPoint(IPAddress.Loopback, apiport));
			var ns = c.GetStream();
			byte[] cmd = Encoding.ASCII.GetBytes(Command);
			ns.Write(cmd, 0, cmd.Length);

			MemoryStream ms = new MemoryStream();
			int cur;
			while ((cur = ns.ReadByte()) > 0) ms.WriteByte((byte)cur);
			byte[] response = ms.ToArray();

			return Encoding.ASCII.GetString(response);
		}


	}


}
