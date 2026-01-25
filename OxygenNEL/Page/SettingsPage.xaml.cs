/*
<OxygenNEL>
Copyright (C) <2025>  <OxygenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Windows.Storage.Pickers;
using WinRT.Interop;
using OxygenNEL.Manager;
using OxygenNEL.type;

namespace OxygenNEL.Page
{
    public sealed partial class SettingsPage : Microsoft.UI.Xaml.Controls.Page
    {
        public static string PageTitle => "设置";
        private readonly SettingData _s;
        private bool _init = true;
        private bool _isPickerOpen;
        private int _currentIndex;
        private readonly string[] _tags = { "appearance", "function", "proxy" };

        public SettingsPage()
        {
            InitializeComponent();
            _s = SettingManager.Instance.Get();
            DataContext = _s;

            ThemeRadios.SelectedIndex = _s.ThemeMode switch { "light" => 1, "dark" => 2, _ => 0 };
            BackdropRadios.SelectedIndex = _s.Backdrop switch { "acrylic" => 1, "custom" => 2, _ => 0 };
            UpdateCustomBackgroundPanel();
            MusicPlayerSwitch.IsOn = _s.MusicPlayerEnabled;
            MusicVolumeSlider.Value = _s.MusicVolume > 0 ? _s.MusicVolume : 0.5;
            MusicVolumeText.Text = $"{(int)(MusicVolumeSlider.Value * 100)}%";
            UpdateMusicPlayerPanel();
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

        private void UpdateCustomBackgroundPanel()
        {
            var isCustom = _s.Backdrop == "custom";
            CustomBackgroundPanel.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
            
            if (!string.IsNullOrEmpty(_s.CustomBackgroundPath))
            {
                BackgroundPathText.Text = Path.GetFileName(_s.CustomBackgroundPath);
                ClearBackgroundBtn.Visibility = Visibility.Visible;
            }
            else
            {
                BackgroundPathText.Text = "未选择";
                ClearBackgroundBtn.Visibility = Visibility.Collapsed;
            }
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
            _s.Backdrop = BackdropRadios.SelectedIndex switch { 1 => "acrylic", 2 => "custom", _ => "mica" };
            UpdateCustomBackgroundPanel();
            MainWindow.ApplyThemeFromSettingsStatic();
        }

        private async void SelectBackgroundBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isPickerOpen) return;
            _isPickerOpen = true;
            
            string? filePath = null;
            try
            {
                var picker = new FileOpenPicker();
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add(".gif");
                picker.FileTypeFilter.Add(".bmp");
                picker.FileTypeFilter.Add(".mp4");
                picker.FileTypeFilter.Add(".webm");
                picker.FileTypeFilter.Add(".wmv");

                var window = App.MainWindow;
                if (window == null) return;
                var hwnd = WindowNative.GetWindowHandle(window);
                InitializeWithWindow.Initialize(picker, hwnd);

                await Task.Delay(50);
                var file = await picker.PickSingleFileAsync();
                filePath = file?.Path;
            }
            catch (Exception ex)
            {
                Serilog.Log.Debug(ex, "文件选择器异常");
                filePath = await ShowManualPathInputDialog("选择背景文件", "请输入图片或视频文件的完整路径：");
            }
            finally
            {
                _isPickerOpen = false;
            }

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                try
                {
                    var destPath = SettingManager.CopyBackgroundToData(filePath);
                    _s.CustomBackgroundPath = destPath;
                    UpdateCustomBackgroundPanel();
                    MainWindow.ApplyThemeFromSettingsStatic();
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "复制背景文件失败");
                }
            }
        }

        private void ClearBackgroundBtn_Click(object sender, RoutedEventArgs e)
        {
            _s.CustomBackgroundPath = string.Empty;
            UpdateCustomBackgroundPanel();
            MainWindow.ApplyThemeFromSettingsStatic();
        }

        private void UpdateMusicPlayerPanel()
        {
            MusicPlayerSettingsPanel.Visibility = _s.MusicPlayerEnabled ? Visibility.Visible : Visibility.Collapsed;
            
            if (!string.IsNullOrEmpty(_s.MusicPath))
            {
                MusicPathText.Text = Path.GetFileName(_s.MusicPath);
                ClearMusicBtn.Visibility = Visibility.Visible;
            }
            else
            {
                MusicPathText.Text = "未选择";
                ClearMusicBtn.Visibility = Visibility.Collapsed;
            }
        }

        private void MusicPlayerSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (_init) return;
            _s.MusicPlayerEnabled = MusicPlayerSwitch.IsOn;
            UpdateMusicPlayerPanel();
            MainWindow.ApplyMusicPlayerSettingsStatic();
        }

        private async void SelectMusicBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isPickerOpen) return;
            _isPickerOpen = true;
            
            string? filePath = null;
            try
            {
                var picker = new FileOpenPicker();
                picker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
                picker.FileTypeFilter.Add(".mp3");
                picker.FileTypeFilter.Add(".wav");
                picker.FileTypeFilter.Add(".flac");
                picker.FileTypeFilter.Add(".m4a");
                picker.FileTypeFilter.Add(".wma");
                picker.FileTypeFilter.Add(".ogg");

                var window = App.MainWindow;
                if (window == null) return;
                var hwnd = WindowNative.GetWindowHandle(window);
                InitializeWithWindow.Initialize(picker, hwnd);

                await Task.Delay(50);
                var file = await picker.PickSingleFileAsync();
                filePath = file?.Path;
            }
            catch (Exception ex)
            {
                Serilog.Log.Debug(ex, "文件选择器异常");
                filePath = await ShowManualPathInputDialog("选择音乐文件", "请输入音乐文件的完整路径：");
            }
            finally
            {
                _isPickerOpen = false;
            }

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                try
                {
                    var destPath = SettingManager.CopyMusicToData(filePath);
                    _s.MusicPath = destPath;
                    UpdateMusicPlayerPanel();
                    MainWindow.ApplyMusicPlayerSettingsStatic();
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "复制音乐文件失败");
                }
            }
        }

        private void ClearMusicBtn_Click(object sender, RoutedEventArgs e)
        {
            _s.MusicPath = string.Empty;
            UpdateMusicPlayerPanel();
            MainWindow.ApplyMusicPlayerSettingsStatic();
        }

        private void MusicVolumeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_init) return;
            _s.MusicVolume = e.NewValue;
            MusicVolumeText.Text = $"{(int)(e.NewValue * 100)}%";
            MainWindow.UpdateMusicVolumeStatic(e.NewValue);
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

        private async Task<string?> ShowManualPathInputDialog(string title, string prompt)
        {
            var inputBox = new TextBox { PlaceholderText = @"C:\path\to\file.ext", Width = 400 };
            var dialog = new ContentDialog
            {
                Title = title,
                Content = new StackPanel
                {
                    Spacing = 8,
                    Children = { new TextBlock { Text = prompt, TextWrapping = TextWrapping.Wrap }, inputBox }
                },
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                XamlRoot = this.XamlRoot
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary ? inputBox.Text?.Trim() : null;
        }
    }
}
