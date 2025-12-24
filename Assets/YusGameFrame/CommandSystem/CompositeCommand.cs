using System;
using System.Collections.Generic;

namespace YusGameFrame
{
    public sealed class CompositeCommand : IUndoableCommand, INamedCommand
    {
        public string Name { get; }

        private readonly IReadOnlyList<IUndoableCommand> _commands;

        public CompositeCommand(IReadOnlyList<IUndoableCommand> commands)
            : this(null, commands)
        {
        }

        public CompositeCommand(string name, IReadOnlyList<IUndoableCommand> commands)
        {
            Name = name;
            _commands = commands ?? throw new ArgumentNullException(nameof(commands));
        }

        public void Execute()
        {
            var executedCount = 0;
            try
            {
                for (var i = 0; i < _commands.Count; i++)
                {
                    _commands[i].Execute();
                    executedCount++;
                }
            }
            catch
            {
                for (var i = executedCount - 1; i >= 0; i--)
                {
                    try
                    {
                        _commands[i].Undo();
                    }
                    catch
                    {
                        // ignored
                    }
                }
                throw;
            }
        }

        public void Undo()
        {
            for (var i = _commands.Count - 1; i >= 0; i--)
                _commands[i].Undo();
        }
    }
}
