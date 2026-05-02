using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Typedown.Core.Interfaces;
using Typedown.Interfaces;
using Typedown.Presentation.ViewModels;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Typedown.Controls.FloatControls
{
    public sealed partial class FrontMenu : MenuFlyout
    {
        public AppViewModel ViewModel { get; }
        private IMarkdownEditor MarkdownEditor => ViewModel.ServiceProvider.GetRequiredService<IMarkdownEditor>();

        public FrontMenu(AppViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
        }

        private void OnClosed(object sender, object e)
        {
            MarkdownEditor.PostMessage("FrontMenuClosed", null);
        }

        public void Open(Rect rect)
        {
            BindingDataContext(Items);
            OverlayInputPassThroughElement = (MarkdownEditor as UIElement).XamlRoot.Content;
            AreOpenCloseAnimationsEnabled = ViewModel.SettingsViewModel.AnimationEnable;
            ShowAt((FrameworkElement)MarkdownEditor.GetDummyRectangle(new(rect.X, rect.Y, rect.Width, rect.Height)));
        }

        private void BindingDataContext(IList<MenuFlyoutItemBase> items)
        {
            foreach (var item in items)
            {
                item.DataContext = ViewModel;
                if (item is MenuFlyoutSubItem sub)
                    BindingDataContext(sub.Items);
            }
        }
    }
}
