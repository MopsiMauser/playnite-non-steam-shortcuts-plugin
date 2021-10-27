using NonSteamShortcuts.DataModels;
using NonSteamShortcuts.Logic;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;

namespace NonSteamShortcuts
{
    public class NonSteamShortcuts : GenericPlugin
    {
        private static readonly ILogger _logger = LogManager.GetLogger();

        private NonSteamShortcutsSettingsViewModel SettingsViewModel { get; set; }

        public override Guid Id { get; } = Guid.Parse("42a413b9-9433-42a5-b286-84fea0c34f71");

        private const string ORIGINAL_GAME_ACTION_NAME = "Launch without Steam";

        private const string NON_STEAM_STEAM_SHORTCUT = "Non-Steam Steam Shortcut";

        private readonly Guid SteamPluginGuid = Guid.Parse("CB91DFC9-B977-43BF-8E70-55F46E410FAB");

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
            SettingsViewModel.Settings.SteamUserProfilePath = SteamUtility.GetSteamUserProfilePath();
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
            if (args.Games.Count == 0)
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
            if (!CreateBackup(shortcutFile))
            {
                return;
            }

            var shortcuts = ShortcutUtility.ReadShortcuts(shortcutFile);
            var gamesUpdated = 0;
            var gamesNew = 0;
            var gamesSkippedNoAction = new List<string>();
            var gamesSkippedSteamNative = new List<string>();
            var gamesSkippedBadEmulator = new List<string>();
            var gamesWithUrl = new List<string>();
            var gamesNotInSteam = new List<string>();
            foreach (var game in args.Games)
            {
                ProcessGame(game, shortcuts, ref gamesUpdated, ref gamesNew, gamesSkippedNoAction,
                    gamesSkippedSteamNative, gamesSkippedBadEmulator, gamesWithUrl, gamesNotInSteam);
            }
            try
            {
                ShortcutUtility.SaveShortcuts(shortcuts, shortcutFile);
            }
            catch (Exception ex)
            {
                PlayniteApi.Dialogs.ShowErrorMessage(string.Format(ResourceProvider.GetString("LOCNSSErrorWritingShortcuts"), shortcutFile, ex.Message),
                    ResourceProvider.GetString("LOCNSSErrorCaption"));
                RestoreBackup(shortcutFile);
                return;
            }

            gamesSkippedNoAction = CheckListCount(gamesSkippedNoAction);
            gamesSkippedSteamNative = CheckListCount(gamesSkippedSteamNative);
            gamesSkippedBadEmulator = CheckListCount(gamesSkippedBadEmulator);
            gamesWithUrl = CheckListCount(gamesWithUrl);
            gamesNotInSteam = CheckListCount(gamesNotInSteam);

            var message = new List<string>();
            var errors = false;
            if (errors)
            {

            }
            else
            {
                PlayniteApi.Dialogs.ShowMessage(string.Join("\n", message), ResourceProvider.GetString("LOCNSSUpdatedShortcutsCaption"));
            }
        }

        private void ProcessGame(Game game, IList<Shortcut> availableShortcuts, ref int gamesUpdated, ref int gamesNew,
            List<string> gamesSkippedNoAction, List<string> gamesSkippedSteamNative, List<string> gamesSkippedBadEmulator,
            List<string> gamesWithUrl, List<string> gamesNotInSteam)
        {
            if (game.PluginId == SteamPluginGuid)
            {
                _logger.Warn($"Game {game.Name} is already a Steam game");
                gamesSkippedSteamNative.Add(game.Name);
                return;
            }
            var playActions = game.GameActions.Where(ga => ga.IsPlayAction);
            if (!playActions.Any())
            {
                _logger.Warn($"Game {game.Name} has no play action");
                gamesSkippedNoAction.Add(game.Name);
                return;
            }

            var nonSteamShortcutAction = playActions.FirstOrDefault(pa => pa.Name == NON_STEAM_STEAM_SHORTCUT);
            playActions.Where(pa => pa != nonSteamShortcutAction).ForEach(pa => pa.IsPlayAction = false);
            if (nonSteamShortcutAction != null)
            {
                var availableShortcut = availableShortcuts.FirstOrDefault(s => s.AppName == game.Name);
                if (availableShortcut == null)
                {
                    _logger.Error($"Game {game.Name} has a Non-Steam Steam Shortcut game action but it is not listed in Steam's shortcuts file");
                    gamesNotInSteam.Add(game.Name);
                    return;
                }
                var originalPlayAction = playActions.FirstOrDefault(pa => pa.Name == ORIGINAL_GAME_ACTION_NAME);
                if (originalPlayAction == null)
                {
                    _logger.Error($"Game {game.Name} has a Non-Steam Steam Shortcut game action but the original play action can't be found");
                    return;
                }
                var shortcutFromOriginalGameAction = ShortcutUtility.CreateShortcutFromPlayniteGame(game, originalPlayAction, _logger,
                    gamesWithUrl, gamesSkippedBadEmulator);
                availableShortcut.AppName = shortcutFromOriginalGameAction.AppName;
                availableShortcut.LaunchOptions = shortcutFromOriginalGameAction.LaunchOptions;
                availableShortcut.StartDir = shortcutFromOriginalGameAction.StartDir;
                availableShortcut.Exe = shortcutFromOriginalGameAction.Exe;
                availableShortcut.Icon = shortcutFromOriginalGameAction.Icon;
                nonSteamShortcutAction.IsPlayAction = true;
                nonSteamShortcutAction.Path = SteamUtility.CreateUrlFromShortcut(availableShortcut);
                ++gamesUpdated;
            }
            else
            {
                var actualPlayAction = playActions.First();
                actualPlayAction.Name = ORIGINAL_GAME_ACTION_NAME;
                var newShortcut = ShortcutUtility.CreateShortcutFromPlayniteGame(game, actualPlayAction, _logger, gamesWithUrl, gamesSkippedBadEmulator);
                var newPlayAction = new GameAction
                {
                    Name = NON_STEAM_STEAM_SHORTCUT,
                    Type = GameActionType.URL,
                    Path = SteamUtility.CreateUrlFromShortcut(newShortcut),
                    IsPlayAction = true
                };
                ++gamesNew;
            }
        }

        private bool CreateBackup(string shortcutFile)
        {
            try
            {
                File.Copy(shortcutFile, shortcutFile + ".bak", true);
                return true;
            }
            catch (Exception ex)
            {
                PlayniteApi.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCNSSErrorCreatingBackup"), ResourceProvider.GetString("LOCNSSErrorCaption"));
                _logger.Error(ex, ex.Message);
                return false;
            }
        }

        private void RestoreBackup(string shortcutFile)
        {
            if (File.Exists(shortcutFile + ".bak"))
            {
                try
                {
                    File.Copy(shortcutFile + ".bak", shortcutFile, true);
                    PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCNSSSuccessfullyRestored"));
                }
                catch (Exception ex)
                {
                    PlayniteApi.Dialogs.ShowErrorMessage(string.Format(ResourceProvider.GetString("LOCNSSFailedRestore"), ex.Message),
                        ResourceProvider.GetString("LOCNSSErrorCaption"));
                }
            }
            else if (File.Exists(shortcutFile))
            {
                File.Delete(shortcutFile);
            }

        }

        private List<string> CheckListCount(List<string> list)
        {
            if(list.Count <= 10)
            {
                return list;
            }

            var newList = list.Take(10).ToList();
            newList.Add("[...]");
            return newList;
        }
    }
}