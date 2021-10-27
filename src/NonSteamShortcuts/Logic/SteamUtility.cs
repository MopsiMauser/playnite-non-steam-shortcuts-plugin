using Force.Crc32;
using Microsoft.Win32;
using NonSteamShortcuts.DataModels;
using System.IO;
using System.Linq;
using System.Text;

namespace NonSteamShortcuts.Logic
{
    internal class SteamUtility
    {
        public static string GetSteamUserProfilePath()
        {
            var steamRegistryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Valve\\Steam", false);
            if (steamRegistryKey == null)
            {
                return string.Empty;
            }

            var steamPath = steamRegistryKey.GetValue("SteamPath")?.ToString().Replace('/', '\\');
            if (!Directory.Exists(steamPath))
            {
                return string.Empty;
            }
            var steamUsersRegistryKey = steamRegistryKey.OpenSubKey("Users", false);
            if (steamUsersRegistryKey == null)
            {
                return string.Empty;
            }

            var userSubkeyName = steamUsersRegistryKey.GetSubKeyNames().FirstOrDefault();
            if (string.IsNullOrEmpty(userSubkeyName))
            {
                return string.Empty;
            }

            return Path.Combine(steamPath, "userdata", userSubkeyName);
        }

        public static string CreateUrlFromShortcut(Shortcut shortcut)
        {
            var input = "\"" + shortcut.Exe + "\"" + shortcut.AppName;
            var crc32 = Crc32Algorithm.Compute(Encoding.UTF8.GetBytes(input));
            crc32 |= 0x80000000;
            ulong fullId = ((ulong)crc32 << 32) | 0x02000000UL;
            return $"steam://rungameid/{fullId}";
        }
    }
}
