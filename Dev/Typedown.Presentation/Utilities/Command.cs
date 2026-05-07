using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;

namespace Typedown.Presentation.Utilities
{
    public class Command<T> : ICommand, INotifyPropertyChanged
    {
        public event EventHandler? CanExecuteChanged;

        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly Subject<T> executeSubject = new();

        private readonly BehaviorSubject<Func<object?, bool>> canExecuteSubject;

        public Command(bool canExecute = true)
        {
            canExecuteSubject = new(_ => canExecute);
            canExecuteSubject.DistinctUntilChanged().Subscribe(_ => CanExecuteChanged?.Invoke(this, EventArgs.Empty));
            CanExecuteChanged += (s, e) => PropertyChanged?.Invoke(this, new(nameof(IsExecutable)));
        }

        public void Execute(object? parameter)
        {
            if (canExecuteSubject.Value(parameter))
            {
                if (parameter == null)
                    executeSubject.OnNext(default!);
                else if (parameter is T typedParameter)
                    executeSubject.OnNext(typedParameter);
                else if (typeof(T).IsEnum && parameter is string enumName)
                    executeSubject.OnNext((T)Enum.Parse(typeof(T), enumName));
                else
                    executeSubject.OnNext((T)Convert.ChangeType(parameter, typeof(T)));
            }
        }

        public bool CanExecute(object? parameter = null) => canExecuteSubject.Value(parameter);

        public bool IsExecutable { get => CanExecute(); set => SetCanExecute.OnNext(value); }

        public IObservable<T> OnExecute => executeSubject.AsObservable();

        public IObserver<bool> SetCanExecute => Observer.Create<bool>(b => canExecuteSubject.OnNext(_ => b));

        public IObserver<Func<object?, bool>> SetCanExecuteFunc => canExecuteSubject.AsObserver();
    }
}
