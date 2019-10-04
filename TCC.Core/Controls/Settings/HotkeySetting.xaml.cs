﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TCC.Annotations;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ModifierKeys = TCC.Data.ModifierKeys;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace TCC.Controls.Settings
{
    //TODO: make base class for settings controls
    public partial class HotkeySetting : INotifyPropertyChanged
    {
        public string ValueString => Value.ToString();

        public HotKey Value
        {
            get => (HotKey)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(HotKey), typeof(HotkeySetting));
        public string SettingName
        {
            get => (string)GetValue(SettingNameProperty);
            set => SetValue(SettingNameProperty, value);
        }
        public static readonly DependencyProperty SettingNameProperty = DependencyProperty.Register("SettingName", typeof(string), typeof(HotkeySetting));
        public Geometry SvgIcon
        {
            get => (Geometry)GetValue(SvgIconProperty);
            set => SetValue(SvgIconProperty, value);
        }
        public static readonly DependencyProperty SvgIconProperty = DependencyProperty.Register("SvgIcon", typeof(Geometry), typeof(HotkeySetting));

        private readonly List<Key> _pressedKeys;
        public HotkeySetting()
        {
            InitializeComponent();
            _pressedKeys = new List<Key>();
            Loaded += OnLoaded;

        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            N(nameof(ValueString));
        }


        private void UIElement_OnKeyDown(object sender, KeyEventArgs e)
        {

            KeyboardHook.Instance.Disable();

            e.Handled = true;
            var k = e.Key;
            if (k == Key.System) k = Key.LeftAlt;
            if (_pressedKeys.Contains(k)) return;
            if (k == Key.Tab && _pressedKeys.Contains(Key.LeftAlt)) return;
            if (e.Key == Key.Enter)
            {
                _pressedKeys.Clear();
                Keyboard.ClearFocus();

                return;
            }
            _pressedKeys.Add(k);
            UpdateValue();
        }

        private void UpdateValue()
        {
            var shift = _pressedKeys.Contains(Key.LeftShift);
            var alt = _pressedKeys.Contains(Key.LeftAlt);
            var key = _pressedKeys.FirstOrDefault(x => x != Key.LeftAlt && x != Key.LeftShift && x != Key.LeftCtrl);
            var mod = ModifierKeys.Control | (shift ? ModifierKeys.Shift : ModifierKeys.None) | (alt ? ModifierKeys.Alt : ModifierKeys.None);

            Enum.TryParse(key.ToString(), out Keys wfKey); // Microsoft pls
            if (wfKey == Keys.None) return;
            Value = new HotKey(wfKey, mod);
            N(nameof(ValueString));

        }

        private void UIElement_OnKeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            var k = e.Key;
            if (k == Key.System) k = Key.LeftAlt;
            if (e.Key == Key.Enter)
            {
                _pressedKeys.Clear();
                Keyboard.ClearFocus();
                return;

            }
            _pressedKeys.Remove(k);
            UpdateValue();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void N([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void UIElement_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            KeyboardHook.Instance.Disable();
        }

        private void UIElement_OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            KeyboardHook.Instance.Enable();
        }
    }
}