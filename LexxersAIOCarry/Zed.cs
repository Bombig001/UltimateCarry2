using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using LexxersAIOCarry;
using SharpDX;
using Color = System.Drawing.Color;

namespace UltimateCarry
{
	class Zed :Champion 
	{
		public static Spell Q;
		public static Spell W;
		public static Spell E;
		public static Spell R;

		public static Obj_AI_Minion Clone_W = null;
		public static bool Clone_W_Created = false;
		public static bool Clone_W_Found = false;
		public static int Clone_W_Tick = 0;
		public static int W_Cast_Tick = 0;

		public static Obj_AI_Minion Clone_R = null;
		public static bool Clone_R_Created = false;
		public static bool Clone_R_Found = false;
		public static int Clone_R_Tick = 0;
		public static int R_Cast_Tick = 0;
		public static Vector3 Clone_R_nearPosition;

		public static int Delay = 300;
		public static int DelayTick = 0;

		public Zed()
		{
			Name = "Zed";
			Chat.Print(Name + " Plugin Loading ...");
			LoadMenu();
			LoadSpells();

			Drawing.OnDraw += Drawing_OnDraw;
			Game.OnGameUpdate += Game_OnGameUpdate;
			GameObject.OnCreate += OnSpellCast;
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
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useE_TeamFight", "Use E").SetValue(true));
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useR_TeamFight", "Use R").SetValue(true));
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("followW_TeamFight", "Follow W in Range").SetValue(true));

			Program.Menu.AddSubMenu(new Menu("Harass", "Harass"));
			Program.Menu.SubMenu("Harass").AddItem(new MenuItem("useQ_Harass", "Use Q").SetValue(true));
			Program.Menu.SubMenu("Harass").AddItem(new MenuItem("useW_Harass", "Use W").SetValue(true));
			Program.Menu.SubMenu("Harass").AddItem(new MenuItem("followW_Harass", "Follow W").SetValue(false));
			Program.Menu.SubMenu("Harass").AddItem(new MenuItem("useE_Harass", "Use E").SetValue(true));

			Program.Menu.AddSubMenu(new Menu("LaneClear", "LaneClear"));
			Program.Menu.SubMenu("LaneClear").AddItem(new MenuItem("useQ_LaneClear", "Use Q").SetValue(true));
			Program.Menu.SubMenu("LaneClear").AddItem(new MenuItem("useE_LaneClear", "Use E").SetValue(true));

			Program.Menu.AddSubMenu(new Menu("LastHit", "LastHit"));
			Program.Menu.SubMenu("LastHit").AddItem(new MenuItem("useQ_LastHit", "Use Q").SetValue(true));

			Program.Menu.AddSubMenu(new Menu("ItemManager", "ItemManager"));

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
			Q = new Spell(SpellSlot.Q, 900);
			Q.SetSkillshot(0.235f, 50f, 1700, false, SkillshotType.SkillshotLine);

			W = new Spell(SpellSlot.W, 550);
			
			E = new Spell(SpellSlot.E, 290);

			R = new Spell(SpellSlot.R, 600);
		}

