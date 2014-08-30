using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Collision = LeagueSharp.Common.Collision;
using Color = System.Drawing.Color;

namespace UltimateCarry
{
	class PlayerInfo
	{
		public Obj_AI_Hero Champ;
		public Dictionary<int, float> IncomingDamage;
		public int LastSeen;
		public Packet.S2C.Recall.Struct Recall;

		public PlayerInfo(Obj_AI_Hero champ)
		{
			Champ = champ;
			Recall = new Packet.S2C.Recall.Struct(champ.NetworkId, Packet.S2C.Recall.RecallStatus.Unknown, Packet.S2C.Recall.ObjectType.Player, 0);
			IncomingDamage = new Dictionary<int, float>();
		}

		public PlayerInfo UpdateRecall(Packet.S2C.Recall.Struct newRecall)
		{
			return this;
		}

		public int GetRecallStart()
		{
			switch((int)Recall.Status)
			{
				case (int)Packet.S2C.Recall.RecallStatus.RecallStarted:
				case (int)Packet.S2C.Recall.RecallStatus.TeleportStart:
					return BaseUlt.RecallT[Recall.UnitNetworkId];
				default:
					return 0;
			}
		}

		public int GetRecallEnd()
		{
			return GetRecallStart() + Recall.Duration;
		}

		public int GetRecallCountdown()
		{
			var countdown = GetRecallEnd() - Environment.TickCount;
			return countdown < 0 ? 0 : countdown;
		}

		override public string ToString()
		{
			var drawtext = Champ.ChampionName + ": " + Recall.Status; //change to better string
			var countdown = GetRecallCountdown() / 1000f;
			if(countdown > 0)
				drawtext += " (" + countdown.ToString("0.00") + "s)";
			return drawtext;
		}
	}

	class BaseUlt
	{
		static bool _compatibleChamp;
		static IEnumerable<Obj_AI_Hero> _ownTeam;
		static IEnumerable<Obj_AI_Hero> _enemyTeam;
		static Vector3 _enemySpawnPos;
		static Utility.Map.MapType _map;
		internal static List<PlayerInfo> PlayerInfo = new List<PlayerInfo>();
		public static Dictionary<int, int> RecallT = new Dictionary<int, int>();
		static int _ultCasted;
		static Spell _ult;

		internal struct UltData
		{
			public DamageLib.StageType StageType;
			public float ManaCost;
			public float DamageMultiplicator;
			public float Width, Delay, Speed;
			public float Range;
		}
		static readonly Dictionary<String, UltData> UltInfo = new Dictionary<string, UltData>
		{
			{"Jinx", new UltData {StageType = DamageLib.StageType.Default, ManaCost = 100f, DamageMultiplicator = 1f, Width = 140f, Delay = 600f / 1000f, Speed = 1700f, Range = 20000f}},
			{"Ashe", new UltData {StageType = DamageLib.StageType.Default, ManaCost = 100f, DamageMultiplicator = 1f, Width = 130f, Delay = 250f / 1000f, Speed = 1600f, Range = 20000f}},
			{"Draven", new UltData {StageType = DamageLib.StageType.FirstDamage, ManaCost = 120f, DamageMultiplicator = 0.7f, Width = 160f, Delay = 400f / 1000f, Speed = 2000f, Range = 20000f}},
			{"Ezreal", new UltData {StageType = DamageLib.StageType.Default, ManaCost = 100f, DamageMultiplicator = 0.7f, Width = 160f, Delay = 1000f / 1000f, Speed = 2000f, Range = 20000f}}
		};

		public BaseUlt()
		{
			OnStart();
		}

		private void OnStart()
		{
			Program.Menu.AddSubMenu(new Menu("Modified BaseUlt from Beaving", "BaseUlt"));
			Program.Menu.SubMenu("BaseUlt").AddItem(new MenuItem("showRecalls", "Show Recalls").SetValue(true));
			Program.Menu.SubMenu("BaseUlt").AddItem(new MenuItem("baseUlt", "Base Ult").SetValue(true));
			Program.Menu.SubMenu("BaseUlt").AddItem(new MenuItem("panicKey", "Panic key (hold for disable)").SetValue(new KeyBind(32, KeyBindType.Press))); //32 == space
			Program.Menu.SubMenu("BaseUlt").AddItem(new MenuItem("extraDelay", "Extra Delay").SetValue(new Slider(0, -2000, 2000)));
			var teamUlt = new Menu("Team Baseult Champs", "TeamUlt");
			Program.Menu.SubMenu("BaseUlt").AddSubMenu(teamUlt);
			var champions = ObjectManager.Get<Obj_AI_Hero>();
			_compatibleChamp = true;
			var objAiHeroes = champions as Obj_AI_Hero[] ?? champions.ToArray();
			_ownTeam = objAiHeroes.Where(x => x.IsAlly);
			_enemyTeam = objAiHeroes.Where(x => x.IsEnemy);
			foreach(var champ in _ownTeam.Where(x => !x.IsMe ))
				teamUlt.AddItem(new MenuItem(champ.ChampionName, champ.ChampionName + " friend with Baseult?").SetValue(false).DontSave());
			_enemySpawnPos = ObjectManager.Get<GameObject>().First(x => x.Type == GameObjectType.obj_SpawnPoint && x.Team != ObjectManager.Player.Team).Position;
			_map = Utility.Map.GetMap();
			foreach(var champ in _enemyTeam)
				PlayerInfo.Add(new PlayerInfo(champ));
			PlayerInfo.Add(new PlayerInfo(ObjectManager.Player));
			_ult = new Spell(SpellSlot.R, 20000f);
			Game.OnGameProcessPacket += Game_OnGameProcessPacket;
			Drawing.OnDraw += Drawing_OnDraw;
			if(_compatibleChamp)
				Game.OnGameUpdate += Game_OnGameUpdate;
		}

