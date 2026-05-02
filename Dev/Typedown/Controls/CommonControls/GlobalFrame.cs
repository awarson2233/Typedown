using System.Linq;
using System.Reactive.Disposables;
using Typedown.Core.Utilities;
using Typedown.Presentation.ViewModels;
using Windows.UI.Xaml.Controls;

namespace Typedown.Controls
{
    public class GlobalFrame : Frame, AppViewModel.INavigationFrame
    {
        private readonly CompositeDisposable disposables = new();

        public GlobalFrame()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            Navigated += OnNavigated;
        }

        private void OnLoaded(object sender, global::Windows.UI.Xaml.RoutedEventArgs e)
        {
            var viewModel = this.GetService<AppViewModel>();
            viewModel.FrameStack = viewModel.FrameStack.Append(this).ToList();
            disposables.Add(Disposable.Create(() => viewModel.FrameStack = viewModel.FrameStack.Where(x => x != this).ToList()));
        }

        private void OnUnloaded(object sender, global::Windows.UI.Xaml.RoutedEventArgs e)
        {
            disposables.Dispose();
        }

        private void OnNavigated(object sender, global::Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            var viewModel = this.GetService<AppViewModel>();
            if (viewModel != null)
                viewModel.GoBackCommand.IsExecutable = viewModel.FrameStack.Any(x => x.CanGoBack);
        }
    }
}
