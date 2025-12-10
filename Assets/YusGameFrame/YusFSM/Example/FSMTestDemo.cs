using UnityEngine;

/// <summary>
/// FSM 测试总控制器
/// 模拟一个简单的游戏流程控制
/// </summary>
public class FSMTestDemo : MonoBehaviour
{
    // 定义状态机
    private YusFSM<FSMTestDemo> fsm;

    // 模拟一些游戏数据
    public int score = 0;
    public float gameTime = 0f;

    void Start()
    {
        YusLogger.Log("<color=yellow>=== FSM 测试开始 ===</color>");
        YusLogger.Log("操作说明:\n[Space] 开始游戏\n[P] 暂停/恢复\n[E] 结束游戏\n[R] Revert(回退测试)");

        // 初始化状态机
        fsm = new YusFSM<FSMTestDemo>(this);
        
        // 启动，进入 Init 状态
        fsm.Start<TestState_Init>();
    }

    void Update()
    {
        // 驱动状态机 Update
        fsm.OnUpdate();

        // 1. 全局测试：强制重置
        if (Input.GetKeyDown(KeyCode.F1))
        {
            YusLogger.Warning("强制重启 FSM");
            fsm.ChangeState<TestState_Init>();
        }
    }

    // --- 下面定义三个具体状态 ---
    // 为了方便演示，把状态类写在同一个文件里 (实际开发建议分开)

    // 1. 初始化状态
    public class TestState_Init : YusState<FSMTestDemo>
    {
        public override void OnEnter()
        {
            YusLogger.Log("<color=cyan>[Init] 进入初始化状态。</color> 等待玩家按 [Space] 开始...");
            owner.score = 0;
            owner.gameTime = 0;
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                fsm.ChangeState<TestState_Gaming>();
            }
        }
    }

    // 2. 游戏进行状态
    public class TestState_Gaming : YusState<FSMTestDemo>
    {
        public override void OnEnter()
        {
            YusLogger.Log("<color=green>[Gaming] 游戏开始！</color> (Update中计时，按 P 暂停，E 结算)");
        }

        public override void OnUpdate()
        {
            // 模拟业务逻辑
            owner.gameTime += Time.deltaTime;
            
            // 模拟每一秒加分
            if (Time.frameCount % 60 == 0) 
            {
                owner.score += 10;
                // 用 Log 假装这是 UI 更新
                // Debug.Log($"Gaming... Time: {owner.gameTime:F1}, Score: {owner.score}"); 
            }

            // 监听输入
            if (Input.GetKeyDown(KeyCode.P))
            {
                fsm.ChangeState<TestState_Pause>();
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                fsm.ChangeState<TestState_Over>();
            }
        }

        public override void OnExit()
        {
            YusLogger.Log($"[Gaming] 离开游戏状态。当前分数为: {owner.score}");
        }
    }

    // 3. 暂停状态
    public class TestState_Pause : YusState<FSMTestDemo>
    {
        public override void OnEnter()
        {
            YusLogger.Log("<color=yellow>[Pause] 游戏暂停。</color> 时间停止流动。按 P 或 R 恢复。");
            Time.timeScale = 0; // 真的暂停一下时间试试
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                fsm.ChangeState<TestState_Gaming>();
            }
            
            // 测试 Revert 功能
            if (Input.GetKeyDown(KeyCode.R))
            {
                YusLogger.Log("测试 RevertState() -> 应该回到 Gaming");
                fsm.RevertState();
            }
        }

        public override void OnExit()
        {
            YusLogger.Log("[Pause] 结束暂停。");
            Time.timeScale = 1; // 恢复时间
        }
    }

    // 4. 结算状态
    public class TestState_Over : YusState<FSMTestDemo>
    {
        public override void OnEnter()
        {
            YusLogger.Log($"<color=red>[GameOver] 游戏结束！</color> 最终得分: {owner.score}。按 Space 重新开始。");
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                fsm.ChangeState<TestState_Init>();
            }
        }
    }
}