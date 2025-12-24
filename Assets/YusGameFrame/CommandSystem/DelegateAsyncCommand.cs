using System;
using System.Threading;
using System.Threading.Tasks;

namespace YusGameFrame
{
    public sealed class DelegateAsyncCommand : IAsyncCommand, INamedCommand
    {
        public string Name { get; }

        private readonly Func<CancellationToken, Task> _executeAsync;
        private readonly Func<bool> _canExecute;

        public DelegateAsyncCommand(Func<CancellationToken, Task> executeAsync, Func<bool> canExecute = null)
            : this(null, executeAsync, canExecute)
        {
        }

        public DelegateAsyncCommand(string name, Func<CancellationToken, Task> executeAsync, Func<bool> canExecute = null)
        {
            Name = name;
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public bool CanExecute() => _canExecute == null || _canExecute();

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (!CanExecute())
                return Task.CompletedTask;

            return _executeAsync(cancellationToken);
        }
    }
}

