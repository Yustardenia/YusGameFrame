using Fungus;

[CommandInfo("YusVN", "Stop Fast Forward", "Stops global fast-forward (use at branch/diff points).")]
public class StopFastForwardCommand : Command
{
    public override void OnEnter()
    {
        FastForwardManager.Instance?.StopFastForward();
        Continue();
    }
}

