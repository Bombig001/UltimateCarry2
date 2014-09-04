using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace UltimateCarry
{
	 class Champion
	{
		public string  Name = "";

		public static bool Packets()
		{
			return Program.Menu.Item("usePackets").GetValue<bool>();
		}

		public static void Game_OnGameSendPacket(GamePacketEventArgs args)
		{
			if(args.PacketData[0] != Packet.C2S.Move.Header)
				return;
			var decodedPacket = Packet.C2S.Move.Decoded(args.PacketData);
			if(decodedPacket.MoveType == 3 &&
				(Program.Orbwalker.GetTarget().IsMinion && !Program.Menu.Item("hitMinions").GetValue<bool>()))
				args.Process = false;
		}

		public static void Cast_BasicLineSkillshot_Enemy(Spell spell,SimpleTs.DamageType damageType = SimpleTs.DamageType.Physical )
		{
			if(!spell.IsReady())
				return;
			var target = SimpleTs.GetTarget(spell.Range, damageType);
			if(target == null)
				return;
			if(target.IsValidTarget(spell.Range) && spell.GetPrediction(target).Hitchance >= HitChance.High)
				spell.Cast(target, Packets());
		}

		public static void Cast_BasicLineSkillshot_Enemy(Spell spell, Vector3 sourcePosition, SimpleTs.DamageType damageType = SimpleTs.DamageType.Physical)
		{
			if(!spell.IsReady())
				return;
			spell.UpdateSourcePosition(sourcePosition, sourcePosition);
			foreach(var hero in ObjectManager.Get<Obj_AI_Hero>()
				.Where(hero => (hero.Distance(sourcePosition) < spell.Range) && hero.IsValidTarget()).Where(hero => spell.GetPrediction(hero).Hitchance >= HitChance.High))
			{
				spell.Cast(hero, Packets());
				return;
			}
		}

		public static void Cast_BasicLineSkillshot_AOE_Farm(Spell spell)
		{
			if(!spell.IsReady())
				return;
			var minions = MinionManager.GetMinions(ObjectManager.Player.Position, spell.Range, MinionTypes.All, MinionTeam.NotAlly);
			if(minions.Count == 0)
				return;
			var castPostion = MinionManager.GetBestLineFarmLocation(minions.Select(minion => minion.ServerPosition.To2D()).ToList(), spell.Width, spell.Range);
			spell.Cast(castPostion.Position, Packets());
		}

		public static void Cast_Speedboost_onFriend(Spell spell)
		{
			if(!spell.IsReady())
				return;
			var champions = ObjectManager.Get<Obj_AI_Hero>();
			var objAiHeroes = champions as IList<Obj_AI_Hero> ?? champions.ToList();
			var friends = objAiHeroes.Where(x => x.IsAlly);
			var enemies = objAiHeroes.Where(x => x.IsEnemy);

			var friend = friends.FirstOrDefault(x => x.Distance(ObjectManager.Player) <= spell.Range &&
				enemies.Any(enemy => x.Distance(enemy) <= Orbwalking.GetRealAutoAttackRange(x) + 200 && x.BaseAttackDamage * x.AttackSpeedMod * 3 >= enemy.Health));

			if (friend == null) 
				return;
			spell.CastOnUnit(friend, Packets());
		}

		public static void Cast_Shield_onFriend(Spell spell, int percent)
		{
			if(!spell.IsReady())
				return;
			foreach (var friend in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsAlly && hero.Distance(ObjectManager.Player) <= spell.Range).Where(friend => friend.Health/friend.MaxHealth*100 <= percent && Utility.CountEnemysInRange( 1000) >=1 ))
			{
				spell.CastOnUnit(friend,Packets());
				return;
			}
		}

		public static void Cast_onEnemy(Spell spell, SimpleTs.DamageType damageType = SimpleTs.DamageType.Physical)
		{
			if(!spell.IsReady())
				return;
			var target = SimpleTs.GetTarget(spell.Range, damageType);
			if(target.IsValidTarget(spell.Range))
				spell.CastOnUnit(target, Packets());
		}

		public static void Cast_onMinion_nearEnemy(Spell spell, float range, SimpleTs.DamageType damageType = SimpleTs.DamageType.Physical, MinionTypes minionTypes = MinionTypes.All, MinionTeam minionTeam = MinionTeam.All)
		{
			if(!spell.IsReady())
				return;
			var target = SimpleTs.GetTarget(spell.Range + range, damageType);
			Obj_AI_Base nearstMinion = null;
			var allminions = MinionManager.GetMinions(target.Position, range, minionTypes, minionTeam);
			foreach (var minion in allminions.Where(minion => minion.Distance(ObjectManager.Player) <= spell.Range && minion.Distance(target) <= range).Where(minion => nearstMinion == null || nearstMinion.Distance(target) >= minion.Distance(target)))
				nearstMinion = minion;

			if (nearstMinion != null)
				spell.CastOnUnit(nearstMinion,Packets());
		}

		public static bool EnoughManaFor(SpellSlot spell)
		{
			return ObjectManager.Player.Spellbook.GetSpell(spell).ManaCost <= ObjectManager.Player.Mana;
		}

		public static bool EnoughManaFor(SpellSlot spell, SpellSlot spell2)
		{
			return ObjectManager.Player.Spellbook.GetSpell(spell).ManaCost +
					ObjectManager.Player.Spellbook.GetSpell(spell2).ManaCost <= ObjectManager.Player.Mana;
		}

		public static bool EnoughManaFor(SpellSlot spell, SpellSlot spell2, SpellSlot spell3)
		{
			return ObjectManager.Player.Spellbook.GetSpell(spell).ManaCost +
					ObjectManager.Player.Spellbook.GetSpell(spell2).ManaCost +
					ObjectManager.Player.Spellbook.GetSpell(spell3).ManaCost <= ObjectManager.Player.Mana;
		}

		public static bool EnoughManaFor(SpellSlot spell, SpellSlot spell2, SpellSlot spell3, SpellSlot spell4)
		{
			return ObjectManager.Player.Spellbook.GetSpell(spell).ManaCost +
					ObjectManager.Player.Spellbook.GetSpell(spell2).ManaCost +
					ObjectManager.Player.Spellbook.GetSpell(spell3).ManaCost +
					ObjectManager.Player.Spellbook.GetSpell(spell4).ManaCost <= ObjectManager.Player.Mana;
		}
	}
 }

