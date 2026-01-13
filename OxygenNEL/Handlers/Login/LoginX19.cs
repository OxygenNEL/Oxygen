/*
<OxygenNEL>
Copyright (C) <2025>  <OxygenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.Collections;
using System.Text.Json;
using Codexus.Cipher.Protocol;
using Codexus.Cipher.Utils.Exception;
using OxygenNEL.Entities.Web;
using OxygenNEL.Manager;
using OxygenNEL.type;
using Serilog;

namespace OxygenNEL.Handlers.Login;

public class LoginX19
{
    public object Execute(string email, string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return new { type = "login_x19_error", message = "邮箱或密码不能为空" };
            }

            InternalQuery.Initialize();
            using var wpf = new WPFLauncher();
            
            var mPayUser = wpf.LoginWithEmailAsync(email, password).GetAwaiter().GetResult();
            var device = wpf.MPay.GetDevice();
            var cookieRequest = WPFLauncher.GenerateCookie(mPayUser, device);
            var (authOtp, channel) = wpf.LoginWithCookie(cookieRequest);

            UserManager.Instance.AddUserToMaintain(authOtp);
            UserManager.Instance.AddUser(new EntityUser
            {
                UserId = authOtp.EntityId,
                Authorized = true,
                AutoLogin = false,
                Channel = channel,
                Type = "netease",
                Details = JsonSerializer.Serialize(new { email, password })
            });

            var list = new ArrayList();
            list.Add(new { type = "Success_login", entityId = authOtp.EntityId, channel });
            var items = GetAccount.GetAccountItems();
            list.Add(new { type = "accounts", items });
            return list;
        }
        catch (VerifyException ve)
        {
            Log.Error(ve, "[LoginX19] 验证失败");
            return new { type = "login_x19_error", message = ve.Message};
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[LoginX19] 登录异常");
            
            if (ex is ObjectDisposedException && ex.Message.Contains("HttpClient"))
            {
                Log.Warning("HttpClient已释放，重置X19服务");
                AppState.ResetX19();
            }
            
            var msg = ex.Message;
            if (msg.Contains("password") || msg.Contains("密码"))
            {
                return new { type = "login_x19_error", message = "邮箱或密码错误" };
            }
            return new { type = "login_x19_error", message = msg.Length == 0 ? "登录失败" : msg };
        }
    }
}
