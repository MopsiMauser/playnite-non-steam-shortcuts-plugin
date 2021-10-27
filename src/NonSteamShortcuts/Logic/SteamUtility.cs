using Microsoft.Win32;
using NonSteamShortcuts.DataModels;
using System.IO;
using System.Linq;

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
            return string.Empty;
        }
    }
}
