using LeagueSharp;
using LeagueSharp.Common;
using LexxersAIOCarry;

namespace UltimateCarry
{
	internal class Morgana : Champion
	{
		public static Spell Q;
		public static Spell W;
		public static Spell E;
		public static Spell R;

		public Morgana()
		{
			Name = "Morgana";
			Chat.Print(Name + " Plugin Loading ...");
			LoadMenu();
			LoadSpells();

			//Drawing.OnDraw += Drawing_OnDraw;
			//Game.OnGameUpdate += Game_OnGameUpdate;
			Chat.Print(Name + " Plugin Loaded!");
		}

		private void LoadMenu()
		{
			Program.Menu.AddSubMenu(new Menu("Packet Setting", "Packets"));
			Program.Menu.SubMenu("Packets").AddItem(new MenuItem("usePackets", "Enable Packets").SetValue(true));

			Program.Menu.Item("Orbwalk").DisplayName = "TeamFight";
			Program.Menu.Item("Farm").DisplayName = "Harass";

			Program.Menu.AddSubMenu(new Menu("TeamFight", "TeamFight"));
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useQ_TeamFight", "Use Q").SetValue(true));
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useW_TeamFight", "Use W").SetValue(true));
			//Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useR_TeamFight", "Use R").SetValue(true));
			//Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("minimumRRange_Teamfight", "R Range min.").SetValue(new Slider(500, 900, 0)));
			//Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("minimumRHit_Teamfight", "R Will Hit min.").SetValue(new Slider(2, 5, 1)));

			//Program.Menu.AddSubMenu(new Menu("Harass", "Harass"));
			//Program.Menu.SubMenu("Harass").AddItem(new MenuItem("useQ_Harass", "Use Q").SetValue(true));
			//Program.Menu.SubMenu("Harass").AddItem(new MenuItem("useW_Harass", "Use W").SetValue(true));

			//Program.Menu.AddSubMenu(new Menu("LaneClear", "LaneClear"));
			//Program.Menu.SubMenu("LaneClear").AddItem(new MenuItem("useQ_LaneClear", "Use Q").SetValue(true));

			//Program.Menu.AddSubMenu(new Menu("LastHit", "LastHit"));
			//Program.Menu.SubMenu("LastHit").AddItem(new MenuItem("useQ_LastHit", "Use Q").SetValue(true));

			//Program.Menu.AddSubMenu(new Menu("ItemManager", "ItemManager"));

			Program.Menu.AddSubMenu(new Menu("Drawing", "Drawing"));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_Disabled", "Disable All").SetValue(false));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_Q", "Draw Q").SetValue(true));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_W", "Draw W").SetValue(true));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_E", "Draw E").SetValue(true));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_R", "Draw R").SetValue(true));
			
			var potionManager = new PotionManager();
		}

		private static void LoadSpells()
		{
			Q = new Spell(SpellSlot.Q, 1000);
			Q.SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.SkillshotLine);
			

			W = new Spell(SpellSlot.W, 1050);
			W.SetSkillshot(0.25f, 80f, 2000f, false, SkillshotType.SkillshotLine);

			E = new Spell(SpellSlot.E, 475);
			E.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotCircle);

			R = new Spell(SpellSlot.R, 3000);
			R.SetSkillshot(1f, 160f, 2000f, false, SkillshotType.SkillshotLine);

		}
	}

	
}

