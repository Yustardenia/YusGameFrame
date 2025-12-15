using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem; // 引入输入系统

// =========================================================
// 1. 扩展控制器：负责初始化和组件获取
// =========================================================
[RequireComponent(typeof(Animator))]
public partial class WarriorController 
{
    // 定义一些逻辑需要的组件和变量
    public Rigidbody rb;
    public float moveSpeed = 5f;
    public Vector2 inputMove; // 缓存输入

    // 实现生成器留下的 "OnInit" 钩子
    partial void OnInit()
    {
        // 获取组件
        rb = GetComponent<Rigidbody>();

        // 注册输入事件 (利用 YusInputExtensions 自动管理生命周期)
        // 假设你的 Input Action Map 叫 Gameplay
        this.YusRegisterInput(YusInputManager.Instance.controls.Gameplay.Move, OnMoveInput,OnMoveInput);
        this.YusRegisterInput(YusInputManager.Instance.controls.Gameplay.Fire, OnAttackInput); // 假设 Fire 是攻击键

        // 启动 FSM，默认进入 Idle
        fsm.Start<WarriorIdleState>();
        
        Debug.Log("战士初始化完成！");
 
    }

    // 输入回调：更新缓存
    private void OnMoveInput(InputAction.CallbackContext ctx)
    {
        inputMove = ctx.ReadValue<Vector2>();
    }

    // 输入回调：攻击
    private void OnAttackInput(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            // 如果不在攻击状态，就切换到攻击 (防止打断自己)
            // "is" 关键字可以判断当前状态类型
            if (!(fsm.CurrentState is WarriorAttackState))
            {
                fsm.ChangeState<WarriorAttackState>();
            }
        }
    }
   
}

// =========================================================
// 2. 扩展 Idle 状态：检测移动
// =========================================================
public partial class WarriorIdleState
{
    // 逻辑：如果有移动输入，切换到 Run
    partial void OnEnterUser()
    {
        this.owner.rb.velocity = Vector2.zero;
    }

    partial void OnUpdateUser()
    {
        if (owner.inputMove.sqrMagnitude > 0.01f)
        {
            fsm.ChangeState<WarriorRunState>();
        }
    }
}

// =========================================================
// 3. 扩展 Run 状态：处理物理移动
// =========================================================
public partial class WarriorRunState
{
    // 进入 Run 时打印日志 (可选)
    partial void OnEnterUser()
    {
         Debug.Log("开始跑步！");
    }

    // 逻辑更新：如果没有输入，切回 Idle
    partial void OnUpdateUser()
    {
        if (owner.inputMove.sqrMagnitude < 0.01f)
        {
            fsm.ChangeState<WarriorIdleState>();
        }
    }

    // 物理更新：移动刚体
    // 注意：_Gen.cs 里已经帮我们在 FixedUpdate 里调用了 fsm.OnFixedUpdate
    public override void OnFixedUpdate()
    {
        // 获取输入向量
        Vector3 dir = new Vector3(owner.inputMove.x, owner.inputMove.y,0);
        
        // 移动
        // 这里直接修改 velocity 或者用 MovePosition
        owner.rb.velocity = new Vector3(dir.x * owner.moveSpeed, dir.y * owner.moveSpeed,owner.rb.velocity.z);
        
    }
}

// =========================================================
// 4. 扩展 Attack 状态：定时退出
// =========================================================
public partial class WarriorAttackState
{
    private float timer;
    private float attackDuration = 0.8f; // 假设攻击动画长 0.8秒

    // 进入时：重置计时器，并让角色停下
    partial void OnEnterUser()
    {
        timer = 0f;
        owner.rb.velocity = Vector3.zero; // 攻击时不能移动
        Debug.Log("喝！攻击！");
    }

    // 更新：计时，时间到了自动回 Idle
    partial void OnUpdateUser()
    {
        timer += Time.deltaTime;

        // 如果播放完毕
        if (timer >= attackDuration)
        {
            fsm.ChangeState<WarriorIdleState>();
        }
    }
    
    // 离开时
    public override void OnExit()
    {
        Debug.Log("攻击后摇结束。");
    }
}