using System;
using UnityEngine;

public class FastForwardManager : MonoBehaviour
{
    public static FastForwardManager Instance { get; private set; }

    public bool IsFastForwarding { get; private set; }

    public event Action<bool> OnFastForwardStateChanged;

    protected virtual void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            return;
        }

        if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void StartFastForward()
    {
        if (IsFastForwarding) return;
        IsFastForwarding = true;
        OnFastForwardStateChanged?.Invoke(true);
    }

    public void StopFastForward()
    {
        if (!IsFastForwarding) return;
        IsFastForwarding = false;
        OnFastForwardStateChanged?.Invoke(false);
    }

    public void ToggleFastForward()
    {
        if (IsFastForwarding) StopFastForward();
        else StartFastForward();
    }
}

