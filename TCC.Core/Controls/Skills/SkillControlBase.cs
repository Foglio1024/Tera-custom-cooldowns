﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using TCC.Annotations;
using TCC.Data;
using TCC.Data.Skills;

namespace TCC.Controls.Skills
{
    public class SkillControlBase : UserControl, INotifyPropertyChanged
    {
        #region INPC
        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        protected virtual void NPC([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
        protected Cooldown Context;
        protected Arc MainArcRef;
        protected Arc PreArcRef;
        protected FrameworkElement ResetArcRef;
        private readonly DoubleAnimation _arcAnimation;

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (_isRunning == value) return;
                _isRunning = value;
                NPC();
            }
        }
        //public string SecondsText => Context == null ? "0" : Utils.TimeFormatter(Convert.ToUInt32(Context.Seconds > uint.MaxValue ? 0 : Context.Seconds));
        public string SecondsText => Context == null ? "0" : Utils.TimeFormatterDurationLeft(Convert.ToUInt32(Context.DurationLeft > uint.MaxValue ? 0 : Context.DurationLeft));  // by HQ

        public SkillControlBase()
        {
            _arcAnimation = new DoubleAnimation(359.9, 0, TimeSpan.FromMilliseconds(1));
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }
        protected virtual void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this) || !(DataContext is Cooldown)) return;
            Context = (Cooldown)DataContext;
            OnSecondsUpdated();
            if (!Context.IsAvailable)
            {
                OnCooldownStarted(Context.Mode);
            }
            Context.Ended += OnCooldownEnded;
            Context.Started += OnCooldownStarted;
            //Context.SecondsUpdated += OnSecondsUpdated;
            Context.DurationLeftUpdated += OnDurationLeftUpdated;    // by HQ
        }
        protected virtual void OnUnloaded(object sender, RoutedEventArgs e) //TODO: maybe use DataContextChanged
        {
            if (Context == null)
            {
                Log.All($"[SkillControlBase.OnUnloaded] Context is null!");
                return;
            }
            Context.Ended -= OnCooldownEnded;
            Context.Started -= OnCooldownStarted;
            //Context.SecondsUpdated -= OnSecondsUpdated;
            Context.DurationLeftUpdated -= OnDurationLeftUpdated;    // by HQ
        }
        private void OnSecondsUpdated()
        {
            NPC(nameof(SecondsText));
        }
        private void OnDurationLeftUpdated()    // by HQ
        {
            NPC(nameof(SecondsText));
        }
        protected virtual void OnCooldownStarted(CooldownMode mode)
        {
            IsRunning = true;
            switch (mode)
            {
                case CooldownMode.Normal:
                    StopArcAnimation(PreArcRef);
                    var newVal = Context.Duration / (double)Context.OriginalDuration;
                    newVal = newVal > 1 ? 1 : newVal;
                    //if (Context.Duration == 0) newVal = 0; //TODO: check this
                    StartArcAnimation(MainArcRef, newVal);
                    break;
                case CooldownMode.Pre:
                    StartArcAnimation(PreArcRef);
                    break;
            }
        }
        protected virtual void OnCooldownEnded(CooldownMode mode)
        {
            IsRunning = false;
            switch (mode)
            {
                case CooldownMode.Normal:
                    StopArcAnimation(MainArcRef);
                    break;
                case CooldownMode.Pre:
                    StopArcAnimation(PreArcRef);
                    break;
            }
        }

        private void StartArcAnimation(Arc arc, double val = 1)
        {
            if (arc == null) return;
            _arcAnimation.Duration = TimeSpan.FromMilliseconds(Context.Duration);
            _arcAnimation.From = 359.9 * val;
            var fps = Context.Duration > 80000 ? 1 : 30;
            Timeline.SetDesiredFrameRate(_arcAnimation, fps);
            arc.BeginAnimation(Arc.EndAngleProperty, _arcAnimation);
        }

        protected void StopArcAnimation(Arc arc)
        {
            if (arc == null) return;
            arc.BeginAnimation(Arc.EndAngleProperty, null); //stop any arc animations
            arc.EndAngle = 0.01;
        }

    }
}