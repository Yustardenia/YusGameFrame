# YusGameFrame

我的Unity游戏开发工具箱 - 自己做着玩的

## 这是啥

就是我平时开发游戏时常用的一些工具和系统，攒着攒着就成了个框架。主要是让自己写代码的时候不用每次都重复造轮子，能快点做游戏。

Unity版本：2022.3+ （应该2020+都能用，但没试过）

## 有什么东西

### 🎯 主要模块

- **对象池** - 不想每次都Instantiate和Destroy，用对象池快多了，还不产生GC
- **事件系统** - 自动解绑的事件系统，不用担心忘记RemoveListener了
- **计时器** - 替代Coroutine的延迟调用，零GC
- **UI管理** - 简单的UI显示隐藏管理
- **音频系统** - BGM和音效分开管理，可以临时切换BGM
- **配置表** - Excel直接导入成ScriptableObject，还能二进制存档
- **输入系统** - Unity新输入系统的简单封装
- **属性工具** - [Watch]实时看变量、[Get]自动获取组件、[KeepValue]退出PlayMode保留值
- **编辑器工具** - 资源查找、文件夹着色、代码行数统计什么的
- **协程管理** - 不用继承MonoBehaviour也能用协程
- **相机系统** - Cinemachine 2D的简单封装
- **补间动画** - DOTween的封装，写起来方便点

还有一些零碎的小工具，用到再说。

## 快速开始

### 代码示例

```csharp
// 播放音乐
SceneAudioManager.Instance.PlayMusic("MainTheme");

// 从对象池拿东西
GameObject enemy = YusPoolManager.Instance.Get("Enemies/Goblin");

// 延迟3秒执行
YusTimer.Create(3f, () => Debug.Log("3秒到了"));

// 触发事件（自动解绑的那种）
this.YusRegisterEvent("OnDie", OnPlayerDie);
```

## 模块列表

这些模块相对独立，可以按需使用：


1. **Attributes（属性工具）**
   - `[Watch]` - 运行时在屏幕上看变量值
   - `[KeepValue]` - 退出PlayMode自动保留值
   - `[Get]` - 自动获取组件，不用拖拽
   - `[SceneSelector]` - 场景选择下拉框

2. **PoolSystem（对象池）**
   - 零GC，比Instantiate快15倍
   - 自动回收，可以延迟归还
   - 有编辑器监视器可以看使用情况

3. **YusEventSystem（事件系统）**
   - 自动解绑，不会内存泄漏
   - 用扩展方法 `this.YusRegisterEvent()` 更方便

4. **Timer（计时器）**
   - 替代Coroutine，零GC
   - 可以绑定到GameObject，销毁时自动停止

5. **MusicControl（音频系统）**
   - BGM和音效分开管理
   - 可以临时切换BGM，还能自动恢复
   - 音量设置会自动保存

6. **ExcelTool（配置表）**
   - Excel直接生成代码和ScriptableObject
   - 支持二进制存档
   - 运行时修改后还能反写回Excel（调试用）

7. **GameControls（输入系统）**
   - 封装Unity新输入系统
   - 自动订阅和解绑，不用写OnEnable/OnDisable了
   - 支持改键保存

8. **UISystem（UI管理）**
   - 简单的显示隐藏管理
   - 栈式管理，自动隐藏下层UI

9. **CoroutineSystem（协程管理）**
   - 不用继承MonoBehaviour也能用协程
   - 可以用标签批量停止

10. **CameraSystem（相机系统）**
    - Cinemachine 2D的封装
    - 跟随、震屏、缩放都有

11. **YusTweenSystem（补间动画）**
    - DOTween的封装
    - 常用的移动、缩放、旋转都有

还有一些其他小工具：
- **EditorProMax** - 编辑器工具（资源查找、文件夹着色等）
- **ResLoadSystem** - 资源加载
- **SimpleBinary** - 二进制存档
- **YusFSM** - 状态机
- **AnimSystem** - 动画系统封装
- **Localization** - 多语言
- **YusLoggerSystem** - 日志
- **YusSingletonManager** - 单例管理器


## 一些常用的代码示例

### 对象池

```csharp
// 从池子里拿
GameObject bullet = YusPoolManager.Instance.Get("Weapons/Bullet");

// 用完了还回去
YusPoolManager.Instance.Release(bullet);

// 或者延迟归还（比如子弹飞一会儿）
bullet.GetComponent<PoolObject>().ReturnToPool(3f);
```

