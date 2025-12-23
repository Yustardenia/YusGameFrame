using UnityEngine;
using Fungus;

[CommandInfo("YusEvent", "Broadcast (Int)", "Broadcast a YusEventSystem event with an int payload.")]
[AddComponentMenu("")]
public class YusEventBroadcastIntCommand : Command
{
    [Tooltip("Pick from YusEvents constants (optional). If set, it will be used when eventName isn't bound to a variable.")]
    [SerializeField, YusEventName] private string eventNameKey = "";

    [Tooltip("Event key used by YusEventSystem")]
    [SerializeField] protected StringData eventName = new StringData("");

    [Tooltip("Int payload")]
    [SerializeField] protected IntegerData payload = new IntegerData(0);

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

        manager.Broadcast(eventName.Value, payload.Value);
        Continue();
    }

    public override string GetSummary()
    {
        if (string.IsNullOrEmpty(eventName.Value)) return "Error: no eventName";
        return $"{eventName.Value} = {payload.Value}";
    }

    public override bool HasReference(Variable variable) => eventName.stringRef == variable || payload.integerRef == variable;

    private void TryApplyEventNameKey()
    {
        if (eventName.stringRef != null) return;
        if (string.IsNullOrEmpty(eventNameKey)) return;
        eventName.Value = eventNameKey;
    }
}
