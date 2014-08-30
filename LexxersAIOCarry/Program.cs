using System;
using LeagueSharp;
using LeagueSharp.Common;

namespace UltimateCarry
{
	class Program
	{
		public const int LocalVersion = 20;
		public static Champion Champion;
		public static Menu Menu;
		public static Orbwalking.Orbwalker Orbwalker;

		// ReSharper disable once UnusedParameter.Local
		private static void Main(string[] args)
		{
			CustomEvents.Game.OnGameLoad  += Game_OnGameLoad;
		}

		private static void Game_OnGameLoad(EventArgs args)
		{
			AutoUpdater.InitializeUpdater();
			Menu = new Menu("UltimateCarry", "UltimateCarry", true);

			var targetSelectorMenu = new Menu("Target Selector", "TargetSelector");
			SimpleTs.AddToMenu(targetSelectorMenu);
			Menu.AddSubMenu(targetSelectorMenu);

			Menu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
			Orbwalker = new Orbwalking.Orbwalker(Menu.SubMenu("Orbwalker"));

			string championName = ObjectManager.Player.ChampionName;
			switch(championName)
			{
				case "Ezreal":
					Champion = new Ezreal();
					break;
				case "Gnar":
					Champion = new Gnar();
					break;
				case "Lucian":
					Champion = new Lucian();
					break;
				case "Morgana":
					Champion = new Morgana();
					break;
				case "Zed":
					Champion = new Zed();
					break;
				default:
					Champion = new Champion();
					break;
			}

			Menu.AddToMainMenu(); 
		}
	}
}
