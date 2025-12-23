using UnityEngine;
using Fungus;

[CommandInfo("YusEvent", "Broadcast", "Broadcast a YusEventSystem event (no args).")]
[AddComponentMenu("")]
public class YusEventBroadcastCommand : Command
{
    [Tooltip("Pick from YusEvents constants (optional). If set, it will be used when eventName isn't bound to a variable.")]
    [SerializeField, YusEventName] private string eventNameKey = "";

    [Tooltip("Event key used by YusEventSystem (e.g. YusEvents.OnPanelOpen)")]
    [SerializeField] protected StringData eventName = new StringData("");

    public override void OnValidate()
    {
        base.OnValidate();
        TryApplyEventNameKey();
    }

    public override void OnEnter()
    {
        TryApplyEventNameKey();

        var manager = YusEventManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[YusEvent] No YusEventManager in scene, broadcast skipped.");
            Continue();
            return;
        }

        if (string.IsNullOrEmpty(eventName.Value))
        {
            Debug.LogWarning("[YusEvent] Broadcast skipped: eventName is empty.");
            Continue();
            return;
        }

        manager.Broadcast(eventName.Value);
        Continue();
    }

    public override string GetSummary() => string.IsNullOrEmpty(eventName.Value) ? "Error: no eventName" : eventName.Value;

    public override bool HasReference(Variable variable) => eventName.stringRef == variable;

    private void TryApplyEventNameKey()
    {
        if (eventName.stringRef != null) return;
        if (string.IsNullOrEmpty(eventNameKey)) return;
        eventName.Value = eventNameKey;
    }
}