		static void Game_OnGameUpdate(EventArgs args)
		{
			try
			{
				var time = Environment.TickCount;
				foreach(var playerInfo in PlayerInfo.Where(x => x.Champ.IsVisible))
					playerInfo.LastSeen = time;
				if(!Program.Menu.Item("baseUlt").GetValue<bool>())
					return;
				foreach(var playerInfo in PlayerInfo.Where(x =>
				x.Champ.IsValid &&
				!x.Champ.IsDead &&
				x.Champ.IsEnemy &&
				x.Recall.Status == Packet.S2C.Recall.RecallStatus.RecallStarted).OrderBy(x => x.GetRecallEnd()))
				{
					if(_ultCasted == 0 || Environment.TickCount - _ultCasted > 20000) //DONT change Environment.TickCount; check for draven ult return
						HandleRecallShot(playerInfo);
				}
			}
			catch(Exception e)
			{
				Game.PrintChat(e.ToString());
			}
		}
		static void HandleRecallShot(PlayerInfo playerInfo)
		{
			var shoot = false;
			foreach(var champ in _ownTeam.Where(x => x.IsValid && (x.IsMe || GetSafeMenuItem<bool>(Program.Menu.Item(x.ChampionName))) && !x.IsDead && !x.IsStunned &&
			(x.Spellbook.CanUseSpell(SpellSlot.R) == SpellState.Ready || (x.Spellbook.GetSpell(SpellSlot.R).Level > 0 && x.Spellbook.CanUseSpell(SpellSlot.R) == SpellState.Surpressed && x.Mana >= UltInfo[x.ChampionName].ManaCost)))) //use when fixed: champ.Spellbook.GetSpell(SpellSlot.R) = Ready or champ.Spellbook.GetSpell(SpellSlot.R).ManaCost)
			{
				if(champ.ChampionName != "Ezreal" && !IsCollidingWithChamps(champ.ServerPosition.To2D(), _enemySpawnPos.To2D(), playerInfo.Champ.NetworkId, UltInfo[champ.ChampionName].Width, UltInfo[champ.ChampionName].Delay, UltInfo[champ.ChampionName].Speed * 10000)) //speed*10k because only calc the current positions
					continue;
				var timeneeded = GetSpellTravelTime(champ, UltInfo[champ.ChampionName].Speed, UltInfo[champ.ChampionName].Delay, _enemySpawnPos) - (Program.Menu.Item("extraDelay").GetValue<Slider>().Value + 65); //increase timeneeded if it should arrive earlier, decrease if later
				if(timeneeded - playerInfo.GetRecallCountdown() > 100)
					continue;
				playerInfo.IncomingDamage[champ.NetworkId] = (float)GetUltDamage(playerInfo.Champ) * UltInfo[champ.ChampionName].DamageMultiplicator;
				if(playerInfo.GetRecallCountdown() <= timeneeded && timeneeded - playerInfo.GetRecallCountdown() < 100)
					if(champ.IsMe)
						shoot = true;
			}
			var totalUltDamage = DamageLib.getDmg(playerInfo.Champ,DamageLib.SpellType.R);
			var targetHealth = GetTargetHealth(playerInfo);
			if(!shoot || Program.Menu.Item("panicKey").GetValue<KeyBind>().Active)
				return;
			playerInfo.IncomingDamage.Clear(); //wrong placement?
			var time = Environment.TickCount;
			if(time - playerInfo.LastSeen > 15000)
			{
				if(totalUltDamage < playerInfo.Champ.MaxHealth)
				return;
			}
			else if(totalUltDamage < targetHealth)
				return;
			_ult.Cast(_enemySpawnPos, true);
			_ultCasted = time;
		}
		static void Drawing_OnDraw(EventArgs args)
		{
			if(!Program.Menu.Item("showRecalls").GetValue<bool>())
				return;
			var index = -1;
			foreach(var playerInfo in PlayerInfo.Where(x =>
			(x.Recall.Status == Packet.S2C.Recall.RecallStatus.RecallStarted || x.Recall.Status == Packet.S2C.Recall.RecallStatus.TeleportStart) &&
			x.Champ.IsValid &&
			!x.Champ.IsDead &&
			(x.Champ.IsEnemy)))
			{
				index++;
				Drawing.DrawText(Drawing.Width * 0.73f, Drawing.Height * 0.88f + (index * 15f), Color.Red, playerInfo.ToString());
			}
		}
		static void Game_OnGameProcessPacket(GamePacketEventArgs args)
		{
			if(args.PacketData[0] == Packet.S2C.Recall.Header)
			{
				var newRecall = RecallDecode(args.PacketData);
				var firstOrDefault = PlayerInfo.FirstOrDefault(x => x.Champ.NetworkId == newRecall.UnitNetworkId);
				if (firstOrDefault != null)
				{
					var playerInfo = firstOrDefault.UpdateRecall(newRecall); //Packet.S2C.Recall.Decoded(args.PacketData)
				}
			}
		}

