/*
<OxygenNEL>
Copyright (C) <2025>  <OxygenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Net.Http;
using System.Text.Json;
using OxygenNEL.type;
using Serilog;

namespace OxygenNEL.Component
{
    public sealed partial class AnnouncementContent : UserControl
    {
        static readonly HttpClient _http = new();

        public AnnouncementContent()
        {
            InitializeComponent();
            this.Loaded += AnnouncementContent_Loaded;
        }

        async void AnnouncementContent_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var resp = await _http.GetAsync("https://api.fandmc.cn/get/announcement");
                var json = await resp.Content.ReadAsStringAsync();
                Log.Information("获取公告返回: {Json}", json);
                
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                {
                    if (root.TryGetProperty("content", out var contentProp))
                    {
                        ContentText.Text = contentProp.GetString() ?? "暂无公告";
                        return;
                    }
                }
                ContentText.Text = "暂无公告";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取公告失败");
                ContentText.Text = "暂无公告";
            }
        }
    }
}
