﻿using System.ComponentModel;
using TCC.Data.Pc;
using TCC.Settings.WindowSettings;
using TCC.Utils;
using TeraDataLite;
using TeraPacketParser.Analysis;
using TeraPacketParser.Messages;

namespace TCC.ViewModels.Widgets
{
    //TODO: remove references to other modules?
    [TccModule]
    public class CharacterWindowViewModel : TccWindowViewModel
    {
        public Player Player => Game.Me;
        public bool ShowRe =>
            (Player.Class == Class.Brawler || Player.Class == Class.Gunner ||
             Player.Class == Class.Ninja || Player.Class == Class.Valkyrie) &&
            ((CharacterWindowSettings)Settings!).ShowStamina;
        public bool ShowElements => Player.Class == Class.Sorcerer &&
                                    ((CharacterWindowSettings)Settings!).SorcererShowElements;

        public bool ShowEdge => Player.Class == Class.Warrior &&
                                ((CharacterWindowSettings) Settings!).WarriorShowEdge;

        public CharacterWindowViewModel(CharacterWindowSettings settings) : base(settings)
        {
            Game.Me.PropertyChanged += MePropertyChanged;
            settings.SorcererShowElementsChanged += () => N(nameof(ShowElements));
            settings.WarriorShowEdgeChanged += () => N(nameof(ShowEdge));
            settings.ShowStaminaChanged += () => N(nameof(ShowRe));
        }

        protected override void InstallHooks()
        {
            PacketAnalyzer.Processor.Hook<S_PLAYER_STAT_UPDATE>(OnPlayerStatUpdate);
            PacketAnalyzer.Processor.Hook<S_CREATURE_LIFE>(OnCreatureLife);
            PacketAnalyzer.Processor.Hook<S_ABNORMALITY_DAMAGE_ABSORB>(OnAbnormalityDamageAbsorb);
            PacketAnalyzer.Processor.Hook<S_CREATURE_CHANGE_HP>(OnCreatureChangeHp);
            PacketAnalyzer.Processor.Hook<S_PLAYER_CHANGE_MP>(OnPlayerChangeMp);
            PacketAnalyzer.Processor.Hook<S_PLAYER_CHANGE_STAMINA>(OnPlayerChangeStamina);
        }

        protected override void RemoveHooks()
        {
            PacketAnalyzer.Processor.Unhook<S_PLAYER_STAT_UPDATE>(OnPlayerStatUpdate);
            PacketAnalyzer.Processor.Unhook<S_CREATURE_LIFE>(OnCreatureLife);
            PacketAnalyzer.Processor.Unhook<S_ABNORMALITY_DAMAGE_ABSORB>(OnAbnormalityDamageAbsorb);
            PacketAnalyzer.Processor.Unhook<S_CREATURE_CHANGE_HP>(OnCreatureChangeHp);
            PacketAnalyzer.Processor.Unhook<S_PLAYER_CHANGE_MP>(OnPlayerChangeMp);
            PacketAnalyzer.Processor.Unhook<S_PLAYER_CHANGE_STAMINA>(OnPlayerChangeStamina);
        }

        private void OnPlayerChangeStamina(S_PLAYER_CHANGE_STAMINA m)
        {
            Player.MaxST = m.MaxST + m.BonusST;
            Player.CurrentST = m.CurrentST;
        }
        private void OnPlayerChangeMp(S_PLAYER_CHANGE_MP m)
        {
            if (!Game.IsMe(m.Target)) return;
            Player.MaxMP = m.MaxMP;
            Player.CurrentMP = m.CurrentMP;
        }
        private void OnCreatureChangeHp(S_CREATURE_CHANGE_HP m)
        {
            if (!Game.IsMe(m.Target)) return;
            Player.MaxHP = m.MaxHP;
            Player.CurrentHP = m.CurrentHP;
        }
        private void OnAbnormalityDamageAbsorb(S_ABNORMALITY_DAMAGE_ABSORB p)
        {
            // todo: add chat message too
            if (!Game.IsMe(p.Target) || Player.CurrentShield < 0) return;
            Player.DamageShield(p.Damage);
        }

        private void OnCreatureLife(S_CREATURE_LIFE m)
        {
            if (!Game.IsMe(m.Target)) return;
            Player.IsAlive = m.Alive;
        }

        // TODO: move these to Game?
        private void OnPlayerStatUpdate(S_PLAYER_STAT_UPDATE m)
        {
            Player.ItemLevel = m.Ilvl;
            Player.Level = m.Level;
            Player.CritFactor = m.TotalCritFactor;
            Player.MaxHP = m.MaxHP;
            Player.MaxMP = m.MaxMP;
            Player.MaxST = m.MaxST + m.BonusST;
            Player.CurrentHP = m.CurrentHP;
            Player.CurrentMP = m.CurrentMP;
            Player.CurrentST = m.CurrentST;
            Player.MagicalResistance = m.TotalMagicalResistance;

            // moved to Game
            //if (Player.Class != Class.Sorcerer) return;
            //Player.Fire = m.Fire;
            //Player.Ice = m.Ice;
            //Player.Arcane = m.Arcane;
        }

        private void MePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            N(e.PropertyName);
            if (e.PropertyName != nameof(Data.Pc.Player.Class)) return;
            N(nameof(ShowRe));
            N(nameof(ShowEdge));
            N(nameof(ShowElements));
        }
    }
}