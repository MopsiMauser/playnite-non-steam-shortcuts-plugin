using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.Generic;

namespace NonSteamShortcuts
{
    public class NonSteamShortcutsSettings : ObservableObject
    {
        private string _steamUserProfilePath = string.Empty;

        public string SteamUserProfilePath
        {
            get => _steamUserProfilePath; 
            set => SetValue(ref _steamUserProfilePath, value); 
        }
    }

    public class NonSteamShortcutsSettingsViewModel : ObservableObject, ISettings
    {
        private readonly NonSteamShortcuts _plugin;
        private NonSteamShortcutsSettings EditingClone { get; set; }

        private NonSteamShortcutsSettings _settings;
        public NonSteamShortcutsSettings Settings
        {
            get => _settings;
            set
            {
                _settings = value;
                OnPropertyChanged();
            }
        }

        public NonSteamShortcutsSettingsViewModel(NonSteamShortcuts plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this._plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<NonSteamShortcutsSettings>();

            // LoadPluginSettings returns null if not saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new NonSteamShortcutsSettings();
            }
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            EditingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            Settings = EditingClone;
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            _plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            if (!System.IO.Directory.Exists(Settings.SteamUserProfilePath))
            {
                var errorMessage = ResourceProvider.GetString("LOCNSSSteamUserPathDoesNotExist");
                errors.Add(errorMessage);
                return false;
            }
            return true;
        }
    }
}