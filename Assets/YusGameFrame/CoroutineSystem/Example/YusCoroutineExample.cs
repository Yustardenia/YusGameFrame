using System.Collections;
using UnityEngine;

public class YusCoroutineExample : MonoBehaviour
{
    [Header("Auto Run")]
    [SerializeField] private bool runOnStart = true;

    [Header("Params")]
    [SerializeField] private float delaySeconds = 1f;
    [SerializeField] private float repeatInterval = 0.5f;
    [SerializeField] private int repeatCount = 5;

    private YusCoroutineHandle _delayHandle;
    private YusCoroutineHandle _repeatHandle;
    private YusCoroutineHandle _routineHandle;

    private void Start()
    {
        if (runOnStart) RunExample();
    }

    private void OnDisable()
    {
        StopExample();
    }

    [ContextMenu("Run Example")]
    public void RunExample()
    {
        StopExample();

        _delayHandle = YusCoroutine.Delay(
            delaySeconds,
            () => YusLogger.Log($"[YusCoroutineExample] Delay done ({delaySeconds:0.###}s)"),
            owner: this,
            tag: "CoroutineExample.Delay");

        _repeatHandle = YusCoroutine.Repeat(
            repeatInterval,
            () => YusLogger.Log($"[YusCoroutineExample] Repeat tick ({repeatInterval:0.###}s)"),
            repeatCount: repeatCount,
            firstDelay: 0f,
            owner: this,
            tag: "CoroutineExample.Repeat");

        _routineHandle = YusCoroutine.Run(Routine(), owner: this, tag: "CoroutineExample.Routine");
    }

    [ContextMenu("Stop Example")]
    public void StopExample()
    {
        if (_delayHandle.IsValid) _delayHandle.Stop();
        if (_repeatHandle.IsValid) _repeatHandle.Stop();
        if (_routineHandle.IsValid) _routineHandle.Stop();

        _delayHandle = default;
        _repeatHandle = default;
        _routineHandle = default;
    }

    [ContextMenu("Stop By Tag (Repeat)")]
    public void StopRepeatByTag()
    {
        YusCoroutine.StopTag("CoroutineExample.Repeat");
    }

    private IEnumerator Routine()
    {
        for (int i = 0; i < 120; i++)
        {
            if (i % 30 == 0) YusLogger.Log($"[YusCoroutineExample] Routine frame {i}");
            yield return null;
        }

        YusLogger.Log("[YusCoroutineExample] Routine finished");
    }
}

