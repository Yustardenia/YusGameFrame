using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace YusGameFrame
{
    public static class YusCommand
    {
        private static ICommandSystem _system = new YusCommandSystem();

        public static ICommandSystem System
        {
            get => _system;
            set => _system = value ?? throw new ArgumentNullException(nameof(value));
        }

        public static int UndoCount => _system.UndoCount;
        public static int RedoCount => _system.RedoCount;
        public static IReadOnlyList<CommandLogEntry> LogEntries => _system.LogEntries;

        public static void Execute(ICommand command) => _system.Execute(command);
        public static Task ExecuteAsync(IAsyncCommand command, CancellationToken cancellationToken = default)
            => _system.ExecuteAsync(command, cancellationToken);
        public static bool TryUndo() => _system.TryUndo();
        public static bool TryRedo() => _system.TryRedo();
        public static void ClearHistory() => _system.ClearHistory();

        public static void Register(string key, Func<ICommand> factory) => _system.Register(key, factory);
        public static void Register<TArg>(string key, Func<TArg, ICommand> factory) => _system.Register(key, factory);
        public static void RegisterAsync(string key, Func<IAsyncCommand> factory) => _system.RegisterAsync(key, factory);
        public static void RegisterAsync<TArg>(string key, Func<TArg, IAsyncCommand> factory) => _system.RegisterAsync(key, factory);
        public static bool Unregister(string key) => _system.Unregister(key);
        public static bool TryExecute(string key) => _system.TryExecute(key);
        public static bool TryExecute<TArg>(string key, TArg arg) => _system.TryExecute(key, arg);
        public static Task<bool> TryExecuteAsync(string key, CancellationToken cancellationToken = default)
            => _system.TryExecuteAsync(key, cancellationToken);
        public static Task<bool> TryExecuteAsync<TArg>(string key, TArg arg, CancellationToken cancellationToken = default)
            => _system.TryExecuteAsync(key, arg, cancellationToken);
        public static IReadOnlyList<string> GetRegisteredKeysSnapshot() => _system.GetRegisteredKeysSnapshot();

        public static void ClearLog() => _system.ClearLog();

        public static IDisposable BeginGroup(string name = null) => _system.BeginGroup(name);
    }
}
