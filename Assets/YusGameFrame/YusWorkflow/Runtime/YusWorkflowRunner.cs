using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class YusWorkflowRunner : MonoBehaviour
{
    [Serializable]
    public sealed class ExternalEventEnter
    {
        public string EventName;
        public string TargetNodeGuid;
    }

    [SerializeField] private YusWorkflowAsset workflow;
    [SerializeField] private Component owner;
    [SerializeField] private List<ExternalEventEnter> externalEventEnters = new List<ExternalEventEnter>();

    [Header("Context Init (Blackboard)")]
    [SerializeField] private List<YusWorkflowBlackboardEntry> initialBlackboard = new List<YusWorkflowBlackboardEntry>();

    [Header("PlayMode Debug")]
    [SerializeField] private bool debugPaused;
    [SerializeField] private bool debugManualTick;
    [SerializeField, Range(0f, 4f)] private float debugDeltaTimeScale = 1f;

    private YusWorkflowMachine machine;
    private readonly Queue<string> pendingEnterGuids = new Queue<string>();
    private readonly List<(string eventName, Action handler)> registeredExternalHandlers = new List<(string, Action)>();

    private void Awake()
    {
        if (owner == null) owner = this;
    }

    public YusWorkflowAsset Workflow => workflow;
    public YusWorkflowMachine Machine => machine;
    public bool DebugPaused { get => debugPaused; set => debugPaused = value; }
    public bool DebugManualTick { get => debugManualTick; set => debugManualTick = value; }
    public float DebugDeltaTimeScale { get => debugDeltaTimeScale; set => debugDeltaTimeScale = Mathf.Clamp(value, 0f, 4f); }

    private void OnEnable()
    {
        RegisterExternalEvents();
    }

    private void Start()
    {
        machine = new YusWorkflowMachine(owner, workflow);
        ApplyInitialBlackboard();
        machine.Start();
        FlushPendingEnters();
    }

    private void Update()
    {
        FlushPendingEnters();
        if (machine == null) return;
        if (debugPaused) return;
        if (debugManualTick) return;
        machine.Tick(Time.deltaTime * debugDeltaTimeScale);
    }

    private void FixedUpdate()
    {
        if (machine == null) return;
        if (debugPaused) return;
        if (debugManualTick) return;
        machine.FixedTick(Time.fixedDeltaTime * debugDeltaTimeScale);
    }

    private void OnDisable()
    {
        UnregisterExternalEvents();
        machine?.Stop();
    }

    private void RegisterExternalEvents()
    {
        UnregisterExternalEvents();
        if (externalEventEnters == null || externalEventEnters.Count == 0) return;

        var manager = YusEventManager.Instance;
        if (manager == null) return;

        for (var i = 0; i < externalEventEnters.Count; i++)
        {
            var binding = externalEventEnters[i];
            if (binding == null) continue;
            if (string.IsNullOrWhiteSpace(binding.EventName)) continue;
            if (string.IsNullOrWhiteSpace(binding.TargetNodeGuid)) continue;

            var targetGuid = binding.TargetNodeGuid;
            Action handler = () => pendingEnterGuids.Enqueue(targetGuid);
            manager.AddListener(binding.EventName, handler);
            registeredExternalHandlers.Add((binding.EventName, handler));
        }
    }

    private void UnregisterExternalEvents()
    {
        if (registeredExternalHandlers.Count == 0) return;
        var manager = YusEventManager.Instance;
        if (manager != null)
        {
            for (var i = 0; i < registeredExternalHandlers.Count; i++)
            {
                var (eventName, handler) = registeredExternalHandlers[i];
                manager.RemoveListener(eventName, handler);
            }
        }
        registeredExternalHandlers.Clear();
    }

    private void FlushPendingEnters()
    {
        if (machine == null) return;
        while (pendingEnterGuids.Count > 0)
        {
            var guid = pendingEnterGuids.Dequeue();
            machine.EnterNodeFromExternalEvent(guid);
        }
    }

    public void DebugStep()
    {
        if (machine == null) return;
        machine.Tick(Time.deltaTime * debugDeltaTimeScale);
    }

    public void DebugFixedStep()
    {
        if (machine == null) return;
        machine.FixedTick(Time.fixedDeltaTime * debugDeltaTimeScale);
    }

    private void ApplyInitialBlackboard()
    {
        if (machine == null) return;

        var merged = new List<YusWorkflowBlackboardEntry>();

        var assetInit = workflow != null ? workflow.InitialBlackboard : null;
        if (assetInit != null && assetInit.Count > 0)
        {
            merged.AddRange(assetInit);
        }

        if (initialBlackboard != null && initialBlackboard.Count > 0)
        {
            merged.AddRange(initialBlackboard);
        }

        machine.Context.SetInitialBlackboardSnapshot(merged);
        machine.Context.ResetBlackboardToInitial();
    }
}
