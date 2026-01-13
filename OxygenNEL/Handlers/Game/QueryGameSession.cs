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
using OxygenNEL.type;
using OxygenNEL.Entities.Web.NEL;
using OxygenNEL.Manager;

namespace OxygenNEL.Handlers.Game;

public class QueryGameSession
{
    public object Execute()
    {
        List<EntityQueryGameSessions> list = (from interceptor in GameManager.Instance.GetQueryInterceptors()
            select new EntityQueryGameSessions
            {
                Id = "interceptor-" + interceptor.Id,
                ServerName = interceptor.Server,
                Guid = interceptor.Name.ToString(),
                CharacterName = interceptor.Role,
                ServerVersion = interceptor.Version,
                StatusText = "Running",
                ProgressValue = 0,
                Type = "Interceptor",
                LocalAddress = interceptor.LocalAddress
            }).ToList();
        return new { type = "query_game_session", items = list };
    }
}
