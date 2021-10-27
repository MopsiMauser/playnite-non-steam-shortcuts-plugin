namespace NonSteamShortcuts.DataModels
{
    internal class Shortcut
    {
        public string Icon { get; set; }

        public string Exe { get; set; }

        public string StartDir { get; set; }

        public string AppName { get; set; }

        public string LaunchOptions { get; set; }

        public string AllowOverlay => "1";

        public string AllowDesktopConfig => "1";

        public string ShortcutPath => string.Empty;

        public string IsHidden => "0";

        public string OpenVr => "0";

        public string LastPlayTime => "0";

        public string DevKit => "0";

        public string DevKitGameId => string.Empty;
    }
}