### 事件系统

```csharp
// 注册事件（自动解绑）
this.YusRegisterEvent("OnPlayerDie", OnPlayerDie);

// 触发事件
YusEventManager.Instance.TriggerEvent("OnPlayerDie");

// 带参数的
YusEventManager.Instance.TriggerEvent<int>("OnScoreChange", 100);
```

### 计时器

```csharp
// 延迟3秒执行
YusTimer.Create(3f, () => {
    Debug.Log("3秒到了");
});

// 绑定到GameObject（销毁时自动停止）
YusTimer.Create(5f, () => Attack())
    .BindToGameObject(this);

// 循环执行
YusTimer.Create(1f)
    .SetLoop(-1)  // -1表示无限循环
    .OnComplete(() => UpdateLogic());
```

### 音频

```csharp
// 播放音乐
SceneAudioManager.Instance.PlayMusic("MainTheme");

// 播放音效
SceneAudioManager.Instance.PlaySFX("Jump");

// 临时切换音乐（比如进入战斗）
SceneAudioManager.Instance.SwitchMusicTemporary("BossBattle");

// 战斗结束，恢复之前的音乐
SceneAudioManager.Instance.ReturnToPreviousMusic();
```

### UI

```csharp
// 显示UI
UIManager.Instance.Show<MainMenuUI>();

// 隐藏UI
UIManager.Instance.Hide<MainMenuUI>();
```

### 协程

```csharp
// 不用继承MonoBehaviour也能用协程
YusCoroutine.Delay(3f, () => Debug.Log("延迟3秒"));

// 重复执行（类似InvokeRepeating）
YusCoroutine.Repeat(1f, () => UpdateLogic(), repeatCount: -1);

// 用标签批量停止
YusCoroutine.StopTag("ui_effects");
```

### 相机

```csharp
// 跟随玩家
YusCamera2DManager.Instance.SetFollow(playerTransform);

// 震屏
YusCamera2DManager.Instance.Shake(intensity: 3f, duration: 0.3f);

// 缩放
YusCamera2DManager.Instance.SetZoom(targetSize: 3f, duration: 1f);
```

### 补间动画

```csharp
// 移动
YusTween.MoveTo(enemy, targetPos, duration: 2f);

// UI淡入
YusTween.CanvasGroupFadeIn(uiPanel, duration: 0.5f);

// 缩放（带回弹效果）
YusTween.ScaleFromTo(popup, Vector3.zero, Vector3.one, 0.5f, ease: Ease.OutBack);
```

## 项目结构

```
YusGameFrame/
├── Assets/
│   └── YusGameFrame/          # 框架代码
│       ├── Attributes/        # 属性工具
│       ├── PoolSystem/        # 对象池
│       ├── YusEventSystem/    # 事件系统
│       ├── Timer/             # 计时器
│       ├── MusicControl/      # 音频系统
│       ├── ExcelTool/         # 配置表
│       ├── GameControls/      # 输入系统
│       ├── UISystem/          # UI管理
│       ├── CoroutineSystem/   # 协程管理
│       ├── CameraSystem/      # 相机系统
│       ├── YusTweenSystem/    # 补间动画
│       └── ...                # 其他模块
└── README.md
```

## 使用建议

- 需要什么模块就用什么，不用全部导入
- 对象池适合频繁创建销毁的对象（子弹、敌人、特效等）
- 计时器比Coroutine性能好，延迟调用优先用计时器
- 事件系统用扩展方法 `this.YusRegisterEvent()` 会自动解绑
- Excel配置表适合策划改的数据

## 常见问题

**Q: 需要导入整个框架吗？**  
A: 不需要，每个模块基本独立，用哪个导哪个。

**Q: Unity版本要求？**  
A: 推荐2022.3 LTS，理论上2020+都能用。

**Q: 会影响性能吗？**  
A: 不会，核心系统都做了优化，对象池和计时器还是零GC的。

**Q: 可以商用吗？**  
A: 可以，MIT协议，随便用。

## 许可证

MIT License - 随便用，改，商用都行。

## 联系

有问题可以提Issue：https://github.com/Yustardenia/YusGameFrame/issues

---

就这样，简单记录一下。
