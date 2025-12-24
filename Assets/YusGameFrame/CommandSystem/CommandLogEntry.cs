using System;

namespace YusGameFrame
{
    public readonly struct CommandLogEntry
    {
        public DateTime UtcTime { get; }
        public CommandLogKind Kind { get; }
        public string Name { get; }
        public string CommandType { get; }
        public string Message { get; }

        public CommandLogEntry(DateTime utcTime, CommandLogKind kind, string name, string commandType, string message)
        {
            UtcTime = utcTime;
            Kind = kind;
            Name = name;
            CommandType = commandType;
            Message = message;
        }

        public override string ToString()
        {
            var name = string.IsNullOrEmpty(Name) ? "-" : Name;
            var type = string.IsNullOrEmpty(CommandType) ? "-" : CommandType;
            var msg = string.IsNullOrEmpty(Message) ? "" : $" | {Message}";
            return $"[{UtcTime:HH:mm:ss}] {Kind} | {name} | {type}{msg}";
        }
    }
}

