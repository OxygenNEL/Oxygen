/*
<OxygenNEL>
Copyright (C) <2025>  <OxygenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.IO;
using System.Threading.Tasks;
using Codexus.Cipher.Entities;
using Codexus.Cipher.Entities.WPFLauncher.NetGame.Texture;
using Codexus.Cipher.Protocol;
using Codexus.Cipher.Utils;
using Codexus.Game.Launcher.Entities;
using Codexus.Game.Launcher.Services.Java;
using Codexus.Game.Launcher.Utils;
using OxygenNEL.Manager;
using OxygenNEL.type;
using OxygenNEL.Entities.Web.NetGame;
using Serilog;

namespace OxygenNEL.Handlers.Game.NetServer;

public class LaunchWhiteGame
{
    private readonly IProgress<EntityProgressUpdate>? _progress;

    public LaunchWhiteGame(IProgress<EntityProgressUpdate>? progress = null)
    {
        _progress = progress;
    }

    public async Task<JoinGameResult> Execute(string accountId, string serverId, string serverName, string roleId)
    {
        if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(serverId) || string.IsNullOrWhiteSpace(roleId))
            return new JoinGameResult { Success = false, Message = "参数错误" };

        var available = UserManager.Instance.GetAvailableUser(accountId);
        if (available == null)
            return new JoinGameResult { NotLogin = true };

        try
        {
            PathUtil.EnsureDirectoriesExist();
            var wpf = AppState.X19;
            var details = wpf.QueryNetGameDetailById(available.UserId, available.AccessToken, serverId);
            var address = wpf.GetNetGameServerAddress(available.UserId, available.AccessToken, serverId);

            var serverIp = address.Data!.Ip;
            var serverPort = address.Data!.Port;
            if (serverPort <= 0 && details.Data != null && !string.IsNullOrWhiteSpace(details.Data.ServerAddress) && details.Data.ServerPort > 0)
            {
                serverIp = details.Data.ServerAddress;
                serverPort = details.Data.ServerPort;
            }
            if (serverPort <= 0) serverPort = 25565;

            var version = details.Data!.McVersionList[0];
            var gameVersion = GameVersionUtil.GetEnumFromGameVersion(version.Name);
            
            var versionStr = GameVersionUtil.GetGameVersionFromEnum(gameVersion);
            var jsonPath = Path.Combine(PathUtil.GameBasePath, ".minecraft", "versions", versionStr, versionStr + ".json");
            Log.Debug("白端: version.Name={Name}, gameVersion={GV}, jsonPath={Path}, exists={Exists}", 
                version.Name, gameVersion, jsonPath, File.Exists(jsonPath));

            var request = new EntityLaunchGame
            {
                GameName = serverName,
                GameId = serverId,
                RoleName = roleId,
                UserId = available.UserId,
                ClientType = EnumGameClientType.Java,
                GameType = EnumGType.ServerGame,
                GameVersionId = GameVersionConverter.Convert(gameVersion),
                GameVersion = version.Name,
                AccessToken = available.AccessToken,
                ServerIp = serverIp,
                ServerPort = serverPort,
                MaxGameMemory = 4096,
                LoadCoreMods = true
            };

            var launcher = LauncherService.CreateLauncher(request, available.AccessToken, wpf, wpf.MPay.GameVersion, _progress ?? new Progress<EntityProgressUpdate>());
            GameManager.Instance.AddLauncher(launcher);
            await InterConn.GameStart(available.UserId, available.AccessToken, serverId);

            Log.Information("白端启动中: ServerId={ServerId}, Role={Role}", serverId, roleId);
            return new JoinGameResult { Success = true, Message = "启动中" };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "白端启动失败");
            return new JoinGameResult { Success = false, Message = ex.Message };
        }
    }
}
