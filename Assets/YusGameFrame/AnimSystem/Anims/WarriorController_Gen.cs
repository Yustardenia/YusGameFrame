using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Animator))]
public partial class WarriorController : MonoBehaviour
{
    public YusFSM<WarriorController> fsm;
    public Animator Animator { get; private set; }
    // 对应生成的 SO 路径: Resources/WarriorAnimConfig
    // 这里简单处理，你可以用 YusResManager 加载

    private void Awake()
    {
        Animator = GetComponent<Animator>();
        fsm = new YusFSM<WarriorController>(this);
        OnInit();
    }

    private void Update() => fsm.OnUpdate();
    private void FixedUpdate() => fsm.OnFixedUpdate();
    partial void OnInit(); // 用户自定义初始化钩子
}

// 状态: Idle
public partial class WarriorIdleState : YusState<WarriorController>
{
    public override void OnEnter()
    {
        // 自动播放动画: Idle
        owner.Animator.CrossFade(2081823275, 0.1f);
        OnEnterUser();
    }

    partial void OnEnterUser();
    public override void OnUpdate() { OnUpdateUser(); }
    partial void OnUpdateUser();
}

// 状态: Run
public partial class WarriorRunState : YusState<WarriorController>
{
    public override void OnEnter()
    {
        // 自动播放动画: Run
        owner.Animator.CrossFade(1748754976, 0.1f);
        OnEnterUser();
    }

    partial void OnEnterUser();
    public override void OnUpdate() { OnUpdateUser(); }
    partial void OnUpdateUser();
}

// 状态: Attack
public partial class WarriorAttackState : YusState<WarriorController>
{
    public override void OnEnter()
    {
        // 自动播放动画: Attack
        owner.Animator.CrossFade(1080829965, 0.1f);
        OnEnterUser();
    }

    partial void OnEnterUser();
    public override void OnUpdate() { OnUpdateUser(); }
    partial void OnUpdateUser();
}

