using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace UltimateCarry
{
	class Thresh : Champion
	{
		public static Spell Q;
		public static Spell W;
		public static Spell E;
		public static Spell R;

		public static int Q_Follow_Tick = 0;
		public const int Q_Follow_Time = 3000;
		public Thresh()
		{
			Name = "Thresh";
			Chat.Print(Name + " Plugin Loading ...");
			LoadMenu();
			LoadSpells();
			Game.OnGameUpdate += Game_OnGameUpdate;
			Drawing.OnDraw += Drawing_OnDraw;
			Interrupter.OnPosibleToInterrupt += Interrupter_OnPosibleToInterrupt;
			Chat.Print(Name + " Plugin Loaded!");
		}

		private static void LoadMenu()
		{
			MenuBasics();

			Program.Menu.AddSubMenu(new Menu("TeamFight", "TeamFight"));
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useQ_TeamFight", "Use Q").SetValue(true));
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useQ_TeamFight_follow", "Follow Q").SetValue(true));
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useW_TeamFight_shield", "W for Shield").SetValue(true));
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useW_TeamFight_enagage", "W for Engage").SetValue(true));
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useE_TeamFight", "E to me").SetValue(true));
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useR_TeamFight", "Use R if Hit").SetValue(new Slider(2, 5, 0)));
			
			Program.Menu.AddSubMenu(new Menu("Harass", "Harass"));
			Program.Menu.SubMenu("Harass").AddItem(new MenuItem("useQ_Harass", "Use Q").SetValue(true));
			Program.Menu.SubMenu("Harass").AddItem(new MenuItem("useW_Harass_safe", "W for SafeFriend").SetValue(true));
			Program.Menu.SubMenu("Harass").AddItem(new MenuItem("useE_Harass", "E away").SetValue(true));
			AddManaManager("Harass",50);

			Program.Menu.AddSubMenu(new Menu("LaneClear", "LaneClear"));
			Program.Menu.SubMenu("LaneClear").AddItem(new MenuItem("useE_LaneClear", "Use E").SetValue(true));
			AddManaManager("Harass", 20);

			Program.Menu.AddSubMenu(new Menu("SupportMode", "SupportMode"));
			Program.Menu.SubMenu("SupportMode").AddItem(new MenuItem("hitMinions", "Hit Minions").SetValue(false));

			Program.Menu.AddSubMenu(new Menu("Passive", "Passive"));
			Program.Menu.SubMenu("Passive").AddItem(new MenuItem("useQ_Interupt", "Q Interrupt").SetValue(false));
			Program.Menu.SubMenu("Passive").AddItem(new MenuItem("useW_Interupt", "W Interrupt").SetValue(false));

			Program.Menu.AddSubMenu(new Menu("Drawing", "Drawing"));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_Disabled", "Disable All").SetValue(false));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_Q", "Draw Q").SetValue(true));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_W", "Draw W").SetValue(true));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_E", "Draw E").SetValue(true));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_R", "Draw R").SetValue(true));
		}

		private static void LoadSpells()
		{

			Q = new Spell(SpellSlot.Q, 1025);
			Q.SetSkillshot(0.491f, 50f, 2000, true, SkillshotType.SkillshotCircle);

			W = new Spell(SpellSlot.W, 950);

			E = new Spell(SpellSlot.E, 400);

			R = new Spell(SpellSlot.R, 400);
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

			if(Program.Menu.Item("Draw_R").GetValue<bool>())
				if(R.Level > 0)
					Utility.DrawCircle(ObjectManager.Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);
		}

		private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
		{
			if(Program.Menu.Item("useE_Interupt").GetValue<bool>())
				if(E.IsReady())
					if(unit.IsValidTarget(W.Range))
					{
						W.Cast(unit, Packets());
						return;
					}
			if(!Program.Menu.Item("useQ_Interupt").GetValue<bool>() || !unit.IsValidTarget(Q.Range) || Q.GetPrediction(unit).Hitchance < HitChance.Low || Environment.TickCount - Q_Follow_Tick < Q_Follow_Time || !Q.IsReady())
				return;
			Q.Cast(unit, Packets());
			Q_Follow_Tick = Environment.TickCount;
			LastQTarget = (Obj_AI_Hero)unit;
		}

		private static void Game_OnGameUpdate(EventArgs args)
		{
			if (LastQTarget != null)
				if (Environment.TickCount - Q_Follow_Tick >= Q_Follow_Time)
					LastQTarget = null;

			switch(Program.Orbwalker.ActiveMode)
			{
				case Orbwalking.OrbwalkingMode.Combo:
					if (Program.Menu.Item("useQ_TeamFight").GetValue<bool>() && Environment.TickCount - Q_Follow_Tick >= Q_Follow_Time)
					{
						var target = Cast_BasicLineSkillshot_Enemy(Q, SimpleTs.DamageType.Magical);
						if (target != null)
						{
							Q_Follow_Tick = Environment.TickCount;
							LastQTarget = target;
						}
					}
					if(Program.Menu.Item("useQ_TeamFight_follow").GetValue<bool>() && Environment.TickCount <= Q_Follow_Tick + Q_Follow_Time && LastQTarget != null)
						Q.Cast();
					if(Program.Menu.Item("useW_TeamFight_shield").GetValue<bool>())
						Cast_Shield_onFriend(W,50,true);
					if (Program.Menu.Item("useW_TeamFight_enagage").GetValue<bool>())
						EngageFriendLatern();
					if (Program.Menu.Item("useE_TeamFight").GetValue<bool>())
						Cast_E("ToMe");
					if(Program.Menu.Item("useR_TeamFight").GetValue<Slider>().Value >= 1)
						if (Utility.CountEnemysInRange((int) R.Range) >= Program.Menu.Item("useR_TeamFight").GetValue<Slider>().Value)
							R.Cast();
					break;
				case Orbwalking.OrbwalkingMode.Mixed:
					if(Program.Menu.Item("useQ_Harass").GetValue<bool>() && Environment.TickCount - Q_Follow_Tick >= Q_Follow_Time)
					{
						var target = Cast_BasicLineSkillshot_Enemy(Q, SimpleTs.DamageType.Magical);
						if(target != null)
						{
							Q_Follow_Tick = Environment.TickCount;
							LastQTarget = target;
						}
					}
					if(Program.Menu.Item("useE_Harass").GetValue<bool>())
						Cast_E();
					if (Program.Menu.Item("useW_Harass").GetValue<bool>())
						SafeFriendLatern();
					break;
				case Orbwalking.OrbwalkingMode.LaneClear:
					if (Program.Menu.Item("useE_Harass").GetValue<bool>())
						Cast_BasicLineSkillshot_AOE_Farm(E);
					break;
			}
		}

		private static void EngageFriendLatern()
		{
			if (!W.IsReady())
				return;
			var bestcastposition = new Vector3(0f, 0f, 0f);
			foreach(var friend in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsAlly && !hero.IsMe && hero.Distance(ObjectManager.Player) <= W.Range + 300 && hero.Distance(ObjectManager.Player) <= W.Range - 300 && hero.Health / hero.MaxHealth * 100 >= 20 && Utility.CountEnemysInRange(150) >= 1))
			{
				var center = ObjectManager.Player.Position;
				const int points = 36;
				var radius = W.Range;

				const double slice = 2 * Math.PI / points;
				for(var i = 0; i < points; i++)
				{
					var angle = slice * i;
					var newX = (int)(center.X + radius * Math.Cos(angle));
					var newY = (int)(center.Y + radius * Math.Sin(angle));
					var p = new Vector3(newX, newY, 0);
					if(p.Distance(friend.Position) <= bestcastposition.Distance(friend.Position))
						bestcastposition = p;
				}
				if (friend.Distance(ObjectManager.Player) <= W.Range)
				{
					W.Cast(bestcastposition, Packets());
					return;
				}
			}
			if(bestcastposition.Distance(new Vector3(0f, 0f, 0f)) >= 100)
				W.Cast(bestcastposition, Packets());
		}

		private static void SafeFriendLatern()
		{
			if (!W.IsReady())
				return;
			var bestcastposition = new Vector3(0f, 0f, 0f);
			foreach (
				var friend in
					ObjectManager.Get<Obj_AI_Hero>()
						.Where(
							hero =>
								hero.IsAlly && !hero.IsMe  && hero.Distance(ObjectManager.Player) <= W.Range + 300 &&
								hero.Distance(ObjectManager.Player) <= W.Range - 200 && hero.Health/hero.MaxHealth*100 >= 20 ))
			{
				foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy && hero.Distance(friend) <= 300))
				{
					var center = ObjectManager.Player.Position;
					const int points = 36;
					var radius = W.Range;

					const double slice = 2*Math.PI/points;
					for (var i = 0; i < points; i++)
					{
						var angle = slice*i;
						var newX = (int) (center.X + radius*Math.Cos(angle));
						var newY = (int) (center.Y + radius*Math.Sin(angle));
						var p = new Vector3(newX, newY, 0);
						if (p.Distance(friend.Position) <= bestcastposition.Distance(friend.Position))
							bestcastposition = p;
					}
				}
				if (bestcastposition.Distance(new Vector3(0f, 0f, 0f)) >= 100)
					W.Cast(bestcastposition, Packets());
			}
		}

		private static void Cast_E(string mode = "")
		{
			if (!E.IsReady() || !ManaManagerAllowCast(E))
				return;
			var target = SimpleTs.GetTarget(E.Range - 10 ,SimpleTs.DamageType.Magical);
			if(target == null)
				return;
			E.Cast(mode == "ToMe" ? GetReversePosition(ObjectManager.Player.Position, target.Position) : target.Position,
				Packets());
		}


		public static Obj_AI_Hero LastQTarget
		{
			get;
			set;
		}
	}
}
