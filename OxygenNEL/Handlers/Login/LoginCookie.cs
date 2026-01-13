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
using Codexus.Cipher.Entities.WPFLauncher;
using OxygenNEL.Manager;

namespace OxygenNEL.Handlers.Login
{
    public class LoginCookie
    {
        public object Execute(string cookie)
        {
            try
            {
                EntityX19CookieRequest req;
                try
                {
                    req = JsonSerializer.Deserialize<EntityX19CookieRequest>(cookie) ?? new EntityX19CookieRequest { Json = cookie};
                }
                catch
                {
                    req = new EntityX19CookieRequest { Json = cookie };
                }
                var (authOtp, channel) = OxygenNEL.type.AppState.X19.LoginWithCookie(req);
                UserManager.Instance.AddUserToMaintain(authOtp);
                UserManager.Instance.AddUser(new OxygenNEL.Entities.Web.EntityUser
                {
                    UserId = authOtp.EntityId,
                    Authorized = true,
                    AutoLogin = false,
                    Channel = channel,
                    Type = "cookie",
                    Details = cookie ?? string.Empty
                }, channel == "netease");
                var list = new System.Collections.ArrayList();
                list.Add(new { type = "Success_login", entityId = authOtp.EntityId, channel });
                var items = GetAccount.GetAccountItems();
                list.Add(new { type = "accounts", items });
                return list;
            }
            catch (ArgumentNullException)
            {
                return new { type = "login_error", message = "当前cookie过期了" };
            }
            catch (Exception ex)
            {
                return new { type = "login_error", message = ex.Message};
            }
        }
    }
}
