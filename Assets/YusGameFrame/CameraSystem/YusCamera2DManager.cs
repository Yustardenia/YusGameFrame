using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A small 2D-friendly wrapper around Cinemachine.
/// Game logic only talks in "follow/bounds/zoom/shake".
/// </summary>
#if YUS_CINEMACHINE
using Cinemachine;

public class YusCamera2DManager : MonoBehaviour
{
    [System.Serializable]
    private sealed class VcamBinding
    {
        public string Key;
        public CinemachineVirtualCamera Vcam;
    }

    private static YusCamera2DManager _instance;
    private static bool _isQuitting;

    public static YusCamera2DManager Instance
    {
        get
        {
            if (_isQuitting) return null;
            if (_instance != null) return _instance;

            var mgr = YusSingletonManager.Instance != null ? YusSingletonManager.Instance : FindObjectOfType<YusSingletonManager>();
            if (mgr != null && mgr.Camera2D != null)
            {
                _instance = mgr.Camera2D;
                return _instance;
            }

            _instance = FindObjectOfType<YusCamera2DManager>();
            if (_instance != null) return _instance;

            var go = new GameObject(nameof(YusCamera2DManager));
            _instance = go.AddComponent<YusCamera2DManager>();
            return _instance;
        }
    }

    [Header("相机引用（为空则自动查找）")]
    [SerializeField] private Camera unityCamera;
    [SerializeField] private CinemachineVirtualCamera vcam;

    [Header("虚拟相机切换")]
    [SerializeField] private bool autoBindChildVcams = true;
    [SerializeField] private string defaultVcamKey = "Default";
    [SerializeField] private int activePriority = 20;
    [SerializeField] private int inactivePriority = 0;
    [SerializeField] private List<VcamBinding> boundVcams = new List<VcamBinding>();

    private CinemachineFramingTransposer _framing;
    private CinemachineConfiner2D _confiner2D;
    private CinemachineBasicMultiChannelPerlin _noise;

    private readonly Stack<Transform> _followStack = new Stack<Transform>();
    private readonly Dictionary<string, CinemachineVirtualCamera> _vcamByKey = new Dictionary<string, CinemachineVirtualCamera>();
    private string _activeVcamKey;
    private float _baseNoiseFrequency = 1f;
    private YusCoroutineHandle _shakeHandle;
    private YusCoroutineHandle _zoomHandle;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        var mgr = YusSingletonManager.Instance != null ? YusSingletonManager.Instance : FindObjectOfType<YusSingletonManager>();
        TryAttachToSingletonManagerOrPersist(mgr);

        EnsureRigReferences();
        RefreshVcameras();
        TryActivateDefaultVcam();
        CacheActiveVcamComponents();
        EnsureMainCameraAndBrain();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!autoBindChildVcams) return;
        if (Application.isPlaying) return;
        RefreshVcameras();
    }
