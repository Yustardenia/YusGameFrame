namespace YusGameFrame
{
    public interface IUndoableCommand : ICommand
    {
        void Undo();
    }
}

