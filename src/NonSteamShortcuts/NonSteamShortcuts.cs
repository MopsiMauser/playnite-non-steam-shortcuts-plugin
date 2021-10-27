using NonSteamShortcuts.DataModels;
using NonSteamShortcuts.Logic;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            if (File.Exists(shortcutFile) && !CreateBackup(shortcutFile))
            {
                return;
            }

            var shortcuts = ShortcutUtility.ReadShortcuts(shortcutFile);
            var gamesUpdated = 0;
            var gamesNew = 0;
            var gamesSkippedNoAction = new List<string>();
            var gamesSkippedSteamNative = new List<string>();
            var gamesSkippedBadEmulator = new List<string>();
            var gamesSkippedWithUrl = new List<string>();
            var gamesSkippedNotInSteam = new List<string>();
            var gamesSkippedWithScript = new List<string>();
            var gamesToUpdate = new List<Game>();
            foreach (var game in args.Games)
            {
                if (ProcessGame(game, shortcuts, ref gamesUpdated, ref gamesNew, gamesSkippedNoAction,
                    gamesSkippedSteamNative, gamesSkippedBadEmulator, gamesSkippedWithUrl, gamesSkippedNotInSteam, gamesSkippedWithScript))
                {
                    gamesToUpdate.Add(game);
                }
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

            var errors = CheckCreationResult(gamesUpdated, gamesNew, gamesSkippedNoAction, gamesSkippedSteamNative, gamesSkippedBadEmulator, gamesSkippedWithUrl,
                gamesSkippedNotInSteam, gamesSkippedWithScript, out var message);
            if (errors)
            {
                message.Add(ResourceProvider.GetString("LOCNSSQuestionOpenLog"));
                var result = PlayniteApi.Dialogs.ShowMessage(string.Join("\n", message), ResourceProvider.GetString("LOCNSSUpdatedShortcutsCaption"),
                    System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Error);
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    OpenExtensionLog();
                }
            }
            else
            {
                PlayniteApi.Database.Games.BeginBufferUpdate();
                PlayniteApi.Database.Games.Update(gamesToUpdate);
                PlayniteApi.Database.Games.EndBufferUpdate();

                PlayniteApi.Dialogs.ShowMessage(string.Join("\n", message), ResourceProvider.GetString("LOCNSSUpdatedShortcutsCaption"));
            }
        }

        private bool ProcessGame(Game game, IList<Shortcut> availableShortcuts, ref int gamesUpdated, ref int gamesNew,
            List<string> gamesSkippedNoAction, List<string> gamesSkippedSteamNative, List<string> gamesSkippedBadEmulator,
            List<string> gamesWithUrl, List<string> gamesNotInSteam, List<string> gamesWithScript)
        {
            if (game.PluginId == SteamPluginGuid)
            {
                _logger.Warn($"Game {game.Name} is already a Steam game");
                gamesSkippedSteamNative.Add(game.Name);
                return false;
            }
            var playActions = game.GameActions.Where(ga => ga.IsPlayAction);
            if (!playActions.Any())
            {
                _logger.Warn($"Game {game.Name} has no play action");
                gamesSkippedNoAction.Add(game.Name);
                return false;
            }

            var nonSteamShortcutAction = playActions.FirstOrDefault(pa => pa.Name == NON_STEAM_STEAM_SHORTCUT);
            
            if (nonSteamShortcutAction != null)
            {
                var availableShortcut = availableShortcuts.FirstOrDefault(s => s.AppName == game.Name);
                if (availableShortcut == null)
                {
                    _logger.Error($"Game {game.Name} has a Non-Steam Steam Shortcut game action but it is not listed in Steam's shortcuts file");
                    gamesNotInSteam.Add(game.Name);
                    return false;
                }
                var originalPlayAction = game.GameActions.FirstOrDefault(pa => pa.Name == ORIGINAL_GAME_ACTION_NAME);
                if (originalPlayAction == null)
                {
                    _logger.Error($"Game {game.Name} has a Non-Steam Steam Shortcut game action but the original play action can't be found");
                    return false;
                }
                var shortcutFromOriginalGameAction = ShortcutUtility.CreateShortcutFromPlayniteGame(game, originalPlayAction, PlayniteApi, _logger,
                    gamesWithUrl, gamesSkippedBadEmulator, gamesWithScript);
                if(shortcutFromOriginalGameAction == null)
                {
                    return false;
                }
                availableShortcut.AppName = shortcutFromOriginalGameAction.AppName;
                availableShortcut.LaunchOptions = shortcutFromOriginalGameAction.LaunchOptions;
                availableShortcut.StartDir = shortcutFromOriginalGameAction.StartDir;
                availableShortcut.Exe = shortcutFromOriginalGameAction.Exe;
                availableShortcut.Icon = shortcutFromOriginalGameAction.Icon;
                nonSteamShortcutAction.IsPlayAction = true;
                nonSteamShortcutAction.Path = SteamUtility.CreateUrlFromShortcut(availableShortcut);
                game.GameActions.Where(pa => pa != nonSteamShortcutAction).ForEach(pa => pa.IsPlayAction = false);
                ++gamesUpdated;
            }
            else
            {
                var actualPlayAction = playActions.First();
                actualPlayAction.Name = ORIGINAL_GAME_ACTION_NAME;
                var newShortcut = ShortcutUtility.CreateShortcutFromPlayniteGame(game, actualPlayAction, PlayniteApi, _logger, gamesWithUrl,
                    gamesSkippedBadEmulator, gamesWithScript);
                if (newShortcut == null)
                {
                    return false;
                }
                var newPlayAction = new GameAction
                {
                    Name = NON_STEAM_STEAM_SHORTCUT,
                    Type = GameActionType.URL,
                    Path = SteamUtility.CreateUrlFromShortcut(newShortcut),
                    IsPlayAction = true
                };
                playActions.Where(pa => pa != nonSteamShortcutAction).ForEach(pa => pa.IsPlayAction = false);
                game.GameActions.Insert(0, newPlayAction);
                availableShortcuts.Add(newShortcut);
                ++gamesNew;
            }
            
            return true;
        }

        private bool CheckCreationResult(int gamesUpdated, int gamesNew, List<string> gamesSkippedNoAction, List<string> gamesSkippedSteamNative, List<string> gamesSkippedBadEmulator,
            List<string> gamesSkippedWithUrl, List<string> gamesSkippedNotInSteam, List<string> gamesSkippedWithScript, out List<string> message)
        {
            var gamesSkippedNoActionCount = gamesSkippedNoAction.Count;
            var gamesSkippedSteamNativeCount = gamesSkippedSteamNative.Count;
            var gamesSkippedBadEmulatorCount = gamesSkippedBadEmulator.Count;
            var gamesSkippedWithUrlCount = gamesSkippedWithUrl.Count;
            var gamesSkippedNotInSteamCount = gamesSkippedNotInSteam.Count;
            var gamesSkippedWithScriptCount = gamesSkippedWithScript.Count;
            gamesSkippedNoAction = CheckListCount(gamesSkippedNoAction);
            gamesSkippedSteamNative = CheckListCount(gamesSkippedSteamNative);
            gamesSkippedBadEmulator = CheckListCount(gamesSkippedBadEmulator);
            gamesSkippedWithUrl = CheckListCount(gamesSkippedWithUrl);
            gamesSkippedNotInSteam = CheckListCount(gamesSkippedNotInSteam);
            gamesSkippedWithScript = CheckListCount(gamesSkippedWithScript);

            message = new List<string>();
            message.Add(ResourceProvider.GetString("LOCNSSRelaunchSteam"));
            message.Add(string.Format(ResourceProvider.GetString("LOCNSSUpdatedShortcuts"), gamesUpdated));
            message.Add(string.Format(ResourceProvider.GetString("LOCNSSCreatedShortcuts"), gamesNew));
            var errors = false;
            if (gamesSkippedSteamNative.Count > 0)
            {
                message.Add(string.Format(ResourceProvider.GetString("LOCNSSSkippedNativeSteamGames"), gamesSkippedSteamNativeCount));
                message.AddRange(gamesSkippedSteamNative);
                errors = true;
            }
            if (gamesSkippedNoAction.Count > 0)
            {
                message.Add(string.Format(ResourceProvider.GetString("LOCNSSSkippedWithoutPlayAction"), gamesSkippedNoActionCount));
                message.AddRange(gamesSkippedNoAction);
                errors = true;
            }
            if (gamesSkippedBadEmulator.Count > 0)
            {
                message.Add(string.Format(ResourceProvider.GetString("LOCNSSSkippedBadEmulator"), gamesSkippedBadEmulatorCount));
                message.AddRange(gamesSkippedBadEmulator);
                errors = true;
            }
            if (gamesSkippedWithUrl.Count > 0)
            {
                message.Add(string.Format(ResourceProvider.GetString("LOCNSSSkippedURLs"), gamesSkippedWithUrlCount));
                message.AddRange(gamesSkippedWithUrl);
                errors = true;
            }
            if (gamesSkippedNotInSteam.Count > 0)
            {
                message.Add(string.Format(ResourceProvider.GetString("LOCNSSSkippedNotInSteam"), gamesSkippedNotInSteamCount));
                message.AddRange(gamesSkippedNotInSteam);
                errors = true;
            }
            if (gamesSkippedWithScript.Count > 0)
            {
                message.Add(string.Format(ResourceProvider.GetString("LOCNSSSkippedWithScript"), gamesSkippedWithScriptCount));
                message.AddRange(gamesSkippedWithScript);
                errors = true;
            }
            return errors;
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
            if (list.Count <= 10)
            {
                return list;
            }

            var newList = list.Take(10).ToList();
            newList.Add("[...]");
            return newList;
        }

        private void OpenExtensionLog()
        {
            var logPath = Path.Combine(PlayniteApi.Paths.ConfigurationPath, "extensions.log");
            if (!File.Exists(logPath))
            {
                PlayniteApi.Dialogs.ShowErrorMessage(string.Format(ResourceProvider.GetString("LOCNSSNoFile"), logPath), ResourceProvider.GetString("LOCNSSErrorCaption"));
                return;
            }
            var startInfo = new ProcessStartInfo
            {
                ErrorDialog = true,
                FileName = logPath,
                Verb = "open",
                UseShellExecute = true
            };
            Process.Start(startInfo);
        }
    }
}