using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public static class YusInputExtensions
{
    /// <summary>
    /// Register input callbacks and auto-unregister on destroy.
    /// </summary>
    public static void YusRegisterInput(
        this MonoBehaviour mono,
        InputAction action,
        Action<InputAction.CallbackContext> onPerformed,
        Action<InputAction.CallbackContext> onCanceled = null)
    {
        if (mono == null) throw new ArgumentNullException(nameof(mono));

        if (action == null)
        {
            Debug.LogWarning("[YusInputExtensions] RegisterInput skipped: action is null");
            return;
        }

        if (onPerformed != null) action.performed += onPerformed;
        if (onCanceled != null) action.canceled += onCanceled;

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

public class YusInputAutoCleaner : MonoBehaviour
{
    private sealed class InputRecord
    {
        public InputAction action;
        public Action<InputAction.CallbackContext> onPerformed;
        public Action<InputAction.CallbackContext> onCanceled;
    }

    private readonly List<InputRecord> records = new List<InputRecord>();

    public void AddRecord(InputAction action, Action<InputAction.CallbackContext> performed, Action<InputAction.CallbackContext> canceled)
    {
        records.Add(new InputRecord { action = action, onPerformed = performed, onCanceled = canceled });
    }

    private void OnDestroy()
    {
        foreach (var record in records)
        {
            if (record.action == null) continue;
            if (record.onPerformed != null) record.action.performed -= record.onPerformed;
            if (record.onCanceled != null) record.action.canceled -= record.onCanceled;
        }
        records.Clear();
    }
}

