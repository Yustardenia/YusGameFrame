namespace YusGameFrame
{
    public enum CommandLogKind
    {
        Execute = 0,
        Undo = 1,
        Redo = 2,
        Cancelled = 3,
        ClearHistory = 4,
        Register = 5,
        Unregister = 6,
        ClearLog = 7,
        Error = 8,
    }
}
