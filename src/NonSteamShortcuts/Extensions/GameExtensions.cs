using Playnite.SDK;
using Playnite.SDK.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NonSteamShortcuts.Extensions
{
    internal static class GameExtensions
    {
        public static string StringExpand(this Game game, string inputString, bool fixSeparators = false, string emulatorDir = null, string romPath = null)
        {
            if (string.IsNullOrEmpty(inputString) || !inputString.Contains('{'))
            {
                return inputString;
            }

            var result = inputString;
            if (!string.IsNullOrWhiteSpace(game.InstallDirectory))
            {
                result = result.Replace(ExpandableVariables.InstallationDirectory, game.InstallDirectory);
                result = result.Replace(ExpandableVariables.InstallationDirName, Path.GetFileName(Path.GetDirectoryName(game.InstallDirectory)));
            }

            if (string.IsNullOrEmpty(romPath) && game.Roms.HasItems())
            {
                var customPath = game.Roms[0].Path;
                if (!string.IsNullOrEmpty(customPath))
                {
                    result = result.Replace(ExpandableVariables.ImagePath, customPath);
                    result = result.Replace(ExpandableVariables.ImageNameNoExtension, Path.GetFileNameWithoutExtension(customPath));
                    result = result.Replace(ExpandableVariables.ImageName, Path.GetFileName(customPath));
                }
            }
            else if (!string.IsNullOrEmpty(romPath))
            {
                result = result.Replace(ExpandableVariables.ImagePath, romPath);
                result = result.Replace(ExpandableVariables.ImageNameNoExtension, Path.GetFileNameWithoutExtension(romPath));
                result = result.Replace(ExpandableVariables.ImageName, Path.GetFileName(romPath));
            }

            result = result.Replace(ExpandableVariables.Name, game.Name);
            result = result.Replace(ExpandableVariables.Platform, game.Platforms?[0].Name);
            result = result.Replace(ExpandableVariables.PluginId, game.PluginId.ToString());
            result = result.Replace(ExpandableVariables.GameId, game.GameId);
            result = result.Replace(ExpandableVariables.DatabaseId, game.Id.ToString());
            result = result.Replace(ExpandableVariables.Version, game.Version);
            result = result.Replace(ExpandableVariables.EmulatorDirectory, emulatorDir ?? string.Empty);
            return fixSeparators ? result.FixSeparators() : result;
        }
    }
}
