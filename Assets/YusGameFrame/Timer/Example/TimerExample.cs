using UnityEngine;
using UnityEngine.UI;

public class TimerExample : MonoBehaviour
{
    [Header("UI References")]
    public Text statusText;

    // 容器示例：用于管理属于这个组件的计时器
    private TimerContainer timerContainer = new TimerContainer();

    private void Start()
    {
        Log("按 '1' 测试简单延时 (2秒)");
        Log("按 '2' 测试循环计时器 (每1秒)");
        Log("按 '3' 测试容器命名计时器 (防重复)");
        Log("按 '4' 测试暂停/恢复");
        Log("按 '5' 测试绑定对象销毁 (3秒后销毁测试物体)");
        Log("按 '6' 测试倒计时 (5秒)");
        Log("按 'C' 清理容器内所有计时器");
    }

    private void Update()
    {
        // 1. 简单延时
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Log("启动 2秒 延时...");
            YusTimer.Create(2.0f, () => {
                Log("2秒时间到！");
            });
        }

        // 2. 循环计时器
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Log("启动循环计时器 (5次)...");
            YusTimer.Create(1.0f, () => {
                Log($"滴答... {Time.time:F2}");
            }).SetLoop(5); // 循环5次
        }

        // 3. 容器命名计时器 (防重复点击)
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Log("尝试启动 'Attack' 计时器 (1秒CD)...");
            // 如果你在1秒内多次按下，旧的会被取消，重新开始计时
            timerContainer.AddTimer("Attack", 1.0f, () => {
                Log("攻击冷却就绪！");
            });
        }

        // 4. 暂停/恢复测试
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            var timer = timerContainer.GetTimer("PauseTest");
            if (timer == null)
            {
                Log("启动 'PauseTest' 计时器 (5秒)...");
                timerContainer.AddTimer("PauseTest", 5.0f, () => Log("PauseTest 完成"));
            }
            else
            {
                if (timer.IsPaused)
                {
                    timer.Resume();
                    Log("计时器已恢复");
                }
                else
                {
                    timer.Pause();
                    Log("计时器已暂停");
                }
            }
        }

        // 5. 绑定对象销毁测试
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            GameObject tempObj = new GameObject("TempObject");
            Log("创建了临时物体 TempObject");

            // 在临时物体上绑定一个计时器
            YusTimer.Create(5.0f, () => {
                Log("这条日志不应该出现！因为物体会被提前销毁。");
            }).Attach(tempObj);

            // 3秒后销毁物体
            YusTimer.Create(3.0f, () => {
                Log("销毁 TempObject");
                Destroy(tempObj);
            });
        }

        // 6. 倒计时（从 duration 倒数到 0）
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            Log("启动 5秒 倒计时...");
            var timer = YusTimer.CreateCountdown(5.0f, () => { Log("倒计时结束！"); })
                .OnUpdate(t =>
                {
                    if (statusText == null) return;
                    statusText.text = $"倒计时剩余: {t:F2}s\n" + statusText.text;
                    if (statusText.text.Length > 500) statusText.text = statusText.text.Substring(0, 500);
                });

            Log($"CurrentTime: {timer.CurrentTime:F2}s");
        }

        // C. 清理容器
        if (Input.GetKeyDown(KeyCode.C))
        {
            timerContainer.Clear();
            Log("容器已清理");
        }
    }

    private void OnDestroy()
    {
        // 良好的习惯：组件销毁时清理容器
        timerContainer.Clear();
    }

    private void Log(string msg)
    {
        Debug.Log($"[TimerExample] {msg}");
        if (statusText != null)
        {
            statusText.text = msg + "\n" + statusText.text;
            if (statusText.text.Length > 500) statusText.text = statusText.text.Substring(0, 500);
        }
    }
}
