using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace YusGameFrame
{
    public sealed class YusCommandSystem : ICommandSystem
    {
        private const int MaxLogCount = 200;

        private readonly CommandInvoker _invoker = new CommandInvoker();
        private readonly Dictionary<string, RegistryEntry> _registry = new Dictionary<string, RegistryEntry>(StringComparer.Ordinal);
        private readonly List<CommandLogEntry> _logs = new List<CommandLogEntry>(MaxLogCount);

        public event Action<CommandLogEntry> Logged;

        public int UndoCount => _invoker.UndoCount;
        public int RedoCount => _invoker.RedoCount;
        public IReadOnlyList<CommandLogEntry> LogEntries => _logs;

#if UNITY_EDITOR
        public CommandInvoker DebugInvoker => _invoker;
        public CommandRegistryInfo[] DebugGetRegistrySnapshot() => GetRegistrySnapshot();
#endif

        private enum RegistryKind
        {
            Sync = 0,
            Async = 1,
        }

        private readonly struct RegistryEntry
        {
            public RegistryKind Kind { get; }
            public Type ArgType { get; }
            public Func<object, object> Factory { get; }

            public RegistryEntry(RegistryKind kind, Type argType, Func<object, object> factory)
            {
                Kind = kind;
                ArgType = argType;
                Factory = factory;
            }
        }

        public void Execute(ICommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            string name = GetCommandName(command);
            string type = command.GetType().FullName;

            try
            {
                _invoker.Execute(command);
                AddLog(CommandLogKind.Execute, name, type, null);
            }
            catch (Exception ex)
            {
                AddLog(CommandLogKind.Error, name, type, ex.Message);
                throw;
            }
        }

        public async Task ExecuteAsync(IAsyncCommand command, CancellationToken cancellationToken = default)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            string name = GetAsyncCommandName(command);
            string type = command.GetType().FullName;

            try
            {
                await command.ExecuteAsync(cancellationToken);

                if (command is IUndoableCommand undoable)
                    _invoker.RecordExecuted(undoable);

                AddLog(CommandLogKind.Execute, name, type, null);
            }
            catch (OperationCanceledException)
            {
                AddLog(CommandLogKind.Cancelled, name, type, null);
                throw;
            }
            catch (Exception ex)
            {
                AddLog(CommandLogKind.Error, name, type, ex.Message);
                throw;
            }
        }

        public bool TryUndo()
        {
            if (!_invoker.TryUndo(out var cmd))
                return false;

            AddLog(CommandLogKind.Undo, GetUndoableName(cmd), cmd?.GetType().FullName, null);
            return true;
        }

        public bool TryRedo()
        {
            if (!_invoker.TryRedo(out var cmd))
                return false;

            AddLog(CommandLogKind.Redo, GetUndoableName(cmd), cmd?.GetType().FullName, null);
            return true;
        }

        public void ClearHistory()
        {
            _invoker.ClearHistory();
            AddLog(CommandLogKind.ClearHistory, null, null, null);
        }

        public void Register(string key, Func<ICommand> factory)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            _registry[key] = new RegistryEntry(RegistryKind.Sync, null, _ => factory());
            AddLog(CommandLogKind.Register, key, null, null);
        }

        public void Register<TArg>(string key, Func<TArg, ICommand> factory)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            _registry[key] = new RegistryEntry(RegistryKind.Sync, typeof(TArg), arg => factory((TArg)arg));
            AddLog(CommandLogKind.Register, key, null, $"Arg={typeof(TArg).Name}");
        }

        public void RegisterAsync(string key, Func<IAsyncCommand> factory)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            _registry[key] = new RegistryEntry(RegistryKind.Async, null, _ => factory());
            AddLog(CommandLogKind.Register, key, null, "Async");
        }

        public void RegisterAsync<TArg>(string key, Func<TArg, IAsyncCommand> factory)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            _registry[key] = new RegistryEntry(RegistryKind.Async, typeof(TArg), arg => factory((TArg)arg));
            AddLog(CommandLogKind.Register, key, null, $"Async Arg={typeof(TArg).Name}");
        }

        public bool Unregister(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;

            if (!_registry.Remove(key))
                return false;

            AddLog(CommandLogKind.Unregister, key, null, null);
            return true;
        }

        public bool TryExecute(string key)
        {
            return TryExecute(key, arg: null);
        }

        public bool TryExecute<TArg>(string key, TArg arg)
            => TryExecute(key, (object)arg);

        private bool TryExecute(string key, object arg)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            if (!_registry.TryGetValue(key, out var entry) || entry.Factory == null)
                return false;

            if (entry.Kind != RegistryKind.Sync)
                return false;

            if (entry.ArgType != null && (arg == null || !entry.ArgType.IsInstanceOfType(arg)))
                return false;

            object cmdObj = null;
            try
            {
                cmdObj = entry.Factory(arg);
            }
            catch (Exception ex)
            {
                AddLog(CommandLogKind.Error, key, null, $"Factory error: {ex.Message}");
                throw;
            }

            if (!(cmdObj is ICommand cmd))
            {
                AddLog(CommandLogKind.Error, key, null, "Factory returned null or non-ICommand.");
                return false;
            }

            Execute(cmd);
            return true;
        }

        public Task<bool> TryExecuteAsync(string key, CancellationToken cancellationToken = default)
            => TryExecuteAsync(key, arg: null, cancellationToken);

        public Task<bool> TryExecuteAsync<TArg>(string key, TArg arg, CancellationToken cancellationToken = default)
            => TryExecuteAsync(key, (object)arg, cancellationToken);

        private async Task<bool> TryExecuteAsync(string key, object arg, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            if (!_registry.TryGetValue(key, out var entry) || entry.Factory == null)
                return false;

            if (entry.ArgType != null && (arg == null || !entry.ArgType.IsInstanceOfType(arg)))
                return false;

            object cmdObj = null;
            try
            {
                cmdObj = entry.Factory(arg);
            }
            catch (Exception ex)
            {
                AddLog(CommandLogKind.Error, key, null, $"Factory error: {ex.Message}");
                throw;
            }

            if (cmdObj == null)
            {
                AddLog(CommandLogKind.Error, key, null, "Factory returned null.");
                return false;
            }

            if (entry.Kind == RegistryKind.Async)
            {
                if (!(cmdObj is IAsyncCommand asyncCmd))
                {
                    AddLog(CommandLogKind.Error, key, null, "Factory returned non-IAsyncCommand.");
                    return false;
                }

                await ExecuteAsync(asyncCmd, cancellationToken);
                return true;
            }

            if (!(cmdObj is ICommand cmd))
            {
                AddLog(CommandLogKind.Error, key, null, "Factory returned non-ICommand.");
                return false;
            }

            Execute(cmd);
            return true;
        }

        public IReadOnlyList<string> GetRegisteredKeysSnapshot()
        {
            if (_registry.Count == 0)
                return Array.Empty<string>();

            var list = new List<string>(_registry.Count);
            foreach (var kv in _registry)
                list.Add(kv.Key);
            list.Sort(StringComparer.Ordinal);
            return list;
        }

        private CommandRegistryInfo[] GetRegistrySnapshot()
        {
            if (_registry.Count == 0)
                return Array.Empty<CommandRegistryInfo>();

            var list = new List<CommandRegistryInfo>(_registry.Count);
            foreach (var kv in _registry)
            {
                var entry = kv.Value;
                list.Add(new CommandRegistryInfo(
                    kv.Key,
                    entry.Kind == RegistryKind.Async,
                    entry.ArgType != null ? entry.ArgType.FullName : null));
            }

            list.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
            return list.ToArray();
        }

        public void ClearLog()
        {
            _logs.Clear();
        }

        public IDisposable BeginGroup(string name = null)
            => _invoker.BeginGroup(name);

        private void AddLog(CommandLogKind kind, string name, string type, string message)
        {
            if (_logs.Count >= MaxLogCount)
                _logs.RemoveAt(0);

            var entry = new CommandLogEntry(DateTime.UtcNow, kind, name, type, message);
            _logs.Add(entry);

            var cb = Logged;
            if (cb == null)
                return;

            try
            {
                cb(entry);
            }
            catch
            {
                // ignored
            }
        }

        private static string GetCommandName(ICommand command)
        {
            if (command is INamedCommand named && !string.IsNullOrWhiteSpace(named.Name))
                return named.Name;

            return command.GetType().Name;
        }

        private static string GetAsyncCommandName(IAsyncCommand command)
        {
            if (command is INamedCommand named && !string.IsNullOrWhiteSpace(named.Name))
                return named.Name;

            return command.GetType().Name;
        }

        private static string GetUndoableName(IUndoableCommand command)
        {
            if (command is INamedCommand named && !string.IsNullOrWhiteSpace(named.Name))
                return named.Name;

            return command != null ? command.GetType().Name : null;
        }
    }
}
