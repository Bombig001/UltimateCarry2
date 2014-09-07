using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;

namespace UltimateCarry
{
    class Riven : Champion
    {
        public Spell Q;
        public Spell W;
        public Spell E;
        public Spell R;
        public Spell Rstart;
        public Obj_AI_Hero Player = ObjectManager.Player;

        public int StackPassive = 0;
        public int QStage = 0;
        public int QDelay = 300;
        public int QTick = 0;
        public int RDelay = 16000;
        public int RTick = 0;
        public Riven()
        {
            LoadMenu();
            LoadSpells();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;
            Obj_AI_Base.OnPlayAnimation += OnAnimation;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
			Chat.Print(ObjectManager.Player.ChampionName + " Plugin Loaded!");
        }

        private void LoadMenu()
        {
            Program.Menu.AddSubMenu(new Menu("TeamFight", "TeamFight"));
            Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useQ_TeamFight", "Use Q").SetValue(true));
            Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useW_TeamFight", "Use W").SetValue(true));
            Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useE_TeamFight", "Use E").SetValue(true));
            Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useR_TeamFight", "Use R").SetValue(true));

            Program.Menu.AddSubMenu(new Menu("Harass", "Harass"));
            Program.Menu.SubMenu("Harass").AddItem(new MenuItem("useQ_Harass", "Use Q").SetValue(true));
            Program.Menu.SubMenu("Harass").AddItem(new MenuItem("useW_Harass", "Use W").SetValue(true));

            Program.Menu.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Program.Menu.SubMenu("LaneClear").AddItem(new MenuItem("useQ_LaneClear", "Use Q").SetValue(true));
            Program.Menu.SubMenu("LaneClear").AddItem(new MenuItem("useW_LaneClear", "Use W").SetValue(true));

            Program.Menu.AddSubMenu(new Menu("Passive", "Passive"));
            Program.Menu.SubMenu("Passive").AddItem(new MenuItem("CancleQAnimation", "Cancle Q Animation").SetValue(true));
			Program.Menu.SubMenu("Passive").AddItem(new MenuItem("QLaugh", "ROFL Combo").SetValue(true));

			Program.Menu.AddSubMenu(new Menu("Drawing", "Drawing"));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_Disabled", "Disable All").SetValue(false));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_Q", "Draw Q").SetValue(true));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_W", "Draw W").SetValue(true));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_E", "Draw E").SetValue(true));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_R", "Draw R").SetValue(true));
        }

        private void LoadSpells()
        {

            Q = new Spell(SpellSlot.Q, 275);
            Q.SetSkillshot(0.1f, 112.5f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            W = new Spell(SpellSlot.W, 260);

            E = new Spell(SpellSlot.E, 400);
            E.SetSkillshot(0.1f, Orbwalking.GetRealAutoAttackRange(Player), float.MaxValue, false, SkillshotType.SkillshotCircle);

            Rstart = new Spell(SpellSlot.R, 900);

            R = new Spell(SpellSlot.R, 900);
            R.SetSkillshot(0.25f, 300f, 1200, false, SkillshotType.SkillshotCone);
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

			if(Program.Menu.Item("Draw_R").GetValue<bool>())
				if(R.Level > 0)
					Utility.DrawCircle(ObjectManager.Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);
		}

        private void Game_OnGameUpdate(EventArgs args)
        {
            var firstOrDefault = Player.Buffs.FirstOrDefault(buff => buff.Name == "rivenpassiveaaboost");
            StackPassive = firstOrDefault != null ? firstOrDefault.Count : 0;
            if (Environment.TickCount - QTick >= QDelay)
            {
                firstOrDefault =
                    Player.Buffs.FirstOrDefault(
                        buff =>
                            buff.Name == "riventricleavesoundone" || buff.Name == "riventricleavesoundtwo" ||
                            buff.Name == "riventricleavesoundthree");
                if (firstOrDefault == null)
                    QStage = 0;
            }

            Q.Width = GetQRadius();

            //if(StackPassive == 3)
            //	return;

            switch (Program.Orbwalker.ActiveMode)
            {

                case Orbwalking.OrbwalkingMode.Combo:
                    if (Program.Menu.Item("useR_TeamFight").GetValue<bool>() && StackPassive != 3)
                        CastR();
                    if (Program.Menu.Item("useQ_TeamFight").GetValue<bool>() && (QStage == 2 || QStage == 0) && StackPassive != 3)
                        Cast_BasicCircleSkillshot_Enemy(Q);
                    if (Program.Menu.Item("useW_TeamFight").GetValue<bool>() && StackPassive != 3)
                        Cast_IfEnemys_inRange(W);
                    if (Program.Menu.Item("useE_TeamFight").GetValue<bool>() && StackPassive != 3)
						CastE();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    if (Program.Menu.Item("useQ_Harass").GetValue<bool>() && QStage == 2 && StackPassive != 3)
                        Cast_BasicCircleSkillshot_Enemy(Q);
                    if (Program.Menu.Item("useW_Harass").GetValue<bool>() && StackPassive != 3)
                        Cast_IfEnemys_inRange(W);
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    if (Program.Menu.Item("useQ_LaneClear").GetValue<bool>() && QStage == 2 && StackPassive != 3)
                        Cast_BasicCircleSkillshot_AOE_Farm(Q);
                    if (Program.Menu.Item("useW_LaneClear").GetValue<bool>() && StackPassive != 3)
                        Cast_W_Farm();
                    break;
            }
        }

        private void Cast_W_Farm()
        {
            if (!W.IsReady())
                return;
            var allminions = MinionManager.GetMinions(Player.Position, W.Range, MinionTypes.All, MinionTeam.NotAlly);
            if (allminions.Count >= 1)
                W.Cast();
        }

        private void Orbwalking_AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            if (unit.IsMe)
            {
                if (StackPassive == 2 || (QStage < 2 && Q.IsReady()))
                {
                    switch (Program.Orbwalker.ActiveMode)
                    {

                        case Orbwalking.OrbwalkingMode.Combo:
                            if (Program.Menu.Item("useR_TeamFight").GetValue<bool>() && StackPassive != 3)
                                CastR();
                            if (Program.Menu.Item("useQ_TeamFight").GetValue<bool>() && Environment.TickCount - QTick >= QDelay &&
                                StackPassive != 3)
                            {
                                QTick = Environment.TickCount;
                                Cast_BasicCircleSkillshot_Enemy(Q);
                            }
                            if (Program.Menu.Item("useW_TeamFight").GetValue<bool>() && StackPassive != 3)
                                Cast_IfEnemys_inRange(W);
                            if (Program.Menu.Item("useE_TeamFight").GetValue<bool>() && StackPassive != 3)
                               CastE();
                            break;
                        case Orbwalking.OrbwalkingMode.Mixed:
                            if (Program.Menu.Item("useQ_Harass").GetValue<bool>() && Environment.TickCount - QTick >= QDelay &&
                                StackPassive != 3)
                                Cast_BasicCircleSkillshot_Enemy(Q);
                            if (Program.Menu.Item("useW_Harass").GetValue<bool>() && StackPassive != 3)
                                Cast_IfEnemys_inRange(W);
                            break;
                        case Orbwalking.OrbwalkingMode.LaneClear:
                            if (Program.Menu.Item("useQ_LaneClear").GetValue<bool>() && Environment.TickCount - QTick >= QDelay &&
                                StackPassive != 3)
                                Cast_BasicCircleSkillshot_AOE_Farm(Q);
                            if (Program.Menu.Item("useW_LaneClear").GetValue<bool>() && StackPassive != 3)
                                Cast_W_Farm();
                            break;
                    }

                }
            }
        }

        private  void OnAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (!sender.IsMe)
                return;
            if (args.Animation == "Spell1a")
            {
                QStage = 1;
				if(Program.Menu.Item("QLaugh").GetValue<bool>())
					Game.Say("/laugh");
                if (Program.Menu.Item("CancleQAnimation").GetValue<bool>())
                    Game.Say("/l");
            }

            if (args.Animation == "Spell1b")
            {
                QStage = 2;
				if(Program.Menu.Item("QLaugh").GetValue<bool>())
					Game.Say("/laugh");
                if (Program.Menu.Item("CancleQAnimation").GetValue<bool>())
                    Game.Say("/l");
            }

            if (args.Animation == "Spell1c")
            {
                QStage = 0;
				if(Program.Menu.Item("QLaugh").GetValue<bool>())
					Game.Say("/laugh");
                if (Program.Menu.Item("CancleQAnimation").GetValue<bool>())
                    Game.Say("/l");
            }
        }

        private float GetQRadius()
        {
            var firstOrDefault = Player.Buffs.FirstOrDefault(buff => buff.Name == "RivenFengShuiEngine");
            if (firstOrDefault == null)
            {
                if (QStage == 0 || QStage == 1)
                    return 162.5f;
                return 200;
            }
            if (QStage == 0 || QStage == 1)
                return 112.5f;
            return 150;
        }

        private void CastR()
        {
            if (!R.IsReady())
                return;
            var firstOrDefault = Player.Buffs.FirstOrDefault(buff => buff.Name == "RivenFengShuiEngine");
            if (firstOrDefault == null)
            {
                if (Cast_IfEnemys_inRange(R, 1, -900 + Orbwalking.GetRealAutoAttackRange(Player) + 75))
                    RTick = Environment.TickCount;
            }
            else
            {
                if (Environment.TickCount - RTick > RDelay || Environment.TickCount - RTick >= RDelay)
                    return;
                var target = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Physical);
                if (target == null)
                    return;
                if (!target.IsValidTarget(R.Range) || R.GetPrediction(target).Hitchance < HitChance.High)
                    return;
                if (DamageLib.getDmg(target, DamageLib.SpellType.R) >= target.Health || Environment.TickCount - RTick >= 6000 || !target.IsValidTarget(R.Range - 400))
                    R.Cast(target, Packets());
            }
        }

        private void CastE()
        {
            if (!E.IsReady())
                return;
            var target = SimpleTs.GetTarget(E.Range + Orbwalking.GetRealAutoAttackRange(Player), SimpleTs.DamageType.Physical);
            if (!target.IsValidTarget(E.Range + Orbwalking.GetRealAutoAttackRange(Player)))
                return;
            E.Cast(target.Position, Packets());
        }
    }
}
