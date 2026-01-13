/*
<OxygenNEL>
Copyright (C) <2025>  <OxygenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media;
using OxygenNEL.Page;
using OxygenNEL.Utils;
using OxygenNEL.Component;
using OxygenNEL.type;
using OxygenNEL.Manager;
using Serilog;

namespace OxygenNEL
{
    public sealed partial class MainWindow : Window
    {
        static MainWindow? _instance;
        AppWindow? _appWindow;
        string _currentBackdrop = "";
        bool _mainNavigationInitialized;
        public static Microsoft.UI.Dispatching.DispatcherQueue? UIQueue => _instance?.DispatcherQueue;
        public static XamlRoot? DialogXamlRoot =>
            _instance == null ? null :
            (_instance.AuthOverlay.Visibility == Visibility.Visible ? _instance.OverlayFrame.XamlRoot : null)
            ?? _instance.ContentFrame.XamlRoot
            ?? _instance.NavView.XamlRoot;
        public static void RefreshAuthUi() => _instance?.UpdateAuthOverlay();

        public MainWindow()
        {
            InitializeComponent();
            _instance = this;
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);
            _appWindow.Title = "Oxygen NEL";
            AppTitleTextBlock.Text = _appWindow.Title;
            ApplyThemeFromSettings();
            InitializeMainNavigationIfNeeded();
            AuthManager.Instance.LoadFromDisk();
            if (AuthManager.Instance.IsLoggedIn)
            {
                _ = VerifyAndAutoLoginAsync();
            }
            UpdateAuthOverlay();
            _ = CheckUpdateAsync();
        }

        async Task VerifyAndAutoLoginAsync()
        {
            var result = await AuthManager.Instance.TokenAuthAsync();
            DispatcherQueue.TryEnqueue(() =>
            {
                if (result.Success)
                {
                    var name = string.IsNullOrWhiteSpace(AuthManager.Instance.Username) ? "用户" : AuthManager.Instance.Username;
                    NotificationHost.ShowGlobal($"欢迎 {name}，已自动登录", ToastLevel.Success);
                    _ = Task.Run(async () => await AuthManager.Instance.GetCrcSaltAsync(default));
                }
                else
                {
                    AuthManager.Instance.Clear();
                    NotificationHost.ShowGlobal("登录已过期，请重新登录", ToastLevel.Warning);
                }
                UpdateAuthOverlay();
            });
        }

        async Task CheckUpdateAsync()
        {
            await Task.Delay(1000);
            
            using var http = new HttpClient();
            try
            {
                var resp = await http.GetAsync("https://api.fandmc.cn/get/lastversion");
                var json = await resp.Content.ReadAsStringAsync();
                Log.Information("获取版本信息返回: {Json}", json);

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                {
                    var version = root.TryGetProperty("version", out var vp) ? vp.GetString() : null;
                    var downloadUrl = root.TryGetProperty("downloadUrl", out var dp) ? dp.GetString() : null;
                    
                    if (!string.IsNullOrWhiteSpace(version) && !string.Equals(version, type.AppInfo.AppVersion, StringComparison.OrdinalIgnoreCase))
                    {
                        var dialog = new ThemedContentDialog
                        {
                            Title = "检测到新版本",
                            Content = $"检测到新版本 {version}\n是否更新？",
                            PrimaryButtonText = "确定",
                            CloseButtonText = "取消",
                            XamlRoot = this.Content.XamlRoot
                        };

                        var result = await dialog.ShowAsync();
                        if (result != ContentDialogResult.Primary) return;

                        await DownloadAndApplyUpdateAsync(downloadUrl!);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "检查版本失败");
            }
        }

        async Task DownloadAndApplyUpdateAsync(string downloadUrl)
        {
            var progressDialog = new ThemedContentDialog
            {
                Title = "更新中",
                XamlRoot = this.Content.XamlRoot
            };

            var progressBar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Width = 300,
                IsIndeterminate = false
            };
            var statusText = new TextBlock { Text = "正在下载..." };
            var panel = new StackPanel { Spacing = 8 };
            panel.Children.Add(statusText);
            panel.Children.Add(progressBar);
            progressDialog.Content = panel;

            _ = progressDialog.ShowAsync();

            try
            {
                var exePath = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                var exeDir = Path.GetDirectoryName(exePath) ?? AppContext.BaseDirectory;
                var exeName = Path.GetFileName(exePath) ?? "OxygenNEL.exe";
                var updateDir = Path.Combine(exeDir, "update");
                Directory.CreateDirectory(updateDir);
                var newExePath = Path.Combine(updateDir, exeName);

                using var headResponse = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Head, downloadUrl));
                headResponse.EnsureSuccessStatusCode();
                var totalBytes = headResponse.Content.Headers.ContentLength ?? -1;
                
                if (totalBytes <= 0)
                {
                    throw new InvalidOperationException("无法获取文件大小");
                }

                var acceptRanges = headResponse.Headers.GetValues("Accept-Ranges").FirstOrDefault();
                if (acceptRanges != "bytes")
                {
                    using var response = await _http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();
                    
                    var buffer = new byte[8192];
                    long downloadedBytes = 0;
                    
                    await using var contentStream = await response.Content.ReadAsStreamAsync();
                    await using var fileStream = new FileStream(newExePath, FileMode.Create, FileAccess.Write, FileShare.None);
                    
                    int bytesRead;
                    while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                        downloadedBytes += bytesRead;
                        
                        var percent = (double)downloadedBytes / totalBytes * 100;
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            progressBar.Value = percent;
                            statusText.Text = $"正在下载... {percent:F1}%";
                        });
                    }
                }
                else
                {
                    const int threadCount = 4;
                    var chunkSize = totalBytes / threadCount;
                    var tasks = new Task[threadCount];
                    var tempFiles = new string[threadCount];
                    var downloadedBytesArray = new long[threadCount];

                    for (int i = 0; i < threadCount; i++)
                    {
                        var start = i * chunkSize;
                        var end = i == threadCount - 1 ? totalBytes - 1 : (i + 1) * chunkSize - 1;
                        var range = $"bytes={start}-{end}";
                        tempFiles[i] = Path.GetTempFileName();
                        
                        tasks[i] = DownloadChunkAsync(downloadUrl, range, tempFiles[i], start, end, totalBytes, progressBar, statusText, downloadedBytesArray, i);
                    }

                    await Task.WhenAll(tasks);

                    await MergeChunksAsync(tempFiles, newExePath);

                    foreach (var tempFile in tempFiles)
                    {
                        try { File.Delete(tempFile); } catch { }
                    }
                }

                DispatcherQueue.TryEnqueue(() => statusText.Text = "下载完成，正在准备更新...");

                var batPath = Path.Combine(exeDir, "update.bat");
                var batContent = $@"@echo off
chcp 65001 >nul
echo 正在更新，请稍候...
ping 127.0.0.1 -n 2 >nul
copy /Y ""{newExePath}"" ""{exePath}""
if %errorlevel% equ 0 (
    echo 更新成功，正在启动...
    start """" ""{exePath}""
    rd /s /q ""{updateDir}""
) else (
    echo 更新失败，请手动替换
    pause
)
del ""%~f0""
";
                await File.WriteAllTextAsync(batPath, batContent);

                progressDialog.Hide();

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = batPath,
                    UseShellExecute = true,
                    CreateNoWindow = false
                });

                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "下载更新失败");
                progressDialog.Hide();
                NotificationHost.ShowGlobal($"下载更新失败: {ex.Message}", ToastLevel.Error);
            }
        }

        static readonly HttpClient _http = new();

        async Task DownloadChunkAsync(string downloadUrl, string range, string tempFile, long start, long end, long totalBytes, ProgressBar progressBar, TextBlock statusText, long[] downloadedBytesArray, int threadIndex)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(start, end);
            
            using var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            var buffer = new byte[8192];
            long chunkDownloadedBytes = 0;
            
            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);
            
            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                chunkDownloadedBytes += bytesRead;
                
                Volatile.Write(ref downloadedBytesArray[threadIndex], chunkDownloadedBytes);
                
                var totalDownloaded = 0L;
                for (int i = 0; i < downloadedBytesArray.Length; i++)
                {
                    totalDownloaded += Volatile.Read(ref downloadedBytesArray[i]);
                }
                
                var percent = (double)totalDownloaded / totalBytes * 100;
                DispatcherQueue.TryEnqueue(() =>
                {
                    progressBar.Value = percent;
                    statusText.Text = $"正在下载... {percent:F1}%";
                });
            }
        }

        async Task MergeChunksAsync(string[] tempFiles, string outputPath)
        {
            await using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
            foreach (var tempFile in tempFiles)
            {
                await using var inputStream = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                await inputStream.CopyToAsync(outputStream);
            }
        }

        void UpdateAuthOverlay()
        {
            if (AuthManager.Instance.IsLoggedIn)
            {
                AuthOverlay.Visibility = Visibility.Collapsed;
                NavView.Visibility = Visibility.Visible;
                OverlayFrame.Content = null;
                UserProfile.UpdateUserInfo();
                return;
            }

            NavView.Visibility = Visibility.Collapsed;
            AuthOverlay.Visibility = Visibility.Visible;
            if (OverlayFrame.Content == null) OverlayFrame.Navigate(typeof(LoginPage));
        }

        private static readonly Dictionary<string, (Type Page, string Title)> Pages = new()
        {
            ["HomePage"] = (typeof(HomePage), HomePage.PageTitle),
            ["AccountPage"] = (typeof(AccountPage), AccountPage.PageTitle),
            ["NetworkServerPage"] = (typeof(NetworkServerPage), NetworkServerPage.PageTitle),
            ["RentalServerPage"] = (typeof(RentalServerPage), RentalServerPage.PageTitle),
            ["PluginsPage"] = (typeof(PluginsPage), PluginsPage.PageTitle),
            ["GamesPage"] = (typeof(GamesPage), GamesPage.PageTitle),
            ["SkinPage"] = (typeof(SkinPage), SkinPage.PageTitle),
            ["ToolsPage"] = (typeof(ToolsPage), ToolsPage.PageTitle),
            ["AboutPage"] = (typeof(AboutPage), AboutPage.PageTitle),
        };

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeMainNavigationIfNeeded();
        }

        private void AddNavItem(Symbol icon, string key)
        {
            if (!Pages.TryGetValue(key, out var info)) return;
            NavView.MenuItems.Add(new NavigationViewItem { Icon = new SymbolIcon(icon), Content = info.Title, Tag = key });
        }

        void InitializeMainNavigationIfNeeded()
        {
            if (_mainNavigationInitialized) return;
            _mainNavigationInitialized = true;

            NavView.MenuItems.Clear();
            AddNavItem(Symbol.Home, "HomePage");
            AddNavItem(Symbol.People, "AccountPage");
            AddNavItem(Symbol.World, "NetworkServerPage");
            AddNavItem(Symbol.Remote, "RentalServerPage");
            AddNavItem(Symbol.AllApps, "PluginsPage");
            AddNavItem(Symbol.Play, "GamesPage");
            AddNavItem(Symbol.AllApps, "SkinPage");
            AddNavItem(Symbol.Setting, "ToolsPage");
            AddNavItem(Symbol.ContactInfo, "AboutPage");

            foreach (NavigationViewItemBase item in NavView.MenuItems)
            {
                if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == "HomePage")
                {
                    NavView.SelectedItem = navItem;
                    ContentFrame.Navigate(typeof(HomePage));
                    break;
                }
            }
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                ContentFrame.Navigate(typeof(SettingsPage));
            }
            else if (args.SelectedItem is NavigationViewItem { Tag: string key } && Pages.TryGetValue(key, out var info))
            {
                ContentFrame.Navigate(info.Page);
            }
        }

        private void NavView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (NavView.PaneDisplayMode == NavigationViewPaneDisplayMode.Left)
            {
                NavView.OpenPaneLength = e.NewSize.Width * 0.2; 
            }
        }

        void ApplyThemeFromSettings()
        {
            try
            {
                var mode = OxygenNEL.Manager.SettingManager.Instance.Get().ThemeMode?.Trim().ToLowerInvariant() ?? "system";
                ElementTheme t = ElementTheme.Default;
                if (mode == "light") t = ElementTheme.Light;
                else if (mode == "dark") t = ElementTheme.Dark;
                RootGrid.RequestedTheme = t;
                NavView.RequestedTheme = t;
                ContentFrame.RequestedTheme = t;
                var actual = t == ElementTheme.Default ? RootGrid.ActualTheme : t;
                UpdateTitleBarColors(actual);

                var bd = OxygenNEL.Manager.SettingManager.Instance.Get().Backdrop?.Trim().ToLowerInvariant() ?? "mica";
                if (bd != _currentBackdrop)
                {
                    if (bd == "acrylic")
                    {
                        SystemBackdrop = new DesktopAcrylicBackdrop();
                        RootGrid.Background = null;
                    }
                    else
                    {
                        SystemBackdrop = new MicaBackdrop();
                        RootGrid.Background = null;
                    }
                    _currentBackdrop = bd;
                }
            }
            catch (Exception ex) { Log.Warning(ex, "应用主题失败"); }
        }

        public static void ApplyThemeFromSettingsStatic()
        {
            _instance?.ApplyThemeFromSettings();
        }

        void UpdateTitleBarColors(ElementTheme theme)
        {
            try
            {
                var tb = _appWindow?.TitleBar;
                if (tb == null) return;
                var fg = ColorUtil.ForegroundForTheme(theme);
                var bg = ColorUtil.Transparent;
                tb.ForegroundColor = fg;
                tb.InactiveForegroundColor = fg;
                tb.ButtonForegroundColor = fg;
                tb.ButtonInactiveForegroundColor = fg;
                tb.BackgroundColor = bg;
                tb.InactiveBackgroundColor = bg;
                tb.ButtonHoverForegroundColor = fg;
                tb.ButtonPressedForegroundColor = fg;
                tb.ButtonBackgroundColor = ColorUtil.Transparent;
                tb.ButtonInactiveBackgroundColor = ColorUtil.Transparent;
                tb.ButtonHoverBackgroundColor = ColorUtil.HoverBackgroundForTheme(theme);
                tb.ButtonPressedBackgroundColor = ColorUtil.PressedBackgroundForTheme(theme);
            }
            catch (Exception ex) { Log.Warning(ex, "更新标题栏颜色失败"); }
        }
    }
}
