using NonSteamShortcuts.DataModels;
using NonSteamShortcuts.Exceptions;
using NonSteamShortcuts.Extensions;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NonSteamShortcuts.Logic
{
    internal class ShortcutUtility
    {
        public static IList<Shortcut> ReadShortcuts(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new List<Shortcut>();
            }

            var shortcuts = new List<Shortcut>();

            var shortcutsValues = new KeyValue();
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                if (!shortcutsValues.TryReadAsBinary(fileStream))
                {
                    throw new NonSteamShortcutsException(string.Format(ResourceProvider.GetString("LOCNSSErrorReadingShortcuts"), filePath));
                }
            }
            //iterate list of shortcuts
            foreach (var shortcutNode in shortcutsValues.Children)
            {
                var shortcut = new Shortcut
                {
                    AppName = shortcutNode.Children.Where(kv => kv.Name == "AppName").FirstOrDefault()?.AsString().Replace("\"", string.Empty),
                    Icon = shortcutNode.Children.Where(kv => kv.Name == "icon").FirstOrDefault()?.AsString().Replace("\"", string.Empty),
                    Exe = shortcutNode.Children.Where(kv => kv.Name == "Exe").FirstOrDefault()?.AsString().Replace("\"", string.Empty),
                    LaunchOptions = shortcutNode.Children.Where(kv => kv.Name == "LaunchOptions").FirstOrDefault()?.AsString().Replace("\"", string.Empty),
                    StartDir = shortcutNode.Children.Where(kv => kv.Name == "StartDir").FirstOrDefault()?.AsString().Replace("\"", string.Empty),
                };
                shortcuts.Add(shortcut);
            }
            return shortcuts;
        }

        public static void SaveShortcuts(IList<Shortcut> shortcuts, string filePath)
        {
            if(shortcuts.Count == 0)
            {
                return;
            }
            var shortcutsValues = new KeyValue("shortcuts");
            var counter = 0;
            foreach (var shortcut in shortcuts)
            {
                var shortcutKeyValue = new KeyValue(counter.ToString());
                shortcutKeyValue.Children.Add(new KeyValue("AppName", shortcut.AppName));
                shortcutKeyValue.Children.Add(new KeyValue("icon", $"\"{shortcut.Icon}\""));
                shortcutKeyValue.Children.Add(new KeyValue("Exe", $"\"{shortcut.Exe}\""));
                shortcutKeyValue.Children.Add(new KeyValue("LaunchOptions", shortcut.LaunchOptions));
                shortcutKeyValue.Children.Add(new KeyValue("StartDir", $"\"{shortcut.StartDir}\""));
                shortcutKeyValue.Children.Add(new KeyValue("ShortcutPath", shortcut.ShortcutPath));
                shortcutKeyValue.Children.Add(new KeyValue("OpenVR", shortcut.OpenVr));
                shortcutKeyValue.Children.Add(new KeyValue("LastPlayTime", shortcut.LastPlayTime));
                shortcutKeyValue.Children.Add(new KeyValue("IsHidden", shortcut.IsHidden));
                shortcutKeyValue.Children.Add(new KeyValue("DevkitGameID", shortcut.DevKitGameId));
                shortcutKeyValue.Children.Add(new KeyValue("Devkit", shortcut.DevKit));
                shortcutKeyValue.Children.Add(new KeyValue("AllowOverlay", shortcut.AllowOverlay));
                shortcutKeyValue.Children.Add(new KeyValue("AllowDesktopConfig", shortcut.AllowDesktopConfig));
                shortcutKeyValue.Children.Add(new KeyValue("tags"));
                shortcutsValues.Children.Add(shortcutKeyValue);
                ++counter;
            }
            shortcutsValues.SaveToFile(filePath, true);
        }

        public static Shortcut CreateShortcutFromPlayniteGame(Game game, GameAction playAction, IPlayniteAPI playniteAPI, ILogger logger,
            List<string> gamesWithUrl, List<string> gamesSkippedBadEmulator, List<string> gamesWithScript)
        {
            Shortcut shortcut = null;
            var expandedPlayAction = playniteAPI.ExpandGameVariables(game, playAction);
            switch (expandedPlayAction.Type)
            {
                case GameActionType.Emulator:
                    shortcut = CreateShortcutFromEmulator(game, expandedPlayAction, playniteAPI, gamesSkippedBadEmulator);
                    break;
                case GameActionType.File:
                    shortcut = CreateShortcutFromFile(game, expandedPlayAction);
                    break;
                case GameActionType.URL:
                    logger.Warn($"Game {game.Name} has a URL as play action");
                    gamesWithUrl.Add(game.Name);
                    shortcut = CreateShortcutFromUrl(game, expandedPlayAction);
                    break;
                case GameActionType.Script:
                    logger.Warn($"Game {game.Name} has a script as play action");
                    gamesWithScript.Add(game.Name);
                    return null;
            }
            if (shortcut != null && !string.IsNullOrWhiteSpace(game.Icon))
            {
                shortcut.Icon = playniteAPI.Database.GetFullFilePath(game.Icon);
            }
            if (shortcut != null && expandedPlayAction.Type != GameActionType.URL && string.IsNullOrWhiteSpace(shortcut.StartDir))
            {
                shortcut.StartDir = new FileInfo(shortcut.Exe).Directory.FullName;
                if (!File.Exists(shortcut.Exe))
                {
                    shortcut.Exe = Path.Combine(shortcut.StartDir, shortcut.Exe);
                }
            }
            return shortcut;
        }

        private static Shortcut CreateShortcutFromEmulator(Game game, GameAction playAction, IPlayniteAPI playniteAPI, List<string> gamesSkippedBadEmulator)
        {
            var emulator = playniteAPI.Database.Emulators.Get(playAction.EmulatorId);
            var profile = emulator.GetProfile(playAction.EmulatorProfileId);
            if (!(profile is CustomEmulatorProfile customEmulatorProfile))
            {
                gamesSkippedBadEmulator.Add(game.Name);
                return null;
            }
            var expandedProfile = customEmulatorProfile.ExpandVariables(game, emulator.InstallDir, string.Empty);
            var shortcut = new Shortcut();
            shortcut.StartDir = expandedProfile.WorkingDirectory;
            shortcut.Exe = expandedProfile.Executable;
            shortcut.AppName = game.Name;
            shortcut.LaunchOptions = expandedProfile.Arguments ?? string.Empty;
            if (!playAction.OverrideDefaultArgs && !string.IsNullOrWhiteSpace(playAction.AdditionalArguments))
            {
                shortcut.LaunchOptions += $" {playAction.AdditionalArguments}";
            }
            return shortcut;
        }

        private static Shortcut CreateShortcutFromFile(Game game, GameAction playAction)
        {
            var shortcut = new Shortcut();
            shortcut.StartDir = playAction.WorkingDir;
            shortcut.Exe = playAction.Path;
            shortcut.AppName = game.Name;
            shortcut.LaunchOptions = playAction.Arguments ?? string.Empty;
            return shortcut;
        }

        private static Shortcut CreateShortcutFromUrl(Game game, GameAction playAction)
        {
            var shortcut = new Shortcut();
            shortcut.StartDir = string.Empty;
            shortcut.Exe = playAction.Path;
            shortcut.AppName = game.Name;
            shortcut.LaunchOptions = string.Empty;
            return shortcut;
        }
    }
}
