/*
<OxygenNEL>
Copyright (C) <2025>  <OxygenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.Text.Json;
using Codexus.Cipher.Protocol;
using Codexus.Cipher.Utils.Exception;
using OxygenNEL.Entities.Web.NEL;
using OxygenNEL.Manager;
using OxygenNEL.type;
using Serilog;

namespace OxygenNEL.Handlers.Login
{
    public class Login4399
    {
        public object Execute(string account, string password, string captchaIdentifier = null, string captcha = null)
        {
            try
            {
                InternalQuery.Initialize();
                using Pc4399 pc = new Pc4399();
                string cookieJson = pc.LoginWithPasswordAsync(account, password, captchaIdentifier, captcha).GetAwaiter().GetResult();
                
                if (AppState.Debug) Log.Information("4399 Login cookieJson length: {Length}", cookieJson?.Length ?? 0);
                if (string.IsNullOrWhiteSpace(cookieJson))
                {
                    return new { type = "login_4399_error", message = "cookie empty" };
                }
                
                var (authOtp, channel) = AppState.X19.LoginWithCookie(cookieJson);
                Log.Information("Login with password: {UserId} Channel: {LoginChannel}", authOtp.EntityId, channel);
                Log.Debug("User details: {UserId},{Token}", authOtp.EntityId, authOtp.Token);
                
                UserManager.Instance.AddUserToMaintain(authOtp);
                UserManager.Instance.AddUser(new Entities.Web.EntityUser
                {
                    UserId = authOtp.EntityId,
                    Authorized = true,
                    AutoLogin = false,
                    Channel = channel,
                    Type = "password",
                    Details = JsonSerializer.Serialize(new EntityPasswordRequest
                    {
                        Account = account,
                        Password = password
                    })
                });
                
                var list = new System.Collections.ArrayList();
                list.Add(new { type = "Success_login", entityId = authOtp.EntityId, channel });
                var items = GetAccount.GetAccountItems();
                list.Add(new { type = "accounts", items });
                return list;
            }
            catch (CaptchaException ex)
            {
                Log.Warning(ex, "4399 登录需要验证码. account={Account}", account ?? string.Empty);
                Log.Information("4399 登录需要验证码: {Message}", ex.Message);
                return HandleCaptchaRequired(account, password);
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                var lower = msg.ToLowerInvariant();
                Log.Error(ex, "4399 登录失败. account={Account}", account ?? string.Empty);
                Log.Information("4399 登录失败信息: {Message}", ex.Message);
                
                if (lower.Contains("unactived"))
                {
                    return new { type = "login_4399_error", message = "账号未激活，请先使用官方启动器进入游戏一次" };
                }
                if (lower.Contains("parameter") && lower.Contains("'s'"))
                {
                    return HandleCaptchaRequired(account, password);
                }
                if (lower.Contains("sessionid"))
                {
                    return new { type = "login_4399_error", message = "账号或密码错误" };
                }
                return new { type = "login_4399_error", message = string.IsNullOrEmpty(msg) ? "登录失败" : msg };
            }
        }

        private object HandleCaptchaRequired(string account, string password)
        {
            var captchaSid = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N").Substring(0, 8);
            var url = "https://ptlogin.4399.com/ptlogin/captcha.do?captchaId=" + captchaSid;
            return new { type = "captcha_required", account, password, captchaIdentifier = captchaSid, captchaUrl = url };
        }
    }
}
