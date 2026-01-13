/*
<OxygenNEL>
Copyright (C) <2025>  <OxygenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace OxygenNEL.type;

public class SettingData : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private string _themeMode = "system";
    private string _backdrop = "mica";
    private bool _autoCopyIpOnStart;
    private bool _debug;
    private string _autoDisconnectOnBan = "none";
    private bool _ircEnabled = true;
    private bool _socks5Enabled;
    private string _socks5Address = string.Empty;
    private int _socks5Port = 1080;
    private string _socks5Username = string.Empty;
    private string _socks5Password = string.Empty;

    [JsonPropertyName("themeMode")] public string ThemeMode { get => _themeMode; set => Set(ref _themeMode, value); }
    [JsonPropertyName("backdrop")] public string Backdrop { get => _backdrop; set => Set(ref _backdrop, value); }
    [JsonPropertyName("autoCopyIpOnStart")] public bool AutoCopyIpOnStart { get => _autoCopyIpOnStart; set => Set(ref _autoCopyIpOnStart, value); }
    [JsonPropertyName("debug")] public bool Debug { get => _debug; set => Set(ref _debug, value); }
    [JsonPropertyName("autoDisconnectOnBan")] public string AutoDisconnectOnBan { get => _autoDisconnectOnBan; set => Set(ref _autoDisconnectOnBan, value); }
    [JsonPropertyName("ircEnabled")] public bool IrcEnabled { get => _ircEnabled; set => Set(ref _ircEnabled, value); }
    [JsonPropertyName("socks5Enabled")] public bool Socks5Enabled { get => _socks5Enabled; set => Set(ref _socks5Enabled, value); }
    [JsonPropertyName("socks5Address")] public string Socks5Address { get => _socks5Address; set => Set(ref _socks5Address, value); }
    [JsonPropertyName("socks5Port")] public int Socks5Port { get => _socks5Port; set => Set(ref _socks5Port, value); }
    [JsonPropertyName("socks5Username")] public string Socks5Username { get => _socks5Username; set => Set(ref _socks5Username, value); }
    [JsonPropertyName("socks5Password")] public string Socks5Password { get => _socks5Password; set => Set(ref _socks5Password, value); }
}
