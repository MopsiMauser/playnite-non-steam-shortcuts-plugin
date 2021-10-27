using NonSteamShortcuts.DataModels;
using NonSteamShortcuts.Exceptions;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                //iterate list of shortcuts
                foreach(var shortcutNode in shortcutsValues.Children)
                {
                    var shortcut = new Shortcut
                    {
                        AppName = shortcutNode.Children.Where(kv => kv.Name == "appname").FirstOrDefault()?.AsString(),
                        Icon = shortcutNode.Children.Where(kv => kv.Name == "icon").FirstOrDefault()?.AsString(),
                        Exe = shortcutNode.Children.Where(kv => kv.Name == "exe").FirstOrDefault()?.AsString(),
                        LaunchOptions = shortcutNode.Children.Where(kv => kv.Name == "launchoptions").FirstOrDefault()?.AsString(),
                        StartDir = shortcutNode.Children.Where(kv => kv.Name == "startdir").FirstOrDefault()?.AsString(),
                    };
                    shortcuts.Add(shortcut);
                }
            }
            
            return shortcuts;
        }

        public static void SaveShortcuts(IList<Shortcut> shortcuts, string filePath)
        {
            var shortcutsValues = new KeyValue("shortcuts");
            foreach(var shortcut in shortcuts)
            {
                var shortcutKeyValue = new KeyValue();
                shortcutKeyValue.Children.Add(new KeyValue("appname", shortcut.AppName));
                shortcutKeyValue.Children.Add(new KeyValue("icon", shortcut.Icon));
                shortcutKeyValue.Children.Add(new KeyValue("exe", shortcut.Exe));
                shortcutKeyValue.Children.Add(new KeyValue("launchoptions", shortcut.LaunchOptions));
                shortcutKeyValue.Children.Add(new KeyValue("startdir", shortcut.StartDir));
                shortcutKeyValue.Children.Add(new KeyValue("shortcutpath", shortcut.ShortcutPath));
                shortcutKeyValue.Children.Add(new KeyValue("openvr", shortcut.OpenVr));
                shortcutKeyValue.Children.Add(new KeyValue("lastplaytime", shortcut.LastPlayTime));
                shortcutKeyValue.Children.Add(new KeyValue("ishidden", shortcut.IsHidden));
                shortcutKeyValue.Children.Add(new KeyValue("devkitgameid", shortcut.DevKitGameId));
                shortcutKeyValue.Children.Add(new KeyValue("devkit", shortcut.DevKit));
                shortcutKeyValue.Children.Add(new KeyValue("allowoverlay", shortcut.AllowOverlay));
                shortcutKeyValue.Children.Add(new KeyValue("allowdesktopconfig", shortcut.AllowDesktopConfig));
                shortcutsValues.Children.Add(shortcutKeyValue);
            }
            shortcutsValues.SaveToFile(filePath, true);
        }

        public static Shortcut CreateShortcutFromPlayniteGame(Game game, GameAction playAction, ILogger logger, List<string> gamesWithUrl, List<string> gamesSkippedBadEmulator)
        {
            var shortcut = new Shortcut();
            if (playAction.Type == GameActionType.URL)
            {
                logger.Warn($"Game {game.Name} has a URL as play action");
                gamesWithUrl.Add(game.Name);
            }
            return shortcut;
        }
    }
}
