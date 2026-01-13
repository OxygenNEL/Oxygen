/*
<OxygenNEL>
Copyright (C) <2025>  <OxygenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using Codexus.Cipher.Protocol;
using Codexus.OpenSDK.Yggdrasil;
using Codexus.OpenSDK.Entities.Yggdrasil;
using OxygenNEL.Manager;

namespace OxygenNEL.type;

internal class Services(
    StandardYggdrasil Yggdrasil
)
{ public StandardYggdrasil Yggdrasil { get; private set; } = Yggdrasil;

    public void RefreshYggdrasil()
    {
        var salt = AuthManager.Instance.CachedSalt;
        Yggdrasil = new StandardYggdrasil(new YggdrasilData
        {
            LauncherVersion = WPFLauncher.GetLatestVersionAsync().GetAwaiter().GetResult(),
            Channel = "netease",
            CrcSalt = salt
        });
    }
}