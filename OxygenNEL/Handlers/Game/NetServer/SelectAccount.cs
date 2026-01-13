/*
<OxygenNEL>
Copyright (C) <2025>  <OxygenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using OxygenNEL.type;
using OxygenNEL.Manager;

namespace OxygenNEL.Handlers.Game.NetServer;

public class SelectAccount
{
    public object Execute(string entityId)
    {
        if (string.IsNullOrWhiteSpace(entityId)) return new { type = "notlogin" };
        var available = UserManager.Instance.GetAvailableUser(entityId);
        if (available == null) return new { type = "notlogin" };
        available.LastLoginTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return new { type = "selected_account", entityId };
    }
}
