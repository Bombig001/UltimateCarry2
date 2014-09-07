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
    /*
     * LaneClear:
     * - allow AA on Tower, Ward (don't Q wards)
     * - improve in early game (possible?)
     * 
     * AutoIgnite:
     * - add GetComboDamage and ignite earlier if combodamage would suffice
     * 
     * Ult KS:
     * - don't KS anymore if enemy is recalling and would arrive base before ult went through (have to include BaseUlt functionality)
     * 
     * Prediction:
     * - improve Q prediction even more by checking if enemy is melee, then adjust Q width (Obj.IsMelee --> what about champs with melee/ranged forms?)
     * - change dynamix width to a fixed widths at fixed ranges? -> If in <400 range, width = 160, if >400, width = 20
    * */

    class Karthus : Champion
    {
        Menu _menu;

        private Spell _spellQ, _spellW, _spellE, _spellR;

        private const float _spellQWidth = 160f;
        private const float _spellWWidth = 160f;

        private bool _comboE;

        public Karthus() : base()
        {
            _menu = Program.Menu;

            var comboMenu = _menu.AddSubMenu(new Menu("Combo", "Combo"));
            comboMenu.AddItem(new MenuItem("comboKey", "Combo").SetValue(new KeyBind(_menu.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press))); //32 == space
            comboMenu.AddItem(new MenuItem("comboQ", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem("comboW", "Use W").SetValue(true));
            comboMenu.AddItem(new MenuItem("comboE", "Use E").SetValue(true));
            comboMenu.AddItem(new MenuItem("comboAA", "Use AA").SetValue(false));
            comboMenu.AddItem(new MenuItem("comboWPercent", "Use W until Mana %").SetValue(new Slider(10, 0, 100)));
            comboMenu.AddItem(new MenuItem("comboEPercent", "Use E until Mana %").SetValue(new Slider(15, 0, 100)));

            var harassMenu = _menu.AddSubMenu(new Menu("Harass", "Harass"));
            harassMenu.AddItem(new MenuItem("harassKey", "Harass").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));
            harassMenu.AddItem(new MenuItem("harassQ", "Use Q").SetValue(true));
            harassMenu.AddItem(new MenuItem("harassQPercent", "Use Q until Mana %").SetValue(new Slider(15, 0, 100)));

            var farmMenu = _menu.AddSubMenu(new Menu("Farm", "Farm"));
            farmMenu.AddItem(new MenuItem("lastHitKey", "Last Hit").SetValue(new KeyBind(_menu.Item("Farm").GetValue<KeyBind>().Key, KeyBindType.Press)));
            farmMenu.AddItem(new MenuItem("laneClearKey", "Lane Clear").SetValue(new KeyBind(_menu.Item("LaneClear").GetValue<KeyBind>().Key, KeyBindType.Press)));
            farmMenu.AddItem(new MenuItem("farmQ", "Use Q").SetValue(new StringList(new[] { "Last Hit", "Lane Clear", "Both", "No" }, 1)));
            farmMenu.AddItem(new MenuItem("farmE", "Use E in Lane Clear").SetValue(true));
            farmMenu.AddItem(new MenuItem("farmAA", "Use AA in Lane Clear").SetValue(false));
            farmMenu.AddItem(new MenuItem("farmQPercent", "Use Q until Mana %").SetValue(new Slider(10, 0, 100)));
            farmMenu.AddItem(new MenuItem("farmEPercent", "Use E until Mana %").SetValue(new Slider(20, 0, 100)));

            var notifyMenu = _menu.AddSubMenu(new Menu("Notify on R killable enemies", "Notify"));
            notifyMenu.AddItem(new MenuItem("notifyR", "Text Notify").SetValue(true));
            notifyMenu.AddItem(new MenuItem("notifyPing", "Ping Notify").SetValue(false));

            var drawMenu = _menu.AddSubMenu(new Menu("Drawing", "Drawing"));
            drawMenu.AddItem(new MenuItem("drawQ", "Draw Q range").SetValue(new Circle(true, System.Drawing.Color.FromArgb(125, 0, 255, 0))));

            var miscMenu = _menu.AddSubMenu(new Menu("Misc", "Misc"));
            miscMenu.AddItem(new MenuItem("ultKS", "Ultimate KS").SetValue(true));

            _spellQ = new Spell(SpellSlot.Q, 875);
            _spellW = new Spell(SpellSlot.W, 1000);
            _spellE = new Spell(SpellSlot.E, 505);
            _spellR = new Spell(SpellSlot.R, 20000f);

            _spellQ.SetSkillshot(1f, 160, float.MaxValue, false, SkillshotType.SkillshotCircle);
            _spellW.SetSkillshot(.5f, 80, float.MaxValue, false, SkillshotType.SkillshotCircle);
            _spellE.SetSkillshot(1f, 505, float.MaxValue, false, SkillshotType.SkillshotCircle);
            _spellR.SetSkillshot(3f, float.MaxValue, float.MaxValue, false, SkillshotType.SkillshotCircle);

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;
        }

        void Game_OnGameUpdate(EventArgs args)
        {
            UpdateLastSeen();

            if (_menu.Item("comboKey").GetValue<KeyBind>().Active)
            {
                Program.Orbwalker.SetAttacks(_menu.Item("comboAA").GetValue<bool>() || ObjectManager.Player.Mana < 100); //if no mana, allow auto attacks!
                Combo();
            }
            else
            {
                Program.Orbwalker.SetAttacks(true);

                if (_menu.Item("harassKey").GetValue<KeyBind>().Active)
                    Harass();
                else if (_menu.Item("laneClearKey").GetValue<KeyBind>().Active)
                {
                    Program.Orbwalker.SetAttacks(_menu.Item("farmAA").GetValue<bool>() || ObjectManager.Player.Mana < 100); //if no mana, allow auto attacks!
                    LaneClear();
                }
                else if (_menu.Item("lastHitKey").GetValue<KeyBind>().Active)
                    LastHit();
                else
                    RegulateEState();
            }

            if (_menu.Item("ultKS").GetValue<bool>())
                UltKS();
        }

        void UpdateLastSeen()
        {
            int time = Environment.TickCount;

            foreach (EnemyInfo playerInfo in Program.Helper._enemyInfo.Where(x => x.Player.IsVisible))
                playerInfo.LastSeen = time;
        }

        void CastW(Obj_AI_Base target, int minManaPercent = 0)
        {
            if(_spellW.IsReady() && GetManaPercent() >= minManaPercent)
            {
                if (target != null)
                {
                    _spellW.Width = GetDynamicWWidth(target);
                    _spellW.Cast(target, Packets());
                }
            }
        }

        void Combo()
        {
            Obj_AI_Hero target;

            if (_menu.Item("comboW").GetValue<bool>())
                CastW(SimpleTs.GetTarget(_spellW.Range, SimpleTs.DamageType.Magical), _menu.Item("comboWPercent").GetValue<Slider>().Value);

            if (_menu.Item("comboE").GetValue<bool>() && _spellE.IsReady() && !IsInPassiveForm())
            {
                target = SimpleTs.GetTarget(_spellE.Range, SimpleTs.DamageType.Magical);

                if (target != null)
                {
                    bool enoughMana = GetManaPercent() >= _menu.Item("comboEPercent").GetValue<Slider>().Value;

                    if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).ToggleState == 1)
                    {
                        if (ObjectManager.Player.Distance(target.ServerPosition) <= _spellE.Range && enoughMana)
                        {
                            _comboE = true;
                            _spellE.Cast(ObjectManager.Player.Position, Packets());
                        }
                    }
                    else if (!enoughMana)
                        RegulateEState(true);
                }
                else
                    RegulateEState();
            }

            if (_menu.Item("comboQ").GetValue<bool>() && _spellQ.IsReady())
                CastQ(SimpleTs.GetTarget(_spellQ.Range, SimpleTs.DamageType.Magical));
        }

        void CastQ(Obj_AI_Base target, int minManaPercent = 0)
        {
            if(_spellQ.IsReady() && GetManaPercent() >= minManaPercent)
            {
                if (target != null)
                {
                    _spellQ.Width = GetDynamicQWidth(target);
                    _spellQ.CastIfHitchanceEquals(target, HitChance.High, Packets());
                }
            }
        }

        void CastQ(Vector2 pos, int minManaPercent = 0)
        {
            if(_spellQ.IsReady())
                if(GetManaPercent() >= minManaPercent)
                    _spellQ.Cast(pos, Packets());
        }

        float GetDynamicWWidth(Obj_AI_Base target)
        {
            return Math.Max(70, (1f - (ObjectManager.Player.Distance(target) / _spellW.Range)) * _spellWWidth);
        }

        float GetDynamicQWidth(Obj_AI_Base target)
        {
            return Math.Max(25, (1f - (ObjectManager.Player.Distance(target) / _spellQ.Range)) * _spellQWidth);
        }

        void Harass()
        {
            if (_menu.Item("harassQ").GetValue<bool>())
                CastQ(SimpleTs.GetTarget(_spellQ.Range, SimpleTs.DamageType.Magical), _menu.Item("harassQPercent").GetValue<Slider>().Value);
        }

        void LaneClear()
        {
            bool farmQ = _menu.Item("farmQ").GetValue<StringList>().SelectedIndex == 1 || _menu.Item("farmQ").GetValue<StringList>().SelectedIndex == 2;
            bool farmE = _menu.Item("farmE").GetValue<bool>();

            List<Obj_AI_Base> minions;

            if (farmQ && _spellQ.IsReady())
            {
                minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _spellQ.Range, MinionTypes.All, MinionTeam.NotAlly);
                bool jungleMobs = minions.Any(x => x.Team == GameObjectTeam.Neutral); //FirstDamage = multitarget hit, differentiate! (check radius around mob pos)

                _spellQ.Width = _spellQWidth;
                MinionManager.FarmLocation farmInfo = _spellQ.GetCircularFarmLocation(minions, _spellQ.Width);

                if (farmInfo.MinionsHit >= 1)
                    CastQ(farmInfo.Position, jungleMobs ? 0 : _menu.Item("farmQPercent").GetValue<Slider>().Value);
            }

            if (farmE && _spellE.IsReady() && !IsInPassiveForm())
            {
                _comboE = false;

                minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _spellE.Range, MinionTypes.All, MinionTeam.NotAlly);

                bool jungleMobs = minions.Any(x => x.Team == GameObjectTeam.Neutral); //FirstDamage = multitarget hit, differentiate! (check radius around mob pos)

                bool enoughMana = GetManaPercent() > _menu.Item("farmEPercent").GetValue<Slider>().Value;

                if (enoughMana && ((minions.Count >= 3 || jungleMobs) && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).ToggleState == 1))
                    _spellE.Cast(ObjectManager.Player.Position, Packets());
                else if (!enoughMana || ((minions.Count <= 2 && !jungleMobs) && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).ToggleState == 2))
                    RegulateEState(!enoughMana);
            }
        }

        void LastHit()
        {
            bool farmQ = _menu.Item("farmQ").GetValue<StringList>().SelectedIndex == 0 || _menu.Item("farmQ").GetValue<StringList>().SelectedIndex == 2;

            if(farmQ && _spellQ.IsReady())
            {
                var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _spellQ.Range, MinionTypes.All, MinionTeam.NotAlly);

                foreach (var minion in minions.Where(x => DamageLib.getDmg(x, DamageLib.SpellType.Q, DamageLib.StageType.FirstDamage) >= //FirstDamage = multitarget hit, differentiate! (check radius around mob predicted pos)
                    HealthPrediction.GetHealthPrediction(x, (int)(_spellQ.Delay * 1000))))
                {
                    CastQ(minion, _menu.Item("farmQPercent").GetValue<Slider>().Value);
                }
            }
        }

        void RegulateEState(bool ignoreTargetChecks = false)
        {
            if (_spellE.IsReady() && !IsInPassiveForm() && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).ToggleState == 2)
            {
                Obj_AI_Hero target = SimpleTs.GetTarget(_spellE.Range, SimpleTs.DamageType.Magical);
                var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _spellE.Range, MinionTypes.All, MinionTeam.NotAlly);

                if (ignoreTargetChecks || (target == null && (_comboE || minions.Count == 0)))
                {
                    _spellE.Cast(ObjectManager.Player.Position, Packets());
                    _comboE = false;
                }
            }
        }

        void UltKS()
        {
            if (_spellR.IsReady())
            {
                int time = Environment.TickCount;

                foreach (EnemyInfo target in Program.Helper._enemyInfo.Where(x =>
                    x.Player.IsValid &&
                    !x.Player.IsDead &&
                    x.Player.IsEnemy &&
                    ((!x.Player.IsVisible && time - x.LastSeen < 10000) || (x.Player.IsVisible && Utility.IsValidTarget(x.Player))) &&
                    DamageLib.getDmg(x.Player, DamageLib.SpellType.R) >= Program.Helper.GetTargetHealth(x, (int)(_spellR.Delay * 1000f))))
                {
                    bool cast = true;

                    if (target.Player.IsVisible || (!target.Player.IsVisible && time - target.LastSeen < 2750)) //allies still attacking target? prevent overkill
                        if (Program.Helper._ownTeam.Any(x => !x.IsMe && x.Distance(target.Player) < 1800))
                            cast = false;

                    if (cast && !Program.Helper._enemyTeam.Any(x => x.IsValid && !x.IsDead && (x.IsVisible || (!x.IsVisible && time - Program.Helper.GetPlayerInfo(x).LastSeen < 2750)) && ObjectManager.Player.Distance(x) < 1800)) //any other enemies around? dont ult
                        _spellR.Cast(ObjectManager.Player.Position, Packets());
                }
            }
        }

        bool IsInPassiveForm()
        {
            return !ObjectManager.Player.IsHPBarRendered;
        }

        void Drawing_OnDraw(EventArgs args)
        {
            if (!ObjectManager.Player.IsDead)
            {
                var drawQ = _menu.Item("drawQ").GetValue<Circle>();

                if (drawQ.Active)
                    Utility.DrawCircle(ObjectManager.Player.Position, _spellQ.Range, drawQ.Color);
            }

            String victims = "";

            int time = Environment.TickCount;

            foreach (EnemyInfo target in Program.Helper._enemyInfo.Where(x =>
                x.Player.IsValid &&
                !x.Player.IsDead &&
                x.Player.IsEnemy &&
                ((!x.Player.IsVisible && time - x.LastSeen < 10000) || (x.Player.IsVisible && Utility.IsValidTarget(x.Player))) &&
                DamageLib.getDmg(x.Player, DamageLib.SpellType.R) >= Program.Helper.GetTargetHealth(x, (int)(_spellR.Delay * 1000f))))
            {
                victims += target.Player.ChampionName + " ";

                if (_menu.Item("notifyPing").GetValue<bool>() && (target.LastPinged == 0 || Environment.TickCount - target.LastPinged > 11000))
                {
                    if (ObjectManager.Player.Distance(target.Player) > 1800 && (target.Player.IsVisible || (!target.Player.IsVisible && time - target.LastSeen > 2750)))
                    {
                        Program.Helper.Ping(target.Player.Position);
                        target.LastPinged = Environment.TickCount;
                    }
                }
            }

            if (victims != "" && _menu.Item("notifyR").GetValue<bool>())
            {
                Drawing.DrawText(Drawing.Width * 0.44f, Drawing.Height * 0.7f, System.Drawing.Color.GreenYellow, "Ult can kill: " + victims);

                //use when pos works
                //new Render.Text((int)(Drawing.Width * 0.44f), (int)(Drawing.Height * 0.7f), "Ult can kill: " + victims, 30, SharpDX.Color.Red); //.Add()
            }
        }
    }
}
