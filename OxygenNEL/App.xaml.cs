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
using System.Linq;
using System.Reflection;
using Microsoft.UI.Xaml;
using Serilog;
using System.Threading.Tasks;
using Codexus.Cipher.Protocol;
using Codexus.Development.SDK.Manager;
using Codexus.Game.Launcher.Utils;
using OxygenNEL.Core.Utils;
using Codexus.Interceptors;
using OxygenNEL.type;
using Codexus.OpenSDK;
using Codexus.OpenSDK.Yggdrasil;
using Codexus.OpenSDK.Entities.Yggdrasil;
using OxygenNEL.Manager;
using OxygenNEL.Utils;
using OxygenNEL.IRC;
using FileUtil = OxygenNEL.Utils.FileUtil;
using LFileUtil = Codexus.Game.Launcher.Utils.FileUtil;

namespace OxygenNEL
{
    public partial class App : Application
    {
        private Window? _window;
        public static Window? MainWindow { get; private set; }
        public static Task? InitializationTask { get; private set; }

        public App()
        {
            InitializeComponent();
            this.UnhandledException += App_UnhandledException;
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            ConfigureLogger();
            _window = new MainWindow();
            MainWindow = _window;
            _window.Activate();
            InitializationTask = Task.Run(async () =>
            {
                try
                {
                    LFileUtil.CreateDirectorySafe(PathUtil.CustomModsPath);
                    AppState.Debug = Debug.Get();
                    AppState.AutoDisconnectOnBan = SettingManager.Instance.Get().AutoDisconnectOnBan;

                    KillVeta.Run();
                    AppState.Services = await CreateServicesAsync();
                    InternalQuery.Initialize();

                    await InitializeSystemComponentsAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "应用初始化失败");
                }
            });
        }
        
        void ConfigureLogger()
        {
            try
            {
                var baseDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
                var logDir = Path.Combine(baseDir, "logs");
                Directory.CreateDirectory(logDir);
                var fileName = DateTime.Now.ToString("yyyy-MM-dd-HHmm-ss") + ".log";
                var filePath = Path.Combine(logDir, fileName);
                var isDebug = Manager.SettingManager.Instance.Get().Debug;
                var logConfig = new LoggerConfiguration()
                    .MinimumLevel.Is(isDebug ? Serilog.Events.LogEventLevel.Debug : Serilog.Events.LogEventLevel.Information)
                    .WriteTo.Console()
                    .WriteTo.Sink(UiLog.CreateSink())
                    .WriteTo.File(filePath);
                Log.Logger = logConfig.CreateLogger();
                Log.Information("日志已创建: {filePath}, Debug={isDebug}", filePath, isDebug);
            }
            catch (Exception ex)
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Console()
                    .WriteTo.Sink(UiLog.CreateSink())
                    .CreateLogger();
                Log.Error(ex, "日志初始化失败");
            }
        }

        static async Task InitializeSystemComponentsAsync()
        {
            var pluginDir = Utils.FileUtil.GetPluginDirectory();
            Directory.CreateDirectory(pluginDir);
            UserManager.Instance.ReadUsersFromDisk();
            Interceptor.EnsureLoaded();
            PacketManager.Instance.RegisterPacketFromAssembly(typeof(App).Assembly);
            PacketManager.Instance.RegisterPacketFromAssembly(typeof(IrcManager).Assembly);
            PacketManager.Instance.EnsureRegistered();
            RegisterIrcHandler();
            try
            {
                PluginManager.Instance.EnsureUninstall();
                PluginManager.Instance.LoadPlugins(pluginDir);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "插件加载失败");
            }
            await Task.CompletedTask;
        }

        static void RegisterIrcHandler()
        {
            IrcManager.Enabled = SettingManager.Instance.Get().IrcEnabled;
            IrcEventHandler.Register(() => AuthManager.Instance.Token ?? "");
        }

        async Task<Services> CreateServicesAsync()
        {
            var yggdrasil = new StandardYggdrasil(new YggdrasilData
            {
                LauncherVersion = WPFLauncher.GetLatestVersionAsync().GetAwaiter().GetResult(),
                Channel = "netease",
                CrcSalt = await AuthManager.Instance.GetCrcSaltAsyncIfNeeded(default)
            });
            return new Services(yggdrasil);
        }
        
        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            try
            {
                Log.Error(e.Exception, "未处理异常");
            }
            catch { }
            e.Handled = true;
        }
    }
}