		public static T GetSafeMenuItem<T>(MenuItem item)
		{
			if(item != null)
				return item.GetValue<T>();
			return default(T);
		}
		public static float GetTargetHealth(PlayerInfo playerInfo)
		{
			if(playerInfo.Champ.IsVisible)
				return playerInfo.Champ.Health;
			var predictedhealth = playerInfo.Champ.Health + playerInfo.Champ.HPRegenRate * ((Environment.TickCount - playerInfo.LastSeen + playerInfo.GetRecallCountdown()) / 1000f);
			return predictedhealth > playerInfo.Champ.MaxHealth ? playerInfo.Champ.MaxHealth : predictedhealth;
		}
		public static float GetSpellTravelTime(Obj_AI_Hero source, float speed, float delay, Vector3 targetpos)
		{
			var distance = Vector3.Distance(source.ServerPosition, targetpos);
			var missilespeed = speed;
			return (distance / missilespeed + delay) * 1000;
		}
		public static bool IsCollidingWithChamps(Vector2 frompos, Vector2 targetpos, int targetnetid, float width, float delay, float speed)
		{
			var collide = false;
			foreach(var hero in ObjectManager.Get<Obj_AI_Hero>().Where( hero => hero.IsValidTarget()))
			{
				var newTargetpos = new Vector3(targetpos.X, targetpos.Y, 0);
				var input = new PredictionInput
				{
					Unit = hero,
					Delay = delay,
					Radius = width,
					Speed = speed,
				};
				var collisionobjekt = Collision.GetCollision(new List<Vector3> { newTargetpos }, input);
				if( collisionobjekt.Exists(  obj=> obj.IsEnemy && !obj.IsMinion ))
					collide = true;
			}
			return collide;
		}
		
		public static Packet.S2C.Recall.Struct RecallDecode(byte[] data)
		{
			var reader = new BinaryReader(new MemoryStream(data));
			var recall = new Packet.S2C.Recall.Struct();
			reader.ReadByte(); //PacketId
			reader.ReadInt32();
			recall.UnitNetworkId = reader.ReadInt32();
			reader.ReadBytes(66);
			recall.Status = Packet.S2C.Recall.RecallStatus.Unknown;
			var teleport = false;
			if(BitConverter.ToString(reader.ReadBytes(6)) != "00-00-00-00-00-00")
			{
				if(BitConverter.ToString(reader.ReadBytes(3)) != "00-00-00")
				{
					recall.Status = Packet.S2C.Recall.RecallStatus.TeleportStart;
					teleport = true;
				}
				else
					recall.Status = Packet.S2C.Recall.RecallStatus.RecallStarted;
			}
			reader.Close();
			var champ = ObjectManager.GetUnitByNetworkId<Obj_AI_Hero>(recall.UnitNetworkId);
			recall.Duration = 0;
			if(champ != null)
			{
				if(teleport)
					recall.Duration = 3500;
				else 
				{
					recall.Duration = _map == Utility.Map.MapType.CrystalScar ? 4500 : 8000;
					if(champ.Masteries.Any(x => x.Page == MasteryPage.Utility && x.Id == 65 && x.Points == 1))
						recall.Duration -= _map == Utility.Map.MapType.CrystalScar ? 500 : 1000; 
				}
				var time = Environment.TickCount - Game.Ping;
				if(!RecallT.ContainsKey(recall.UnitNetworkId))
					RecallT.Add(recall.UnitNetworkId, time); 
				else
				{
					if(RecallT[recall.UnitNetworkId] == 0)
						RecallT[recall.UnitNetworkId] = time;
					else
					{
						if(time - RecallT[recall.UnitNetworkId] > recall.Duration - 75)
							recall.Status = teleport ? Packet.S2C.Recall.RecallStatus.TeleportEnd : Packet.S2C.Recall.RecallStatus.RecallFinished;
						else
							recall.Status = teleport ? Packet.S2C.Recall.RecallStatus.TeleportAbort : Packet.S2C.Recall.RecallStatus.RecallAborted;
						RecallT[recall.UnitNetworkId] = 0;
					}
				}
			}
			return recall;
		}

		static double GetUltDamage( Obj_AI_Hero enemy)
		{
			return DamageLib.getDmg(enemy, DamageLib.SpellType.R);
		}
	}
}
