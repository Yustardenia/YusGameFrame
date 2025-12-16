

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif


// 如果定义了这个宏，才引入 Addressables 命名空间，防止报错
#if YUS_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

public enum LoadMode
{
    Resources,          // 开发期/简单项目
    EditorDatabase,     // 编辑器专用 (开发期最快)
    AssetBundle,        // 传统打包方式 (需要自己管理依赖)
    Addressables        // 现代打包方式 (推荐)
}
public class YusResManager : MonoBehaviour
{
    // --- 单例模式 (由 YusSingletonManager 统一管理) ---
    public static YusResManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    // 资源对象缓存 (Path -> Object)
    private Dictionary<string, UnityEngine.Object> assetCache = new Dictionary<string, UnityEngine.Object>();

    // AssetBundle 包缓存 (BundlePath -> AssetBundle)
    // 防止重复加载同一个 AB 包导致报错
    private Dictionary<string, AssetBundle> bundleCache = new Dictionary<string, AssetBundle>();

    // ================================================
    // 1. 同步加载 (Load)
    // ================================================
    public T Load<T>(string path, LoadMode mode = LoadMode.Resources) where T : UnityEngine.Object
    {
        if (CheckCache(path, out T cached)) return cached;

        T result = null;

        switch (mode)
        {
            case LoadMode.Resources:
                result = Resources.Load<T>(path);
                break;

            case LoadMode.EditorDatabase:
#if UNITY_EDITOR
                result = AssetDatabase.LoadAssetAtPath<T>(path);
#endif
                break;

            case LoadMode.AssetBundle:
                // 约定路径格式: "Bundle路径|资源名称"
                // 例如: "StreamingAssets/ui.bundle|MainPanel"
                result = LoadFromAssetBundleSync<T>(path);
                break;

            case LoadMode.Addressables:
#if YUS_ADDRESSABLES
                // Addressables 1.17+ 支持同步加载 (WaitForCompletion)
                // 注意：这可能会卡顿主线程，建议尽量用异步
                var op = Addressables.LoadAssetAsync<T>(path);
                result = op.WaitForCompletion();
#else
                YusLogger.Error("请先安装 Addressables 包并在 PlayerSettings 定义 YUS_ADDRESSABLES");
#endif
                break;
        }

        if (result != null) assetCache[path] = result;
        else Debug.LogWarning($"[YusRes] Load失败: {path} [{mode}]");

        return result;
    }

    // ================================================
    // 2. 异步加载 (LoadAsync)
    // ================================================
    public void LoadAsync<T>(string path, Action<T> callback, LoadMode mode = LoadMode.Resources) where T : UnityEngine.Object
    {
        YusCoroutine.Run(LoadAsyncRoutine(path, callback, mode), this, tag: "YusResManager.LoadAsync");
    }

    private IEnumerator LoadAsyncRoutine<T>(string path, Action<T> callback, LoadMode mode) where T : UnityEngine.Object
    {
        if (CheckCache(path, out T cached))
        {
            callback?.Invoke(cached);
            yield break;
        }

        T result = null;

        switch (mode)
        {
            case LoadMode.Resources:
                ResourceRequest req = Resources.LoadAsync<T>(path);
                yield return req;
                result = req.asset as T;
                break;

            case LoadMode.EditorDatabase:
#if UNITY_EDITOR
                yield return null; // 模拟异步
                result = AssetDatabase.LoadAssetAtPath<T>(path);
#endif
                break;

            case LoadMode.AssetBundle:
                // AB 异步加载逻辑比较复杂，需要先 LoadBundleAsync 再 LoadAssetAsync
                yield return LoadFromAssetBundleAsync<T>(path, (res) => result = res);
                break;

            case LoadMode.Addressables:
#if YUS_ADDRESSABLES
                var op = Addressables.LoadAssetAsync<T>(path);
                yield return op;
                if (op.Status == AsyncOperationStatus.Succeeded)
                    result = op.Result;
#endif
                break;
        }

        if (result != null) assetCache[path] = result;
        else Debug.LogWarning($"[YusRes] AsyncLoad失败: {path} [{mode}]");

        callback?.Invoke(result);
    }

    // ================================================
    // 3. AssetBundle 专用逻辑 (Internal)
    // ================================================

    // 解析 "包路径|资源名"
    private (string bundlePath, string assetName) ParseABPath(string rawPath)
    {
        string[] parts = rawPath.Split('|');
        if (parts.Length != 2) return (null, null);
        return (parts[0], parts[1]);
    }

    private T LoadFromAssetBundleSync<T>(string path) where T : UnityEngine.Object
    {
        var info = ParseABPath(path);
        if (info.bundlePath == null) return null;

        AssetBundle bundle = null;
        
        // 1. 检查 Bundle 是否已加载
        if (!bundleCache.TryGetValue(info.bundlePath, out bundle))
        {
            // 假设包在 StreamingAssets 下
            string fullPath = Path.Combine(Application.streamingAssetsPath, info.bundlePath);
            bundle = AssetBundle.LoadFromFile(fullPath);
            if (bundle != null) bundleCache[info.bundlePath] = bundle;
        }

        if (bundle != null) return bundle.LoadAsset<T>(info.assetName);
        return null;
    }

    private IEnumerator LoadFromAssetBundleAsync<T>(string path, Action<T> callback) where T : UnityEngine.Object
    {
        var info = ParseABPath(path);
        if (info.bundlePath == null)
        {
            callback?.Invoke(null);
            yield break;
        }

        AssetBundle bundle = null;

        // 1. 检查 Bundle 缓存
        if (!bundleCache.TryGetValue(info.bundlePath, out bundle))
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, info.bundlePath);
            var bundleReq = AssetBundle.LoadFromFileAsync(fullPath);
            yield return bundleReq;
            
            bundle = bundleReq.assetBundle;
            if (bundle != null) bundleCache[info.bundlePath] = bundle;
        }

        if (bundle != null)
        {
            var assetReq = bundle.LoadAssetAsync<T>(info.assetName);
            yield return assetReq;
            callback?.Invoke(assetReq.asset as T);
        }
        else
        {
            callback?.Invoke(null);
        }
    }

    // ================================================
    // 4. 辅助与清理
    // ================================================

    private bool CheckCache<T>(string path, out T result) where T : UnityEngine.Object
    {
        if (assetCache.TryGetValue(path, out UnityEngine.Object obj))
        {
            if (obj != null) { result = obj as T; return true; }
            assetCache.Remove(path);
        }
        result = null;
        return false;
    }

    public GameObject LoadPrefab(string path, Transform parent = null, LoadMode mode = LoadMode.Resources)
    {
        GameObject prefab = Load<GameObject>(path, mode);
        return prefab ? Instantiate(prefab, parent) : null;
    }

    public void ClearCache()
    {
        assetCache.Clear();
        
        // 卸载所有 AB 包 (true 表示连同加载出来的资源一起销毁，慎用，一般用 false)
        foreach (var kvp in bundleCache)
        {
            if (kvp.Value != null) kvp.Value.Unload(false);
        }
        bundleCache.Clear();

        Resources.UnloadUnusedAssets();
        GC.Collect();
    }
}
