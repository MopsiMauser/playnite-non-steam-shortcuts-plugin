using Playnite.SDK.Models;

namespace NonSteamShortcuts.Extensions
{
    internal static class EmulatorProfileExtensions
    {
        public static CustomEmulatorProfile ExpandVariables(this CustomEmulatorProfile profile, Game game, string emulatorDir, string romPath)
        {
            var gameClone = game.GetClone();
            gameClone.Roms = new System.Collections.ObjectModel.ObservableCollection<GameRom> { new GameRom("", romPath) };
            var profileClone = profile.GetClone();
            profileClone.Arguments = gameClone.StringExpand(profileClone.Arguments, false, emulatorDir);
            profileClone.WorkingDirectory = gameClone.StringExpand(profileClone.WorkingDirectory, true, emulatorDir);
            profileClone.Executable = gameClone.StringExpand(profileClone.Executable, true, emulatorDir);
            return profileClone;
        }
    }
}
