using UnityEngine;
using Fungus;

[CommandInfo("YusEvent", "Broadcast (GameObject)", "Broadcast a YusEventSystem event with a GameObject payload.")]
[AddComponentMenu("")]
public class YusEventBroadcastGameObjectCommand : Command
{
    [Tooltip("Pick from YusEvents constants (optional). If set, it will be used when eventName isn't bound to a variable.")]
    [SerializeField, YusEventName] private string eventNameKey = "";

    [Tooltip("Event key used by YusEventSystem")]
    [SerializeField] protected StringData eventName = new StringData("");

    [Tooltip("GameObject payload")]
    [SerializeField] protected GameObjectData payload;

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
        var goName = payload.Value != null ? payload.Value.name : "null";
        return $"{eventName.Value} = {goName}";
    }

    public override bool HasReference(Variable variable) => eventName.stringRef == variable || payload.gameObjectRef == variable;

    private void TryApplyEventNameKey()
    {
        if (eventName.stringRef != null) return;
        if (string.IsNullOrEmpty(eventNameKey)) return;
        eventName.Value = eventNameKey;
    }
}
