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
using System.Text.Json;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using OxygenNEL.type;

namespace OxygenNEL.Manager;

public class SettingManager
{
    private const string SettingsFilePath = "setting.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly Lazy<SettingManager> _lazy = new(() => new SettingManager());
    public static SettingManager Instance => _lazy.Value;

    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private SettingData _settings = new();

    private SettingManager()
    {
        ReadFromDisk();
        _settings.PropertyChanged += (_, _) => SaveToDisk();
    }

    public SettingData Get() => _settings;

    public ElementTheme GetAppTheme()
    {
        var mode = _settings.ThemeMode?.Trim().ToLowerInvariant() ?? "system";
        return mode switch { "light" => ElementTheme.Light, "dark" => ElementTheme.Dark, _ => ElementTheme.Default };
    }

    public void ApplyTheme(ContentDialog dialog) => dialog.RequestedTheme = GetAppTheme();
    public void ApplyTheme(FrameworkElement element) => element.RequestedTheme = GetAppTheme();

    public string? GetCopyIpText(string? ip, int port)
    {
        if (!_settings.AutoCopyIpOnStart || string.IsNullOrWhiteSpace(ip)) return null;
        return port > 0 ? $"{ip}:{port}" : ip;
    }

    public void ReadFromDisk()
    {
        try
        {
            if (!File.Exists(SettingsFilePath)) { _settings = new(); return; }
            var obj = JsonSerializer.Deserialize<SettingData>(File.ReadAllText(SettingsFilePath)) ?? new();
            _settings = obj;
            _settings.PropertyChanged += (_, _) => SaveToDisk();
            Log.Information("设置已加载");
        }
        catch (Exception ex) { Log.Error(ex, "加载设置失败"); _settings = new(); }
    }

    public void SaveToDisk()
    {
        if (!_saveLock.Wait(0)) return;
        try { File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(_settings, JsonOptions)); }
        catch (Exception ex) { Log.Error(ex, "保存设置失败"); }
        finally { _saveLock.Release(); }
    }
}
