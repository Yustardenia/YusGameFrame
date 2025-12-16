using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class YusInputManager : MonoBehaviour
{
    public static YusInputManager Instance { get; private set; }

    [Header("Init")]
    [SerializeField] private bool autoLoadBindingOverrides = true;
    [SerializeField] private string bindingOverridesKey = "KeyBindings";

    public GameControls controls;

    private int uiLockCount;
    private int disableAllLockCount;

    public int UiLockCount => uiLockCount;
    public int DisableAllLockCount => disableAllLockCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (controls == null) controls = new GameControls();

        if (autoLoadBindingOverrides)
        {
            LoadBindingOverrides();
        }

        ApplyLocks();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (controls != null)
        {
            controls.Disable();
            controls.Dispose();
            controls = null;
        }
    }

    public IDisposable AcquireUI()
    {
        uiLockCount++;
        ApplyLocks();
        return new InputModeHandle(this, InputModeHandle.Mode.UI);
    }

    public IDisposable AcquireDisableAll()
    {
        disableAllLockCount++;
        ApplyLocks();
        return new InputModeHandle(this, InputModeHandle.Mode.DisableAll);
    }

    public void EnableGameplay()
    {
        uiLockCount = 0;
        disableAllLockCount = 0;
        ApplyLocks();
    }

    public void EnableUI()
    {
        disableAllLockCount = 0;
        uiLockCount = 1;
        ApplyLocks();
    }

    public void DisableAll()
    {
        disableAllLockCount = 1;
        ApplyLocks();
    }

    private void ReleaseUI()
    {
        if (uiLockCount > 0) uiLockCount--;
        ApplyLocks();
    }

    private void ReleaseDisableAll()
    {
        if (disableAllLockCount > 0) disableAllLockCount--;
        ApplyLocks();
    }

    private void ApplyLocks()
    {
        if (controls == null) return;

        if (disableAllLockCount > 0)
        {
            controls.Gameplay.Disable();
            controls.UI.Disable();
            return;
        }

        if (uiLockCount > 0)
        {
            controls.Gameplay.Disable();
            controls.UI.Enable();
            return;
        }

        controls.UI.Disable();
        controls.Gameplay.Enable();
    }

    public InputAction GetAction(string mapName, string actionName)
    {
        if (controls == null || controls.asset == null) return null;

        var map = controls.asset.FindActionMap(mapName, false);
        if (map == null)
        {
            Debug.LogWarning($"[YusInputManager] ActionMap not found: {mapName}");
            return null;
        }

        var action = map.FindAction(actionName, false);
        if (action == null)
        {
            Debug.LogWarning($"[YusInputManager] Action not found: {mapName}/{actionName}");
            return null;
        }

        return action;
    }

    public void SaveBindingOverrides()
    {
        if (controls == null) return;
        string json = controls.SaveBindingOverridesAsJson();
        SimpleSingleValueSaver.Save(bindingOverridesKey, json);
    }

    public void LoadBindingOverrides()
    {
        if (controls == null) return;
        string json = SimpleSingleValueSaver.Load<string>(bindingOverridesKey, "");
        if (!string.IsNullOrEmpty(json))
        {
            controls.LoadBindingOverridesFromJson(json);
        }
    }

    private sealed class InputModeHandle : IDisposable
    {
        internal enum Mode { UI, DisableAll }

        private YusInputManager manager;
        private Mode mode;
        private bool disposed;

        public InputModeHandle(YusInputManager manager, Mode mode)
        {
            this.manager = manager;
            this.mode = mode;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            if (manager == null) return;

            switch (mode)
            {
                case Mode.UI:
                    manager.ReleaseUI();
                    break;
                case Mode.DisableAll:
                    manager.ReleaseDisableAll();
                    break;
            }

            manager = null;
        }
    }
}

