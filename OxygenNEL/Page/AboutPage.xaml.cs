/*
<OxygenNEL>
Copyright (C) <2025>  <OxygenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using Microsoft.UI.Xaml.Controls;
using OxygenNEL.type;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using System;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using System.Reflection;


namespace OxygenNEL.Page
{
    public sealed partial class AboutPage : Microsoft.UI.Xaml.Controls.Page
    {
        public static string PageTitle => "关于";
        
        public string AppVersion => AppInfo.AppVersion;
        public string GithubUrl => AppInfo.GithubUrL;
        public string QQGroup => AppInfo.QQGroup;

        public List<Contributor> Contributors { get; } = new List<Contributor>
        {
            new Contributor { Name = "FandMC", Role = "Owner", ColorBrush = new SolidColorBrush(Color.FromArgb(255, 0, 120, 215)) },
            new Contributor { Name = "OxygenNEL Team", Role = "Developer", ColorBrush = new SolidColorBrush(Color.FromArgb(255, 40, 167, 69)) },
        };

        public AboutPage()
        {
            InitializeComponent();
        }

        private async void License_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://www.gnu.org/licenses/gpl-3.0-standalone.html"));
        }

        private async void Github_Click(object sender, RoutedEventArgs e)
        {
             await Windows.System.Launcher.LaunchUriAsync(new Uri(AppInfo.GithubUrL));
        }

        private async void QQGroup_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://qm.qq.com/q/JOFAVmoScY"));
        }
    }

    public class Contributor
    {
        public string Name { get; set; }
        public string Role { get; set; }
        public SolidColorBrush ColorBrush { get; set; }
    }
}
