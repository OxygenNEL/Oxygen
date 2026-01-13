/*
<OxygenNEL>
Copyright (C) <2025>  <OxygenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using OxygenNEL.Manager;
using OxygenNEL.type;

namespace OxygenNEL.Page
{
    public sealed partial class SettingsPage : Microsoft.UI.Xaml.Controls.Page
    {
        public static string PageTitle => "设置";
        private readonly SettingData _s;
        private bool _init = true;
        private int _currentIndex;
        private readonly string[] _tags = { "appearance", "function", "proxy" };

        public SettingsPage()
        {
            InitializeComponent();
            _s = SettingManager.Instance.Get();
            DataContext = _s;

            ThemeRadios.SelectedIndex = _s.ThemeMode switch { "light" => 1, "dark" => 2, _ => 0 };
            BackdropRadios.SelectedIndex = _s.Backdrop == "acrylic" ? 1 : 0;
            AutoCopyIpSwitch.IsOn = _s.AutoCopyIpOnStart;
            IrcEnabledSwitch.IsOn = _s.IrcEnabled;
            DebugSwitch.IsOn = _s.Debug;
            Socks5EnableSwitch.IsOn = _s.Socks5Enabled;
            Socks5HostBox.Text = _s.Socks5Address;
            Socks5PortBox.Value = _s.Socks5Port;
            Socks5UsernameBox.Text = _s.Socks5Username;
            Socks5PasswordBox.Password = _s.Socks5Password;
            UpdateSocks5Enabled();

            for (int i = 0; i < AutoDisconnectOnBanCombo.Items.Count; i++)
                if (AutoDisconnectOnBanCombo.Items[i] is ComboBoxItem item && (item.Tag as string) == _s.AutoDisconnectOnBan)
                    { AutoDisconnectOnBanCombo.SelectedIndex = i; break; }
            if (AutoDisconnectOnBanCombo.SelectedIndex < 0) AutoDisconnectOnBanCombo.SelectedIndex = 0;

            _init = false;
        }

        private void UpdateSocks5Enabled()
        {
            Socks5HostBox.IsEnabled = Socks5PortBox.IsEnabled = Socks5UsernameBox.IsEnabled = Socks5PasswordBox.IsEnabled = _s.Socks5Enabled;
        }

        private void ThemeRadios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_init) return;
            _s.ThemeMode = ThemeRadios.SelectedIndex switch { 1 => "light", 2 => "dark", _ => "system" };
            MainWindow.ApplyThemeFromSettingsStatic();
        }

        private void BackdropRadios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_init) return;
            _s.Backdrop = BackdropRadios.SelectedIndex == 1 ? "acrylic" : "mica";
            MainWindow.ApplyThemeFromSettingsStatic();
        }

        private void AutoCopyIpSwitch_Toggled(object sender, RoutedEventArgs e) { if (!_init) _s.AutoCopyIpOnStart = AutoCopyIpSwitch.IsOn; }
        private void IrcEnabledSwitch_Toggled(object sender, RoutedEventArgs e) { if (!_init) { _s.IrcEnabled = IrcEnabledSwitch.IsOn; AppState.IrcEnabled = _s.IrcEnabled; } }
        private void DebugSwitch_Toggled(object sender, RoutedEventArgs e) { if (!_init) { _s.Debug = DebugSwitch.IsOn; AppState.Debug = _s.Debug; } }
        private void Socks5HostBox_TextChanged(object sender, TextChangedEventArgs e) { if (!_init) _s.Socks5Address = Socks5HostBox.Text; }
        private void Socks5PortBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) { if (!_init) _s.Socks5Port = (int)Math.Clamp(sender.Value, 0, 65535); }
        private void Socks5UsernameBox_TextChanged(object sender, TextChangedEventArgs e) { if (!_init) _s.Socks5Username = Socks5UsernameBox.Text; }
        private void Socks5PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) { if (!_init) _s.Socks5Password = Socks5PasswordBox.Password; }
        private void Socks5EnableSwitch_Toggled(object sender, RoutedEventArgs e) { if (!_init) { _s.Socks5Enabled = Socks5EnableSwitch.IsOn; UpdateSocks5Enabled(); } }

        private void AutoDisconnectOnBanCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_init) return;
            var val = (AutoDisconnectOnBanCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "none";
            _s.AutoDisconnectOnBan = val;
            AppState.AutoDisconnectOnBan = val;
        }

        private void SettingsNav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var tag = (args.SelectedItem as NavigationViewItem)?.Tag as string;
            var newIndex = Array.IndexOf(_tags, tag);
            if (newIndex < 0) newIndex = 0;

            var panel = tag switch
            {
                "appearance" => AppearancePanel,
                "function" => FunctionPanel,
                "proxy" => ProxyPanel,
                _ => AppearancePanel
            };

            AppearancePanel.Visibility = tag == "appearance" ? Visibility.Visible : Visibility.Collapsed;
            FunctionPanel.Visibility = tag == "function" ? Visibility.Visible : Visibility.Collapsed;
            ProxyPanel.Visibility = tag == "proxy" ? Visibility.Visible : Visibility.Collapsed;

            if (!_init && newIndex != _currentIndex)
            {
                var fromRight = newIndex > _currentIndex;
                AnimatePanel(panel, fromRight);
            }
            _currentIndex = newIndex;
        }

        private void AnimatePanel(UIElement panel, bool fromRight)
        {
            var visual = ElementCompositionPreview.GetElementVisual(panel);
            var compositor = visual.Compositor;

            var offsetStart = fromRight ? 80f : -80f;
            visual.Offset = new Vector3(offsetStart, 0, 0);
            visual.Opacity = 0;

            var offsetAnim = compositor.CreateVector3KeyFrameAnimation();
            offsetAnim.InsertKeyFrame(0, new Vector3(offsetStart, 0, 0));
            offsetAnim.InsertKeyFrame(1, Vector3.Zero, compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f)));
            offsetAnim.Duration = TimeSpan.FromMilliseconds(250);

            var opacityAnim = compositor.CreateScalarKeyFrameAnimation();
            opacityAnim.InsertKeyFrame(0, 0);
            opacityAnim.InsertKeyFrame(1, 1);
            opacityAnim.Duration = TimeSpan.FromMilliseconds(200);

            visual.StartAnimation("Offset", offsetAnim);
            visual.StartAnimation("Opacity", opacityAnim);
        }
    }
}
