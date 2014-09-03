using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace UltimateCarry
{
	// ReSharper disable EmptyGeneralCatchClause

	class Activator
	{
		public static SpellSlot Smite = SpellSlot.Unknown;
		public static SpellSlot Barrier = SpellSlot.Unknown;
		public static SpellSlot Heal = SpellSlot.Unknown;
		public static SpellSlot Dot = SpellSlot.Unknown;
		public static SpellSlot Exhoust = SpellSlot.Unknown;

		public Activator()
		{
			Program.Menu.AddSubMenu(new Menu("Supported Items", "supportedextras"));
			Program.Menu.SubMenu("supportedextras").AddSubMenu(new Menu("Active", "ItemsActive"));
			Program.Menu.SubMenu("supportedextras").AddSubMenu(new Menu("Defensive", "ItemsDefensive"));
			Program.Menu.SubMenu("supportedextras").AddSubMenu(new Menu("Neutral", "ItemsNeutral"));
			foreach(Item item in GetallItems().Where(item => item.IsMap()))
				Program.Menu.SubMenu("supportedextras").SubMenu("Items" + item.Modestring).AddItem(new MenuItem("Item" + item.Id + item.Modestring, item.Name).SetValue(true));

			AddSummonerMenu();

			Game.OnGameUpdate += Game_OnGameUpdate;
		}

		private static void Game_OnGameUpdate(EventArgs args)
		{
			Check_Smite();
			Check_Barrier();
			Check_Heal();
			Check_Dot();
			Check_Exhaust();
			Check_Active_Items();
			Check_AntiStun_Me();
			Check_AntiStun_Friend();
		}

		private static void Check_Active_Items()
		{
			if(Program.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
			{
				Check_HEXGUN();
				Check_HYDRA();
				Check_TIAMANT();
				Check_DFG();
				Check_BILGEWATER();
				Check_BOTRK();
				Check_YOMO();
			}
		}

		private static void Check_YOMO()
		{
			try
			{
				var item = new Item(3142, "Youmuu's Ghostblade", "1,2,3,4", "Active");
				Obj_AI_Hero[] nearenemy = {null};
				foreach(var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget()).Where(enemy => nearenemy[0] == null || nearenemy[0].Distance(ObjectManager.Player) < enemy.Distance(ObjectManager.Player)))
					nearenemy[0] = enemy;

				if(nearenemy[0] == null)
					return;
				if(Orbwalking.GetRealAutoAttackRange(nearenemy[0]) <= nearenemy[0].Distance(ObjectManager.Player))
					Items.UseItem(item.Id);
			}
			catch
			{
			}
		}

		private static void Check_TIAMANT()
		{
			try
			{
				var item = new Item(3077, "Tiamat", "1,2,3,4", "Active", 400);
				var targ = ObjectManager.Get<Obj_AI_Hero>().First(hero => hero.IsValidTarget(item.Range));
				if(Items.CanUseItem(item.Id) && item.IsEnabled() && item.IsMap() && targ != null)
					Items.UseItem(item.Id);
			}
			catch
			{
			}
		}

		private static void Check_HYDRA()
		{
			try
			{
				var item = new Item(3074, "Ravenous Hydra", "1,2,3,4", "Active", 400);
				var targ = ObjectManager.Get<Obj_AI_Hero>().First(hero => hero.IsValidTarget(item.Range));
				if(Items.CanUseItem(item.Id) && item.IsEnabled() && item.IsMap() && targ != null)
					Items.UseItem(item.Id);
			}
			catch
			{
			}
		}

		private static void Check_HEXGUN()
		{
			try
			{
				var item = new Item(3146, "Hextech Gunblade", "1,2,3,4", "Active", 700);
				var targ = SimpleTs.GetTarget(item.Range, SimpleTs.DamageType.Magical);
				if(Items.CanUseItem(item.Id) && item.IsEnabled() && item.IsMap() && targ != null)
					Items.UseItem(item.Id, targ);
			}
			catch
			{
			}
		}

		private static void Check_BILGEWATER()
		{
			try
			{
				var item = new Item(3144, "Bilgewater Cutlass", "1,2,3,4", "Active", 450);
				var targ = SimpleTs.GetTarget(item.Range, SimpleTs.DamageType.Physical);
				if(Items.CanUseItem(item.Id) && item.IsEnabled() && item.IsMap() && targ != null)
					Items.UseItem(item.Id, targ);
			}
			catch
			{
			}
		}

		private static void Check_DFG()
		{
			try
			{
				var item = new Item(3128, "Deathfire Grasp", "1,4", "Active", 750);
				var targ = SimpleTs.GetTarget(item.Range, SimpleTs.DamageType.Magical);
				if(Items.CanUseItem(item.Id) && item.IsEnabled() && item.IsMap() && targ != null)
					Items.UseItem(item.Id, targ);
			}
			catch
			{
			}
		}

		private static void Check_BOTRK()
		{
			try
			{
				var item = new Item(3153, "Blade of the Ruined King", "1,2,3,4", "Active", 450);
				var targ = SimpleTs.GetTarget(item.Range, SimpleTs.DamageType.Magical);
				if(Items.CanUseItem(item.Id) && item.IsEnabled() && item.IsMap() && targ != null)
				{
					if((targ.MaxHealth / 100 * 10) > (ObjectManager.Player.MaxHealth - ObjectManager.Player.Health))
						Items.UseItem(item.Id, targ);
					if(ObjectManager.Player.ChampionName == "Zed")
						if(Zed.CloneR != null)
							Items.UseItem(item.Id, targ);
				}
			}
			catch
			{
			}
		}

		private static void Check_AntiStun_Me()
		{
			try
			{
				// remove deathmark from zed 
				var itemList = new List<Item>();
				itemList.Add(new Item(3139, "Mercurial Scimitar", "1,4", "Defensive"));
				itemList.Add(new Item(3137, "Dervish Blade", "2,3", "Defensive"));
				itemList.Add(new Item(3140, "Quicksilver Sash", "1,2,3,4", "Defensive"));

				foreach(Item item in from item in itemList
									 where item.IsMap()
									 where Items.CanUseItem(item.Id)
									 where ObjectManager.Player.HasBuffOfType(BuffType.Snare) || ObjectManager.Player.HasBuffOfType(BuffType.Stun)
									 where item.IsEnabled()
									 select item)
				{
					Items.UseItem(item.Id);
					return;
				}
			}
			catch
			{
			}
		}

		private static void Check_AntiStun_Friend()
		{
			try
			{
				// todo remove deathmark from zed 
				var item = new Item(3222, "Mikael's Crucible", "1,2,3,4", "Defensive", 750);
				var friend = ObjectManager.Get<Obj_AI_Hero>().First(hero => hero.IsAlly && !hero.IsDead && (hero.HasBuffOfType(BuffType.Snare) || hero.HasBuffOfType(BuffType.Stun)) && hero.Distance(ObjectManager.Player) <= item.Range && Items.CanUseItem(item.Id) && item.IsMap() && item.IsEnabled());
				if(friend == null)
					return;
				Items.UseItem(item.Id, friend);
			}
			catch
			{
			}


		}

		internal static void AddSummonerMenu()
		{
			var spells = ObjectManager.Player.SummonerSpellbook.Spells;
			foreach(var spell in spells.Where(spell => spell.Name.ToLower() == "summonersmite"))
				Smite = spell.Slot;
			foreach(var spell in spells.Where(spell => spell.Name.ToLower() == "summonerbarrier"))
				Barrier = spell.Slot;
			foreach(var spell in spells.Where(spell => spell.Name.ToLower() == "summonerheal"))
				Heal = spell.Slot;
			foreach(var spell in spells.Where(spell => spell.Name.ToLower() == "summonerdot"))
				Dot = spell.Slot;
			foreach(var spell in spells.Where(spell => spell.Name.ToLower() == "summonerexhaust"))
				Exhoust = spell.Slot;

			if(Smite != SpellSlot.Unknown)
			{
				Program.Menu.SubMenu("supportedextras").AddSubMenu(new Menu("Smite", "sumSmite"));
				Program.Menu.SubMenu("supportedextras").SubMenu("sumSmite").AddItem(new MenuItem("useSmite", "Use Smite").SetValue(true));
			}

			if(Barrier != SpellSlot.Unknown)
			{
				Program.Menu.SubMenu("supportedextras").AddSubMenu(new Menu("Barrier", "sumBarrier"));
				Program.Menu.SubMenu("supportedextras").SubMenu("sumBarrier").AddItem(new MenuItem("useBarrier", "Use Barrier").SetValue(true));
				Program.Menu.SubMenu("supportedextras").SubMenu("sumBarrier").AddItem(new MenuItem("useBarrierPercent", "at percent").SetValue(new Slider(20, 99, 1)));
				Program.Menu.SubMenu("supportedextras").SubMenu("sumBarrier").AddItem(new MenuItem("useBarrierifEnemy", "just if Enemy near").SetValue(false));
			}

			if(Heal != SpellSlot.Unknown)
			{
				Program.Menu.SubMenu("supportedextras").AddSubMenu(new Menu("Heal", "sumHeal"));
				Program.Menu.SubMenu("supportedextras").SubMenu("sumHeal").AddItem(new MenuItem("useHeal", "Use Heal").SetValue(true));
				Program.Menu.SubMenu("supportedextras").SubMenu("sumHeal").AddItem(new MenuItem("useHealPercent", "at percent").SetValue(new Slider(20, 99, 1)));
				Program.Menu.SubMenu("supportedextras").SubMenu("sumHeal").AddItem(new MenuItem("useHealFriend", "also for Friend").SetValue(true));
				Program.Menu.SubMenu("supportedextras").SubMenu("sumHeal").AddItem(new MenuItem("useHealifEnemy", "just if Enemy near").SetValue(false));
			}

			if(Dot != SpellSlot.Unknown)
			{
				Program.Menu.SubMenu("supportedextras").AddSubMenu(new Menu("Ignite", "sumDot"));
				Program.Menu.SubMenu("supportedextras").SubMenu("sumDot").AddItem(new MenuItem("useDot1", "Use Dot for KS").SetValue(true));
				Program.Menu.SubMenu("supportedextras").SubMenu("sumDot").AddItem(new MenuItem("useDot2", "Use Dot on Lowest Health").SetValue(false));
			}

			if(Exhoust == SpellSlot.Unknown)
				return;
			Program.Menu.SubMenu("supportedextras").AddSubMenu(new Menu("Exhaust", "sumExhaust"));
			Program.Menu.SubMenu("supportedextras").SubMenu("sumExhaust").AddItem(new MenuItem("useExhaust", "Use Exhaust").SetValue(true));
		}

		private static void Check_Smite()
		{
			if(Smite == SpellSlot.Unknown ||
				(!Program.Menu.Item("useSmite").GetValue<bool>() ||
				 ObjectManager.Player.SummonerSpellbook.CanUseSpell(Smite) !=
				 SpellState.Ready))
				return;
			var minion = SmiteTarget.GetNearest(ObjectManager.Player.Position);
			if(minion != null && minion.Health <= SmiteTarget.Damage())
				ObjectManager.Player.SummonerSpellbook.CastSpell(Smite, minion);
		}

		private static void Check_Barrier()
		{
			if(Barrier == SpellSlot.Unknown ||
				(!Program.Menu.Item("useBarrier").GetValue<bool>() ||
				 ObjectManager.Player.SummonerSpellbook.CanUseSpell(Barrier) !=
				 SpellState.Ready))
				return;
			if(Program.Menu.Item("useBarrierifEnemy").GetValue<bool>())
			{
				var target = SimpleTs.GetTarget(1000, SimpleTs.DamageType.Physical);
				if(target == null)
					return;
			}
			if(!(ObjectManager.Player.Health / ObjectManager.Player.MaxHealth * 100 <=
				  Program.Menu.Item("useBarrierPercent").GetValue<Slider>().Value))
				return;
			ObjectManager.Player.SummonerSpellbook.CastSpell(Barrier);
		}

		private static void Check_Heal()
		{
			if(Heal == SpellSlot.Unknown ||
				(!Program.Menu.Item("useHeal").GetValue<bool>() ||
				 ObjectManager.Player.SummonerSpellbook.CanUseSpell(Heal) !=
				 SpellState.Ready))
				return;
			if(Program.Menu.Item("useHealifEnemy").GetValue<bool>())
			{
				var target = SimpleTs.GetTarget(1000, SimpleTs.DamageType.Physical);
				if(target == null)
					return;
			}
			if(ObjectManager.Player.Health / ObjectManager.Player.MaxHealth * 100 <=
				Program.Menu.Item("useHealPercent").GetValue<Slider>().Value)
			{
				ObjectManager.Player.SummonerSpellbook.CastSpell(Heal);
				return;
			}
			const int range = 700;
			if(Program.Menu.Item("useHealFriend").GetValue<bool>() && ObjectManager.Get<Obj_AI_Hero>().Any(hero => hero.Health / hero.MaxHealth * 100 <= Program.Menu.Item("useHealPercent").GetValue<Slider>().Value && hero.IsAlly && hero.Distance(ObjectManager.Player.Position) <= range))
				ObjectManager.Player.SummonerSpellbook.CastSpell(Heal);
		}

		private static void Check_Dot()
		{
			if(Dot == SpellSlot.Unknown ||
				 ObjectManager.Player.SummonerSpellbook.CanUseSpell(Dot) !=
				 SpellState.Ready)
				return;
			if(!(Program.Menu.Item("useDot1").GetValue<bool>() || Program.Menu.Item("useDot2").GetValue<bool>()))
				return;
			const int range = 600;
			if(Program.Menu.Item("useDot1").GetValue<bool>())
				foreach(var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(range) && DamageLib.getDmg(hero, DamageLib.SpellType.IGNITE) >= hero.Health))
				{
					ObjectManager.Player.SummonerSpellbook.CastSpell(Dot, enemy);
					return;
				}
			if(!Program.Menu.Item("useDot2").GetValue<bool>())
				return;
			Obj_AI_Hero lowhealthEnemy = null;
			foreach(var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(range) && (enemy.Health / enemy.MaxHealth * 100 <= 60)))
			{
				if(lowhealthEnemy != null)
				{
					if(lowhealthEnemy.Health > enemy.Health)
						lowhealthEnemy = enemy;
				}
				else
					lowhealthEnemy = enemy;
			}
			if(lowhealthEnemy == null)
				return;
			ObjectManager.Player.SummonerSpellbook.CastSpell(Dot, lowhealthEnemy);
		}

		private static void Check_Exhaust()
		{
			if(Exhoust == SpellSlot.Unknown ||
				(!Program.Menu.Item("useExhaust").GetValue<bool>() ||
				 ObjectManager.Player.SummonerSpellbook.CanUseSpell(Exhoust) !=
				 SpellState.Ready) || Program.Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
				return;
			Obj_AI_Hero maxDpsHero = null;
			float maxDps = 0;
			const int range = 550;
			foreach(var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(range + 200)))
			{
				var dps = enemy.BaseAttackDamage * enemy.AttackSpeedMod;
				if(maxDpsHero != null && !(maxDps < dps))
					continue;
				maxDps = dps;
				maxDpsHero = enemy;
			}
			if(maxDpsHero == null)
				return;
			ObjectManager.Player.SummonerSpellbook.CastSpell(Exhoust, maxDpsHero);
		}

		private static IEnumerable<Item> GetallItems()
		{
			var list = new List<Item>
			{
				new Item(3139, "Mercurial Scimitar", "1,4", "Defensive"),
				new Item(3137, "Dervish Blade", "2,3", "Defensive"),
				new Item(3140, "Quicksilver Sash", "1,2,3,4", "Defensive"),
				new Item(3222, "Mikael's Crucible", "1,2,3,4", "Defensive", 750),
				new Item(3146, "Hextech Gunblade", "1,2,3,4", "Active"),
				new Item(3074, "Ravenous Hydra", "1,2,3,4", "Active"),
				new Item(3077, "Tiamat", "1,2,3,4", "Active"),
				new Item(3144, "Bilgewater Cutlass", "1,2,3,4", "Active", 450),
				new Item(3128, "Deathfire Grasp", "1,4", "Active", 750),
				new Item(3153, "Blade of the Ruined King", "1,2,3,4", "Active", 450),
				new Item(3142, "Youmuu's Ghostblade","1,2,3,4", "Active", (int)Orbwalking.GetRealAutoAttackRange(ObjectManager.Player))
			};

			return list;
		}

		internal class SmiteTarget
		{
			private static readonly string[] MinionNames = { "Worm", "Dragon", "LizardElder", "AncientGolem", "TT_Spiderboss", "TTNGolem", "TTNWolf", "TTNWraith" };

			public static Obj_AI_Minion GetNearest(Vector3 pos)
			{
				var minions = ObjectManager.Get<Obj_AI_Minion>().Where(minion => minion.IsValid && MinionNames.Any(name => minion.Name.StartsWith(name)));
				var objAiMinions = minions as Obj_AI_Minion[] ?? minions.ToArray();
				var sMinion = objAiMinions.FirstOrDefault();
				double? nearest = null;
				var index = 0;
				for(; index < objAiMinions.Length; index++)
				{
					var minion = objAiMinions[index];
					var distance = Vector3.Distance(pos, minion.Position);
					if(nearest != null && !(nearest > distance))
						continue;
					nearest = distance;
					sMinion = minion;
				}
				return sMinion;
			}

			public static double Damage()
			{
				var level = ObjectManager.Player.Level;
				int[] stages = { 20 * level + 370, 30 * level + 330, 40 * level + 240, 50 * level + 100 };
				return stages.Max();
			}
		}

		internal class Item
		{
			public int Id;
			public string Name;
			public string Mapstring;
			public string Modestring;
			public int Range;

			public Item(int id, string name, string mapstring, string modestring, int range = 0)
			{
				Id = id;
				Name = name;
				Modestring = modestring;
				Range = range;

				mapstring = mapstring.Replace("1", Utility.Map.MapType.SummonersRift.ToString());
				mapstring = mapstring.Replace("2", Utility.Map.MapType.TwistedTreeline.ToString());
				mapstring = mapstring.Replace("3", Utility.Map.MapType.CrystalScar.ToString());
				mapstring = mapstring.Replace("4", Utility.Map.MapType.HowlingAbyss.ToString());
				Mapstring = mapstring;
			}

			internal bool IsEnabled()
			{
				try
				{
					var ret = Program.Menu.Item("Item" + Id + Modestring).GetValue<bool>();
					return ret;
				}
				catch(Exception)
				{
					return false;
				}
			}

			internal bool IsMap()
			{
				return Mapstring.Contains(Utility.Map.GetMap().ToString());
			}
		}
	}
}
