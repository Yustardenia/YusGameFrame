using System;
using UnityEngine;
using Fungus;

[CommandInfo("YusEvent", "Wait", "Wait until a YusEventSystem event is broadcast, then continue.")]
[AddComponentMenu("")]
public class YusEventWaitCommand : Command
{
    public enum PayloadType
    {
        None,
        String,
        Integer,
        Float,
        Boolean,
        GameObject,
    }

    [Tooltip("Pick from YusEvents constants (optional). If set, it will be used when eventName isn't bound to a variable.")]
    [SerializeField, YusEventName] private string eventNameKey = "";

    [Tooltip("Event key used by YusEventSystem (e.g. YusEvents.OnPanelOpen)")]
    [SerializeField] protected StringData eventName = new StringData("");

    [Tooltip("Expected payload type for this event (must match how the event is broadcast)")]
    [SerializeField] protected PayloadType payloadType = PayloadType.None;

    [VariableProperty]
    [SerializeField] protected Variable payloadVariable;

    private string registeredEventName;
    private PayloadType registeredPayloadType;
    private bool isWaiting;
    private bool triggered;

    private Action noArgHandler;
    private Action<string> stringHandler;
    private Action<int> intHandler;
    private Action<float> floatHandler;
    private Action<bool> boolHandler;
    private Action<GameObject> gameObjectHandler;

    public override void OnValidate()
    {
        base.OnValidate();
        TryApplyEventNameKey();
    }

    public override void OnEnter()
    {
        TryApplyEventNameKey();
        Register();
    }

    public override void OnExit()
    {
        Unregister();
    }

    public override void OnStopExecuting()
    {
        Unregister();
    }

    public override string GetSummary()
    {
        if (string.IsNullOrEmpty(eventName.Value)) return "Error: no eventName";
        return payloadType == PayloadType.None ? eventName.Value : $"{eventName.Value} ({payloadType})";
    }

    public override bool HasReference(Variable variable)
    {
        return eventName.stringRef == variable || payloadVariable == variable || base.HasReference(variable);
    }

    private void TryApplyEventNameKey()
    {
        if (eventName.stringRef != null) return;
        if (string.IsNullOrEmpty(eventNameKey)) return;
        eventName.Value = eventNameKey;
    }

    private void Register()
    {
        Unregister();

        var manager = YusEventManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[YusEvent] No YusEventManager in scene, wait skipped.");
            Continue();
            return;
        }

        if (string.IsNullOrEmpty(eventName.Value))
        {
            Debug.LogWarning("[YusEvent] Wait skipped: eventName is empty.");
            Continue();
            return;
        }

        registeredEventName = eventName.Value;
        registeredPayloadType = payloadType;
        isWaiting = true;
        triggered = false;

        switch (payloadType)
        {
            case PayloadType.None:
                noArgHandler = HandleNoPayload;
                manager.AddListener(registeredEventName, noArgHandler);
                break;
            case PayloadType.String:
                stringHandler = value => HandlePayload(value);
                manager.AddListener<string>(registeredEventName, stringHandler);
                break;
            case PayloadType.Integer:
                intHandler = value => HandlePayload(value);
                manager.AddListener<int>(registeredEventName, intHandler);
                break;
            case PayloadType.Float:
                floatHandler = value => HandlePayload(value);
                manager.AddListener<float>(registeredEventName, floatHandler);
                break;
            case PayloadType.Boolean:
                boolHandler = value => HandlePayload(value);
                manager.AddListener<bool>(registeredEventName, boolHandler);
                break;
            case PayloadType.GameObject:
                gameObjectHandler = value => HandlePayload(value);
                manager.AddListener<GameObject>(registeredEventName, gameObjectHandler);
                break;
            default:
                Debug.LogWarning($"[YusEvent] Unsupported payloadType: {payloadType}");
                Continue();
                break;
        }
    }

    private void Unregister()
    {
        if (!isWaiting) return;
        if (string.IsNullOrEmpty(registeredEventName))
        {
            isWaiting = false;
            return;
        }

        var manager = YusEventManager.Instance;
        if (manager != null)
        {
            switch (registeredPayloadType)
            {
                case PayloadType.None:
                    if (noArgHandler != null) manager.RemoveListener(registeredEventName, noArgHandler);
                    break;
                case PayloadType.String:
                    if (stringHandler != null) manager.RemoveListener<string>(registeredEventName, stringHandler);
                    break;
                case PayloadType.Integer:
                    if (intHandler != null) manager.RemoveListener<int>(registeredEventName, intHandler);
                    break;
                case PayloadType.Float:
                    if (floatHandler != null) manager.RemoveListener<float>(registeredEventName, floatHandler);
                    break;
                case PayloadType.Boolean:
                    if (boolHandler != null) manager.RemoveListener<bool>(registeredEventName, boolHandler);
                    break;
                case PayloadType.GameObject:
                    if (gameObjectHandler != null) manager.RemoveListener<GameObject>(registeredEventName, gameObjectHandler);
                    break;
            }
        }

        registeredEventName = null;
        isWaiting = false;
        triggered = false;
        noArgHandler = null;
        stringHandler = null;
        intHandler = null;
        floatHandler = null;
        boolHandler = null;
        gameObjectHandler = null;
    }

    private void HandleNoPayload()
    {
        if (triggered) return;
        triggered = true;
        Unregister();
        Continue();
    }

    private void HandlePayload<T>(T value)
    {
        if (triggered) return;
        triggered = true;
        if (payloadVariable != null)
        {
            payloadVariable.SetValue(value);
        }
        Unregister();
        Continue();
    }
}

