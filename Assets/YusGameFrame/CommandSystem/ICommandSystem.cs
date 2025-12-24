using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace YusGameFrame
{
    public interface ICommandSystem
    {
        event Action<CommandLogEntry> Logged;

        int UndoCount { get; }
        int RedoCount { get; }

        IReadOnlyList<CommandLogEntry> LogEntries { get; }

        void Execute(ICommand command);
        Task ExecuteAsync(IAsyncCommand command, CancellationToken cancellationToken = default);
        bool TryUndo();
        bool TryRedo();
        void ClearHistory();

        void Register(string key, Func<ICommand> factory);
        void Register<TArg>(string key, Func<TArg, ICommand> factory);
        void RegisterAsync(string key, Func<IAsyncCommand> factory);
        void RegisterAsync<TArg>(string key, Func<TArg, IAsyncCommand> factory);
        bool Unregister(string key);
        bool TryExecute(string key);
        bool TryExecute<TArg>(string key, TArg arg);
        Task<bool> TryExecuteAsync(string key, CancellationToken cancellationToken = default);
        Task<bool> TryExecuteAsync<TArg>(string key, TArg arg, CancellationToken cancellationToken = default);
        IReadOnlyList<string> GetRegisteredKeysSnapshot();

        void ClearLog();

        IDisposable BeginGroup(string name = null);
    }
}
