using System.Threading.Tasks;

namespace OxygenNEL.Core.Utils;

public static class CrcSalt
{
    private const string Salt = "85CFCD08EEF5623718341B35BAA06813";
    public static string GetCached() => Salt;

}
