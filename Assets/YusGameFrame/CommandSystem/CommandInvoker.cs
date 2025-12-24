using System;
using System.Collections.Generic;

namespace YusGameFrame
{
    public sealed class CommandInvoker
    {
        private readonly Stack<IUndoableCommand> _undoStack = new Stack<IUndoableCommand>();
        private readonly Stack<IUndoableCommand> _redoStack = new Stack<IUndoableCommand>();
        private readonly Stack<CommandGroup> _groups = new Stack<CommandGroup>();

        private sealed class CommandGroup
        {
            public readonly string Name;
            public readonly List<IUndoableCommand> Commands = new List<IUndoableCommand>();

            public CommandGroup(string name)
            {
                Name = name;
            }
        }

        private sealed class GroupScope : IDisposable
        {
            private readonly CommandInvoker _invoker;
            private bool _disposed;

            public GroupScope(CommandInvoker invoker)
            {
                _invoker = invoker;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _invoker.EndGroup();
            }
        }

        public int UndoCount => _undoStack.Count;
        public int RedoCount => _redoStack.Count;

        public IDisposable BeginGroup(string name = null)
        {
            _groups.Push(new CommandGroup(name));
            return new GroupScope(this);
        }

        public void Execute(ICommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            command.Execute();

            if (command is IUndoableCommand undoable)
            {
                RecordUndoable(undoable);
            }
        }

        public void RecordExecuted(IUndoableCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            RecordUndoable(command);
        }

        private void RecordUndoable(IUndoableCommand undoable)
        {
            _redoStack.Clear();

            if (_groups.Count > 0)
            {
                _groups.Peek().Commands.Add(undoable);
                return;
            }

            _undoStack.Push(undoable);
        }

        private void EndGroup()
        {
            if (_groups.Count == 0)
                return;

            var g = _groups.Pop();
            if (g.Commands.Count == 0)
                return;

            var composite = new CompositeCommand(g.Name, g.Commands);
            RecordUndoable(composite);
        }

        public bool TryUndo()
            => TryUndo(out _);

        public bool TryUndo(out IUndoableCommand command)
        {
            if (_undoStack.Count == 0)
            {
                command = null;
                return false;
            }

            command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);
            return true;
        }

        public bool TryRedo()
            => TryRedo(out _);

        public bool TryRedo(out IUndoableCommand command)
        {
            if (_redoStack.Count == 0)
            {
                command = null;
                return false;
            }

            command = _redoStack.Pop();
            command.Execute();
            _undoStack.Push(command);
            return true;
        }

#if UNITY_EDITOR
        public IUndoableCommand[] DebugGetUndoSnapshot() => _undoStack.ToArray();
        public IUndoableCommand[] DebugGetRedoSnapshot() => _redoStack.ToArray();
#endif

        public void ClearHistory()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _groups.Clear();
        }
    }
}
