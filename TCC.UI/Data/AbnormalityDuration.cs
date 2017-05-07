﻿using System;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Threading;
using TCC.Data;
using TCC.ViewModels;

namespace TCC
{
    public class AbnormalityDuration : TSPropertyChanged, IDisposable 
    {
        public ulong Target { get; set; }
        public Abnormality Abnormality { get; set; }
        private int _duration;
        public int Duration
        {
            get { return _duration; }
            set
            {
                if (value == _duration) return;
                _duration = value;
                NotifyPropertyChanged("Duration");
            }
        }
        private int _stacks;
        public int Stacks
        {
            get { return _stacks; }
            set
            {
                if (value == _stacks) return;
                _stacks = value;
                NotifyPropertyChanged("Stacks");
            }
        }
        private readonly System.Timers.Timer timer;
        private int _durationLeft;
        public int DurationLeft
        {
            get { return _durationLeft; }
            set
            {
                if (value == _durationLeft) return;
                _durationLeft = value;
                NotifyPropertyChanged("DurationLeft");
            }
        }
        public AbnormalityDuration(Abnormality b, int d, int s, ulong t, Dispatcher disp)
        {
            _dispatcher = disp;
            Abnormality = b;
            Duration = d;
            Stacks = s;
            Target = t;

            DurationLeft = d;
            timer = new System.Timers.Timer(1000);
            timer.Elapsed += (se, ev) => DurationLeft = DurationLeft - 1000;
            if (!Abnormality.Infinity)
            {
                timer.Start();
            }
        }
        public void Refresh()
        {
            timer.Stop();
            if (Duration != 0) timer.Start();
            NotifyPropertyChanged("Refresh");
        }

        public void Dispose()
        {
            timer.Stop();
            timer.Dispose();
        }
    }

}
