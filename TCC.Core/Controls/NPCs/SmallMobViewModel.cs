﻿using System.ComponentModel;
using TCC.Data.NPCs;

namespace TCC.Controls.NPCs
{
    public class SmallMobViewModel : NpcViewModel
    {

        public bool Compact => WindowManager.BossWindow.VM.IsCompact;



        public SmallMobViewModel(NPC npc) : base(npc)
        {
            NPC = npc;

            WindowManager.BossWindow.VM.NpcListChanged += () => N(nameof(Compact));

            NPC.PropertyChanged += OnPropertyChanged;
            NPC.DeleteEvent += () =>
            {
                WindowManager.BossWindow.VM.RemoveMe(NPC, Delay);
                DeleteTimer.Start();
            };


        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(NPC.CurrentHP):
                    InvokeHpChanged();
                    break;
            }
        }

    }
}
