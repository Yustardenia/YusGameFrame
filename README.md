# Unity项目完整教程

基于Yus框架的Unity开发完整解决方案

-   [1. Attributes](#attributes)
-   [2. EditorProMax](#editorpromax)
-   [3. ExcelTool](#exceltool)
-   [4. GameControls](#gamecontrols)
-   [5. MusicControl](#musiccontrol)
-   [6. PoolSystem](#poolsystem)
-   [7. ResLoadSystem](#resloadsystem)
-   [8. SimpleBinary](#simplebinary)
-   [9. UISystem](#uisystem)
-   [10. YusAssetExporter](#yusassetexporter)
-   [11. YusEventSystem](#yuseventsystem)
-   [12. YusFSM](#yusfsm)
-   [13. AnimSystem](#anim)
-   [14. YusGameFrame 本地化系统](#localizationsystem)

[Top](#top "回到顶部") [1](#attributes "跳转到第1层") [2](#editorpromax "跳转到第2层") [3](#exceltool "跳转到第3层") [4](#gamecontrols "跳转到第4层") [5](#musiccontrol "跳[...]")

## 1. MyAttributes - 强大自定义属性系统（完整版）

一套专为快速迭代调试而生的属性工具集合，完全自动化，无需手动注册，支持运行时实时监视、PlayMode 值保留、自动组件注入、场景选择器等功能。

实时屏幕监视

退出PlayMode自动保存值

自动获取组件（无需拖拽）

场景选择下拉框

### 核心特性一览

#### [Watch] + GlobalWatcher 运行时

标记字段/属性后，运行时会在屏幕左上角实时显示其值（绿色粗体）。支持自定义标签名。

```
[Watch]
// 或
[Watch("玩家血量 HP")]
public int health = 100;

[Watch("当前状态")]
public PlayerState state;
```

GlobalWatcher 会自动在游戏启动时创建一个名为 `[GlobalWatcher]` 的 DontDestroyOnLoad 对象，每秒扫描一次场景中所有标记的字段并显示。

#### [KeepValue] 编辑器专用

退出 Play Mode 时自动保存字段值，重新进入 Play Mode 时自动恢复。非常适合调试参数。

支持类型：int、float、bool、string、Vector2/3、Color、以及任何带 [Serializable] 的类/结构体（通过 JsonUtility）。

```
[KeepValue]
public float moveSpeed = 5f;

[KeepValue]
public Vector3 spawnPoint;

[KeepValue]
public GameMode currentMode;
```

恢复后会在控制台输出彩色日志，并自动标记场景为“已修改”（出现 * 号）。

#### [Get] 自动组件注入 运行时+编辑器

无需 [SerializeField] 也能自动获取组件引用。支持 private 字段，完美解决“运行时报空”问题。

```
// 从自身获取
[Get]
private Rigidbody rb;

[Get]
private Animator anim;

// 从子物体获取（包括未激活的）
[Get(true)]
private Transform muzzle;

// 自动注入时机：
// 编辑器：按下 Play 前一刻
// 运行时：AfterSceneLoad（Domain Reload 后自动补回）
```

如果已经手动拖了组件，会优先保留手动赋值，不覆盖。

#### [SceneSelector] 场景选择器 编辑器专用

将 string 或 int 字段变成场景下拉选择框（只显示 Build Settings 中启用的场景）。

```
[SceneSelector]
public string nextLevel;          // 显示场景名

[SceneSelector]
public int levelIndex;            // 显示 Build Index
```

对应的自定义绘制器代码在 `SceneSelectorDrawer.cs`

### 完整使用教程（一步一步教你）

#### 步骤1：把整个 MyAttributes 文件夹放入项目

路径建议：`Assets/Plugins/MyAttributes/`

包含以下文件（缺一不可）：

-   `MyAttributes.cs`（属性定义）
-   `GlobalWatcher.cs`（运行时监视器）
-   `Editor/AutoGetInjector.cs`
-   `Editor/KeepValueProcessor.cs`
-   `Editor/SceneSelectorDrawer.cs`（上面已给出完整代码）

#### 步骤2：在任意 MonoBehaviour 上使用

```
public class PlayerController : MonoBehaviour
{
    // 1. 实时监视
    [Watch("生命值 ❤")]
    public int health = 100;

    [Watch]
    public Vector3 velocity;

    // 2. 调试时保留值
    [KeepValue]
    public float moveSpeed = 7f;

    [KeepValue]
    public bool godMode = false;

    // 3. 自动获取组件（无需拖拽）
    [Get]
    private Animator anim;

    [Get(true)]
    private AudioSource sfxSource;

    // 4. 场景选择器
    [SceneSelector]
    public string nextSceneName;

    [SceneSelector]
    public int nextSceneIndex = 1;
}
```

#### 步骤3：直接按 Play 即可看到效果

-   屏幕左上角出现绿色文字实时显示所有 `[Watch]` 的值
-   修改 `[KeepValue]` 的字段 → 停止 Play → 再次 Play → 值还在！
-   `[Get]` 的组件即使是 private 且没 [SerializeField]，运行时也不会空
-   `[SceneSelector]` 字段在 Inspector 变成下拉框

### 工作原理速览（技术向）

#### GlobalWatcher

`RuntimeInitializeOnLoadMethod(AfterSceneLoad)` 自动创建 → 每秒 `FindObjectsOfType<MonoBehaviour>` + 反射扫描 `[Watch]` → OnGUI 绘制

#### KeepValue

退出 PlayMode → 用 `GlobalObjectId` + `EditorPrefs` 保存 → 进入 EditMode → 恢复并 `SetDirty`

#### Get 自动注入

编辑器按 Play 前 + 运行时 AfterSceneLoad 两个时机执行 `GetComponent/InChildren` 注入

### 常见问题 & 注意事项

-   **性能：** GlobalWatcher 每秒扫描一次，1000 个物体以下几乎无感知。物体极多时可改为手动注册。
-   **KeepValue 不支持的类型：** 纯 C# 类（无 [Serializable]）、GameObject/Transform 引用等复杂引用类型会失败。
-   **Domain Reload：** 进入 PlayMode 时脚本域重载会导致 private 字段变 null，`[Get]` 的运行时注入专门解决这个问题。
-   **不要删除自动生成的 [GlobalWatcher] 对象**，它是 DontDestroyOnLoad 的单例。
-   所有功能在 Build 后自动失效（#if UNITY_EDITOR 包裹），不会影响打包体积和性能。

**现在你已经拥有了一个比 NaughtyAttributes 更轻量、更专注调试的超级属性工具包！**  
写代码 → 加属性 → 直接 Play → 调参飞起 → 永远不用重复设置调试值

## 2. EditorProMax - 编辑器工具集

提供强大的编辑器扩展功能，包括资源侦探、场景切换、代码统计、文件夹着色等开发工具。

### 核心功能

#### AssetDetective

资源侦探工具，支持三种模式：

-   引用查找：查找谁引用了指定资源
-   废弃资源：检测未使用的资源
-   重复资源：通过MD5查找重复文件

#### EssentialToolkit

开发效率工具集：

-   快速场景切换
-   代码行数统计
-   待办事项便签
-   资源收藏夹

#### FolderColorizer

文件夹着色工具，为不同类型的文件夹设置颜色标识。

### 使用教程

#### 资源侦探使用

右键点击资源选择相应功能：

```
// 查找引用
Assets/Asset Detective/🔍 查找谁引用了我

// 查找废弃资源
Tools/Asset Detective/🗑️ 查找废弃资源

// 查找重复资源
Tools/Asset Detective/👯 查找重复资源
```

#### 文件夹着色配置

通过Tools菜单打开配置窗口：

```
Tools/🎨 文件夹染色配置

// 默认颜色规则：
- Scripts: 红色
- Scenes: 绿色
- Prefabs: 紫色
- Resources: 蓝色
- Editor: 灰色
```

### 工作流程

1. 选择资源

→

2. 执行检测

→

3. 查看结果

→

4. 清理优化

## 3. ExcelTool - 终极二进制配置表 + 存档系统

一套**完全自动化**的 Excel → C# → ScriptableObject → 运行时读写 + 二进制存档 + 资源自动重连 + Excel反写 的闭环数据解决方案。  
比 Excel2SO、Odin、YooAsset 配置表更轻量、更快、更适合中型 RPG/对话重度项目。

一键生成 Data + Table 类

自动导出 SO 配置表

二进制极速存档

图片/Prefab 自动重连

运行时修改 → 反写回 Excel

完美集成 Fungus 对话系统

### 核心架构图

Excel  
(Excels/)

生成代码 + 导出 SO

Gen/*.cs  
+ Resources/YusData/*.asset

运行时克隆 + 资源重连

YusBaseManager<TTable,TData>

修改 → Save()

persistentDataPath/SaveData/*.yus

Dev_WriteBackToExcel()

Excel 被反写！

### 核心类详解

#### ExcelYusTool 编辑器工具

菜单 `Tools → Yus Data` 的两大核心功能：

-   **1. 生成代码** → 自动生成 `*Data.cs` + `*Table.cs`
-   **2. 导出数据到 SO** → 生成 `Resources/YusData/*.asset`

#### YusTableSO<TKey,TData> 运行时配置表基类

所有生成的 `*Table` 继承自它，提供 `Get(key)`、`GetAll()`、自动字典缓存。

#### YusBaseManager<TTable,TData> 运行时数据管理器基类

你只需要继承一次，全部功能自动拥有：

-   自动加载配置表或读档
-   资源（Sprite/Prefab）自动重连（解决存档后图片丢失）
-   Save() 一键二进制存档
-   Dev_WriteBackToExcel() 右键反写回 Excel
-   Dev_ResetSave() 重置存档

#### YusDataManager 全局单例

核心枢纽，负责：

-   配置表缓存（Resources.Load）
-   二进制读写
-   运行时克隆 + 资源重连
-   编辑器下调用 ExcelYusWriter 反写

#### ExcelYusWriter 反写工具

运行时修改数据后 → 右键 → “开发者/反写回 Excel”，即可把内存数据写回原 Excel 文件！

### 使用教程（手把手教学）

#### 步骤1：准备 Excel（只需要做一次）

放入 `Assets/ExcelTool/Excels/` 目录，格式严格如下：

```
# 第1行：字段名（英文）
id          name        durability    icon         desc
# 第2行：类型（支持简写)
int         string      float         Sprite       string
# 第3行：key标记（有且仅有一列写 key)
key                                     
```

支持类型：int、float、bool、string、Vector3、Sprite、GameObject(Prefab)

#### 步骤2：一键生成代码 + 导出数据

菜单 → **Tools → Yus Data → 1. 生成代码**  
→ **2. 导出数据到 SO**

会自动生成：

-   `Assets/ExcelTool/Yus/Gen/BackpackData.cs`
-   `BackpackTable.cs`
-   `Assets/Resources/YusData/BackpackTable.asset`

#### 步骤3：创建运行时管理器（只需继承一次）

```
public class BackpackManager : YusBaseManager<BackpackTable, BackpackData>
{
    public static BackpackManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    protected override string SaveFileName => "PlayerBackpack"; // 存档文件名

    // 示例：使用物品
    public void UseItem(int itemId)
    {
        var item = DataList.Find(x => x.id == itemId);
        if (item != null)
        {
            item.durability -= 10;
            Save();                    // 自动二进制存档
            Dev_WriteBackToExcel();    // 调试时反写回 Excel
        }
    }
}
```

挂到场景任意 GameObject 即可，推荐做成单例。

#### 步骤4：Fungus 对话系统完美集成（开箱即用）

已内置 3 个 Fungus Command：

-   **Dialogue Trigger Condition** → 判断对话是否可触发
-   **Increment Dialogue Count** → 触发次数+1
-   **Set Dialogue Trigger** → 强制设置可触发状态

配合 `DialogueKeyManager.cs` 使用，支持运行时动态添加对话键。

### 进阶功能展示

#### 资源自动重连（解决存档后图片丢失）

存档只存名字，读档后自动根据 ID 从配置表把 Sprite/Prefab 重新塞回去，**永不丢失图片**。

#### Excel 反写（调试神器）

运行时改了耐久、开关状态 → 右键管理器 → “开发者/反写回 Excel” → Excel 文件被实时更新！

#### 支持运行时动态添加数据

```
// DialogueKeyManager 示例
DialogueKeyManager.Instance.AddDynamicDialogue(
    newId: 999,
    npcId: 1,
    text: "这是运行时生成的对话！",
    initialCanTrigger: true
);
```

### 目录结构一览（建议）

Assets/ExcelTool/
├── Excels/                  ← 放所有 .xlsx
├── Yus/
│   └── Gen/                 ← 自动生成代码（勿手动修改）
├── Scripts/                 ← 核心运行时代码
├── Editor/                  ← 编辑器工具
├── Example-Backpack/        ← 示例：背包系统
└── Fungus-DialogueKey/      ← Fungus 专用对话钥匙系统 + 3个Command
    

### 常见问题 & 注意事项

-   Excel 文件名就是表名（如 `Backpack.xlsx` → `BackpackTable`）
-   有且仅有 **一列** 第三行写 `key`
-   修改 Excel 后记得重新 “生成代码 + 导出数据”
-   打包后自动移除所有 Editor 代码（反写功能只在编辑器）
-   存档路径：PC 为 `%userprofile%\AppData\LocalLow\你的公司\你的游戏\SaveData\`
-   性能极高：1000条数据存档

**恭喜！你现在拥有了一个比 90% 商业项目还强的配置表+存档系统！**  
从此告别手动拖资源、JSON 字符串、存档图片丢失、策划改表要重打 AB 包的痛苦

## 4. GameControls - 全新输入系统（终极版）

基于 Unity 新输入系统（Input System Package）的完整封装，**零手动订阅、自动防漏、支持改键保存、模式切换、一键生成控制器**，彻底告别 `OnEnable/On[...]`

自动注册 + 自动解绑

一键生成控制器代码

Gameplay / UI 模式无缝切换

改键永久保存

支持 Hold、MultiTap 等交互

完全兼容 Player Input 组件

### 核心架构图

GameControls.inputactions  
（可视化编辑器）

自动生成

GameControls.cs  
（勿手动修改）

全局单例

YusInputManager  
模式切换 + 改键保存

扩展方法

this.YusRegisterInput()  
自动订阅 + 自动清理

一键生成

PlayerController / UIController  
干净、标准、无需写 OnEnable

### 核心类详解

#### YusInputManager 全局单例

整个输入系统的核心枢纽，挂一个空物体即可：

-   `EnableGameplay()` → 开启移动、跳跃、攻击
-   `EnableUI()` → 开启 UI 操作（自动禁用游戏输入）
-   `DisableAll()` → 过场动画、锁输入
-   自动加载/保存玩家改键（Json 存本地）

#### YusInputExtensions + YusInputAutoCleaner 黑魔法

**彻底解放你**：再也不用写 `OnEnable/OnDisable` 订阅事件！

```
this.YusRegisterInput(
    YusInputManager.Instance.controls.Gameplay.Jump,
    ctx => Jump()
);
```

物体销毁时自动解绑，杜绝内存泄漏。

#### YusInputCodeGenerator 编辑器神器

菜单 `Tools → Yus Tools → 6. 输入脚本生成器`  
自动扫描 `GameControls.inputactions`，生成以下内容：

-   `PlayerController.cs`（Gameplay 动作）
-   `UIController.cs`（UI 动作）
-   每个 Action 都自动生成缓存字段 + OnXXX 方法

#### GameControls.cs 自动生成

由 Unity 官方生成器生成，包含 `Gameplay` 和 `UI` 两个 Action Map。

### 使用教程（3分钟上手）

#### 步骤1：创建并配置 Input Actions（只需一次）

右键 → Create → Input Actions → 命名为 `GameControls`

建议配置：

-   Action Map: `Gameplay`（移动、跳跃、攻击、冲刺）
-   Action Map: `UI`（确认、取消、导航）
-   支持 Interactions：Hold、Press、MultiTap 等

#### 步骤2：挂载 YusInputManager（只需一次）

创建一个空物体 → 挂上 `YusInputManager.cs` → 自动成为全局单例

#### 步骤3：一键生成控制器代码（推荐）

**Tools → Yus Tools → 6. 输入脚本生成器**

自动生成两个脚本：

```
// PlayerController.cs（示例）
public class PlayerController : MonoBehaviour
{
    [Header("Input Cache")]
    [SerializeField] private Vector2 _inputMove;

    void Start()
    {
        this.YusRegisterInput(YusInputManager.Instance.controls.Gameplay.Move,   OnMove);
        this.YusRegisterInput(YusInputManager.Instance.controls.Gameplay.Jump,   OnJump);
        this.YusRegisterInput(YusInputManager.Instance.controls.Gameplay.Fire,   OnFire);
        this.YusRegisterInput(YusInputManager.Instance.controls.Gameplay.Dash,   OnDash);
    }

    private void OnMove(InputAction.CallbackContext ctx)   => _inputMove = ctx.ReadValue();
    private void OnJump(InputAction.CallbackContext ctx)   => Jump();
    private void OnFire(InputAction.CallbackContext ctx)   => Fire();
    private void OnDash(InputAction.CallbackContext ctx)   => Dash();

    void FixedUpdate() => Move(_inputMove);
}
```

#### 步骤4：模式切换（关键！）

```
// 打开背包 / 对话框时
YusInputManager.Instance.EnableUI();

// 关闭背包 / 对话结束
YusInputManager.Instance.EnableGameplay();

// 播放过场动画
YusInputManager.Instance.DisableAll();
```

#### 步骤5：支持玩家改键 + 永久保存

在设置界面调用：

```
// 开始改键（示例：重新绑定跳跃）
var rebindOp = YusInputManager.Instance.controls.Gameplay.Jump.PerformInteractiveRebinding()
    .OnComplete(op => {
        YusInputManager.Instance.SaveBindingOverrides(); // 保存
        op.Dispose();
    })
    .Start();
```

游戏启动时自动调用 `LoadBindingOverrides()` 即可恢复玩家设置。

### 最佳实践示例

#### 对话系统集成（Fungus / Dialogue System）

```
public void StartDialogue()
{
    YusInputManager.Instance.EnableUI();     // 锁住玩家操作
    // ... 开启对话
}

public void EndDialogue()
{
    YusInputManager.Instance.EnableGameplay(); // 恢复操作
}
```

#### 暂停菜单

```
public void OpenPauseMenu()
{
    YusInputManager.Instance.EnableUI();
    Time.timeScale = 0;
}

public void ClosePauseMenu()
{
    YusInputManager.Instance.EnableGameplay();
    Time.timeScale = 1;
}
```

### 目录结构建议

Assets/GameControls/
├── GameControls.inputactions          ← 主输入资产
├── GameControls.cs                    ← 自动生成（勿改）
├── YusInputManager.cs                 ← 全局管理器
├── YusInputExtensions.cs              ← 自动注册扩展
├── YusInputAutoCleaner.cs             ← 隐形清理组件
├── Controllers/
│   ├── PlayerController.cs            ← 自动生成
│   └── UIController.cs                 ← 自动生成（如有 UI 动作）
└── Editor/
    └── YusInputCodeGenerator.cs        ← 一键生成器
    

### 常见问题 & 注意事项

-   永远不要手动 `+=` 事件！使用 `YusRegisterInput` 即可
-   移动类输入必须缓存到字段，在 `FixedUpdate` 使用
-   改键后务必调用 `SaveBindingOverrides()`
-   支持手柄、键盘、触摸，完全自动适配
-   打包后自动移除所有 Editor 代码

**恭喜！你现在拥有了一个比 99% 商业游戏还先进的输入系统！**  
从此告别输入漏订阅、模式混乱、改键不保存、代码重复的痛苦。  
真正的“一次配置，永久爽”。

## 5. MusicControl - 专业级音频管理系统（商业级）

一套**完整、优雅、零坑**的音频解决方案，彻底解决 BGM 被打断无法恢复、音效音量不统一、音量设置不保存、Fungus 播放混乱等 99% 项目都踩过的坑。

BGM 与 SFX 完全分离

全局音量自动保存

临时切换 + 自动恢复（战斗/剧情神器）

AudioLibrary 集中管理 + 音量微调

Fungus 原生三连命令（开箱即用）

音量变化实时广播

### 核心功能亮点

#### 临时切换 + 自动恢复

进入战斗 → 切 Boss 战 BGM → 战斗结束 → 自动回到之前进度继续播放地图音乐  
`SwitchMusicTemporary("BossTheme") → ReturnToPreviousMusic()`

#### 全局音量永久保存

玩家在设置里调了音量 → 自动二进制存档 → 下次启动自动恢复

#### 单音效音量微调

某个跳跃音效太吵？在 AudioLibrary 里把 `volumeScale` 调到 0.6 即可

### 核心类详解

#### AudioData 静态数据层

只负责存取和广播，永不播放：

-   `AudioData.MusicVolume` / `SFXVolume`
-   自动加载/保存（基于 SimpleSingleValueSaver）
-   音量变化 → 自动广播 `YusEvents.OnMusicVolChange`

#### AudioLibrary ScriptableObject 音效库

集中管理所有音效，支持多库：

-   支持 `soundName` 自定义 Key
-   每个音效独立 `volumeScale` 微调
-   运行时自动构建字典，查找 O(1)

#### SceneAudioManager 场景单例

全局唯一音频播放器，挂一个空物体即可：

-   自动创建 `MusicSource` 和 `SFXSource`
-   支持 `PlayMusic(clip/name)`、`PlaySFX(name)`
-   完整临时切换逻辑（记住进度 + 自动恢复）
-   实时监听音量变化自动更新

### 使用教程（3分钟上手）

#### 步骤1：创建 AudioLibrary（推荐拆分多个库）

右键 → Create → Audio → AudioLibrary

```
// 示例：UI音效库
[CreateAssetMenu(menuName = "Audio/AudioLibrary")]
public class AudioLibrary : ScriptableObject
{
    public List sounds;

    [Serializable]
    public class SoundItem
    {
        public string soundName;     // 关键！比如 "Jump", "Coin", "Button_Click"
        public AudioClip clip;
        [Range(0f, 1f)] public float volumeScale = 1f;
    }
}
```

建议按类型拆库：UI库、角色库、环境库、BGM库

#### 步骤2：挂载 SceneAudioManager（只需一次）

创建一个空物体 → 挂上 `SceneAudioManager.cs`

配置：

-   `Default BGM`：启动时自动播放
-   `Audio Libraries`：拖入所有你创建的库

#### 步骤3：播放音效（超简单）

```
// 播放背景音乐（支持名字）
SceneAudioManager.Instance.PlayMusic("MainTheme");
SceneAudioManager.Instance.PlayMusic("BossBattle");

// 播放音效
SceneAudioManager.Instance.PlaySFX("Jump");
SceneAudioManager.Instance.PlaySFX("Coin_Pickup");

// 临时切换（战斗开始）
SceneAudioManager.Instance.SwitchMusicTemporary("BossBattle");

// 战斗结束 → 自动回到之前那首 + 进度
SceneAudioManager.Instance.ReturnToPreviousMusic();

// 暂停/恢复（打开菜单）
SceneAudioManager.Instance.PauseMusic();
SceneAudioManager.Instance.ResumeMusic();
```

#### 步骤4：设置界面控制音量（自动保存）

```
// Slider 拖动时调用
AudioData.SetMusicVolume(slider.value);
AudioData.SetSFXVolume(slider.value);

// 所有播放中的音源会立刻更新音量
// 关闭游戏后再次进入依然保留玩家设置
```

### Fungus 完美集成（三连命令，开箱即用）

#### Play Music (Yus)

播放指定背景音乐

`SceneAudioManager.Instance.PlayMusic(musicName)`

#### Play SFX (Yus)

播放音效

`SceneAudioManager.Instance.PlaySFX(soundName)`

#### Switch/Return Music

临时切换或恢复上一首

`SwitchMusicTemporary("Boss") 或 ReturnToPreviousMusic()`

### 最佳实践场景

#### 战斗系统集成

```
public void StartBattle()
{
    SceneAudioManager.Instance.SwitchMusicTemporary("BossBattle");
}

public void EndBattle()
{
    SceneAudioManager.Instance.ReturnToPreviousMusic();
}
```

#### 剧情过场

```
// Fungus Flowchart
Play Music (Yus) → "EmotionalScene"
→ 对话...
→ Switch/Return Music → Return
```

### 目录结构建议

Assets/MusicControl/
├── AudioData.cs
├── AudioLibrary.cs
├── SoundItem.cs
├── SceneAudioManager.cs
├── Libraries/
│   ├── BGM_Library.asset
│   ├── UI_SFX_Library.asset
│   ├── Character_SFX_Library.asset
│   └── Environment_SFX_Library.asset
└── FungusEx/
    ├── PlayMusicCommand.cs
    ├── PlaySFXCommand.cs
    └── SwitchMusicCommand.cs
    

### 常见问题 & 注意事项

-   `soundName` 必须填写，否则用文件名（容易冲突）
-   多个 AudioLibrary 时，相同 `soundName` 后加入的会覆盖前面的
-   BGM 建议放在专门的 BGM 库，避免和 SFX 混淆
-   音效不要勾 `Play On Awake`，全部由系统控制
-   所有音量调节都走 `AudioData.SetXXXVolume`，不要直接改 AudioSource.volume

**恭喜！你现在拥有了一个比大多数商业游戏还强的音频系统！**  
从此告别：

-   BGM 被打断后变成死寂
-   玩家调了音量下次启动又恢复默认
-   某个音效特别吵只能全局压低
-   Fungus 里写一堆 AudioSource.PlayOneShot

真正的“一次配置，全游戏完美”。

## 6. PoolSystem - 工业级对象池系统（性能杀手级）

一套**零 GC、自动回收、延迟归还、实时监控、完全防漏**的对象池框架，专治“子弹/敌人/粒子/特效一多就卡死”的顽疾。

零 GC Alloc（真正意义上的）

延迟自动回收（子弹、粒子神器）

IPoolable 生命周期完美替代 Start/OnEnable

编辑器实时监控 + 使用率可视化

自动整理 Hierarchy（池子分门别类）

支持预热 + 压力测试

### 核心架构图

Prefab  
（挂 IPoolable）

YusPoolManager.Get("路径")

从池取出  
OnSpawn()

使用中

Release() 或 ReturnToPool(2f)

归还池中  
OnRecycle() + StopAllCoroutines()

下次直接复用

### 核心类详解

#### YusPoolManager 全局单例

整个系统的核心大脑，挂一个空物体即可：

-   按资源路径自动分池（同一 Prefab 自动归一池）
-   自动创建 `PoolObject` 标记组件
-   提供 `ReturnToPool(delay)` 一键延迟回收
-   自动整理到 `=== YusPoolSystem ===` 下，层次结构超级干净
-   支持 `ClearAll()` 释放内存

#### PoolObject 自动添加

每个池对象都会自动挂上这个组件：

-   记录所属池路径
-   提供 `ReturnToPool(delay)` 一键延迟回收
-   自动停止所有协程（防止回收后还在跑逻辑）

#### IPoolable 生命周期接口

彻底替代 `Start/OnEnable/OnDisable`：

```
public void OnSpawn()   → 取出时调用（真正意义上的 Start）
public void OnRecycle() → 归还时调用（真正意义上的 OnDisable）
```

#### YusPoolDebugger 实时监控神器

菜单 `Tools → Yus Data → 5. 对象池监视器`

-   实时显示每个池的“闲置 / 使用中”数量
-   使用率进度条可视化
-   搜索 + 一键清空闲置对象
-   点击“选中池子根节点”直接跳到 Hierarchy

### 使用教程（2分钟上手）

#### 步骤1：挂载 YusPoolManager（只需一次）

创建一个空物体 → 挂上 `YusPoolManager.cs` → 自动成为全局单例

#### 步骤2：让 Prefab 支持池化（推荐实现 IPoolable）

```
public class Bullet : MonoBehaviour, IPoolable
{
    private Rigidbody rb;

    public void OnSpawn()
    {
        rb = GetComponent();
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        // 给一个初速度
        rb.AddForce(transform.forward * 50f, ForceMode.VelocityChange);
        
        // 5秒后自动回收
        this.ReturnToPool(5f);
    }

    public void OnRecycle()
    {
        // 清理逻辑（可选）
        Debug.Log($"{name} 已回收");
    }
}
```

#### 步骤3：生成与回收（超简单）

```
// 生成（路径相对于 Resources）
GameObject bullet = YusPoolManager.Instance.Get("Weapons/Bullet");

// 或者指定父节点
GameObject enemy = YusPoolManager.Instance.Get("Enemies/Goblin", enemyParent);

// 回收
YusPoolManager.Instance.Release(bullet);

// 延迟回收（粒子、子弹必备）
bullet.GetComponent().ReturnToPool(3f);
```

#### 步骤4：实时监控（开发必备）

**Tools → Yus Data → 5. 对象池监视器**

你会看到：

-   池子总数：32

---

## English Translation

# Complete Unity Project Guide

A complete Unity development solution built on the Yus framework.

-   [1. Attributes](#attributes)
-   [2. EditorProMax](#editorpromax)
-   [3. ExcelTool](#exceltool)
-   [4. GameControls](#gamecontrols)
-   [5. MusicControl](#musiccontrol)
-   [6. PoolSystem](#poolsystem)
-   [7. ResLoadSystem](#resloadsystem)
-   [8. SimpleBinary](#simplebinary)
-   [9. UISystem](#uisystem)
-   [10. YusAssetExporter](#yusassetexporter)
-   [11. YusEventSystem](#yuseventsystem)
-   [12. YusFSM](#yusfsm)
-   [13. AnimSystem](#anim)
-   [14. YusGameFrame Localization System](#localizationsystem)

[Top](#top)

## 1. MyAttributes — Powerful Custom Attribute System (Full Version)

A toolkit of attributes designed for rapid iteration and debugging. Fully automated with no manual registration required. Features include runtime live monitoring, PlayMode value retention, automatic component injection, scene selectors, and more.

-   On-screen live monitoring
-   Auto-save values when exiting PlayMode
-   Auto-fetch components (no drag-and-drop needed)
-   Scene selection dropdowns

### Key Features Overview

#### [Watch] + GlobalWatcher (Runtime)

Mark fields or properties with [Watch] and their values will be shown in bold green at the top-left of the screen at runtime. Custom labels are supported.

```
[Watch]
// or
[Watch("Player HP")]
public int health = 100;

[Watch("Current State")]
public PlayerState state;
```

GlobalWatcher automatically creates a DontDestroyOnLoad GameObject named `[GlobalWatcher]` at game start, scans marked fields once per second, and renders them via OnGUI.

#### [KeepValue] (Editor-only)

Automatically saves field values when exiting Play Mode and restores them when entering Play Mode again. Great for tuning debug parameters.

Supported types: int, float, bool, string, Vector2/3, Color, and any [Serializable] classes/structs (via JsonUtility).

```
[KeepValue]
public float moveSpeed = 5f;

[KeepValue]
public Vector3 spawnPoint;

[KeepValue]
public GameMode currentMode;
```

Restored values print a colored log to the Console and mark the scene as modified.

#### [Get] Automatic Component Injection (Editor + Runtime)

Automatically injects component references without [SerializeField], including private fields, eliminating common null reference issues at runtime.

```
// get from same GameObject
[Get]
private Rigidbody rb;

[Get]
private Animator anim;

// get from children (including inactive)
[Get(true)]
private Transform muzzle;

// injection timings:
// Editor: just before Play is pressed
// Runtime: AfterSceneLoad (auto-fix after Domain Reload)
```

If a component has been manually assigned in the Inspector, the manual value is preserved.

#### [SceneSelector] Scene Dropdown (Editor-only)

Turns string or int fields into a scene dropdown showing only scenes enabled in Build Settings.

```
[SceneSelector]
public string nextLevel;          // shows scene name

[SceneSelector]
public int levelIndex;            // shows build index
```

The custom drawer is implemented in SceneSelectorDrawer.cs.

### Usage Guide (Step by step)

1. Copy the entire MyAttributes folder into your project.

Suggested path: `Assets/Plugins/MyAttributes/`

Required files:

-   `MyAttributes.cs`
-   `GlobalWatcher.cs`
-   `Editor/AutoGetInjector.cs`
-   `Editor/KeepValueProcessor.cs`
-   `Editor/SceneSelectorDrawer.cs`

2. Use attributes on any MonoBehaviour:

```
public class PlayerController : MonoBehaviour
{
    [Watch("HP ❤")]
    public int health = 100;

    [Watch]
    public Vector3 velocity;

    [KeepValue]
    public float moveSpeed = 7f;

    [KeepValue]
    public bool godMode = false;

    [Get]
    private Animator anim;

    [Get(true)]
    private AudioSource sfxSource;

    [SceneSelector]
    public string nextSceneName;

    [SceneSelector]
    public int nextSceneIndex = 1;
}
```

3. Press Play to see the effects:

-   `[Watch]` values appear as green text in the top-left of the screen
-   `[KeepValue]` fields retain values after stopping/starting Play Mode
-   `[Get]` injected fields won't be null at runtime even if private or not serialized
-   `[SceneSelector]` fields become dropdowns in the Inspector

### How it works (technical)

GlobalWatcher: created with `RuntimeInitializeOnLoadMethod(AfterSceneLoad)`, scans with `FindObjectsOfType<MonoBehaviour>` once per second + reflection, draws via OnGUI.

KeepValue: on exiting PlayMode it saves values using GlobalObjectId + EditorPrefs, then restores them in EditMode and calls SetDirty.

Get injection: performed both just before Play in the Editor and on AfterSceneLoad at runtime.

### FAQ & Notes

-   Performance: GlobalWatcher scans once per second and is negligible under ~1000 objects. For very large scenes, consider manual registration.
-   KeepValue does not support pure C# classes (non-[Serializable]) or complex references like GameObject/Transform.
-   Domain Reload can null private fields on entering PlayMode; runtime [Get] injection handles this.
-   Do not delete the auto-created [GlobalWatcher] GameObject — it is a DontDestroyOnLoad singleton.
-   All features are editor-only (#if UNITY_EDITOR) and do not affect builds.

Now you have a lightweight, debugging-focused attribute toolkit that’s even more streamlined than NaughtyAttributes.

## 2. EditorProMax — Editor Tools Collection

A set of powerful editor extensions including an asset detective, quick scene switching, code stats, folder coloring, and other productivity tools.

### Highlights

#### AssetDetective

Three modes:

-   Find references (who references this asset)
-   Find unused assets
-   Find duplicate files by MD5

#### EssentialToolkit

Productivity helpers:

-   Quick scene switching
-   Code line counting
-   TODO sticky notes
-   Asset favorites

#### FolderColorizer

Colorize folders by type for visual clarity.

### How to use

Right-click an asset and choose:

```
// Find references
Assets/Asset Detective/🔍 Find who references me

// Find unused assets
Tools/Asset Detective/🗑️ Find unused assets

// Find duplicates
Tools/Asset Detective/👯 Find duplicate assets
```

Folder color settings are under Tools → 🎨 Folder Color Config.

Workflow: select asset → run detection → review results → clean up.

## 3. ExcelTool — Ultimate Binary Table + Save System

A fully automated pipeline: Excel → C# → ScriptableObject → runtime read/write + binary saves + automatic resource reconnection + Excel writeback. Lighter and faster than Excel2SO/Odin/YooAsset — ideal for mid-size RPGs and dialogue-heavy projects.

-   One-click code generation for Data + Table classes
-   Auto-export to SO
-   Fast binary save system
-   Automatic reconnecting of Sprites/Prefabs
-   Runtime edits can be written back to Excel
-   Built-in Fungus integration

### Architecture

Excel (Excels/) → generate code + export SO → Gen/*.cs + Resources/YusData/*.asset → runtime clone + reconnect → YusBaseManager<TTable,TData> → Save() → persistentDataPath/SaveData/*.yus → Dev_WriteBackToExcel() → Excel is updated back!

### Key classes

-   ExcelYusTool (Editor): Tools → Yus Data: generate code and export SO
-   YusTableSO<TKey,TData>: base class for generated tables, with Get/GetAll and caching
-   YusBaseManager<TTable,TData>: runtime data manager handling load/save, reconnects, and dev tools
-   YusDataManager: global singleton for table caching and binary IO
-   ExcelYusWriter: editor tool to write runtime changes back to Excel

### Quick start

1. Put Excel files in `Assets/ExcelTool/Excels/` with the required format (first row: field names; second row: types; third row: key marker).

2. Tools → Yus Data → 1. Generate Code → 2. Export Data to SO

3. Create a manager by inheriting YusBaseManager<BackpackTable, BackpackData> and attach it as a singleton in the scene.

4. Use the built-in Fungus Commands for dialogue integration (Dialogue Trigger Condition, Increment Dialogue Count, Set Dialogue Trigger).

Advanced features include resource reconnection (prevents lost images in saves), Excel writeback for debugging, and runtime dynamic data addition.

## 4. GameControls — Input System (Ultimate)

A complete wrapper around Unity’s new Input System with zero manual subscription, automatic cleanup, rebind saving, mode switching, and one-click controller code generation.

-   Auto-register and auto-unregister handlers
-   One-click code generation for controllers
-   Seamless Gameplay/UI mode switching
-   Persistent rebinds
-   Supports Hold, MultiTap, etc.
-   Fully compatible with PlayerInput

Core components: GameControls.inputactions → auto-generated GameControls.cs (do not edit) → YusInputManager singleton for mode/rebind management → YusRegisterInput extension that auto-subscribes and auto-cleans.

Usage: create Input Actions asset, add YusInputManager to the scene, optionally generate controller scripts via Tools → Yus Tools → 6. Input Code Generator, and use this.YusRegisterInput(...) to register actions without writing OnEnable/OnDisable.

## 5. MusicControl — Production-grade Audio System

A complete, robust audio system that separates BGM and SFX, persists global volumes, supports temporary switches with automatic restoration (great for battles/cutscenes), manages audio via AudioLibrary ScriptableObjects, and integrates with Fungus.

Features: temporary music switching and restore, persistent global volume saved to disk, per-sound volume scaling, runtime dictionary lookup for O(1) access, and scene-level audio manager for playback.

## 6. PoolSystem — Industrial-grade Object Pooling

A zero-GC, auto-recycling, delayed-return, monitored pooling system that prevents performance spikes for bullets, enemies, particles, and VFX. Includes IPoolable lifecycle methods (OnSpawn / OnRecycle), editor monitoring, hierarchy organization, prewarm and stress-test support.

--- (End of appended translation — if you want me to translate the remaining sections beyond PoolSystem or refine wording, I can update the file again.)