		private void Game_OnGameUpdate(EventArgs args)
		{

			if(Clone_W_Created && !Clone_W_Found)
				SearchForClone("W");
			if(Clone_R_Created && !Clone_R_Found)
				SearchForClone("R");

			if(Clone_W != null && (Clone_W_Tick < Environment.TickCount - 4000))
			{
				Clone_W = null;
				Clone_W_Created = false;
				Clone_W_Found = false;
			}

			if(Clone_R != null && (Clone_R_Tick < Environment.TickCount - 6000))
			{
				Clone_R = null;
				Clone_R_Created = false;
				Clone_R_Found = false;
			}

			switch(Program.Orbwalker.ActiveMode)
			{
				case Orbwalking.OrbwalkingMode.Combo:
					if(Program.Menu.Item("useQ_TeamFight").GetValue<bool>())
						CastQEnemy();
					if(Program.Menu.Item("useE_TeamFight").GetValue<bool>())
						CastE();
					if(Program.Menu.Item("useW_TeamFight").GetValue<bool>())
						CastWEnemy();
					if(Program.Menu.Item("useR_TeamFight").GetValue<bool>())
						CastR();
					break;
				case Orbwalking.OrbwalkingMode.Mixed:
					if(Program.Menu.Item("useQ_Harass").GetValue<bool>())
						CastQEnemy();
					if(Program.Menu.Item("useE_Harass").GetValue<bool>())
						CastE();
					if(Program.Menu.Item("useW_Harass").GetValue<bool>())
						CastWEnemy();
					break;
				case Orbwalking.OrbwalkingMode.LaneClear:
					if(Program.Menu.Item("useQ_LaneClear").GetValue<bool>())
					{
						CastQEnemy();
						CastQMinion();
					}
					if(Program.Menu.Item("useE_LaneClear").GetValue<bool>())
						CastE();
					break;
				case Orbwalking.OrbwalkingMode.LastHit:
					if(Program.Menu.Item("useQ_LastHit").GetValue<bool>())
						CastQMinion();
					break;
			}
		}

		private void Drawing_OnDraw(EventArgs args)
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

		private static void CastQEnemy()
		{
			if (!Q.IsReady())
				return;
			var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);
			if (target != null)
				if (target.IsValidTarget(Q.Range) && Q.GetPrediction(target).Hitchance >= HitChance.High)
				{
					Q.Cast(target, Packets());
					return;
				}

