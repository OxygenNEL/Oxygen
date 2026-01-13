/*
<OxygenNEL>
Copyright (C) <2025>  <OxygenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/

using OxygenNEL.Manager;

namespace OxygenNEL.Utils;

public class Debug
{
    public static bool Get()
    {
        try
        {
            var s = SettingManager.Instance.Get();
            return s?.Debug ?? false;
        }
        catch{}
        return false;
    }
}
