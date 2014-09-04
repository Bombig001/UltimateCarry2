using System;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;

namespace UltimateCarry
{
	class Ezreal : Champion
	{
		public static Spell Q;
		public static Spell W;
		public static Spell E;
		public static Spell R;

		public Ezreal()
		{
			Name = "Ezreal";
			Chat.Print(Name + " Plugin Loading ...");
			LoadMenu();
			LoadSpells();

			Drawing.OnDraw += Drawing_OnDraw;
			Game.OnGameUpdate += Game_OnGameUpdate;
			Chat.Print(Name + " Plugin Loaded!");
		}

		private static void LoadMenu()
		{
			Program.Menu.AddSubMenu(new Menu("Packet Setting", "Packets"));
			Program.Menu.SubMenu("Packets").AddItem(new MenuItem("usePackets", "Enable Packets").SetValue(true));

			Program.Menu.Item("Orbwalk").DisplayName = "TeamFight";
			Program.Menu.Item("Farm").DisplayName = "Harass";

			Program.Menu.AddSubMenu(new Menu("TeamFight", "TeamFight"));
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useQ_TeamFight", "Use Q").SetValue(true));
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useW_TeamFight", "Use W").SetValue(true));
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useR_TeamFight", "Use R").SetValue(true));
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("minimumRRange_Teamfight", "R Range min.").SetValue(new Slider(500, 900, 0)));
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("minimumRHit_Teamfight", "R Will Hit min.").SetValue(new Slider(2, 5, 1)));

			Program.Menu.AddSubMenu(new Menu("Harass", "Harass"));
			Program.Menu.SubMenu("Harass").AddItem(new MenuItem("useQ_Harass", "Use Q").SetValue(true));
			Program.Menu.SubMenu("Harass").AddItem(new MenuItem("useW_Harass", "Use W").SetValue(true));

			Program.Menu.AddSubMenu(new Menu("LaneClear", "LaneClear"));
			Program.Menu.SubMenu("LaneClear").AddItem(new MenuItem("useQ_LaneClear", "Use Q").SetValue(true));

			Program.Menu.AddSubMenu(new Menu("LastHit", "LastHit"));
			Program.Menu.SubMenu("LastHit").AddItem(new MenuItem("useQ_LastHit", "Use Q").SetValue(true));

			//Program.Menu.AddSubMenu(new Menu("KillSteal", "KillSteal"));

			Program.Menu.AddSubMenu(new Menu("Drawing", "Drawing"));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_Disabled", "Disable All").SetValue(false));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_Q", "Draw Q").SetValue(true));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_W", "Draw W").SetValue(true));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_E", "Draw E").SetValue(true));

			// Currently Dissabled couse need Remake
			// var baseult = new BaseUlt();
		
		}

		private static void LoadSpells()
		{
			Q = new Spell(SpellSlot.Q, 1200);
			Q.SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.SkillshotLine);

			W = new Spell(SpellSlot.W, 1050);
			W.SetSkillshot(0.25f, 80f, 2000f, false, SkillshotType.SkillshotLine);

			E = new Spell(SpellSlot.E, 475);
			E.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotCircle);

			R = new Spell(SpellSlot.R, 3000);
			R.SetSkillshot(1f, 160f, 2000f, false, SkillshotType.SkillshotLine);

		}

		private static void Game_OnGameUpdate(EventArgs args)
		{
			switch(Program.Orbwalker.ActiveMode)
			{
				case Orbwalking.OrbwalkingMode.Combo:
					if (Program.Menu.Item("useQ_TeamFight").GetValue<bool>())
						Cast_BasicLineSkillshot_Enemy(Q);
					if(Program.Menu.Item("useW_TeamFight").GetValue<bool>())
						Cast_BasicLineSkillshot_Enemy(W, SimpleTs.DamageType.Magical);
					if(Program.Menu.Item("useR_TeamFight").GetValue<bool>())
						CastREnemy();
					break;
				case Orbwalking.OrbwalkingMode.Mixed:
					if(Program.Menu.Item("useQ_Harass").GetValue<bool>())
						Cast_BasicLineSkillshot_Enemy(Q);
					if(Program.Menu.Item("useW_Harass").GetValue<bool>())
						Cast_BasicLineSkillshot_Enemy(W, SimpleTs.DamageType.Magical);
					break;
				case Orbwalking.OrbwalkingMode.LaneClear:
					if(Program.Menu.Item("useQ_LaneClear").GetValue<bool>())
					{
						Cast_BasicLineSkillshot_Enemy(Q);
						CastQMinion();
					}
					break;
				case Orbwalking.OrbwalkingMode.LastHit:
					if(Program.Menu.Item("useQ_LastHit").GetValue<bool>())
						CastQMinion();
					break;
			}
		}

		private static void Drawing_OnDraw(EventArgs args)
		{
			if(Program.Menu.Item("Draw_Disabled").GetValue<bool>())
				return;

			if(Program.Menu.Item("Draw_Q").GetValue<bool>())
				if(Q.Level > 0)
					Utility.DrawCircle(ObjectManager.Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red);

			if(Program.Menu.Item("Draw_W").GetValue<bool>())
				if(W.Level > 0)
					Utility.DrawCircle(ObjectManager.Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red);

			if(Program.Menu.Item("Draw_E").GetValue<bool>())
				if(E.Level > 0)
					Utility.DrawCircle(ObjectManager.Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);
		}

		private static void CastQMinion()
		{
			if(!Q.IsReady())
				return;
			var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly);
			foreach(var minion in allMinions)
			{
				if(!minion.IsValidTarget())
					continue;
				var minionInRangeAa = Orbwalking.InAutoAttackRange(minion);
				var minionInRangeSpell = minion.Distance(ObjectManager.Player) <= Q.Range;
				var minionKillableAa = DamageLib.getDmg(minion, DamageLib.SpellType.AD) >= minion.Health;
				var minionKillableSpell = DamageLib.getDmg(minion, DamageLib.SpellType.Q) >= minion.Health;
				var lastHit = Program.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit;
				var laneClear = Program.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear;

				if((lastHit && minionInRangeSpell && minionKillableSpell) && ((minionInRangeAa && !minionKillableAa) || !minionInRangeAa))
					Q.Cast(minion.Position, Packets());
				else if((laneClear && minionInRangeSpell && !minionKillableSpell) && ((minionInRangeAa && !minionKillableAa) || !minionInRangeAa))
					Q.Cast(minion.Position, Packets());
			}
		}

		private static void CastREnemy()
		{
			if(!R.IsReady())
				return;
			var minRange = Program.Menu.Item("minimumRRange_Teamfight").GetValue<Slider>().Value;
			var minHit = Program.Menu.Item("minimumRHit_Teamfight").GetValue<Slider>().Value;

			var target = SimpleTs.GetTarget(2000, SimpleTs.DamageType.Physical);
			if(target == null)
				return;
			if(target.Distance(ObjectManager.Player) >= minRange)
				R.CastIfWillHit(target, minHit - 1, Packets());
		}

	}
}
