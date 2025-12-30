using UnityEngine;
using System;

[RequireComponent(typeof(CanvasGroup))]
public abstract class BasePanel : MonoBehaviour
{
    protected CanvasGroup canvasGroup;

    // 闈㈡澘鏄惁鐢?UIManager 缂撳瓨绠＄悊锛堝鏋滄槸涓存椂寮圭獥鍙兘涓嶉渶瑕佺紦瀛橈級
    public bool isCacheable = true;

    protected virtual void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// 鍒濆鍖栵紝鍙墽琛屼竴娆?
    /// </summary>
    public virtual void Init() { }

    /// <summary>
    /// 鏄剧ず闈㈡澘
    /// </summary>
    public virtual void Open()
    {
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true; // 鍏佽鐐瑰嚮
        canvasGroup.interactable = true;

        // 杩欎竴姝ユ槸涓轰簡纭繚鍦ㄥ眰绾ф渶涓婃柟
        transform.SetAsLastSibling();

        YusEventManager.Instance.Broadcast(YusEvents.OnPanelOpen);
    }

    /// <summary>
    /// 闅愯棌闈㈡澘
    /// </summary>
    public virtual void Close()
    {
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false; // 绌块€忕偣鍑?
        canvasGroup.interactable = false;

        YusEventManager.Instance.Broadcast(YusEvents.OnPanelClose);
    }

    /// <summary>
    /// 褰撻潰鏉胯鏆傚仠锛堣鍏朵粬闈㈡澘瑕嗙洊锛?
    /// </summary>
    public virtual void OnPause()
    {
        canvasGroup.blocksRaycasts = false; // 鏆傚仠浜や簰
    }

    /// <summary>
    /// 褰撻潰鏉挎仮澶嶏紙涓婂眰闈㈡澘鍏抽棴锛?
    /// </summary>
    public virtual void OnResume()
    {
        canvasGroup.blocksRaycasts = true; // 鎭㈠浜や簰
        transform.SetAsLastSibling(); // 纭繚鍦ㄦ渶涓婂眰
    }

    /// <summary>
    /// 鍒刷柊鏁版嵁锛堢敱瀛愮被瀹炵幇鍏蜂綋閫昏緫锛?
    /// </summary>
    public virtual void UpdateView() { }

    public virtual void OpenWithTransition(Action onComplete = null)
    {
        Open();
        onComplete?.Invoke();
    }

    public virtual void CloseWithTransition(Action onComplete = null)
    {
        Close();
        onComplete?.Invoke();
    }
}

