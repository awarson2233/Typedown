using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using Typedown.Core.Utilities;
using Typedown.Presentation.Interfaces;
using Typedown.Presentation.Utilities;

namespace Typedown.Presentation.ViewModels
{
    public sealed partial class AppViewModel : INotifyPropertyChanged, IDisposable
    {
        public interface INavigationFrame
        {
            bool CanGoBack { get; }

            void GoBack();
        }

        public IServiceProvider ServiceProvider { get; }

        public EditorViewModel EditorViewModel => ServiceProvider.GetRequiredService<EditorViewModel>();

        public FileViewModel FileViewModel => ServiceProvider.GetRequiredService<FileViewModel>();

        public FloatViewModel FloatViewModel => ServiceProvider.GetRequiredService<FloatViewModel>();

        public FormatViewModel FormatViewModel => ServiceProvider.GetRequiredService<FormatViewModel>();

        public ParagraphViewModel ParagraphViewModel => ServiceProvider.GetRequiredService<ParagraphViewModel>();

        public SettingsViewModel SettingsViewModel => ServiceProvider.GetRequiredService<SettingsViewModel>();

        public UIViewModel UIViewModel => ServiceProvider.GetRequiredService<UIViewModel>();

        public IReadOnlyList<INavigationFrame> FrameStack { get; set; } = new List<INavigationFrame>();

        public Command<Unit> GoBackCommand { get; } = new(false);

        public Command<string> NavigateCommand { get; } = new();

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

        public object ViewRoot
        {
            get => WindowContext.ViewRoot ?? throw new InvalidOperationException("View root is not initialized.");
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
            GC.SuppressFinalize(this);
        }

        public string GetImageAbsolutePath(string path)
        {
            try
            {
                if (UriHelper.IsAbsolutePath(path))
                    return path;
                return Path.GetFullPath(Path.Combine(FileViewModel.ImageBasePath, path));
            }
            catch
            {
                return null;
            }
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
