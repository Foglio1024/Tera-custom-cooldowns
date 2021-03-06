﻿using System;
using TCC.Data.Skills;
using TCC.ViewModels.ClassManagers;
using TeraDataLite;
using TeraPacketParser.Messages;

namespace TCC.Data.Abnormalities
{
    public class SorcererAbnormalityTracker : AbnormalityTracker
    {
        private const int ManaBoostId = 500150;
        private const int ManaBoost2Id = 501602;
        private const int ManaBoost2MId = 503061;
        private const int FlameFusionIncreaseId = 502070;   // Equipoise-Flame
        private const int FrostFusionIncreaseId = 502071;   // Equipoise-Frost
        private const int ArcaneFusionIncreaseId = 502072;  // Equipoise-Arcane

        private const int FireIceFusionId = 502020;
        //private const int FireArcaneFusionId = 502030;
        //private const int IceArcaneFusionId = 502040;

        private readonly Skill _fireIceFusion;
        //private static Skill _fireArcaneFusion;
        //private static Skill _iceArcaneFusion;

        public static event Action? BoostChanged;

        private static bool IsManaBoost(uint id)
        {
            return id == ManaBoostId || id == ManaBoost2Id || id == ManaBoost2MId;
        }

        public SorcererAbnormalityTracker()
        {
            Game.DB!.AbnormalityDatabase.Abnormalities.TryGetValue(FireIceFusionId, out var ab);
            _fireIceFusion = new Skill(ab ?? throw new NullReferenceException("Skill not found"), Class.Sorcerer);
        }
        private static void CheckManaBoost(S_ABNORMALITY_BEGIN p)
        {
            if (!IsManaBoost(p.AbnormalityId)) return;
            if (!IsViewModelAvailable<SorcererLayoutVM>(out var vm)) return;

            vm!.ManaBoost.StartEffect(p.Duration);

        }
        private static void CheckManaBoost(S_ABNORMALITY_REFRESH p)
        {
            if (!IsManaBoost(p.AbnormalityId)) return;
            if (!IsViewModelAvailable<SorcererLayoutVM>(out var vm)) return;

            vm!.ManaBoost.RefreshEffect(p.Duration);

        }
        private static void CheckManaBoost(S_ABNORMALITY_END p)
        {
            if (!IsManaBoost(p.AbnormalityId)) return;
            if (!IsViewModelAvailable<SorcererLayoutVM>(out var vm)) return;

            vm!.ManaBoost.StopEffect();
        }

        private static void CheckFusionBoost(S_ABNORMALITY_BEGIN p)
        {
            if (FlameFusionIncreaseId == p.AbnormalityId)
            {
                Game.SetSorcererElementsBoost(true, false, false);
            }
            else if (FrostFusionIncreaseId == p.AbnormalityId)
            {
                Game.SetSorcererElementsBoost(false, true, false);
            }
            else if (ArcaneFusionIncreaseId == p.AbnormalityId)
            {
                Game.SetSorcererElementsBoost(false, false, true);
            }
            else return;
            BoostChanged?.Invoke();
        }
        private static void CheckFusionBoost(S_ABNORMALITY_REFRESH p)
        {
            if (FlameFusionIncreaseId == p.AbnormalityId)
            {
                Game.SetSorcererElementsBoost(true, false, false);
            }
            else if (FrostFusionIncreaseId == p.AbnormalityId)
            {
                Game.SetSorcererElementsBoost(false, true, false);
            }
            else if (ArcaneFusionIncreaseId == p.AbnormalityId)
            {
                Game.SetSorcererElementsBoost(false, false, true);
            }
            else return;
            BoostChanged?.Invoke();

        }
        private static void CheckFusionBoost(S_ABNORMALITY_END p)
        {
            if (FlameFusionIncreaseId == p.AbnormalityId || FrostFusionIncreaseId == p.AbnormalityId ||
                ArcaneFusionIncreaseId == p.AbnormalityId)
            {
                Game.SetSorcererElementsBoost(false, false, false);
            }
            else return;
            BoostChanged?.Invoke();

        }
        private void CheckFusions(S_ABNORMALITY_BEGIN p)
        {
            if (FireIceFusionId == p.AbnormalityId)
            {
                StartPrecooldown(_fireIceFusion, p.Duration);
            }
            //else if (FireArcaneFusionId == p.AbnormalityId)
            //{
            //    StartPrecooldown(_fireArcaneFusion, p.Duration);
            //}
            //else if (IceArcaneFusionId == p.AbnormalityId)
            //{
            //    StartPrecooldown(_iceArcaneFusion, p.Duration);
            //}
        }
        private static void CheckFusions(S_ABNORMALITY_END p)
        {
            if (FireIceFusionId == p.AbnormalityId)
            {
                if (!IsViewModelAvailable<SorcererLayoutVM>(out var vm)) return;

                vm!.EndFireIcePre();
            }
            //else if (FireArcaneFusionId == p.AbnormalityId)
            //{
            //    StartPrecooldown(_fireArcaneFusion, p.Duration);
            //}
            //else if (IceArcaneFusionId == p.AbnormalityId)
            //{
            //    StartPrecooldown(_iceArcaneFusion, p.Duration);
            //}
        }

        public override void CheckAbnormality(S_ABNORMALITY_BEGIN p)
        {
            if (!Game.IsMe(p.TargetId)) return;
            CheckFusions(p);
            CheckManaBoost(p);
            CheckFusionBoost(p);
        }
        public override void CheckAbnormality(S_ABNORMALITY_REFRESH p)
        {
            if (!Game.IsMe(p.TargetId)) return;
            CheckManaBoost(p);
            CheckFusionBoost(p);
        }
        public override void CheckAbnormality(S_ABNORMALITY_END p)
        {
            if (!Game.IsMe(p.TargetId)) return;
            CheckManaBoost(p);
            CheckFusionBoost(p);
            CheckFusions(p);
        }


    }
}
