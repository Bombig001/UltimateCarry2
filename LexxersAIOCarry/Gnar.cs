using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using LeagueSharp;
using LeagueSharp.Common;
using LexxersAIOCarry;
using SharpDX;
using Color = System.Drawing.Color;

namespace UltimateCarry
{
	class Gnar : Champion
	{
		public static Spell Q_mini;
		public static Spell Q_mega;
		public static Spell W_mega;
		public static Spell E_mini;
		public static Spell E_mega;
		public static Spell R_mega;

		public static Spell Q;
		public static Spell W;
		public static Spell E;
		public static Spell R;

		public static string Transform_Soon = "gnartransformsoon";
		public static string Transformed = "gnartransform";
		public static int GnarState = 1;

		public Gnar()
		{
			Name = "Gnar";
			Chat.Print(Name + " Plugin Loading ...");
			LoadMenu();
			LoadSpells();

			Drawing.OnDraw += Drawing_OnDraw;
			Game.OnGameUpdate += Game_OnGameUpdate;
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
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useE_TeamFight", "Use E").SetValue(true));
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useR_TeamFight", "Use R Collision").SetValue(true));

			Program.Menu.AddSubMenu(new Menu("Harass", "Harass"));
			Program.Menu.SubMenu("Harass").AddItem(new MenuItem("useQ_Harass", "Use Q").SetValue(true));
			Program.Menu.SubMenu("Harass").AddItem(new MenuItem("useW_Harass", "Use W").SetValue(true));

			Program.Menu.AddSubMenu(new Menu("LaneClear", "LaneClear"));
			Program.Menu.SubMenu("LaneClear").AddItem(new MenuItem("useQ_LaneClear", "Use Q").SetValue(true));
			Program.Menu.SubMenu("LaneClear").AddItem(new MenuItem("useW_LaneClear", "Use W").SetValue(true));

			Program.Menu.AddSubMenu(new Menu("LastHit", "LastHit"));
			Program.Menu.SubMenu("LastHit").AddItem(new MenuItem("useQ_LastHit", "Use Q").SetValue(true));

			//Program.Menu.AddSubMenu(new Menu("ItemManager", "ItemManager"));

			Program.Menu.AddSubMenu(new Menu("Drawing", "Drawing"));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_Disabled", "Disable All").SetValue(false));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_Q", "Draw Q").SetValue(true));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_W", "Draw W").SetValue(true));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_E", "Draw E").SetValue(true));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_R", "Draw R").SetValue(true));

			var potionManager = new PotionManager();
		}

		private void LoadSpells()
		{
			Q_mini = new Spell(SpellSlot.Q, 1100);
			Q_mini.SetSkillshot(0.066f, 50f, 1200f, true, SkillshotType.SkillshotLine);

			Q_mega = new Spell(SpellSlot.Q, 1100);
			Q_mega.SetSkillshot(0.066f, 70f, 1200f, true, SkillshotType.SkillshotLine);
			
			W_mega = new Spell(SpellSlot.W,525);
			W_mega.SetSkillshot(0.25f,80f,1200,false,SkillshotType.SkillshotLine);

			E_mini = new Spell(SpellSlot.E,475);
			E_mini.SetSkillshot(0.695f, 150f, float.MaxValue, false, SkillshotType.SkillshotCircle );

			E_mega = new Spell(SpellSlot.E, 475);
			E_mega.SetSkillshot(0.695f, 350f, float.MaxValue, false, SkillshotType.SkillshotCircle);

			R_mega = new Spell(SpellSlot.R, 1);
			R_mega.SetSkillshot(0.066f, 400f, 1400f, false, SkillshotType.SkillshotCircle);

			W = W_mega;
			R = R_mega;
		}

		private void Game_OnGameUpdate(EventArgs args)
		{
			CheckState();
			switch (GnarState)
			{
				case 1:
					Q = Q_mini;
					E = E_mini;
					break;
				default:
					Q = Q_mega;
					E = E_mega;
					break;
			}

			switch(Program.Orbwalker.ActiveMode)
			{
				case Orbwalking.OrbwalkingMode.Combo:
					if(Program.Menu.Item("useQ_TeamFight").GetValue<bool>())
						CastQEnemy();
					if(Program.Menu.Item("useW_TeamFight").GetValue<bool>() && GnarState > 1)
						CastWEnemy();
					if(Program.Menu.Item("useE_TeamFight").GetValue<bool>() && GnarState > 1)
						CastEEnemy();
					if(Program.Menu.Item("useR_TeamFight").GetValue<bool>() && GnarState > 1)
						CastREnemy();
					break;
				case Orbwalking.OrbwalkingMode.Mixed:
					if(Program.Menu.Item("useQ_Harass").GetValue<bool>())
						CastQEnemy();
					if(Program.Menu.Item("useW_Harass").GetValue<bool>() && GnarState > 1)
						CastWEnemy();
					break;
				case Orbwalking.OrbwalkingMode.LaneClear:
					if(Program.Menu.Item("useQ_LaneClear").GetValue<bool>())
					{
						CastQEnemy();
						CastQMinion();
					}
					if(Program.Menu.Item("useW_LaneClear").GetValue<bool>() && GnarState > 1)
					{
						CastWEnemy();
						CastWMinion();
					}
					break;
				case Orbwalking.OrbwalkingMode.LastHit:
					if(Program.Menu.Item("useQ_LastHit").GetValue<bool>())
						CastQMinion();
					break;
			}
		}

		private static void CastREnemy()
		{
			if (!R.IsReady())
				return;
			foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(R.Width)))
				CastRToCollision(GetCollision(target));
		}

		private static void CastRToCollision(int collisionId)
		{
			if (collisionId == -1)
				return;
			var center = ObjectManager.Player.Position ;
			const int points = 36;
			const int radius = 300;

			const double slice = 2 * Math.PI / points;
			for(var i = 0; i < points; i++)
			{
				var angle = slice * i;
				var newX = (int)(center.X + radius * Math.Cos(angle));
				var newY = (int)(center.Y + radius * Math.Sin(angle));
				var p = new Vector3(newX, newY, 0);
				if (collisionId == i)
					R.Cast(p, Packets());
			}
		}

		private static int GetCollision(Obj_AI_Hero enemy)
		{
			var center = enemy.Position;
			const int points = 36;
			const int radius = 300;
			var positionList = new List<Vector3>();

			const double slice = 2 * Math.PI / points;
			for(var i = 0; i < points; i++)
			{
				var angle = slice * i;
				var newX = (int)(center.X + radius * Math.Cos(angle));
				var newY = (int)(center.Y + radius * Math.Sin(angle));
				var p = new Vector3(newX, newY, 0);

				if (NavMesh.GetCollisionFlags(p) == CollisionFlags.Wall || NavMesh.GetCollisionFlags(p) == CollisionFlags.Building)
					return i;
			}
			return -1;
		}

		private static void Drawing_OnDraw(EventArgs args)
		{
			if(Program.Menu.Item("Draw_Disabled").GetValue<bool>())
				return;

			if(Program.Menu.Item("Draw_Q").GetValue<bool>())
				if(Q_mini.Level > 0)
					Utility.DrawCircle(ObjectManager.Player.Position, Q_mini.Range, Q_mini.IsReady() ? Color.Green : Color.Red);

			if(Program.Menu.Item("Draw_W").GetValue<bool>())
				if(W_mega.Level > 0)
					Utility.DrawCircle(ObjectManager.Player.Position, W_mega.Range, W_mega.IsReady() ? Color.Green : Color.Red);

			if(Program.Menu.Item("Draw_E").GetValue<bool>())
				if(E_mega.Level > 0)
					Utility.DrawCircle(ObjectManager.Player.Position, E_mega.Range, E_mega.IsReady() ? Color.Green : Color.Red);

			if(Program.Menu.Item("Draw_R").GetValue<bool>())
				if(R_mega.Level > 0)
					Utility.DrawCircle(ObjectManager.Player.Position, R_mega.Width, R_mega.IsReady() ? Color.Green : Color.Red);
		}

		private static void CastQEnemy()
		{
			if(!Q.IsReady())
				return;
			var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);
			if(target == null)
				return;
			if(target.IsValidTarget(Q.Range) && Q.GetPrediction(target).Hitchance >= HitChance.High)
				Q.Cast(target, Packets());
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

		private static void CastWEnemy()
		{
			if(!W.IsReady())
				return;
			var target = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
			if(target == null)
				return;
			if(target.IsValidTarget(W.Range) && W.GetPrediction(target).Hitchance >= HitChance.High)
				W.Cast(target, Packets());
		}

		private static void CastWMinion()
		{
			if(!W.IsReady())
				return;
			var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range, MinionTypes.All, MinionTeam.NotAlly);
			foreach(var minion in allMinions)
			{
				if(!minion.IsValidTarget())
					continue;
				var minionInRangeAa = Orbwalking.InAutoAttackRange(minion);
				var minionInRangeSpell = minion.Distance(ObjectManager.Player) <= W.Range;
				var minionKillableAa = DamageLib.getDmg(minion, DamageLib.SpellType.AD) >= minion.Health;
				var minionKillableSpell = DamageLib.getDmg(minion, DamageLib.SpellType.W) >= minion.Health;
				var lastHit = Program.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit;
				var laneClear = Program.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear;

				if((lastHit && minionInRangeSpell && minionKillableSpell) && ((minionInRangeAa && !minionKillableAa) || !minionInRangeAa))
					W.Cast(minion.Position, Packets());
				else if((laneClear && minionInRangeSpell && !minionKillableSpell) && ((minionInRangeAa && !minionKillableAa) || !minionInRangeAa))
					W.Cast(minion.Position, Packets());
			}
		}

		private static void CastEEnemy()
		{
			if(!E.IsReady())
				return;
			var target = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Physical);
			if(target == null)
				return;
			if(target.IsValidTarget(Q.Range) && E.GetPrediction(target).Hitchance >= HitChance.High)
				E.Cast(target, Packets());
		}
		//private static void CastREnemy()
		//{
		//	if(!R_mega.IsReady())
		//		return;
		//	var minRange = Program.Menu.Item("minimumRRange_Teamfight").GetValue<Slider>().Value;
		//	var minHit = Program.Menu.Item("minimumRHit_Teamfight").GetValue<Slider>().Value;

		//	var target = SimpleTs.GetTarget(2000, SimpleTs.DamageType.Physical);
		//	if(target == null)
		//		return;
		//	if(target.Distance(ObjectManager.Player) >= minRange)
		//		R_mega.CastIfWillHit(target, minHit - 1, Packets());
		//}

		private static void CheckState()
		{
			var tempState = 1;
			foreach(var buff in ObjectManager.Player.Buffs)
			{
				if(buff.Name == Transform_Soon)
					tempState = 2;
				if(buff.Name == Transformed)
					tempState = 3;
			}
			GnarState = tempState;
		}

		private static bool Packets()
		{
			return Program.Menu.Item("usePackets").GetValue<bool>();
		}

	}
}
