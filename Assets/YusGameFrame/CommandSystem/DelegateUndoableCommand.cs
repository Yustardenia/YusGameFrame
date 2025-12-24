using System;

namespace YusGameFrame
{
    public sealed class DelegateUndoableCommand : IUndoableCommand, INamedCommand
    {
        public string Name { get; }

        private readonly Action _execute;
        private readonly Action _undo;
        private readonly Func<bool> _canExecute;

        public DelegateUndoableCommand(Action execute, Action undo, Func<bool> canExecute = null)
            : this(null, execute, undo, canExecute)
        {
        }

        public DelegateUndoableCommand(string name, Action execute, Action undo, Func<bool> canExecute = null)
        {
            Name = name;
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _undo = undo ?? throw new ArgumentNullException(nameof(undo));
            _canExecute = canExecute;
        }

        public bool CanExecute() => _canExecute == null || _canExecute();

        public void Execute()
        {
            if (!CanExecute())
                return;

            _execute();
        }

        public void Undo() => _undo();
    }
}
