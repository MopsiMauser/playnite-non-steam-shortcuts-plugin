using System.Windows.Controls;

namespace NonSteamShortcuts
{
    public partial class NonSteamShortcutsSettingsView : UserControl
    {
        public NonSteamShortcutsSettingsView(NonSteamShortcutsSettingsViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }

        public NonSteamShortcutsSettingsView() : this(null)
        {
        }
    }
}