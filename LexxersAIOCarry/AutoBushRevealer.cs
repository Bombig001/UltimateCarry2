using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace UltimateCarry
{
    class PlayerInfo
    {
        public Obj_AI_Hero Player;
        public int LastSeen;

        public PlayerInfo(Obj_AI_Hero player)
        {
            Player = player;
        }
    }

    class AutoBushRevealer
    {
        static int lastTimeWarded;
        private static List<PlayerInfo> _playerInfo = new List<PlayerInfo>();
        static Menu _menu;

        public AutoBushRevealer()
        {
            _menu = Program.Menu.AddSubMenu(new Menu("Auto Bush Revealer", "AutoBushRevealer"));
            _menu.AddItem(new MenuItem("AutoBushEnabled", "Enabled").SetValue<bool>(true));
            _menu.AddItem(new MenuItem("AutoBushKey", "Key").SetValue(new KeyBind(Program.Menu.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press))); //32 == space

            _playerInfo = ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy).Select(x => new PlayerInfo(x)).ToList();

            Game.OnGameUpdate += Game_OnGameUpdate;
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            int time = Environment.TickCount;

            foreach (PlayerInfo playerInfo in _playerInfo.Where(x => x.Player.IsVisible))
                playerInfo.LastSeen = time;

            if (_menu.Item("comboKey").GetValue<KeyBind>().Active)
            {
                foreach (Obj_AI_Hero enemy in _playerInfo.Where(x =>
                    x.Player.IsValid &&
                    !x.Player.IsVisible &&
                    !x.Player.IsDead &&
                    x.Player.Distance(ObjectManager.Player.ServerPosition) < 1000 &&
                    time - x.LastSeen < 2500).Select(x => x.Player))
                {
                    Vector3 bestWardPos = GetWardPos(enemy.ServerPosition, 165, 2);

                    if (bestWardPos != null && bestWardPos != enemy.ServerPosition && bestWardPos != Vector3.Zero && bestWardPos.Distance(ObjectManager.Player.ServerPosition) <= 600)
                    {
                        if (lastTimeWarded == 0 || Environment.TickCount - lastTimeWarded > 500)
                        {
                            InventorySlot wardSlot = LeagueSharp.Common.Items.GetWardSlot();

                            if (wardSlot != null && wardSlot.Id != ItemId.Unknown)
                            {
                                wardSlot.UseItem(bestWardPos);
                                lastTimeWarded = Environment.TickCount;
                            }
                        }
                    }
                }
            }
        }

        static Vector3 GetWardPos(Vector3 lastPos, int radius = 165, int precision = 3)
        {
            int count = precision;

            while (count > 0)
            {
                int vertices = radius;

                WardLocation[] wardLocations = new WardLocation[vertices];
                double angle = 2 * Math.PI / vertices;

                for (int i = 0; i < vertices; i++)
                {
                    double th = angle * i;
                    Vector3 pos = new Vector3((float)(lastPos.X + radius * Math.Cos(th)), (float)(lastPos.Y + radius * Math.Sin(th)), 0);
                    wardLocations[i] = new WardLocation(pos, NavMesh.IsWallOfGrass(pos));
                }

                List<GrassLocation> grassLocations = new List<GrassLocation>();

                for (int i = 0; i < wardLocations.Length; i++)
                {
                    if (wardLocations[i].Grass)
                    {
                        if (i != 0 && wardLocations[i - 1].Grass)
                            grassLocations.Last().Count++;
                        else
                            grassLocations.Add(new GrassLocation(i, 1));
                    }
                }

                GrassLocation grassLocation = grassLocations.OrderByDescending(x => x.Count).FirstOrDefault();

                if (grassLocation != null) //else: no pos found. increase/decrease radius?
                {
                    int midelement = (int)Math.Ceiling((float)grassLocation.Count / 2f);
                    lastPos = wardLocations[grassLocation.Index + midelement - 1].Pos;
                    radius = (int)Math.Floor((float)radius / 2f);
                }

                count--;
            }

            return lastPos;
        }

        class WardLocation
        {
            public Vector3 Pos;
            public bool Grass;

            public WardLocation(Vector3 pos, bool grass)
            {
                Pos = pos;
                Grass = grass;
            }
        }

        class GrassLocation
        {
            public int Index;
            public int Count;

            public GrassLocation(int index, int count)
            {
                Index = index;
                Count = count;
            }
        }
    }
}
