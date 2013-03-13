using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace WischisMinecraftLauncherCoreDLL.PluginSystem
{
	public abstract class WischiLauncherPlugin
	{
		#region Allgemeine Eigenschaften

		public abstract string Name { get; }
		public abstract string Description { get; }
		public abstract string Autor { get; }
		public abstract Guid ID { get; }
		public abstract Version Version { get; }
		public abstract Control PluginFrame { get; }
		public override string ToString() { return Name; }

		#endregion

		/// <summary>
		/// Lädt ein Plugin
		/// </summary>
		/// <param name="Host"></param>
		public abstract void Initialize(IPluginHost Host, string WorkingPath);

		/// <summary>
		/// Entlädt ein Plugin
		/// </summary>
		public abstract void Unload();
	}
}
