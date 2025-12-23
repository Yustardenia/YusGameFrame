using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

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
#if YUS_DOTWEEN
    public YusTweenManager Tween;
#endif
    public SceneAudioManager Audio;
    public YusCamera2DManager Camera2D;
    public YusPoolManager Pool;
    public UIManager UI;

    [Header("=== Rebind ===")]
    [SerializeField] private bool rebindOnSceneLoaded = true;
    [SerializeField] private bool autoDestroyDuplicateSingletons = true;

    [Header("=== Input ===")]
    [SerializeField] private bool autoCreateInputManager = true;

    [Header("=== Coroutine ===")]
    [SerializeField] private bool autoCreateCoroutineManager = true;

#if YUS_DOTWEEN
    [Header("=== Tween ===")]
    [SerializeField] private bool autoCreateTweenManager = true;
#endif

    [Header("=== 业务逻辑系统 ===")]
    public BubbleManager Bubble;
    public DialogueKeyManager DialogueKey;
    public PlayerManager Player;

    [Header("=== 自动扫描到的其他单例 ===")]
    [SerializeField] private List<MonoBehaviour> otherSingletons = new List<MonoBehaviour>();
    [SerializeField] private List<string> otherSingletonTypeNames = new List<string>();
    [SerializeField] private bool autoScanAllSingletonTypesAtRuntime = false;

    private void OnEnable()
    {
        if (rebindOnSceneLoaded)
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }
    }

    private void OnDisable()
    {
        if (rebindOnSceneLoaded)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CheckComponentsRebind(forceRebind: true);

        YusLogger.Log("[YusSingletonManager] 核心系统启动完成");
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckComponentsRebind(forceRebind: true);
    }

    private void CheckComponentsRebind(bool forceRebind)
    {
        Event = EnsureChildSingleton(Event, forceRebind, autoCreate: false);
        Res = EnsureChildSingleton(Res, forceRebind, autoCreate: false);

        Input = EnsureChildSingleton(Input, forceRebind, autoCreate: autoCreateInputManager, nameOverride: nameof(YusInputManager));
        if (!Input && YusInputManager.Instance) Input = YusInputManager.Instance;

        Coroutine = EnsureChildSingleton(Coroutine, forceRebind, autoCreate: autoCreateCoroutineManager, nameOverride: nameof(YusCoroutineManager));

#if YUS_DOTWEEN
        Tween = EnsureChildSingleton(Tween, forceRebind, autoCreate: autoCreateTweenManager, nameOverride: nameof(YusTweenManager));
#endif

        Audio = EnsureChildSingleton(Audio, forceRebind, autoCreate: false);
        Camera2D = EnsureChildSingleton(Camera2D, forceRebind, autoCreate: false);
        Pool = EnsureChildSingleton(Pool, forceRebind, autoCreate: false);
        UI = EnsureChildSingleton(UI, forceRebind, autoCreate: false);

        Bubble = EnsureChildSingleton(Bubble, forceRebind, autoCreate: false);
        DialogueKey = EnsureChildSingleton(DialogueKey, forceRebind, autoCreate: false);
        Player = EnsureChildSingleton(Player, forceRebind, autoCreate: false);

        ManageOtherSingletons(forceRebind);
        CleanupOtherSingletons();
    }

    private void ManageOtherSingletons(bool forceRebind)
    {
        for (int i = 0; i < otherSingletons.Count; i++)
        {
            var mb = otherSingletons[i];
            if (!mb) continue;

            var ensured = EnsureChildSingletonByType(mb.GetType(), forceRebind, autoCreate: false, preferred: mb) as MonoBehaviour;
            if (ensured) otherSingletons[i] = ensured;
        }

        for (int i = 0; i < otherSingletonTypeNames.Count; i++)
        {
            var typeName = otherSingletonTypeNames[i];
            if (string.IsNullOrWhiteSpace(typeName)) continue;

            var type = Type.GetType(typeName);
            if (type == null || !typeof(Component).IsAssignableFrom(type)) continue;

            var ensured = EnsureChildSingletonByType(type, forceRebind, autoCreate: false) as MonoBehaviour;
            if (ensured) RegisterSingleton(ensured);
        }

        if (!autoScanAllSingletonTypesAtRuntime) return;

        var singletonTypes = FindAllSingletonTypesRuntime();
        for (int i = 0; i < singletonTypes.Count; i++)
        {
            var type = singletonTypes[i];
            if (type == null || type == typeof(YusSingletonManager)) continue;
            if (!typeof(Component).IsAssignableFrom(type)) continue;

            var ensured = EnsureChildSingletonByType(type, forceRebind, autoCreate: false) as MonoBehaviour;
            if (ensured) RegisterSingleton(ensured);
        }
    }

    private void CleanupOtherSingletons()
    {
        for (int i = otherSingletons.Count - 1; i >= 0; i--)
        {
            if (!otherSingletons[i])
            {
                otherSingletons.RemoveAt(i);
            }
        }
    }

    private T EnsureChildSingleton<T>(T current, bool forceRebind, bool autoCreate, string nameOverride = null)
        where T : Component
    {
        if (!forceRebind && current && current.transform && current.transform.IsChildOf(transform))
        {
            return current;
        }

        var underManager = GetComponentsInChildren<T>(true);
        if (underManager != null && underManager.Length > 0)
        {
            var chosenUnderManager = ChoosePreferred(underManager);
            AdoptUnderManager(chosenUnderManager);
            HandleDuplicates(underManager, chosenUnderManager);
            return chosenUnderManager;
        }

        var inScene = FindObjectsOfType<T>(true);
        if (inScene != null && inScene.Length > 0)
        {
            var chosenInScene = ChoosePreferred(inScene);
            AdoptUnderManager(chosenInScene);
            HandleDuplicates(inScene, chosenInScene);
            return chosenInScene;
        }

        if (!autoCreate)
        {
            return null;
        }

        var go = new GameObject(nameOverride ?? typeof(T).Name);
        go.transform.SetParent(transform, false);
        ResetLocal(go.transform);
        return go.AddComponent<T>();
    }

    private Component EnsureChildSingletonByType(Type type, bool forceRebind, bool autoCreate, Component preferred = null)
    {
        if (type == null) return null;

        var underManager = GetComponentsInChildren(type, true);
        if (underManager != null && underManager.Length > 0)
        {
            var chosenUnderManager = ChoosePreferred(underManager, preferred);
            AdoptUnderManager(chosenUnderManager);
            HandleDuplicates(underManager, chosenUnderManager);
            return chosenUnderManager;
        }

        var inSceneObjects = FindObjectsOfType(type, true);
        if (inSceneObjects != null && inSceneObjects.Length > 0)
        {
            var components = new Component[inSceneObjects.Length];
            for (int i = 0; i < inSceneObjects.Length; i++)
            {
                components[i] = inSceneObjects[i] as Component;
            }

            var chosenInScene = ChoosePreferred(components, preferred);
            AdoptUnderManager(chosenInScene);
            HandleDuplicates(components, chosenInScene);
            return chosenInScene;
        }

        if (!autoCreate)
        {
            return null;
        }

        var go = new GameObject(type.Name);
        go.transform.SetParent(transform, false);
        ResetLocal(go.transform);
        return go.AddComponent(type) as Component;
    }

    private void AdoptUnderManager(Component comp)
    {
        if (!comp || !comp.transform) return;
        if (comp.transform.IsChildOf(transform)) return;

        comp.transform.SetParent(transform, false);
        ResetLocal(comp.transform);
    }

    private static void ResetLocal(Transform t)
    {
        if (!t) return;
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
    }

    private static T ChoosePreferred<T>(T[] candidates) where T : Component
    {
        T chosen = null;
        int chosenScore = int.MinValue;

        for (int i = 0; i < candidates.Length; i++)
        {
            var candidate = candidates[i];
            if (!candidate) continue;

            int score = 0;
            if (candidate.gameObject.activeInHierarchy) score += 10;
            if (candidate.gameObject.activeSelf) score += 5;
            score += -candidate.GetInstanceID();

            if (chosen == null || score > chosenScore)
            {
                chosen = candidate;
                chosenScore = score;
            }
        }

        return chosen;
    }

    private static Component ChoosePreferred(Component[] candidates, Component preferred)
    {
        if (preferred) return preferred;
        if (candidates == null || candidates.Length == 0) return null;

        Component chosen = null;
        int chosenScore = int.MinValue;

        for (int i = 0; i < candidates.Length; i++)
        {
            var candidate = candidates[i];
            if (!candidate) continue;

            int score = 0;
            if (candidate.gameObject.activeInHierarchy) score += 10;
            if (candidate.gameObject.activeSelf) score += 5;
            score += -candidate.GetInstanceID();

            if (chosen == null || score > chosenScore)
            {
                chosen = candidate;
                chosenScore = score;
            }
        }

        return chosen;
    }

    private void HandleDuplicates<T>(T[] candidates, T chosen) where T : Component
    {
        if (candidates == null || candidates.Length <= 1 || !chosen) return;

        for (int i = 0; i < candidates.Length; i++)
        {
            var candidate = candidates[i];
            if (!candidate || candidate == chosen) continue;

            if (autoDestroyDuplicateSingletons)
            {
                Destroy(candidate.gameObject);
            }
            else
            {
                candidate.gameObject.SetActive(false);
            }

            Debug.LogWarning($"[YusSingletonManager] Duplicate singleton detected: {typeof(T).Name}, removed: {candidate.name}");
        }
    }

    private void HandleDuplicates(Component[] candidates, Component chosen)
    {
        if (candidates == null || candidates.Length <= 1 || !chosen) return;

        for (int i = 0; i < candidates.Length; i++)
        {
            var candidate = candidates[i];
            if (!candidate || candidate == chosen) continue;

            if (autoDestroyDuplicateSingletons)
            {
                Destroy(candidate.gameObject);
            }
            else
            {
                candidate.gameObject.SetActive(false);
            }

            Debug.LogWarning($"[YusSingletonManager] Duplicate singleton detected: {chosen.GetType().Name}, removed: {candidate.name}");
        }
    }

    private static List<Type> FindAllSingletonTypesRuntime()
    {
        var types = new List<Type>();

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            var assembly = assemblies[i];
            var name = assembly.FullName ?? string.Empty;

            if (name.StartsWith("Unity")) continue;
            if (name.StartsWith("System")) continue;
            if (name.StartsWith("mscorlib")) continue;

            Type[] assemblyTypes;
            try
            {
                assemblyTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                assemblyTypes = ex.Types;
            }
            catch
            {
                continue;
            }

            if (assemblyTypes == null) continue;

            for (int j = 0; j < assemblyTypes.Length; j++)
            {
                var type = assemblyTypes[j];
                if (type == null) continue;
                if (!type.IsSubclassOf(typeof(MonoBehaviour)) || type.IsAbstract) continue;
                if (type == typeof(YusSingletonManager)) continue;

                var prop = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                var field = type.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
                if (prop != null || field != null)
                {
                    types.Add(type);
                }
            }
        }

        return types;
    }

    public void RegisterSingleton(MonoBehaviour mb)
    {
        if (mb == null) return;

        AdoptUnderManager(mb);

        if (mb == Event || mb == Res || mb == Input || mb == Coroutine
#if YUS_DOTWEEN
            || mb == Tween
#endif
            || mb == Audio || mb == Camera2D || mb == Pool || mb == UI || mb == Bubble || mb == DialogueKey || mb == Player)
            return;

        if (!otherSingletons.Contains(mb))
        {
            otherSingletons.Add(mb);
        }
    }

    public void RegisterSingletonTypeName(string assemblyQualifiedTypeName)
    {
        if (string.IsNullOrWhiteSpace(assemblyQualifiedTypeName)) return;

        var type = Type.GetType(assemblyQualifiedTypeName);
        if (type == typeof(YusEventManager)) return;
        if (type == typeof(YusResManager)) return;
        if (type == typeof(YusInputManager)) return;
        if (type == typeof(YusCoroutineManager)) return;
#if YUS_DOTWEEN
        if (type == typeof(YusTweenManager)) return;
#endif
        if (type == typeof(SceneAudioManager)) return;
        if (type == typeof(YusCamera2DManager)) return;
        if (type == typeof(YusPoolManager)) return;
        if (type == typeof(UIManager)) return;
        if (type == typeof(BubbleManager)) return;
        if (type == typeof(DialogueKeyManager)) return;
        if (type == typeof(PlayerManager)) return;

        if (!otherSingletonTypeNames.Contains(assemblyQualifiedTypeName))
        {
            otherSingletonTypeNames.Add(assemblyQualifiedTypeName);
        }
    }
}

