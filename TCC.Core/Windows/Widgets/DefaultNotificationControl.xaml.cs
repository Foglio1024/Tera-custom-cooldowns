﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using FoglioUtils;
using TCC.Controls;

namespace TCC.Windows.Widgets
{
    public partial class DefaultNotificationControl
    {
        private readonly DoubleAnimation _arcAnimation;
        private readonly DoubleAnimation _slideInAnimation;
        private readonly DoubleAnimation _slideOutAnimation;
        private readonly DoubleAnimation _fadeInAnimation;
        private readonly DoubleAnimation _fadeOutAnimation;
        private readonly DoubleAnimation _shrinkAnimation;
        private readonly DispatcherTimer _duration;
        private readonly Effect _rootEffect;
        private NotificationInfo _dc;
        public DefaultNotificationControl()
        {
            Loaded += OnLoaded;
            _arcAnimation = AnimationFactory.CreateDoubleAnimation(4000, 0, 359.9, completed: OnTimeExpired);
            _slideInAnimation = AnimationFactory.CreateDoubleAnimation(150, 0, -100, easing: true, completed: OnSlideInCompleted, framerate: 60);
            _fadeInAnimation = AnimationFactory.CreateDoubleAnimation(150, 1, 0, framerate: 30);

            _slideOutAnimation = AnimationFactory.CreateDoubleAnimation(150, -100, 0, completed: OnFadeFinished, easing: true, framerate: 60);
            _fadeOutAnimation = AnimationFactory.CreateDoubleAnimation(150, 0, framerate: 30);
            _shrinkAnimation = AnimationFactory.CreateDoubleAnimation(150, 0, 1, easing: true, completed: OnShrinkFinished, framerate: 60);

            _duration = new DispatcherTimer();
            InitializeComponent();
            Root.Opacity = 0;

            // keep Root shadow reference, remove it from view, apply after animation
            _rootEffect = Root.Effect;
            Root.Effect = null;
        }

        private void OnSlideInCompleted(object sender, EventArgs e)
        {
            if (_dc == null) return;
            _duration.Interval = TimeSpan.FromMilliseconds(_dc.Duration);
            _duration.Tick += OnTimeExpired;
            _duration.Start();
            Root.Effect = _rootEffect;
            //_arcAnimation.Duration = TimeSpan.FromMilliseconds(_dc.Duration);
            //TimeArc.BeginAnimation(Arc.EndAngleProperty, _arcAnimation);
        }

        private void OnShrinkFinished(object sender, EventArgs e)
        {
            if (_dc == null) return;

            WindowManager.ViewModels.NotificationArea.DeleteNotification(_dc);
        }

        private void OnFadeFinished(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Root.LayoutTransform.BeginAnimation(ScaleTransform.ScaleYProperty, _shrinkAnimation);
            }), DispatcherPriority.Background);
        }

        private void OnTimeExpired(object sender, EventArgs e)
        {
            _duration.Stop();
            _duration.Tick -= OnTimeExpired;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Root.Effect = null;
                Root.BeginAnimation(OpacityProperty, _fadeOutAnimation);
                Root.RenderTransform.BeginAnimation(TranslateTransform.XProperty, _slideOutAnimation);
            }), DispatcherPriority.Background);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _dc = (NotificationInfo)DataContext;
            if (_dc == null) return;

            Root.BeginAnimation(OpacityProperty, _fadeInAnimation);
            Root.RenderTransform.BeginAnimation(TranslateTransform.XProperty, _slideInAnimation);
        }
    }
}