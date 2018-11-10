﻿using System;
using System.Windows.Threading;

namespace TCC.Data.Skills
{
    public class Cooldown : TSPropertyChanged, IDisposable
    {
        // events
        public event Action<CooldownMode> Started;
        public event Action<CooldownMode> Ended;
        public event Action FlashingForced;
        public event Action FlashingStopForced;
        public event Action SecondsUpdated;
        public event Action DurationLeftUpdated;    // by HQ
        public event Action Reset;

        // fields
        private DispatcherTimer _mainTimer;
        //private DispatcherTimer _offsetTimer;
        //private DispatcherTimer _secondsTimer;
        private DispatcherTimer _durationleftTimer; // by HQ
        private ulong _seconds;
        private ulong _durationleft;                // by HQ
        private bool _flashOnAvailable;
        private Skill _skill;

        DateTime Starttime;                         // by HQ
        ulong interval;                             // by HQ

        // properties
        public Skill Skill
        {
            get => _skill;
            set
            {
                if (_skill == value) return;
                _skill = value;
                NPC();
            }
        }
        public ulong Duration { get; private set; }
        public ulong OriginalDuration { get; private set; }
        public CooldownType CooldownType { get; set; }
        public CooldownMode Mode { get; private set; }
        public bool FlashOnAvailable
        {
            get => _flashOnAvailable;
            set
            {
                _flashOnAvailable = value;
                NPC();
                if (value) ForceFlashing();
                else ForceStopFlashing();
            }
        }
        public ulong Seconds
        {
            get => _seconds;
            set
            {
                if (_seconds == value) return;
                _seconds = value;
                Dispatcher.Invoke(() => SecondsUpdated?.Invoke());
            }
        }
        public ulong DurationLeft   // by HQ
        {
            get => _durationleft;
            set
            {
                if (_durationleft == value) return;
                _durationleft = value;
                Dispatcher.Invoke(() => DurationLeftUpdated?.Invoke());
            }
        }

        public bool IsAvailable => !_mainTimer.IsEnabled;

        // ctors
        public Cooldown()
        {
            Dispatcher = App.BaseDispatcher;
            Dispatcher.Invoke(() =>
            {
                _mainTimer = new DispatcherTimer();
                //_offsetTimer = new DispatcherTimer();
                //_secondsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                _durationleftTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };    // by HQ
            });


            _mainTimer.Tick += CooldownEnded;
            //_offsetTimer.Tick += StartSecondsTimer;
            //_secondsTimer.Tick += DecreaseSeconds;
            _durationleftTimer.Tick += DecreaseDurationLeft;                                        // by HQ

            SessionManager.CombatChanged += OnCombatStatusChanged;
            SessionManager.EncounterChanged += OnCombatStatusChanged;

        }
        public Cooldown(Skill sk, bool flashOnAvailable, CooldownType t = CooldownType.Skill) : this()
        {
            CooldownType = t;
            Skill = sk;
            FlashOnAvailable = flashOnAvailable;
        }

        public Cooldown(Skill sk, ulong cooldown, CooldownType type = CooldownType.Skill, CooldownMode mode = CooldownMode.Normal) : this(sk, false, type)
        {
            if (cooldown == 0) return;
            if (type == CooldownType.Item) cooldown = cooldown * 1000;
            Start(cooldown, mode);
        }
        private void OnCombatStatusChanged()
        {
            if ((SessionManager.Encounter || SessionManager.Combat) && FlashOnAvailable)
                ForceFlashing();
            else
                ForceStopFlashing();
        }

        // timers tick handlers

