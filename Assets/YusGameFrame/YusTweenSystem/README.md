# YusTweenSystem

（更美的版本在 `Assets/YusGameFrame/YusTweenSystem/README.html`）

## 推荐入口

- 代码：用 `YusTween.*`（统一 `SetUpdate / SetLink / SetId` 等默认行为）
- Inspector：挂 `YusTweenSequencePlayer`，在 Inspector 里拼 Move/Scale/Rotate/Color/Fade

## YusTweenSequencePlayer 快速上手

1. 给物体挂 `YusTweenSequencePlayer`
2. 点 `自动绑定目标`（可选，但强烈建议）
3. 添加一个或多个轨道（Tracks），设置：
   - `类型`
   - `延迟 / 时长 / 缓动`
   - `To`（需要明确起点就勾 `指定起点（From）` 并填写 From）
4. 把 `播放时机` 设为 `OnEnable` 或 `Start`，或用 UnityEvent 调 `PlayForward()`

## Legacy

`YusTweenManager` 保留用于兼容旧用法，但新代码/新资源建议只使用上面的入口。
