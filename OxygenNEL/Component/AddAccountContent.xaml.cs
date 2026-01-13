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
using System.Threading.Tasks;
using System.Linq;
using OxygenNEL.Handlers.Login;
using OxygenNEL.Manager;
using Microsoft.UI.Xaml.Media.Imaging;

namespace OxygenNEL.Component
{
        public sealed partial class AddAccountContent : UserControl
        {
            public event Action AutoLoginSucceeded;
            public event Action ConfirmRequested;
            public event Action CancelRequested;
            public event Action<string,string,string,string> CaptchaRequired;
            public Func<string, System.Threading.Tasks.Task<string>> CaptchaInputRequested;
        private string _pc4399SessionId;
        public AddAccountContent()
        {
            InitializeComponent();
            var mode = SettingManager.Instance.Get().ThemeMode?.Trim().ToLowerInvariant() ?? "system";
            ElementTheme t = ElementTheme.Default;
            if (mode == "light") t = ElementTheme.Light;
            else if (mode == "dark") t = ElementTheme.Dark;
            this.RequestedTheme = t;
        }

        public string SelectedType => (AccountTypePivot.SelectedItem as PivotItem)?.Header?.ToString();

        public string CookieText => CookieInput.Text;
        
        public string Pc4399User => Pc4399Username.Text;
        public string Pc4399Pass => Pc4399Password.Password;
        private string _pc4399CaptchaUrl;
        public string Pc4399SessionId => _pc4399SessionId;
        public string Pc4399CaptchaUrl => _pc4399CaptchaUrl;
        public string Pc4399Captcha => Pc4399CaptchaInput?.Text ?? string.Empty;
        
        public string NeteaseMail => NeteaseEmail.Text;
        public string NeteasePass => NeteasePassword.Password;

        public bool TryDetectSuccess(object result)
        {
            if (result == null) return false;
            var tProp = result.GetType().GetProperty("type");
            if (tProp != null)
            {
                var tVal = tProp.GetValue(result) as string;
                if (string.Equals(tVal, "login_error", StringComparison.OrdinalIgnoreCase))
                {
                    var mProp = result.GetType().GetProperty("message");
                    var msg = mProp?.GetValue(result) as string ?? "登录失败";
                    NotificationHost.ShowGlobal(msg, ToastLevel.Error);
                    return false;
                }
                if (string.Equals(tVal, "login_4399_error", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(tVal, "login_x19_error", StringComparison.OrdinalIgnoreCase))
                {
                    var mProp = result.GetType().GetProperty("message");
                    var msg = mProp?.GetValue(result) as string ?? "登录失败";
                    NotificationHost.ShowGlobal(msg, ToastLevel.Error);
                    return false;
                }
            }
            if (result is System.Collections.IEnumerable en)
            {
                foreach (var item in en)
                {
                    var p = item?.GetType().GetProperty("type");
                    var v = p != null ? p.GetValue(item) as string : null;
                    if (string.Equals(v, "Success_login", StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            if (GetAccount.HasAuthorizedUser()) return true;
            return false;
        }
    }
}
