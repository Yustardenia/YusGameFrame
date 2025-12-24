using System;

namespace YusGameFrame
{
    public sealed class DelegateCommand : ICommand, INamedCommand
    {
        public string Name { get; }

        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public DelegateCommand(Action execute, Func<bool> canExecute = null)
            : this(null, execute, canExecute)
        {
        }

        public DelegateCommand(string name, Action execute, Func<bool> canExecute = null)
        {
            Name = name;
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute() => _canExecute == null || _canExecute();

        public void Execute()
        {
            if (!CanExecute())
                return;

            _execute();
        }
    }
}