#endif

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    private void TryAttachToSingletonManagerOrPersist(YusSingletonManager mgr = null)
    {
        if (mgr == null) mgr = YusSingletonManager.Instance != null ? YusSingletonManager.Instance : FindObjectOfType<YusSingletonManager>();
        if (mgr != null)
        {
            if (mgr.Camera2D == null) mgr.Camera2D = this;
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    private void EnsureRigReferences()
    {
        if (unityCamera == null) unityCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        if (vcam == null) vcam = GetComponentInChildren<CinemachineVirtualCamera>(true);
    }

    public void RefreshVcameras()
    {
        _vcamByKey.Clear();
        boundVcams.Clear();

        if (!autoBindChildVcams) return;

        var vcams = GetComponentsInChildren<CinemachineVirtualCamera>(true);
        foreach (var c in vcams)
        {
            if (c == null) continue;
            if (c.gameObject == gameObject) continue;

            string key = c.gameObject.name;
            if (string.IsNullOrEmpty(key)) continue;

            if (_vcamByKey.ContainsKey(key))
            {
                Debug.LogWarning($"[YusCamera2D] 检测到重复 VCam 名称：{key}，将忽略后续同名对象。");
                continue;
            }

            _vcamByKey.Add(key, c);
            boundVcams.Add(new VcamBinding { Key = key, Vcam = c });
        }
    }

    private void TryActivateDefaultVcam()
    {
        if (_vcamByKey.Count > 0)
        {
            if (!string.IsNullOrEmpty(defaultVcamKey) && _vcamByKey.ContainsKey(defaultVcamKey))
            {
                Activate(defaultVcamKey);
                return;
            }

            foreach (var kv in _vcamByKey)
            {
                Activate(kv.Key);
                return;
            }
        }

        if (vcam != null)
        {
            _activeVcamKey = null;
            SetVcamPriority(vcam, activePriority);
        }
    }

    private CinemachineVirtualCamera GetActiveVcam()
    {
        if (!string.IsNullOrEmpty(_activeVcamKey) && _vcamByKey.TryGetValue(_activeVcamKey, out var found) && found != null)
        {
            return found;
        }

        return vcam;
    }

    public bool Activate(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;

        if (_vcamByKey.Count == 0 && autoBindChildVcams) RefreshVcameras();
        if (!_vcamByKey.TryGetValue(key, out var target) || target == null) return false;

        foreach (var kv in _vcamByKey)
        {
            SetVcamPriority(kv.Value, kv.Key == key ? activePriority : inactivePriority);
        }

        _activeVcamKey = key;
        CacheActiveVcamComponents();
        return true;
    }

    private static void SetVcamPriority(CinemachineVirtualCamera target, int priority)
    {
        if (target == null) return;
        target.Priority = priority;
    }

    private void CacheActiveVcamComponents()
    {
        var active = GetActiveVcam();
        if (active == null) return;

        _framing = active.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (_framing == null) _framing = active.AddCinemachineComponent<CinemachineFramingTransposer>();

        _noise = active.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        if (_noise == null) _noise = active.AddCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

        _confiner2D = active.GetComponent<CinemachineConfiner2D>();
        if (_confiner2D == null) _confiner2D = active.gameObject.AddComponent<CinemachineConfiner2D>();

        if (_noise != null) _baseNoiseFrequency = Mathf.Max(1f, _noise.m_FrequencyGain);
    }

    private void EnsureMainCameraAndBrain()
    {
        if (unityCamera != null)
        {
            unityCamera.orthographic = true;
        }

        if (unityCamera != null)
        {
            var brain = unityCamera.GetComponent<CinemachineBrain>();
            if (brain == null) unityCamera.gameObject.AddComponent<CinemachineBrain>();
        }
    }

    public bool IsReady => unityCamera != null && GetActiveVcam() != null;

    public Camera UnityCamera => unityCamera;
    public CinemachineVirtualCamera VirtualCamera => GetActiveVcam();

    public void SetFollow(Transform target)
    {
        var active = GetActiveVcam();
        if (active == null) return;
        active.Follow = target;
    }

    public void SetLookAt(Transform target)
    {
        var active = GetActiveVcam();
        if (active == null) return;
        active.LookAt = target;
    }

    public void PushFollow(Transform target)
    {
        var active = GetActiveVcam();
        if (active == null) return;
        _followStack.Push(active.Follow);
        active.Follow = target;
    }

    public void PopFollow()
    {
        var active = GetActiveVcam();
        if (active == null) return;
        if (_followStack.Count == 0) return;
        active.Follow = _followStack.Pop();
    }

    public void SetBounds(Collider2D bounds)
    {
        if (_confiner2D == null) return;
        _confiner2D.m_BoundingShape2D = bounds;
        _confiner2D.enabled = bounds != null;
        _confiner2D.InvalidateCache();
    }

    public void ClearBounds()
    {
        SetBounds(null);
    }

    public void SetOrthoSize(float size)
    {
        var active = GetActiveVcam();
        if (active == null) return;
        var lens = active.m_Lens;
        lens.Orthographic = true;
        lens.OrthographicSize = Mathf.Max(0.01f, size);
        active.m_Lens = lens;
    }

    public void ZoomTo(float size, float duration, bool? unscaledTime = null)
    {
        var active = GetActiveVcam();
        if (active == null) return;

        if (_zoomHandle.IsValid) _zoomHandle.Stop();
        bool useUnscaled = unscaledTime ?? true;

        float from = active.m_Lens.OrthographicSize;
        float to = Mathf.Max(0.01f, size);

        if (duration <= 0f)
        {
            SetOrthoSize(to);
            return;
        }

        _zoomHandle = YusCoroutine.Run(ZoomRoutine(from, to, duration, useUnscaled), this, tag: "YusCamera2DManager.Zoom");
    }

    private System.Collections.IEnumerator ZoomRoutine(float from, float to, float duration, bool unscaledTime)
    {
        float t = 0f;
        while (t < duration)
        {
            t += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            SetOrthoSize(Mathf.Lerp(from, to, k));
            yield return null;
        }
        SetOrthoSize(to);
    }

    public void Shake(float amplitude, float frequency, float duration, bool? unscaledTime = null)
    {
        if (_noise == null) return;

        if (_shakeHandle.IsValid) _shakeHandle.Stop();

        bool useUnscaled = unscaledTime ?? true;
        _shakeHandle = YusCoroutine.Run(ShakeRoutine(amplitude, frequency, duration, useUnscaled), this, tag: "YusCamera2DManager.Shake");
    }

    public void StopShake()
    {
        if (_shakeHandle.IsValid) _shakeHandle.Stop();
        ResetNoise();
    }

    private System.Collections.IEnumerator ShakeRoutine(float amplitude, float frequency, float duration, bool unscaledTime)
    {
        _noise.m_AmplitudeGain = Mathf.Max(0f, amplitude);
        _noise.m_FrequencyGain = Mathf.Max(0f, frequency);

        float t = 0f;
        while (t < duration)
        {
            t += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        ResetNoise();
    }

    private void ResetNoise()
    {
        if (_noise == null) return;
        _noise.m_AmplitudeGain = 0f;
        _noise.m_FrequencyGain = _baseNoiseFrequency;
    }
}
#else
public class YusCamera2DManager : MonoBehaviour
{
    public static YusCamera2DManager Instance => null;
    public bool IsReady => false;

    public bool Activate(string key)
    {
        Debug.LogWarning("[YusCamera2D] 当前未启用 Cinemachine 支持。请在菜单启用：YusGameFrame/Camera/Cinemachine 2D/Enable");
        return false;
    }

    public void RefreshVcameras()
    {
        Debug.LogWarning("[YusCamera2D] 当前未启用 Cinemachine 支持。请在菜单启用：YusGameFrame/Camera/Cinemachine 2D/Enable");
    }

    public void SetFollow(Transform target)
    {
        Debug.LogWarning("[YusCamera2D] 当前未启用 Cinemachine 支持。请在菜单启用：YusGameFrame/Camera/Cinemachine 2D/Enable");
    }

    public void SetLookAt(Transform target)
    {
        Debug.LogWarning("[YusCamera2D] 当前未启用 Cinemachine 支持。请在菜单启用：YusGameFrame/Camera/Cinemachine 2D/Enable");
    }

    public void PushFollow(Transform target)
    {
        Debug.LogWarning("[YusCamera2D] 当前未启用 Cinemachine 支持。请在菜单启用：YusGameFrame/Camera/Cinemachine 2D/Enable");
    }

    public void PopFollow()
    {
        Debug.LogWarning("[YusCamera2D] 当前未启用 Cinemachine 支持。请在菜单启用：YusGameFrame/Camera/Cinemachine 2D/Enable");
    }

    public void SetBounds(Collider2D bounds)
    {
        Debug.LogWarning("[YusCamera2D] 当前未启用 Cinemachine 支持。请在菜单启用：YusGameFrame/Camera/Cinemachine 2D/Enable");
    }

    public void ClearBounds()
    {
        Debug.LogWarning("[YusCamera2D] 当前未启用 Cinemachine 支持。请在菜单启用：YusGameFrame/Camera/Cinemachine 2D/Enable");
    }

    public void SetOrthoSize(float size)
    {
        Debug.LogWarning("[YusCamera2D] 当前未启用 Cinemachine 支持。请在菜单启用：YusGameFrame/Camera/Cinemachine 2D/Enable");
    }

    public void ZoomTo(float size, float duration, bool? unscaledTime = null)
    {
        Debug.LogWarning("[YusCamera2D] 当前未启用 Cinemachine 支持。请在菜单启用：YusGameFrame/Camera/Cinemachine 2D/Enable");
    }

    public void Shake(float amplitude, float frequency, float duration, bool? unscaledTime = null)
    {
        Debug.LogWarning("[YusCamera2D] 当前未启用 Cinemachine 支持。请在菜单启用：YusGameFrame/Camera/Cinemachine 2D/Enable");
    }

    public void StopShake()
    {
        Debug.LogWarning("[YusCamera2D] 当前未启用 Cinemachine 支持。请在菜单启用：YusGameFrame/Camera/Cinemachine 2D/Enable");
    }
}
#endif