			if(Clone_W != null)
				foreach(var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => (hero.Distance(Clone_W.Position) < Q.Range) && hero.IsValidTarget() && hero.IsVisible))
				{
					Q.Cast(hero.Position, Packets());
					return;
				}

			if(Clone_R == null )
				return;
			foreach(var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => (hero.Distance(Clone_R.Position) < Q.Range) && hero.IsValidTarget() && hero.IsVisible))
				Q.Cast(hero.Position, Packets());
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
			var follow = (Program.Menu.Item("followW_Harass").GetValue<bool>() &&
						   Orbwalking.OrbwalkingMode.Mixed == Program.Orbwalker.ActiveMode) || (Program.Menu.Item("followW_TeamFight").GetValue<bool>() &&
																								Orbwalking.OrbwalkingMode.Combo == Program.Orbwalker.ActiveMode);

			var target = SimpleTs.GetTarget(W.Range + Q.Range, SimpleTs.DamageType.Physical);
	
			if((IsTeleportToClone("W") || Delay >= Environment.TickCount - DelayTick))
				return;
			DelayTick = Environment.TickCount;
			if(target == null)
				return;
			if((W.IsReady() && Q.IsReady() && target.IsValidTarget(Q.Range + W.Range) && IsEnoughEnergy(GetCost(SpellSlot.Q) + GetCost(SpellSlot.W)))
			   || (W.IsReady() && E.IsReady() && target.IsValidTarget(W.Range + E.Range) && IsEnoughEnergy(GetCost(SpellSlot.W) + GetCost(SpellSlot.E)))
			   || (W.IsReady() && target.IsValidTarget(E.Range + Orbwalking.GetRealAutoAttackRange(target))))
			{
				W.Cast(target.Position, Packets());
				if (follow)W.Cast();
			}
			if(IsTeleportToClone("W") && follow)
			{
				if(ObjectManager.Player.Health * 100 / ObjectManager.Player.MaxHealth < target.Health * 100 / target.MaxHealth || ObjectManager.Player.Health * 100 / ObjectManager.Player.MaxHealth > 5)
					if(Clone_W.Position.Distance(target.Position) > ObjectManager.Player.Position.Distance(target.Position))
						W.Cast();
			}
		}

		private static void CastE()
		{
			if (!E.IsReady())
				return;
			if (Program.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo ||
			    Program.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed ||
				Program.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
			{
				var target = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Physical);
				if (target != null)
				{
					E.Cast();
					return;
				}
				if(Clone_W != null)
					if (ObjectManager.Get<Obj_AI_Hero>().Any(hero => (hero.Distance(Clone_W.Position) < E.Range) && hero.IsValidTarget() && hero.IsVisible))
					{
						E.Cast();
						return;
					}

				if(Clone_R != null )
					if (ObjectManager.Get<Obj_AI_Hero>().Any(hero => (hero.Distance(Clone_R.Position) < E.Range) && hero.IsValidTarget() && hero.IsVisible))
					{
						E.Cast();
						return;
					}
			}
			if (Program.Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LaneClear) 
				return;
			var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.NotAlly);
			foreach(var Minion in allMinions)
			{
				if(Minion != null)
					if(Minion.IsValidTarget(E.Range))
						if((DamageLib.getDmg(Minion, DamageLib.SpellType.E) > Minion.Health) || (DamageLib.getDmg(Minion, DamageLib.SpellType.E) + 100 < Minion.Health))
							E.Cast();
			}
		}

		private static void CastR()
		{
			if(!R.IsReady())
				return;
			var target = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Physical);
			if (!IsTeleportToClone("R"))
			{

				var dmg = DamageLib.getDmg(target, DamageLib.SpellType.Q);
				dmg += DamageLib.getDmg(target, DamageLib.SpellType.E);
				dmg += DamageLib.getDmg(target, DamageLib.SpellType.R);
				dmg += DamageLib.getDmg(target, DamageLib.SpellType.AD)*2;

				if (dmg >= target.Health)
				{
					R.Cast(target);
					SearchForClone("R");
				}
			}
			else
				if(ObjectManager.Player.Health * 100 / ObjectManager.Player.MaxHealth < target.Health * 100 / target.MaxHealth)
					if(Clone_R.Position.Distance(target.Position) > ObjectManager.Player.Position.Distance(target.Position))
						R.Cast();
		}

		private static bool Packets()
		{
			return Program.Menu.Item("usePackets").GetValue<bool>();
		}

		private static bool IsTeleportToClone(string Spell)
		{
			if (Spell == "W")
				if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name == "zedw2")
					return true;
			if (Spell != "R") 
				return false;
			return ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "ZedR2";
		}

		private static bool IsEnoughEnergy(float energy)
		{
			return energy <= ObjectManager.Player.Mana;
		}

		private static float GetCost(SpellSlot Spell)
		{
			return ObjectManager.Player.Spellbook.GetSpell(Spell).ManaCost;
		}

		private static void SearchForClone(string p)
		{
			Obj_AI_Minion shadow;
			if(p != null && p == "W")
			{
				shadow = ObjectManager.Get<Obj_AI_Minion>().FirstOrDefault(hero => (hero.Name == "Shadow" && hero.IsAlly && (hero != Clone_R)));
				if(shadow != null)
				{
					Clone_W = shadow;
					Clone_W_Found = true;
					Clone_W_Tick = Environment.TickCount;
				}
			}
			if (p == null || p != "R") 
				return;
			shadow = ObjectManager.Get<Obj_AI_Minion>().FirstOrDefault(hero => ((hero.ServerPosition.Distance(Clone_R_nearPosition)) < 50) && hero.Name == "Shadow" && hero.IsAlly && hero != Clone_W);
			if (shadow == null) 
				return;
			Clone_R = shadow;
			Clone_R_Found = true;
			Clone_R_Tick = Environment.TickCount;
		}

		private static void OnSpellCast(GameObject sender, EventArgs args)
		{
			var spell = (Obj_SpellMissile)sender;
			var unit = spell.SpellCaster.Name;
			var name = spell.SData.Name;

			if(unit == ObjectManager.Player.Name && name == "ZedShadowDashMissile")
				Clone_W_Created = true;
			if(unit == ObjectManager.Player.Name && name == "ZedUltMissile")
			{
				Clone_R_Created = true;
				Clone_R_nearPosition = ObjectManager.Player.ServerPosition;
			}
		}
	}
}
