﻿using TCC.Data.Abnormalities;

namespace TCC.Data
{
    public class ArcherFocusTracker : BaseStackBuffTracker
    {
        public ArcherFocusTracker()
        {
            if (Game.DB.AbnormalityDatabase.Abnormalities.TryGetValue(601400, out var ab))
            {
                Icon = ab.IconName;
            }
        }
        public void StartFocus(long duration)
        {
            base.StartBaseBuff(duration);
        }
        public void SetFocusStacks(int stacks, long duration)
        {
            base.RefreshBaseBuff(stacks, duration);
        }
        public void StartFocusX(long duration)
        {
            base.StartEmpoweredBuff(duration);
        }
        public void StopFocusX()
        {
            base.Stop();
        }
        public void StopFocus()
        {
            base.Stop();
        }
    }

    public class LancerLineHeldTracker : BaseStackBuffTracker
    {
        public LancerLineHeldTracker()
        {
            if (!Game.DB.AbnormalityDatabase.Abnormalities.TryGetValue(LancerAbnormalityTracker.LineHeldId, out var ab)) return;
            Icon = ab.IconName;
            BaseStacksChanged += (stacks) => { if (stacks == 0) Stop(); };
        }
    }
}
