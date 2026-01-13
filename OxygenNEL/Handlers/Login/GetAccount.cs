/*
<OxygenNEL>
Copyright (C) <2025>  <OxygenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System.Collections.Generic;
using System.Linq;
using OxygenNEL.Manager;

namespace OxygenNEL.Handlers.Login;

public class AccountItem
{
    public string EntityId { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = "offline";
    public string Alias { get; set; } = string.Empty;
}

public class GetAccount
{
    public static List<AccountItem> GetAccountList()
    {
        var users = UserManager.Instance.GetUsersNoDetails();
        return users.Select(u => new AccountItem
        {
            EntityId = u.UserId,
            Channel = u.Channel,
            Status = u.Authorized ? "online" : "offline",
            Alias = u.Alias
        }).ToList();
    }

    public static object[] GetAccountItems()
    {
        var users = UserManager.Instance.GetUsersNoDetails();
        return users.Select(u => new { entityId = u.UserId, channel = u.Channel, status = u.Authorized ? "online" : "offline", alias = u.Alias}).ToArray();
    }

    public static bool HasAuthorizedUser()
    {
        var users = UserManager.Instance.GetUsersNoDetails();
        return users.Any(u => u.Authorized);
    }
}
