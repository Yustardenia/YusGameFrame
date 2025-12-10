using UnityEngine;
using System;

[RequireComponent(typeof(CanvasGroup))]
public abstract class BasePanel : MonoBehaviour
{
    protected CanvasGroup canvasGroup;
    
    // 面板是否由 UIManager 缓存管理（如果是临时弹窗可能不需要缓存）
    public bool isCacheable = true;
    

    protected virtual void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// 初始化，只执行一次
    /// </summary>
    public virtual void Init() { }

    /// <summary>
    /// 显示面板
    /// </summary>
    public virtual void Open()
    {
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true; // 允许点击
        canvasGroup.interactable = true;
        
        // 这一步是为了确保在层级最上方
        transform.SetAsLastSibling(); 
        
        YusEventManager.Instance.Broadcast(YusEvents.OnPanelOpen);
    }

    /// <summary>
    /// 隐藏面板
    /// </summary>
    public virtual void Close()
    {
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false; // 穿透点击
        canvasGroup.interactable = false;
        
        YusEventManager.Instance.Broadcast(YusEvents.OnPanelClose);
    }

    /// <summary>
    /// 刷新数据（由子类实现具体逻辑）
    /// </summary>
    public virtual void UpdateView() { }
}