        private void CooldownEnded(object sender, EventArgs e)
        {
            _mainTimer.Stop();
            NPC(nameof(IsAvailable));
            //_secondsTimer.Stop();
            _durationleftTimer.Stop();  // by HQ
            Seconds = 0;
            Dispatcher.Invoke(() => Ended?.Invoke(Mode));
        }
        /*
        private void StartSecondsTimer(object sender, EventArgs e)
        {
            _offsetTimer.Stop();
            _secondsTimer.Start();
        }
        */
        /*
        private void DecreaseSeconds(object sender, EventArgs e)
        {
            if (Seconds > 0) Seconds = Seconds - 1;
            else _secondsTimer.Stop();
        }
        */
        private void DecreaseDurationLeft(object sender, EventArgs e)   // by HQ
        {
            UpdateNextinterval();

            _durationleftTimer.Interval = TimeSpan.FromMilliseconds(interval);
            _durationleftTimer.Stop();
            if (interval > 0)
            {
                _durationleftTimer.Start();
            }
        }
        private void UpdateNextinterval()                               // by HQ
        {
            TimeSpan Timediff = DateTime.Now - Starttime;

            if (Duration > (uint)Timediff.TotalMilliseconds)
            {
                DurationLeft = Duration - (uint)Timediff.TotalMilliseconds;
                Seconds = DurationLeft / 1000;
                ulong DurationLeftRemainder = 0;
                if ((DurationLeft <= Settings.Settings.SkillDecimalPlaceSeconds * 1000) && (Settings.Settings.ShowSkillDecimalPlace == true))
                {
                    DurationLeftRemainder = DurationLeft % 100;
                    interval = DurationLeftRemainder + 1;
                }
                else
                {
                    DurationLeftRemainder = DurationLeft % 1000;
                    interval = DurationLeftRemainder + 1;
                }
            }
            else
            {
                DurationLeft = 0;
                Seconds = 0;
                interval = 0;
            }
        }
        // methods
        public void Start(ulong cd, CooldownMode mode = CooldownMode.Normal)
        {
            Duration = cd;
            OriginalDuration = cd;
            Seconds = Duration / 1000;
            Mode = mode;
            Start(this);
        }
        public void Start(Cooldown sk)
        {
            if(sk!=this) sk.Dispose();
            if (sk.Duration >= Int32.MaxValue) return;
            if (_mainTimer.IsEnabled)
            {
                if (Mode == CooldownMode.Pre)
                {

                    _mainTimer.Stop();
                    NPC(nameof(IsAvailable));
                    //_secondsTimer.Stop();
                    //_offsetTimer.Stop();
                    _durationleftTimer.Stop();  // by HQ

                    Dispatcher.Invoke(() => Ended?.Invoke(Mode));
                }
            }

            Mode = sk.Mode;
            Seconds = sk.Seconds;
            Duration = sk.Duration;
            OriginalDuration = sk.OriginalDuration;

            _mainTimer.Interval = TimeSpan.FromMilliseconds(Duration);
            _mainTimer.Start();
            NPC(nameof(IsAvailable));

            //_offsetTimer.Interval = TimeSpan.FromMilliseconds(Duration % 1000);
            //_offsetTimer.Start();
            Starttime = DateTime.Now;                                           // by HQ
            UpdateNextinterval();                                               // by HQ
            _durationleftTimer.Interval = TimeSpan.FromMilliseconds(interval);  // by HQ
            _durationleftTimer.Stop();                                          // by HQ
            if (interval > 0)                                                   // by HQ
            {
                _durationleftTimer.Start();
            }

            Dispatcher.Invoke(() => Started?.Invoke(Mode));
        }
        public void Refresh(ulong cd, CooldownMode mode)
        {
            _mainTimer.Stop();
            NPC(nameof(IsAvailable));

            if (cd == 0 || cd >= Int32.MaxValue)
            {
                Seconds = 0;
                Duration = 0;
                Dispatcher?.Invoke(() => Ended?.Invoke(Mode));
                return;
            }
            Mode = mode;
            Duration = cd;
            Seconds = Duration / 1000;

            //_offsetTimer.Interval = TimeSpan.FromMilliseconds(cd % 1000);
            //_offsetTimer.Start();

            _mainTimer.Interval = TimeSpan.FromMilliseconds(cd);
            _mainTimer.Start();
            NPC(nameof(IsAvailable));

            Starttime = DateTime.Now;                                           // by HQ
            UpdateNextinterval();                                               // by HQ
            _durationleftTimer.Stop();                                          // by HQ
            _durationleftTimer.Interval = TimeSpan.FromMilliseconds(interval);  // by HQ
            if (interval > 0)                                                   // by HQ
            {
                _durationleftTimer.Start();
            }

            Dispatcher?.Invoke(() => Started?.Invoke(Mode));

        }
        public void Refresh(ulong id, ulong cd, CooldownMode mode)
        {
            if (Skill.Id % 10 == 0 && id % 10 != 0) return; //TODO: check this; discards updates if new id is not base
            Refresh(cd, mode);
        }


        private void ForceFlashing()
        {
            Dispatcher.Invoke(() => FlashingForced?.Invoke());
        }
        public void ForceStopFlashing()
        {
            Dispatcher.Invoke(() => FlashingStopForced?.Invoke());
        }
        public void ForceEnded()
        {
            CooldownEnded(null, null);
        }
        public void ProcReset()
        {
            Dispatcher.Invoke(() => Reset?.Invoke());
        }

        public void Dispose()
        {
            _mainTimer.Stop();
            //_offsetTimer.Stop();
            //_secondsTimer.Stop();
            _durationleftTimer.Stop();      // by HQ
            SessionManager.CombatChanged -= OnCombatStatusChanged;
            SessionManager.EncounterChanged -= OnCombatStatusChanged;
        }
        public override string ToString()
        {
            return Skill.Name;
        }

    }
}
