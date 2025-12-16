using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 全局单例管理器
/// 作用：
/// 1. 作为唯一的 DontDestroyOnLoad 物体，保持场景整洁
/// 2. 统筹管理所有单例系统的生命周期
/// 3. 提供统一的访问入口
/// </summary>
public class YusSingletonManager : MonoBehaviour
{
    public static YusSingletonManager Instance { get; private set; }

    [Header("=== 核心架构系统 ===")]
    public YusEventManager Event;
    public YusResManager Res;
    public YusInputManager Input;
    public YusCoroutineManager Coroutine;
    public SceneAudioManager Audio;
    public YusPoolManager Pool;
    public UIManager UI;

    [Header("=== Input ===")]
    [SerializeField] private bool autoCreateInputManager = true;

    [Header("=== Coroutine ===")]
    [SerializeField] private bool autoCreateCoroutineManager = true;

    [Header("=== 业务逻辑系统 ===")]
    public BubbleManager Bubble;
    public DialogueKeyManager DialogueKey;
    public PlayerManager Player;

    [Header("=== 自动扫描到的其他单例 ===")]
    [SerializeField] private List<MonoBehaviour> otherSingletons = new List<MonoBehaviour>();

    private void Awake()
    {
        // 1. 确保全局唯一
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 2. 确保所有组件都已就位 (双重保险)
        CheckComponents();

        // 3. 初始化 (如果需要控制顺序，可以在这里调用各模块的 Init)
        YusLogger.Log("[YusSingletonManager] 核心系统启动完成");
    }

    private void CheckComponents()
    {
        // 如果 Inspector 没赋值，尝试获取
        if (!Event) Event = GetComponentInChildren<YusEventManager>();
        if (!Res) Res = GetComponentInChildren<YusResManager>();
        if (!Input) Input = GetComponentInChildren<YusInputManager>(true);
        if (!Input) Input = FindObjectOfType<YusInputManager>();
        if (!Input && autoCreateInputManager)
        {
            var go = new GameObject(nameof(YusInputManager));
            go.transform.SetParent(transform);
            Input = go.AddComponent<YusInputManager>();
        }
        if (!Input && YusInputManager.Instance) Input = YusInputManager.Instance;

        if (!Coroutine) Coroutine = GetComponentInChildren<YusCoroutineManager>(true);
        if (!Coroutine) Coroutine = FindObjectOfType<YusCoroutineManager>();
        if (!Coroutine && autoCreateCoroutineManager)
        {
            var go = new GameObject(nameof(YusCoroutineManager));
            go.transform.SetParent(transform);
            Coroutine = go.AddComponent<YusCoroutineManager>();
        }

        if (!Audio) Audio = GetComponentInChildren<SceneAudioManager>();
        if (!Pool) Pool = GetComponentInChildren<YusPoolManager>();
        if (!UI) UI = GetComponentInChildren<UIManager>();
        if (!Bubble) Bubble = GetComponentInChildren<BubbleManager>();
        if (!DialogueKey) DialogueKey = GetComponentInChildren<DialogueKeyManager>();
        if (!Player) Player = GetComponentInChildren<PlayerManager>();
    }
    
    /// <summary>
    /// 供编辑器脚本调用：注册扫描到的其他单例
    /// </summary>
    public void RegisterSingleton(MonoBehaviour mb)
    {
        if (mb == null) return;
        
        // 检查是否是核心字段之一
        if (mb == Event || mb == Res || mb == Input || mb == Coroutine || mb == Audio || mb == Pool || mb == UI || mb == Bubble || mb == DialogueKey || mb == Player)
            return;

        if (!otherSingletons.Contains(mb))
        {
            otherSingletons.Add(mb);
        }
    }
}
