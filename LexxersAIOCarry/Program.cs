using System;
using LeagueSharp;
using LeagueSharp.Common;

namespace UltimateCarry
{
	class Program
	{
		public const int LocalVersion = 40;
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
			var orbwalking = Menu.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
			Orbwalker = new Orbwalking.Orbwalker(orbwalking);

			//var overlay = new Overlay();
			var potionManager = new PotionManager();
			var activator = new Activator();
		
			var championName = ObjectManager.Player.ChampionName;
			try
			{
				var handle = System.Activator.CreateInstance(null, "UltimateCarry." + championName);
				Champion = (Champion) handle.Unwrap();
			}
			catch (Exception)
			{
				Champion = new Champion(); //Champ not supported
			}
					
			Menu.AddToMainMenu();
		}
	}
}
