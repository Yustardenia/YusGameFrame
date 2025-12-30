using UnityEngine;
using Fungus;
using System;

public class FungusCommandRunner : MonoBehaviour
{
    /// <summary>
    /// 运行一个 Fungus Command。
    /// 你可以在 initializer 里对命令对象的参数进行设置。
    /// </summary>
    public static void RunCommand<T>(Action<T> initializer = null) where T : Command
    {
        var host = new GameObject($"[TempFungusCommand:{typeof(T).Name}]");
        var command = host.AddComponent<T>();

        // 没有 Flowchart 的上下文，这里 ParentBlock / ItemId 就不用管
        command.ParentBlock = null;
        command.ItemId = -1;

        // 允许外部传入参数配置
        initializer?.Invoke(command);

        Debug.Log($"[FungusCommandRunner] Running {typeof(T).Name}");

        // 执行
        command.OnEnter();
        command.OnExit();

        // 执行完清理
        GameObject.Destroy(host);
    }
}
