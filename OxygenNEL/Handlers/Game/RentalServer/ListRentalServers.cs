/*
<OxygenNEL>
Copyright (C) <2025>  <OxygenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System.Linq;
using OxygenNEL.Manager;
using OxygenNEL.type;
using OxygenNEL.Entities.Web.RentalGame;
using Serilog;

namespace OxygenNEL.Handlers.Game.RentalServer;

public class ListRentalServers
{
    public ListRentalServersResult Execute(int offset, int limit)
    {
        var user = UserManager.Instance.GetLastAvailableUser();
        if (user == null)
        {
            return new ListRentalServersResult { NotLogin = true };
        }

        try
        {
            var result = AppState.X19.GetRentalGameList(user.UserId, user.AccessToken, offset);
            
            var items = result.Data?.Select(item => new RentalServerItem
            {
                EntityId = item.EntityId,
                Name = string.IsNullOrEmpty(item.ServerName) ? item.Name : item.ServerName,
                PlayerCount = (int)item.PlayerCount,
                HasPassword = item.HasPassword == "1",
                McVersion = item.McVersion
            }).ToList() ?? new();
            
            var hasMore = items.Count >= limit;
            return new ListRentalServersResult { Success = true, Items = items, HasMore = hasMore };
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "获取租赁服列表失败");
            return new ListRentalServersResult { Success = false, Message = "获取失败" };
        }
    }
}
