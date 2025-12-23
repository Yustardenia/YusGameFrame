using System;
using UnityEngine;
using Fungus;

[EventHandlerInfo("YusEvent", "Yus Event Received", "Executes the block when a YusEventSystem event is broadcast.")]
[AddComponentMenu("")]
public class YusEventReceived : Fungus.EventHandler
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

    [Tooltip("Event key used by YusEventSystem (must match the broadcast eventName)")]
    [SerializeField, YusEventName] protected string eventName = "";

    [Tooltip("Expected payload type for this event (must match how the event is broadcast)")]
    [SerializeField] protected PayloadType payloadType = PayloadType.None;

    [VariableProperty()]
    [SerializeField] protected Variable payloadVariable;

    private string registeredEventName;
    private PayloadType registeredPayloadType;

    private Action noArgHandler;
    private Action<string> stringHandler;
    private Action<int> intHandler;
    private Action<float> floatHandler;
    private Action<bool> boolHandler;
    private Action<GameObject> gameObjectHandler;

    protected virtual void OnEnable()
    {
        Register();
    }

    protected virtual void OnDisable()
    {
        Unregister();
    }

    private void Register()
    {
        Unregister();

        if (string.IsNullOrEmpty(eventName)) return;

        var manager = YusEventManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[YusEvent] No YusEventManager in scene, subscribe skipped.");
            return;
        }

        registeredEventName = eventName;
        registeredPayloadType = payloadType;

        switch (payloadType)
        {
            case PayloadType.None:
                noArgHandler = () => ExecuteBlock();
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
                break;
        }
    }

    private void Unregister()
    {
        if (string.IsNullOrEmpty(registeredEventName)) return;

        var manager = YusEventManager.Instance;
        if (manager == null)
        {
            registeredEventName = null;
            noArgHandler = null;
            stringHandler = null;
            intHandler = null;
            floatHandler = null;
            boolHandler = null;
            gameObjectHandler = null;
            return;
        }

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

        registeredEventName = null;
        noArgHandler = null;
        stringHandler = null;
        intHandler = null;
        floatHandler = null;
        boolHandler = null;
        gameObjectHandler = null;
    }

    private void HandlePayload<T>(T value)
    {
        if (payloadVariable != null)
        {
            payloadVariable.SetValue(value);
        }
        ExecuteBlock();
    }

    public override string GetSummary()
    {
        if (string.IsNullOrEmpty(eventName)) return "Error: no eventName";
        return payloadType == PayloadType.None ? eventName : $"{eventName} ({payloadType})";
    }
}
