using System;
using Typedown.Core.Interfaces;
using Typedown.Interfaces;
using Typedown.Core.Utilities;
using Typedown.Presentation.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Typedown.Controls
{
    public sealed partial class StatusBar : UserControl
    {
        public AppViewModel ViewModel => DataContext as AppViewModel;

        public SettingsViewModel Settings => ViewModel?.SettingsViewModel;

        public EditorViewModel Editor => ViewModel?.EditorViewModel;
        public IMarkdownEditor MarkdownEditor => this.GetService<IMarkdownEditor>();

        public StatusBar()
        {
            this.InitializeComponent();
        }

        private string CharacterUnit(int number) => number != 1 ? Locale.GetString("Characters") : Locale.GetString("Character");

        private string WordUnit(int number) => number != 1 ? Locale.GetString("Words") : Locale.GetString("Word");

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Bindings?.StopTracking();
        }
    }
}
