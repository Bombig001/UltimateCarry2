using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace UltimateCarry
{
    class EnemyInfo
    {
        public Obj_AI_Hero Player;
        public int LastSeen;
        public int LastPinged;

        public EnemyInfo(Obj_AI_Hero player)
        {
            Player = player;
        }
    }

    class Helper
    {
        public IEnumerable<Obj_AI_Hero> _enemyTeam;
        public IEnumerable<Obj_AI_Hero> _ownTeam;
        public List<EnemyInfo> _enemyInfo = new List<EnemyInfo>();

        public Helper()
        {
            List<Obj_AI_Hero> champions = ObjectManager.Get<Obj_AI_Hero>().ToList();

            _ownTeam = champions.Where(x => x.IsAlly);
            _enemyTeam = champions.Where(x => x.IsEnemy);

            _enemyInfo = _enemyTeam.Select(x => new EnemyInfo(x)).ToList();

            Game.OnGameUpdate += Game_OnGameUpdate;
        }

        void Game_OnGameUpdate(EventArgs args)
        {
            var time = Environment.TickCount;

            foreach (EnemyInfo enemyInfo in _enemyInfo.Where(x => x.Player.IsVisible))
                enemyInfo.LastSeen = time;
        }

        public EnemyInfo GetPlayerInfo(Obj_AI_Hero enemy)
        {
            return Program.Helper._enemyInfo.Find(x => x.Player.NetworkId == enemy.NetworkId);
        }

        public float GetTargetHealth(EnemyInfo playerInfo, int additionalTime)
        {
            if (playerInfo.Player.IsVisible)
                return playerInfo.Player.Health;

            float predictedhealth = playerInfo.Player.Health + playerInfo.Player.HPRegenRate * ((Environment.TickCount - playerInfo.LastSeen + additionalTime) / 1000f);

            return predictedhealth > playerInfo.Player.MaxHealth ? playerInfo.Player.MaxHealth : predictedhealth;
        }

        public void Ping(Vector3 pos)
        {
            Packet.S2C.Ping.Encoded(new Packet.S2C.Ping.Struct(pos.X, pos.Y, 0, 0, Packet.PingType.NormalSound)).Process();
        }
    }
}
