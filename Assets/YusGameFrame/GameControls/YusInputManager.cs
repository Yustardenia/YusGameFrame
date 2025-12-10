using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class YusInputManager : MonoBehaviour
{
    public static YusInputManager Instance { get; private set; }
    public GameControls controls; // 生成的 C# 类

    private void Awake()
    {
        Instance = this;

        controls = new GameControls();
        EnableGameplay(); // 默认开启游戏操作
    }

    // ===========================================
    // 1. 模式切换 (核心：控制输入开关)
    // ===========================================

    /// <summary>
    /// 开启游戏操作 (移动、攻击)，关闭 UI 操作
    /// </summary>
    public void EnableGameplay()
    {
        controls.UI.Disable();
        controls.Gameplay.Enable();
        // 锁定鼠标
        // Cursor.lockState = CursorLockMode.Locked; 
    }

    /// <summary>
    /// 开启 UI 操作，禁止游戏操作 (对话、暂停时调用)
    /// </summary>
    public void EnableUI()
    {
        controls.Gameplay.Disable();
        controls.UI.Enable();
        // 解锁鼠标
        // Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>
    /// 全局禁止 (过场动画)
    /// </summary>
    public void DisableAll()
    {
        controls.Gameplay.Disable();
        controls.UI.Disable();
    }

    // ===========================================
    // 2. 改键功能 (Rebinding)
    // ===========================================
    
    // 保存改键数据
    public void SaveBindingOverrides()
    {
        string json = controls.SaveBindingOverridesAsJson();
        SimpleSingleValueSaver.Save("KeyBindings", json);
    }

    // 加载改键数据
    public void LoadBindingOverrides()
    {
        string json = SimpleSingleValueSaver.Load<string>("KeyBindings", "");
        if (!string.IsNullOrEmpty(json))
        {
            controls.LoadBindingOverridesFromJson(json);
        }
    }
}