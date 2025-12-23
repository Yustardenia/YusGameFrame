# Yus Fungus Event System

用于把 Fungus Flowchart 和 `Assets/YusGameFrame/YusEventSystem` 事件中心打通：

- Fungus 中通过命令广播事件（`YusEventBroadcast*Command`）。
- Fungus 中通过事件处理器订阅事件并触发 Block（`YusEventReceived`）。
- 代码侧继续使用 `YusEventManager` / `YusRegister` 订阅同一套事件。

## 1) 在 Fungus 里广播

在 Block 里添加命令：

- `YusEvent / Broadcast`
- `YusEvent / Broadcast (String|Int|Float|Bool|GameObject)`

`eventName` 填 `YusEvents.xxx` 或你自定义的字符串 Key。

## 2) 在 Fungus 里订阅并触发 Block

在挂有 Flowchart 的物体上添加组件：

- `YusEventReceived`

配置 `eventName` + `payloadType`，然后把这个组件的 Block 指向你要执行的 Block。

## 3) 在代码里订阅

```csharp
public class Example : MonoBehaviour
{
    private void Start()
    {
        this.YusRegister(YusEvents.OnPanelOpen, OnPanelOpen);
        this.YusRegister<string>("MyEventWithString", OnStringEvent);
    }

    private void OnPanelOpen() { }
    private void OnStringEvent(string value) { }
}
```

