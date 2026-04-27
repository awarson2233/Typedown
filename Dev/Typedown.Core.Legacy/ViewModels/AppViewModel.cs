using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using Typedown.Core.Interfaces;
using Typedown.Core.Utilities;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Typedown.Core.ViewModels
{
    public sealed partial class AppViewModel : INotifyPropertyChanged, IDisposable
    {
        public IServiceProvider ServiceProvider { get; }

        public EditorViewModel EditorViewModel => ServiceProvider.GetRequiredService<EditorViewModel>();

        public FileViewModel FileViewModel => ServiceProvider.GetRequiredService<FileViewModel>();

        public FloatViewModel FloatViewModel => ServiceProvider.GetRequiredService<FloatViewModel>();

        public FormatViewModel FormatViewModel => ServiceProvider.GetRequiredService<FormatViewModel>();

        public ParagraphViewModel ParagraphViewModel => ServiceProvider.GetRequiredService<ParagraphViewModel>();

        public SettingsViewModel SettingsViewModel => ServiceProvider.GetRequiredService<SettingsViewModel>();

        public UIViewModel UIViewModel => ServiceProvider.GetRequiredService<UIViewModel>();

        public IReadOnlyList<Frame> FrameStack { get; set; } = new List<Frame>();

        public Command<Unit> GoBackCommand { get; } = new(false);

        public Command<string> NavigateCommand { get; } = new();

        public IMarkdownEditor MarkdownEditor => ServiceProvider.GetRequiredService<IMarkdownEditor>();

        public IWindowContext WindowContext => ServiceProvider.GetRequiredService<IWindowContext>();

        public string[] CommandLineArgs { get; set; } = Environment.GetCommandLineArgs();

        public IntPtr MainWindow
        {
            get => (IntPtr)(WindowContext?.WindowHandle ?? default);
            set
            {
                if (WindowContext != null)
                    WindowContext.WindowHandle = value;
            }
        }

        public XamlRoot XamlRoot
        {
            get => WindowContext.ViewRoot as XamlRoot ?? throw new InvalidOperationException("XamlRoot is not initialized.");
            set
            {
                WindowContext.ViewRoot = value;
            }
        }

        private static readonly List<WeakReference<AppViewModel>> instances = new();

        private readonly CompositeDisposable disposables = new();

        public AppViewModel(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            disposables.Add(GoBackCommand.OnExecute.Subscribe(_ => GoBack()));
            lock (instances) 
                instances.Add(new(this));
        }

        public void GoBack()
        {
            FrameStack.Where(x => x.CanGoBack).Last().GoBack();
        }

        public void Dispose()
        {
            lock (instances) 
                instances.RemoveAll(x => !x.TryGetTarget(out var target) || target == this);
            disposables.Dispose();
        }

        ~AppViewModel()
        {
            Dispose();
        }

        public static List<AppViewModel> GetInstances()
        {
            lock (instances)
                return instances.Select(x => x.TryGetTarget(out var val) ? val : null).OfType<AppViewModel>().ToList();
        }
    }
}
