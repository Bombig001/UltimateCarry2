using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace UltimateCarry
{
	class Lulu : Champion
	{
		public static Spell Q;
		public static Spell QPix;
		public static Spell W;
		public static Spell E;
		public static Spell R;

		public Lulu()
		{
			Name = "Lulu";
			Chat.Print(Name + " Plugin Loading ...");
			LoadMenu();
			LoadSpells();

			Drawing.OnDraw += Drawing_OnDraw;
			Game.OnGameUpdate += Game_OnGameUpdate;
			Game.OnGameSendPacket += Game_OnGameSendPacket;
			Interrupter.OnPosibleToInterrupt += Interrupter_OnPosibleToInterrupt;
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
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useW_TeamFight_assist", "Use W Assist").SetValue(true));
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useE_TeamFight_Aggro", "Use E pref Aggro").SetValue(false));
			Program.Menu.Item("useE_TeamFight_Aggro").ValueChanged += SwitchAggro;
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useE_TeamFight_Passiv", "Use E pref Passive").SetValue(true));
			Program.Menu.Item("useE_TeamFight_Passiv").ValueChanged += SwitchPassiv;
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useR_TeamFight", "Use Smart R").SetValue(true));

			Program.Menu.AddSubMenu(new Menu("Harass", "Harass"));
			Program.Menu.SubMenu("Harass").AddItem(new MenuItem("useQ_Harass", "Use Q").SetValue(true));
			Program.Menu.SubMenu("Harass").AddItem(new MenuItem("useE_Harass", "Use E").SetValue(true));
			Program.Menu.SubMenu("Harass").AddItem(new MenuItem("useE_Harass_pix", "Use E on Minion for Q").SetValue(true));
			
			Program.Menu.AddSubMenu(new Menu("LaneClear", "LaneClear"));
			Program.Menu.SubMenu("LaneClear").AddItem(new MenuItem("useQ_LaneClear", "Use Q").SetValue(true));

			Program.Menu.AddSubMenu(new Menu("Passive", "Passive"));
			Program.Menu.SubMenu("Passive").AddItem(new MenuItem("useE_Interupt", "E Interrupt").SetValue(false));

			Program.Menu.AddSubMenu(new Menu("SupportMode", "SupportMode"));
			Program.Menu.SubMenu("SupportMode").AddItem(new MenuItem("hitMinions", "Hit Minions").SetValue(false));

			Program.Menu.AddSubMenu(new Menu("Drawing", "Drawing"));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_Disabled", "Disable All").SetValue(false));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_Q", "Draw Q").SetValue(true));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_W", "Draw W").SetValue(true));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_E", "Draw E").SetValue(true));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_R", "Draw R").SetValue(true));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_pix", "Draw Pix").SetValue(true));
			
		}

		private static void SwitchAggro(object sender, OnValueChangeEventArgs e)
		{
			if (e.GetNewValue<bool>())
				Program.Menu.Item("useE_TeamFight_Passiv").SetValue(false);
		}

		private static void SwitchPassiv(object sender, OnValueChangeEventArgs e)
		{
			if(e.GetNewValue<bool>())
				Program.Menu.Item("useE_TeamFight_Aggro").SetValue(false);
		}

		private static void LoadSpells()
		{
			Q = new Spell(SpellSlot.Q, 945);
			Q.SetSkillshot(0.25f,50,1400,false,SkillshotType.SkillshotLine);

			QPix = new Spell(SpellSlot.Q, 945);
			QPix.SetSkillshot(0.25f, 50, 1500, false, SkillshotType.SkillshotLine,PixPosition(),PixPosition());
			
			W = new Spell(SpellSlot.W, 650);

			E = new Spell(SpellSlot.E, 650);

			R = new Spell(SpellSlot.R, 900);
		
		}

		private static Vector3 PixPosition()
		{
			foreach (var pix in ObjectManager.Get<Obj_AI_Minion>().Where(pix => pix.Name == "RobotBuddy" && pix.IsAlly))
				return pix.Position;
			return ObjectManager.Player.Position;
		}

		private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
		{
			if(!Program.Menu.Item("useE_Interupt").GetValue<bool>())
				return;
			if(ObjectManager.Player.Distance(unit) < W.Range && W.IsReady() && unit.IsEnemy )
				W.Cast(unit, Packets());
		}

		private static void Game_OnGameUpdate(EventArgs args)
		{
			switch(Program.Orbwalker.ActiveMode)
			{
				case Orbwalking.OrbwalkingMode.Combo:
					if (Program.Menu.Item("useQ_TeamFight").GetValue<bool>())
					{
						Cast_BasicLineSkillshot_Enemy(Q, SimpleTs.DamageType.Magical);
						Cast_BasicLineSkillshot_Enemy(QPix, PixPosition(), SimpleTs.DamageType.Magical);
					}
					if (Program.Menu.Item("useW_TeamFight_assist").GetValue<bool>())
						Cast_Speedboost_onFriend(W);
					if (Program.Menu.Item("useE_TeamFight_Aggro").GetValue<bool>() ||
					    Program.Menu.Item("useE_TeamFight_Passiv").GetValue<bool>())
						Cast_E();
					if (Program.Menu.Item("useR_TeamFight").GetValue<bool>())
						Cast_R();
					break;
				case Orbwalking.OrbwalkingMode.Mixed:
					if(Program.Menu.Item("useQ_Harass").GetValue<bool>())
					{
						Cast_BasicLineSkillshot_Enemy(Q, SimpleTs.DamageType.Magical);
						Cast_BasicLineSkillshot_Enemy(QPix, PixPosition(),SimpleTs.DamageType.Magical);	
					}
					if(Program.Menu.Item("useE_Harass").GetValue<bool>())
						Cast_onEnemy(E, SimpleTs.DamageType.Magical);
					if(Program.Menu.Item("useE_Harass_pix").GetValue<bool>() && EnoughManaFor(SpellSlot.Q,SpellSlot.E) && Q.IsReady())
						Cast_onMinion_nearEnemy(E, Q.Range,SimpleTs.DamageType.Magical );
					break;
				case Orbwalking.OrbwalkingMode.LaneClear:
					if(Program.Menu.Item("useQ_LaneClear").GetValue<bool>())
						Cast_BasicLineSkillshot_AOE_Farm(Q);

					break;
			}
		}

		private static void Cast_R()
		{
			if(!R.IsReady())
				return;
			foreach(var friend in from friend in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsAlly && hero.Distance(ObjectManager.Player) <= R.Range)
								  let enemyCount = ObjectManager.Get<Obj_AI_Hero>().Count(hero => hero.IsEnemy && hero.Distance(friend) <= 250)
								  let frinedhealthprecent = friend.Health / friend.MaxHealth * 100
								  where frinedhealthprecent <= 20 || (frinedhealthprecent <= 30 && enemyCount >= 2)
								  select friend)
			{
				R.CastOnUnit(friend, Packets());
				return;
			}
		}

		private static void Cast_E()
		{
			var healatpercent = Program.Menu.Item("useE_TeamFight_Passiv").GetValue<bool>() ? 70 : 35;
			var attack = Program.Menu.Item("useE_TeamFight_Aggro").GetValue<bool>();
			Cast_Shield_onFriend(E,healatpercent);
			if (attack)
				Cast_onEnemy(E,SimpleTs.DamageType.Magical);
		}

		private static void Drawing_OnDraw(EventArgs args)
		{

			if(Program.Menu.Item("Draw_Disabled").GetValue<bool>())
				return;

			if(Program.Menu.Item("Draw_pix").GetValue<bool>())
			{
				Utility.DrawCircle(PixPosition(), 100, Color.Blue);
				if(Q.Level > 0)
					Utility.DrawCircle(PixPosition(), QPix.Range, QPix.IsReady() ? Color.Green : Color.Red);
			}

			if(Program.Menu.Item("Draw_Q").GetValue<bool>())
				if(Q.Level > 0)
					Utility.DrawCircle(ObjectManager.Player.Position, QPix.Range, QPix.IsReady() ? Color.Green : Color.Red);

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

	}
}
