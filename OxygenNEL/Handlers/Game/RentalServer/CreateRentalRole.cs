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

public class CreateRentalRole
{
    public RentalServerRolesResult Execute(string serverId, string roleName)
    {
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null)
        {
            return new RentalServerRolesResult { NotLogin = true };
        }
        if (string.IsNullOrWhiteSpace(serverId) || string.IsNullOrWhiteSpace(roleName))
        {
            return new RentalServerRolesResult { Success = false, Message = "参数错误" };
        }
        try
        {
            var result = AppState.X19.AddRentalGameRole(last.UserId, last.AccessToken, serverId, roleName);
            
            if (result.Code != 0)
            {
                Log.Error("[RentalServer] 创建角色失败: {Message}", result.Message);
                return new RentalServerRolesResult { Success = false, Message = result.Message ?? "创建失败" };
            }

            var entities = AppState.X19.GetRentalGameRolesList(last.UserId, last.AccessToken, serverId);
            var items = entities.Data.Select(r => new RentalRoleItem { Id = r.Name, Name = r.Name }).ToList();
            return new RentalServerRolesResult { Success = true, ServerId = serverId, Items = items };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[RentalServer] 创建租赁服角色失败: serverId={ServerId}", serverId);
            return new RentalServerRolesResult { Success = false, Message = "创建失败" };
        }
    }
}
