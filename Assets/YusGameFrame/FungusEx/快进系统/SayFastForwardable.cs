using Fungus;
using UnityEngine;

[CommandInfo("YusVN", "Say (Fast Forward)", "Say text that can be fast-forwarded by FastForwardManager.")]
public class SayFastForwardable : Say
{
    private Writer writer;
    private int lastFastForwardFrame = -1;

    public override void OnEnter()
    {
        base.OnEnter();
        CacheWriter();

        if (IsFastForwarding())
        {
            TryFastForwardOnce();
        }
    }

    private void Update()
    {
        if (!IsExecuting) return;
        if (!IsFastForwarding()) return;
        TryFastForwardOnce();
    }

    private void CacheWriter()
    {
        var dialog = SayDialog.GetSayDialog();
        if (dialog == null) return;
        writer = dialog.GetComponentInChildren<Writer>();
    }

    private bool IsFastForwarding()
    {
        return FastForwardManager.Instance != null && FastForwardManager.Instance.IsFastForwarding;
    }

    private void TryFastForwardOnce()
    {
        if (Time.frameCount == lastFastForwardFrame) return;

        if (writer == null)
        {
            CacheWriter();
        }

        if (writer == null || !writer.IsWriting) return;

        lastFastForwardFrame = Time.frameCount;
        writer.OnNextLineEvent();
    }
}

