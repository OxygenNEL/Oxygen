/*
<OxygenNEL>
Copyright (C) <2025>  <OxygenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.Linq;
using OxygenNEL.type;
using OxygenNEL.Manager;
using OxygenNEL.Entities.Web.RentalGame;
using Serilog;

namespace OxygenNEL.Handlers.Game.RentalServer;

public class OpenRentalServer
{
    public RentalServerRolesResult Execute(string serverId)
    {
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null)
        {
            return new RentalServerRolesResult { NotLogin = true };
        }
        if (string.IsNullOrWhiteSpace(serverId))
        {
            return new RentalServerRolesResult { Success = false, Message = "参数错误" };
        }
        try
        {
            var entities = AppState.X19.GetRentalGameRolesList(last.UserId, last.AccessToken, serverId);
            var items = entities.Data.Select(r => new RentalRoleItem { Id = r.Name, Name = r.Name }).ToList();
            return new RentalServerRolesResult { Success = true, ServerId = serverId, Items = items };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取租赁服角色失败: serverId={ServerId}", serverId);
            return new RentalServerRolesResult { Success = false, Message = "获取失败" };
        }
    }

    public RentalServerRolesResult ExecuteForAccount(string accountId, string serverId)
    {
        if (string.IsNullOrWhiteSpace(accountId)) return new RentalServerRolesResult { Success = false, Message = "参数错误" };
        if (string.IsNullOrWhiteSpace(serverId)) return new RentalServerRolesResult { Success = false, Message = "参数错误" };
        try
        {
            var u = UserManager.Instance.GetAvailableUser(accountId);
            if (u == null) return new RentalServerRolesResult { NotLogin = true };
            var entities = AppState.X19.GetRentalGameRolesList(u.UserId, u.AccessToken, serverId);
            var items = entities.Data.Select(r => new RentalRoleItem { Id = r.Name, Name = r.Name }).ToList();
            return new RentalServerRolesResult { Success = true, ServerId = serverId, Items = items };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取租赁服角色失败: serverId={ServerId}", serverId);
            return new RentalServerRolesResult { Success = false, Message = "获取失败" };
        }
    }
}
