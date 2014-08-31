using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace UltimateCarry
{
	class Activator
	{
		public static SpellSlot smite = SpellSlot.Unknown;
		public static SpellSlot barrier = SpellSlot.Unknown;
		public static SpellSlot heal = SpellSlot.Unknown;
		public static SpellSlot dot = SpellSlot.Unknown;
		public static SpellSlot exhoust = SpellSlot.Unknown;

		public Activator()
		{
			Program.Menu.AddSubMenu(new Menu("Supported Items", "supportedextras"));
			Program.Menu.SubMenu("supportedextras").AddSubMenu(new Menu("Active", "ItemsActive"));
			Program.Menu.SubMenu("supportedextras").AddSubMenu(new Menu("Defensive", "ItemsDefensive"));
			Program.Menu.SubMenu("supportedextras").AddSubMenu(new Menu("Neutral", "ItemsNeutral"));
			//foreach (Item item in GetallItems().Where(item => item.IsMap()))
			//	Program.Menu.SubMenu("supportedextras").SubMenu("Items" + item.Modestring).AddItem(new MenuItem("Item" + item.Id + item.Modestring, item.Name).SetValue(true));

			AddSummonerMenu();
			
			Game.OnGameUpdate += Game_OnGameUpdate;
		}

		internal static void AddSummonerMenu()
		{

			var spells = ObjectManager.Player.SummonerSpellbook.Spells;
			foreach(var spell in spells.Where(spell => spell.Name.ToLower() == "summonersmite"))
				smite = spell.Slot;
			foreach(var spell in spells.Where(spell => spell.Name.ToLower() == "summonerbarrier"))
				barrier = spell.Slot;
			foreach(var spell in spells.Where(spell => spell.Name.ToLower() == "summonerheal"))
				heal = spell.Slot;
			foreach(var spell in spells.Where(spell => spell.Name.ToLower() == "summonerdot"))
				dot = spell.Slot;
			foreach(var spell in spells.Where(spell => spell.Name.ToLower() == "summonerexhaust"))
				exhoust = spell.Slot;

			if(smite != SpellSlot.Unknown)
			{
				Program.Menu.SubMenu("supportedextras").AddSubMenu(new Menu("Smite", "sumSmite"));
				Program.Menu.SubMenu("supportedextras").SubMenu("sumSmite").AddItem(new MenuItem("useSmite", "Use Smite").SetValue(true));
			}

			if(barrier != SpellSlot.Unknown)
			{
				Program.Menu.SubMenu("supportedextras").AddSubMenu(new Menu("Barrier", "sumBarrier"));
				Program.Menu.SubMenu("supportedextras").SubMenu("sumBarrier").AddItem(new MenuItem("useBarrier", "Use Barrier").SetValue(true));
				Program.Menu.SubMenu("supportedextras").SubMenu("sumBarrier").AddItem(new MenuItem("useBarrierPercent", "at percent").SetValue(new Slider(20, 99, 1)));
				Program.Menu.SubMenu("supportedextras").SubMenu("sumBarrier").AddItem(new MenuItem("useBarrierifEnemy", "just if Enemy near").SetValue(false));
			}

			if(heal != SpellSlot.Unknown)
			{
				Program.Menu.SubMenu("supportedextras").AddSubMenu(new Menu("Heal", "sumHeal"));
				Program.Menu.SubMenu("supportedextras").SubMenu("sumHeal").AddItem(new MenuItem("useHeal", "Use Heal").SetValue(true));
				Program.Menu.SubMenu("supportedextras").SubMenu("sumHeal").AddItem(new MenuItem("useHealPercent", "at percent").SetValue(new Slider(20, 99, 1)));
				Program.Menu.SubMenu("supportedextras").SubMenu("sumHeal").AddItem(new MenuItem("useHealFriend", "also for Friend").SetValue(true));
				Program.Menu.SubMenu("supportedextras").SubMenu("sumHeal").AddItem(new MenuItem("useHealifEnemy", "just if Enemy near").SetValue(false));
			}

			if(dot != SpellSlot.Unknown)
			{
				Program.Menu.SubMenu("supportedextras").AddSubMenu(new Menu("Ignite", "sumDot"));
				Program.Menu.SubMenu("supportedextras").SubMenu("sumDot").AddItem(new MenuItem("useDot1", "Use Dot for KS").SetValue(true));
				Program.Menu.SubMenu("supportedextras").SubMenu("sumDot").AddItem(new MenuItem("useDot2", "Use Dot on Lowest Health").SetValue(false));
			}

			if(exhoust == SpellSlot.Unknown)
				return;
			Program.Menu.SubMenu("supportedextras").AddSubMenu(new Menu("Exhaust", "sumExhaust"));
			Program.Menu.SubMenu("supportedextras").SubMenu("sumExhaust").AddItem(new MenuItem("useExhaust", "Use Exhaust").SetValue(true));
		}

		private static void Game_OnGameUpdate(EventArgs args)
		{
			Check_Smite();
			Check_Barrier();
			Check_Heal();
			Check_Dot();
			Check_Exhaust();
		}

		private static void Check_Smite()
		{
			if(smite == SpellSlot.Unknown ||
				(!Program.Menu.Item("useSmite").GetValue<bool>() ||
				 ObjectManager.Player.SummonerSpellbook.CanUseSpell(smite) !=
				 SpellState.Ready))
				return;
			var minion = SmiteTarget.GetNearest(ObjectManager.Player.Position);
			if(minion != null && minion.Health <= SmiteTarget.Damage())
				ObjectManager.Player.SummonerSpellbook.CastSpell(smite, minion);
		}

		private static void Check_Barrier()
		{
			if(barrier == SpellSlot.Unknown ||
				(!Program.Menu.Item("useBarrier").GetValue<bool>() ||
				 ObjectManager.Player.SummonerSpellbook.CanUseSpell(barrier) !=
				 SpellState.Ready))
				return;
			if(Program.Menu.Item("useBarrierifEnemy").GetValue<bool>())
			{
				var target = SimpleTs.GetTarget(1000, SimpleTs.DamageType.Physical);
				if(target == null)
					return;
			}
			if (!(ObjectManager.Player.Health/ObjectManager.Player.MaxHealth*100 <=
			      Program.Menu.Item("useBarrierPercent").GetValue<Slider>().Value)) return;
			ObjectManager.Player.SummonerSpellbook.CastSpell(barrier);
		}

		private static void Check_Heal()
		{
			if(heal == SpellSlot.Unknown ||
			    (!Program.Menu.Item("useHeal").GetValue<bool>() ||
				 ObjectManager.Player.SummonerSpellbook.CanUseSpell(heal) !=
			     SpellState.Ready))
				return;
			if (Program.Menu.Item("useHealifEnemy").GetValue<bool>())
			{
				var target = SimpleTs.GetTarget(1000, SimpleTs.DamageType.Physical);
				if (target == null)
					return;
			}
			if (ObjectManager.Player.Health/ObjectManager.Player.MaxHealth*100 <=
			    Program.Menu.Item("useHealPercent").GetValue<Slider>().Value)
			{
				ObjectManager.Player.SummonerSpellbook.CastSpell(heal);
				return;
			}
			const int range = 700;
			if(Program.Menu.Item("useHealFriend").GetValue<bool>() && ObjectManager.Get<Obj_AI_Hero>().Any(hero => hero.Health / hero.MaxHealth * 100 <= Program.Menu.Item("useHealPercent").GetValue<Slider>().Value && hero.IsAlly && hero.Distance(ObjectManager.Player.Position) <= range))
				ObjectManager.Player.SummonerSpellbook.CastSpell(heal);
		}

		private static void Check_Dot()
		{
			if(dot == SpellSlot.Unknown ||
				(!Program.Menu.Item("useDot1").GetValue<bool>() || !Program.Menu.Item("useDot2").GetValue<bool>() ||
				 ObjectManager.Player.SummonerSpellbook.CanUseSpell(dot) !=
				 SpellState.Ready))
				return;
			const int range = 600;
			if(Program.Menu.Item("useDot1").GetValue<bool>())
				foreach(var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(range) && DamageLib.getDmg(hero, DamageLib.SpellType.IGNITE) >= hero.Health))
				{
					ObjectManager.Player.SummonerSpellbook.CastSpell(dot, enemy);
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
			ObjectManager.Player.SummonerSpellbook.CastSpell(dot, lowhealthEnemy);
		}
		
		private static void Check_Exhaust()
		{
			if(exhoust == SpellSlot.Unknown ||
			    (!Program.Menu.Item("useExhaust").GetValue<bool>() ||
				 ObjectManager.Player.SummonerSpellbook.CanUseSpell(exhoust) !=
			     SpellState.Ready) || Program.Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo) 
				return;
			Obj_AI_Hero  maxDpsHero = null;
			float maxDps = 0;
			const int range = 550;
			foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(range + 200)))
			{
				var dps = enemy.BaseAttackDamage * enemy.AttackSpeedMod;
				if (maxDpsHero != null && !(maxDps < dps)) 
					continue;
				maxDps = dps;
				maxDpsHero = enemy;
			}
			if (maxDpsHero == null) 
				return;
			ObjectManager.Player.SummonerSpellbook.CastSpell(exhoust, maxDpsHero);
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
				new Item(2041, "Crystalline Flask", "1,2,3", "Neutral"),
				new Item(2004, "Health Potion", "1,2,3,4", "Neutral"),
				new Item(2010, "Biscuit", "1,2,3,4", "Neutral")
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
				int[] stages = {20*level + 370, 30*level + 330, 40*level + 240, 50*level + 100};
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
