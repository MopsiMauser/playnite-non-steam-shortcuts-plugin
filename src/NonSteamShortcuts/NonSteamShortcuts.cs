using Microsoft.Win32;
using NonSteamShortcuts.Logic;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NonSteamShortcuts
{
    public class NonSteamShortcuts : GenericPlugin
    {
        private static readonly ILogger _logger = LogManager.GetLogger();

        private NonSteamShortcutsSettingsViewModel SettingsViewModel { get; set; }

        public override Guid Id { get; } = Guid.Parse("42a413b9-9433-42a5-b286-84fea0c34f71");

        public NonSteamShortcuts(IPlayniteAPI api) : base(api)
        {
            _logger.Debug("NonSteamShortcuts plugin initialization");
            SettingsViewModel = new NonSteamShortcutsSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            _logger.Debug("NonSteamShortcuts retrieve ISettings");
            if (!string.IsNullOrWhiteSpace(SettingsViewModel.Settings.SteamUserProfilePath))
            {
                return SettingsViewModel;
            }

            _logger.Debug("NonSteamShortcuts try to automatically determine Steam user profile folder");
            var steamRegistryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Valve\\Steam", false);
            if (steamRegistryKey == null)
            {
                return SettingsViewModel;
            }

            var steamPath = steamRegistryKey.GetValue("SteamPath")?.ToString().Replace('/', '\\');
            if (!Directory.Exists(steamPath))
            {
                return SettingsViewModel;
            }
            var steamUsersRegistryKey = steamRegistryKey.OpenSubKey("Users", false);
            if (steamUsersRegistryKey == null)
            {
                return SettingsViewModel;
            }

            var userSubkeyName = steamUsersRegistryKey.GetSubKeyNames().FirstOrDefault();
            if (string.IsNullOrEmpty(userSubkeyName))
            {
                return SettingsViewModel;
            }

            SettingsViewModel.Settings.SteamUserProfilePath = Path.Combine(steamPath, "userdata", userSubkeyName);
            return SettingsViewModel;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            _logger.Debug("NonSteamShortcuts retrieve settings view");
            return new NonSteamShortcutsSettingsView(SettingsViewModel);
        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            var menuItem = new GameMenuItem
            {
                Action = CreateShortcuts,
                Description = ResourceProvider.GetString("LOCNSSCreateShortcut")
            };
            return Enumerable.Repeat(menuItem, 1);
        }

        private void CreateShortcuts(GameMenuItemActionArgs args)
        {
            if(args.Games.Count == 0)
            {
                return;
            }
            var steamUserProfilePath = SettingsViewModel.Settings.SteamUserProfilePath;
            if (!Directory.Exists(steamUserProfilePath))
            {
                _logger.Error($"Path {steamUserProfilePath} does not exist");
                PlayniteApi.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCNSSSteamUserPathDoesNotExist"), ResourceProvider.GetString("LOCNSSErrorCaption"));
                return;
            }
            var shortcutFile = Path.Combine(steamUserProfilePath, "config", "shortcuts.vdf");
            try
            {
                File.Copy(shortcutFile, shortcutFile + ".bak");
            }
            catch (Exception ex)
            {
                PlayniteApi.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCNSSErrorCreatingBackup"), ResourceProvider.GetString("LOCNSSErrorCaption"));
                _logger.Error(ex, ex.Message);
                return;
            }
            var shortcutUtility = new ShortcutUtility();
            var shortcuts = shortcutUtility.ReadShortcuts(shortcutFile);

            foreach(var game in args.Games)
            {
                
            }

            shortcutUtility.SaveShortcuts(shortcuts, shortcutFile);
        }
    }
}