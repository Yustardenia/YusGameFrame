using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public static class YusInputExtensions
{
    /// <summary>
    /// 注册输入事件 (自动管理生命周期)
    /// </summary>
    /// <param name="action">Input Action (例如 YusInputManager.Instance.controls.Gameplay.Jump)</param>
    /// <param name="onPerformed">按下/触发的回调</param>
    /// <param name="onCanceled">松开的回调 (可选)</param>
    public static void YusRegisterInput(this MonoBehaviour mono, InputAction action, 
        Action<InputAction.CallbackContext> onPerformed, 
        Action<InputAction.CallbackContext> onCanceled = null)
    {
        // 1. 绑定事件
        if (onPerformed != null) action.performed += onPerformed;
        if (onCanceled != null) action.canceled += onCanceled;

        // 2. 添加到自动清理器
        mono.GetInputCleaner().AddRecord(action, onPerformed, onCanceled);
    }

    private static YusInputAutoCleaner GetInputCleaner(this MonoBehaviour mono)
    {
        var cleaner = mono.GetComponent<YusInputAutoCleaner>();
        if (cleaner == null)
        {
            cleaner = mono.gameObject.AddComponent<YusInputAutoCleaner>();
            cleaner.hideFlags = HideFlags.HideInInspector;
        }
        return cleaner;
    }
}

// 隐形组件，负责销毁时退订
public class YusInputAutoCleaner : MonoBehaviour
{
    private class InputRecord
    {
        public InputAction action;
        public Action<InputAction.CallbackContext> onPerformed;
        public Action<InputAction.CallbackContext> onCanceled;
    }

    private List<InputRecord> records = new List<InputRecord>();

    public void AddRecord(InputAction action, Action<InputAction.CallbackContext> p, Action<InputAction.CallbackContext> c)
    {
        records.Add(new InputRecord { action = action, onPerformed = p, onCanceled = c });
    }

    private void OnDestroy()
    {
        foreach (var record in records)
        {
            if (record.action != null)
            {
                if (record.onPerformed != null) record.action.performed -= record.onPerformed;
                if (record.onCanceled != null) record.action.canceled -= record.onCanceled;
            }
        }
        records.Clear();
    }
}