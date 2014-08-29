using System;
using System.ComponentModel;
using LexxersAIOCarry;

namespace UltimateCarry
{
	class AutoUpdater
	{
		public static int Localversion = Program.LocalVersion;
		internal static bool IsInitialized;

		internal static void InitializeUpdater()
		{
			IsInitialized = true;
			UpdateCheck();
		}

		private static void UpdateCheck()
		{
			Chat.Print ("UltimateCarry by Lexxes loading ...");
			var bgw = new BackgroundWorker();
			bgw.DoWork += bgw_DoWork;
			bgw.RunWorkerAsync();
		}

		private static void bgw_DoWork(object sender, DoWorkEventArgs e)
		{
			var myUpdater = new Updater("https://raw.githubusercontent.com/LXMedia1/Leage-Sharp/master/Versions/UltimateCarry.ver",
					"https://github.com/LXMedia1/Leage-Sharp/raw/master/UltimateCarry.exe", Localversion);
			if (myUpdater.NeedUpdate)
			{
				Chat.Print("UltimateCarry is Updateing ...");
				Chat.Print("-- Using trellis Updater --");
				if (myUpdater.Update())
				{
					Chat.Print("UltimateCarry is Updateed, Reload Please.");
					UltimateCarry.Properties.Settings.Default.Reset();
				}
			}
			else
				Chat.Print(string.Format("UltimateCarry ( Version: {0} ) loaded!", Localversion));
		}
	}

	internal class Updater
	{
		private readonly string _updatelink;

		private readonly System.Net.WebClient _wc = new System.Net.WebClient
		{
			Proxy = null
		};
		public bool NeedUpdate = false;

		public Updater(string versionlink, string updatelink, int localversion)
		{
			_updatelink = updatelink;

			NeedUpdate = Convert.ToInt32(_wc.DownloadString(versionlink)) > localversion;
		}

		public bool Update()
		{
			try
			{
				if(
					System.IO.File.Exists(
						System.IO.Path.Combine(System.Reflection.Assembly.GetExecutingAssembly().Location) + ".bak"))
				{
					System.IO.File.Delete(
						System.IO.Path.Combine(System.Reflection.Assembly.GetExecutingAssembly().Location) + ".bak");
				}
				System.IO.File.Move(System.Reflection.Assembly.GetExecutingAssembly().Location,
					System.IO.Path.Combine(System.Reflection.Assembly.GetExecutingAssembly().Location) + ".bak");
				_wc.DownloadFile(_updatelink,
					System.IO.Path.Combine(System.Reflection.Assembly.GetExecutingAssembly().Location));
				return true;
			}
			catch(Exception ex)
			{
				Chat.Print("UltimateCarry-Updater Error: " + ex.Message);
				return false;
			}
		}
	}
}
