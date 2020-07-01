﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Nostrum.Extensions;
using Nostrum.Factories;

namespace TCC.UI.Windows
{
    public class TccWindow : Window
    {

        private static readonly List<TccWindow> _createdWindows = new List<TccWindow>();

        public event Action Hidden = null!;
        public event Action Showed = null!;

        private readonly bool _canClose;
        private readonly DoubleAnimation _showAnim;
        private readonly DoubleAnimation _hideAnim;

        public IntPtr Handle { get; private set; }


        protected TccWindow(bool canClose)
        {
            _createdWindows.Add(this);
            _canClose = canClose;
            Closing += OnClosing;
            Loaded += OnLoaded;
            _showAnim = AnimationFactory.CreateDoubleAnimation(150, 1);
            _hideAnim = AnimationFactory.CreateDoubleAnimation(150, 0, completed: (_, __) =>
            {
                Hide();
                if (App.Settings.ForceSoftwareRendering) RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
                Hidden?.Invoke();
            });
        }

        public virtual void HideWindow()

        {
            Dispatcher?.InvokeAsync(() =>
            {
                BeginAnimation(OpacityProperty, _hideAnim);
            });
        }
        public virtual void ShowWindow()
        {
            if (App.Settings.ForceSoftwareRendering) RenderOptions.ProcessRenderMode = RenderMode.Default;
            Dispatcher?.InvokeAsync(() =>
            {
                BeginAnimation(OpacityProperty, null);
                Opacity = 0;
                Show();
                Showed?.Invoke();
                RefreshTopmost();
                BeginAnimation(OpacityProperty, _showAnim);
            });
        }

        protected virtual void OnLoaded(object sender, RoutedEventArgs e)
        {
            Handle = new WindowInteropHelper(this).Handle;
        }
        protected virtual void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_canClose)
            {
                _createdWindows.Remove(this);
                return;
            }
            e.Cancel = true;
            HideWindow();
        }

        protected void Drag(object sender, MouseButtonEventArgs e)
        {
            ((UIElement)Content).Opacity = .7;
            this.TryDragMove();
            ((UIElement)Content).Opacity = 1;
        }

        protected void RefreshTopmost()
        {
            if (FocusManager.PauseTopmost) return;

            Dispatcher?.InvokeAsync(() =>
            {
                Topmost = false;
                Topmost = true;
            });
        }

        public static bool IsCreated(Type type)
        {
            return _createdWindows.Any(w => w.GetType() == type);
        }
    }
}