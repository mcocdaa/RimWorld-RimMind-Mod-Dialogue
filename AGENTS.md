# AGENTS.md — RimMind-Dialogue

本文件供 AI 编码助手阅读，描述 RimMind-Dialogue 的架构、代码约定和扩展模式。

## 项目定位

RimMind-Dialogue 是 RimMind 套件的**AI 对话系统**。它通过拦截游戏事件触发 AI 生成的对话，为 RimWorld 小人注入动态人格表达。

**核心职责**：
1. **事件拦截** — 通过 Harmony Patch 监听 Chitchat、Hediff、技能升级、心情变化等事件
2. **AI 对话生成** — 调用 RimMind-Core API 生成上下文感知的对话内容
3. **Thought 注入** — 将对话产生的心理影响转化为游戏内 Thought（独白 + 关系）
4. **关系变化** — AI 可输出 `relation_delta` 影响小人间好感度
5. **玩家对话** — 提供主动与小人对话的 UI 界面（多轮对话）
6. **对话日志** — 记录所有对话历史，支持分类查看
7. **记忆桥接** — 通过 `MemoryBridge` 将对话推送到 RimMind-Memory（反射松耦合）

**依赖关系**：
- 依赖 RimMind-Core 提供的 API（`RimMindAPI`、`SettingsUIHelper`、`ContextKeyRegistry`、`TaskInstructionBuilder`）和上下文构建
- 与 RimMind-Personality 协作（人格档案影响对话风格，通过 Core 上下文系统自动注入）
- 与 RimMind-Memory 松耦合（通过反射调用 `RimMindMemoryAPI.AddMemory`，运行时通过 `ModsConfig.IsActive("mcocdaa.RimMindMemory")` 检测）
- 与 RimMind-Actions 松耦合（通过反射调用 `RimMindActionsAPI.Execute`，运行时通过 `ModsConfig.IsActive("mcocdaa.RimMindActions")` 检测）

**构建信息**：
- Target: `net48`，LangVersion 9.0，Nullable enable
- Assembly: `RimMindDialogue`，RootNamespace: `RimMind.Dialogue`
- Harmony ID: `"mcocdaa.RimMindDialogueStandalone"`
- 条件编译：默认生成 `V1_6` 宏（`FloatMenuPatch` 使用 `#if V1_5` 适配 1.5 API）
- NuGet: `Krafs.Rimworld.Ref (1.6.*)`、`Lib.Harmony.Ref (2.*)`、`Newtonsoft.Json 13.0.*`
- 本地引用: `RimMindCore.dll`（`../../RimMind-Core/$(GameVersion)/Assemblies/`）

## 源码结构

```
Source/
├── RimMindDialogueMod.cs           Mod 入口，注册 Harmony，初始化设置，注册上下文 Provider / API 回调
├── Core/
│   ├── RimMindDialogueService.cs   核心服务：事件处理、对话生成、日志管理、并发控制
│   ├── DialogueService.cs          玩家对话服务（多轮，使用 RimMindAPI.Chat）
│   ├── NpcResponseHandler.cs       统一处理 NpcChatResult（自动对话和玩家对话共用）
│   └── MemoryBridge.cs            跨模组记忆桥接（反射调用 RimMindMemoryAPI）
├── Comps/
│   ├── CompRimMindDialogue.cs      ThingComp：自动对话触发 + Gizmo
│   └── CompProperties_RimMindDialogue.cs
├── UI/
│   ├── Window_Dialogue.cs          玩家对话窗口（聊天界面）
│   ├── Window_DialogueLog.cs       对话日志窗口（分类 + 双栏对话视图）
│   └── DialogueOverlay.cs          MapComponent + 内嵌 Harmony Patch，屏幕浮窗覆盖层
├── Thoughts/
│   ├── Thought_RimMindDialogue.cs  自定义独白 Thought（Thought_Memory 子类）
│   ├── Thought_RelationDialogue.cs 自定义关系 Thought（Thought_MemorySocial 子类）
│   └── ThoughtInjector.cs          Thought 注入工具（独白 + 关系）
├── Patches/
│   ├── BubblePatch.cs              Chitchat/DeepTalk 拦截
│   ├── HediffPatch.cs              健康变化监听
│   ├── SkillLearnPatch.cs          技能升级监听（Prefix 记录旧等级 + Postfix 检测提升）
│   ├── ThoughtPatch.cs             心情变化监听
│   ├── FloatMenuPatch.cs           右键菜单添加对话选项（V1_5/V1_6 条件编译）
│   ├── GameLoadPatch.cs            游戏加载/新游戏初始化
│   └── AddCompToHumanlikePatch.cs  为人形种族自动挂载 Comp
├── Settings/
│   └── RimMindDialogueSettings.cs  模组设置（全部可序列化）
└── Debug/
    └── DialogueDebugActions.cs     Dev 菜单调试动作
```

## 关键类与 API

### RimMindDialogueService

核心服务类（static），处理所有自动触发的对话：

```csharp
public enum DialogueTriggerType { Chitchat, Hediff, LevelUp, Thought, Auto, PlayerInput }

public enum DialogueCategory { ColonistMonologue, ColonistDialogue, PlayerDialogue, NonColonistMonologue, NonColonistDialogue }

public static class RimMindDialogueService
{
    static void HandleTrigger(Pawn pawn, string context, DialogueTriggerType type, Pawn? recipient, bool isReply = false);
    static void TryTriggerReply(Pawn originalSender, Pawn replier, string originalMessage);
    static void DisplayInteraction(Pawn initiator, Pawn? recipient, string replyText);
    static string GetTriggerLabel(DialogueTriggerType type);
    static DialogueCategory GetCategory(Pawn initiator, Pawn? recipient);

    static void AddLogEntry(Pawn pawn, Pawn? recipient, DialogueTriggerType triggerType, string context, string reply, string? thoughtTag, string? thoughtDesc);
    static void RecordDailyDialogue(int idA, int idB);
    static int GetDailyDialogueCount(int idA, int idB);
    static bool IsDialoguePending(int pawnIdA, int pawnIdB);

    static Pawn? GetActiveRecipient(Pawn pawn);
    static void SetActiveRecipient(Pawn pawn, Pawn? recipient);

    static event Action? OnLogUpdated;
    static bool IsReady;
    static IReadOnlyList<DialogueLogEntry> LogEntries;
    static void ClearLog();
    static void NotifyGameLoaded();
}
```

**HandleTrigger 检查流程**：
1. `RimMindDialogueSettings.Get().enabled` — 总开关
2. `RimMindAPI.IsConfigured()` — API 是否配置
3. `IsReady` — 游戏开始延迟是否已过
4. `_pendingPawns.ContainsKey(pawnId)` — 同一小人是否正在请求
5. `RimMindAPI.ShouldSkipDialogue(pawn, type.ToString())` — Core 层跳过判定
6. `_pendingDialoguePairs.ContainsKey(pairKey)` — 同一对话对是否正在请求（仅对话）
7. `IsMonologueOnCooldown(pawn, type)` — 独白冷却检查
8. `IsDailyDialogueLimitReached(idA, idB)` — 每日对话限制检查（仅非回复对话）

**并发控制**：
- `_pendingPawns` ConcurrentDictionary<int, byte> — 防止同一小人并发请求
- `_pendingDialoguePairs` ConcurrentDictionary<(int,int), byte> — 防止同一对话对并发请求

**主线程调度**：
- ContinueWith 回调内使用 `LongEventHandler.ExecuteWhenFinished()` 将所有工作调度到主线程
- 这确保 MoteMaker、ThoughtInjector、Messages.Message 等 Unity/RimWorld API 在正确线程执行

**冷却机制**：
- `_recentTriggers` List — 按类型记录独白冷却
- `_dailyDialogueCounts` ConcurrentDictionary — 每日对话计数（按天清理）

**Pawn 缓存**：
- `_pawnCache` Dictionary<int, Pawn> — thingIDNumber → Pawn 映射，600 tick 刷新
- `_activeRecipients` ConcurrentDictionary<int, int> — 当前活跃对话对象映射

**AI 响应处理流程**（在 LongEventHandler.ExecuteWhenFinished 回调中，经 NpcResponseHandler.Handle）：
1. 从 `_pendingPawns` / `_pendingDialoguePairs` 移除
2. `DisplayInteraction()` — MoteMaker.ThrowText 显示气泡
3. 解析 `express_emotion` / `change_relationship` 命令
4. `ThoughtInjector.Inject()` — 注入独白 Thought
5. `ThoughtInjector.InjectRelationDelta()` — 注入关系 Thought
6. `AddLogEntry()` — 记录日志（ConcurrentBag，上限 500 条）
7. `MemoryBridge.AddMemory()` — 推送记忆（仅对话且 `ModsConfig.IsActive("mcocdaa.RimMindMemory")`）
8. `RecordDailyDialogue()` — 记录每日对话计数
9. `showThoughtNotification` — 可选屏幕通知
10. `TryTriggerReply()` — 可选自动回复（仅对话且 `enableDialogueReply`）
11. `_activeRecipients.TryRemove()` — 清理对话对象映射

### NpcResponseHandler

统一处理 NpcChatResult 的响应逻辑（自动对话和玩家对话共用）：

```csharp
public static class NpcResponseHandler
{
    static void Handle(NpcChatResult result, Pawn pawn, Pawn? recipient, string context, DialogueTriggerType type);
}
```

**命令解析**：
- `express_emotion` — 解析 emotion 参数，注入 Thought
- `change_relationship` — 解析 delta 参数，注入关系 Thought（仅 recipient != null）
- 其他命令 — 通过反射调用 RimMindActionsAPI.Execute（可选依赖 RimMind-Actions，带缓存）

### DialogueService

玩家主动对话服务（static）：

```csharp
public static class DialogueService
{
    static void RequestReply(Pawn pawn, string playerMessage, Pawn? initiator,
        Action<string> onReply, Action<string> onError);
}
```

内部使用 `RimMindAPI.Chat`，ContinueWith 回调通过 `LongEventHandler.ExecuteWhenFinished` 调度到主线程，然后调用 `NpcResponseHandler.Handle` + `onReply`/`onError` 委托。

⚠️ 已知问题：Gizmo 按钮发起的对话中 `initiator=null`，导致 `NpcResponseHandler.Handle` 将玩家对话当作独白处理。FloatMenu 路径已修复（传入 `initiator`）。详见 `docs/06-problem/RimMind-Dialogue.md` #1。

### MemoryBridge

跨模组记忆桥接（internal static），通过反射调用 `RimMindMemoryAPI.AddMemory`：

```csharp
internal static class MemoryBridge
{
    static void AddMemory(string content, string memoryType, int tick, float importance, string? pawnId = null);
}
```

反射目标：`RimMind.Memory.RimMindMemoryAPI, RimMindMemory` 程序集。首次调用时 Resolve 并缓存 `MethodInfo`，失败则静默跳过。调用方额外检查 `ModsConfig.IsActive("mcocdaa.RimMindMemory")`。

### ThoughtInjector

将对话产生的心理影响注入游戏（static）：

```csharp
static class ThoughtInjector
{
    static void Inject(Pawn pawn, Pawn? recipient, string tag, string? description);
    static void InjectRelationDelta(Pawn pawn, Pawn recipient, float delta);
}
```

Thought 标签与心情值映射：
- ENCOURAGED → +1（受到鼓励）
- HURT → -1（感到受伤）
- VALUED → +2（感到被重视）
- CONNECTED → +2（感到亲近）
- STRESSED → -2（感到压力）
- IRRITATED → -1（感到烦躁）

### DialogueLogEntry

```csharp
public class DialogueLogEntry
{
    public int tick;
    public string initiatorName = string.Empty;
    public int initiatorId;
    public bool initiatorIsColonist;
    public string? recipientName;
    public int recipientId;            // 无对象时为 -1
    public bool recipientIsColonist;
    public DialogueCategory category;
    public string trigger = string.Empty;  // 枚举的 ToString()
    public string context = string.Empty;
    public string reply = string.Empty;
    public string thoughtTag = "NONE";
    public string thoughtDesc = string.Empty;

    bool IsMonologue;                  // recipientName == null
    string PairKey;                    // 名字排序拼接的对话配对标识
    string TimeStr;                    // 格式化时间
}
```

### CompRimMindDialogue

ThingComp，挂载到所有人形 Pawn：

```csharp
public class CompRimMindDialogue : ThingComp
{
    int _lastTriggerTick = -99999;  // 序列化到存档
    Pawn Pawn => (Pawn)parent;

    override void CompTick();                  // 非殖民者首行 return；每 1000 ticks 检查自动对话
    override IEnumerable<Gizmo> CompGetGizmosExtra();  // Gizmo 按钮（仅玩家阵营）
    override void PostExposeData();            // 存档序列化
    bool IsEligible();                         // IsFreeNonSlaveColonist && !Dead && !Downed && !Drafted && Map != null && mood != null
}
```

## 数据流

```
游戏事件（Chitchat/Hediff/LevelUp/Thought/Auto）
    │
    ├── Patch 拦截
    │       ▼
    ├── 检查设置：该类型是否启用？
    │       ▼
    ├── RimMindDialogueService.HandleTrigger()
    │       ▼
    ├── 检查总开关 / API 配置 / 游戏延迟 / 并发 / ShouldSkipDialogue / 冷却 / 每日限制
    │       ▼
    ├── RimMindAPI.Chat(request)
    │       ▼
    ├── AI 生成回复（异步）
    │       ▼
    ├── ContinueWith → LongEventHandler.ExecuteWhenFinished
    │       ▼
    ├── NpcResponseHandler.Handle()
    │       ├── DisplayInteraction()（MoteMaker.ThrowText 显示气泡）
    │       ├── 解析命令（express_emotion / change_relationship / 自定义）
    │       ├── ThoughtInjector.Inject() / InjectRelationDelta()
    │       ├── AddLogEntry()（日志上限 500 条）
    │       ├── MemoryBridge.AddMemory()（仅对话且 RimMindMemory 活跃）
    │       ├── RecordDailyDialogue()
    │       ├── showThoughtNotification → Messages.Message()
    │       └── TryTriggerReply()（仅对话且 enableDialogueReply）
```

## 上下文注入

Dialogue 在 Mod 初始化时向 Core 注册两个 Pawn 上下文 Provider 和两个 Context Key：

| 注册类型 | Key | 内容 |
|---------|-----|------|
| PawnContextProvider | `dialogue_state` | 当前活跃的 RimMindDialogue Thought 列表（描述 + 剩余时间） |
| PawnContextProvider | `dialogue_relation` | 当前会话对象的关系详情：Opinion、Compatibility、RomanceChance、DirectRelation |
| ContextKey | `dialogue_task` | 自动对话任务指令（TaskInstructionBuilder.Build，12 个子键） |
| ContextKey | `player_dialogue_task` | 玩家对话任务指令（TaskInstructionBuilder.Build，8 个子键） |

两个 Context Key 仅在 `ScenarioIds.Dialogue` 时返回内容。

## RimMindDialogueMod 注册

Mod 初始化时向 Core 注册以下回调：

| 注册项 | Key / 方法 | 说明 |
|--------|-----------|------|
| SettingsTab | `"dialogue"` | 注册模组设置页签 |
| ModCooldown | `"Dialogue"` | 注册冷却时间（`monologueCooldownTicks`） |
| ToggleBehavior | `"dialogue_overlay"` | 注册浮窗开关切换 |
| DialogueTrigger | 回调 | 注册外部对话触发回调（使用 `DialogueTriggerType.Chitchat`） |

## UI 层

### Window_Dialogue
聊天窗口，初始大小 480×540。构造函数 `Window_Dialogue(Pawn pawn, Pawn? initiator = null)`。支持滚动消息列表、输入框、发送按钮。消息按 User/Pawn 分色显示。等待 AI 回复时显示"思考中"状态。构造时调用 `SetActiveRecipient`，关闭时清除。检查 pawn 死亡自动关闭。

### Window_DialogueLog
对话日志窗口，初始大小 720×560。三栏布局：分类栏→标签列表→内容区。支持 5 种分类过滤，独白为列表视图，对话为双栏视图。PlayerDialogue 分类通过 `trigger == "PlayerInput"` 过滤（非 category 字段）。

### DialogueOverlay
MapComponent，通过内嵌 Harmony Patch（`DialogueOverlayPatch`）在 `UIRoot_Play.UIRootOnGUI` 的 Prefix 和 Postfix 中调用 `DrawOverlay()`，使用 `_skip` 标志隔帧绘制。支持拖拽标题栏移动、拖拽右下角缩放、位置和大小持久化（`_positionDirty` 脏标记，仅变更时保存到 Settings）。分类颜色映射区分独白/对话/玩家对话。

## 事件触发机制

### Patch 列表

| Patch | Harmony 目标 | 触发条件 | 备注 |
|-------|-------------|----------|------|
| BubblePatch | `Pawn_InteractionsTracker.TryInteractWith` | Chitchat/DeepTalk 成功 | 仅殖民者发起；检测 RimTalk 兼容 |
| HediffPatch | `Pawn_HealthTracker.AddHediff` | 添加显著 Hediff（isBad/tendable/makesSickThought） | 使用 Traverse 获取 pawn |
| SkillLearnPatch | `SkillRecord.Learn` | 技能等级提升 | Prefix 记旧等级，Postfix 检测提升 |
| ThoughtPatch | `MemoryThoughtHandler.TryGainMemory` | 心情变化绝对值 >= 阈值 | 过滤 `RimMindDialogue_` 前缀的 Thought |
| FloatMenuPatch | `FloatMenuMakerMap.ChoicesAtFor`(V1_5) / `.GetOptions`(V1_6) | 右键点击附近人形 Pawn | 条件编译；使用 `RimMindAPI.ShouldSkipFloatMenu()` |
| GameLoadPatch | `GameComponentUtility.LoadedGame` / `Game.InitNewGame` | 游戏加载/新游戏 | 调用 NotifyGameLoaded |
| AddCompToHumanlikePatch | `ThingDef.ResolveReferences` | 人形种族 Def 解析 | 自动添加 CompProperties_RimMindDialogue |
| CompRimMindDialogue.CompTick | 每 1000 ticks | 自动触发 | 非殖民者首行 return；冷却控制 |

### 设置项一览

```csharp
enabled = true;                      // 总开关
chitchatEnabled = true;              // 拦截原生闲聊
hediffEnabled = true;                // 健康变化反应
levelUpEnabled = true;               // 技能升级反应
thoughtEnabled = true;               // 心情变化反应
autoDialogueEnabled = true;          // 空闲自动独白
playerDialogueEnabled = true;        // 玩家主动对话
moodChangeThreshold = 3f;            // ThoughtPatch 触发阈值
autoDialogueCooldownHours = 12;      // 自动独白间隔（游戏小时）
maxDailyDialogueRounds = 6;          // 每对殖民者每天最大对话轮数
showThoughtNotification = false;     // 注入 Thought 时屏幕通知
enableDialogueReply = true;          // 收到对话后自动生成回复
monologueCooldownTicks = 36000;      // 独白冷却（默认 10 游戏小时）
startDelayEnabled = true;            // 游戏开始延迟
startDelaySeconds = 10;              // 延迟秒数
overlayEnabled = true;               // 浮窗开关
overlayOpacity = 0.75f;              // 浮窗透明度
overlayMaxMessages = 8;              // 浮窗最大消息数
overlayX/Y/W/H = 20/20/420/220;     // 浮窗位置和大小
```

## 代码约定

### 命名空间

| 目录 | 命名空间 | 说明 |
|------|----------|------|
| Source/ | `RimMind.Dialogue` | Mod 入口、MemoryBridge、Thought 类 |
| Source/Core/ | `RimMind.Dialogue.Core` | 核心服务 |
| Source/UI/ | `RimMind.Dialogue.UI` | 窗口界面 |
| Source/UI/ | `RimMind.Dialogue.Overlay` | DialogueOverlay（与目录不对应） |
| Source/Comps/ | `RimMind.Dialogue.Comps` | ThingComp |
| Source/Patches/ | `RimMind.Dialogue.Patches` | Harmony 补丁 |
| Source/Settings/ | `RimMind.Dialogue.Settings` | 设置 |
| Source/Debug/ | `RimMind.Dialogue.Debug` | 调试动作 |

### 全部静态服务

`RimMindDialogueService`、`DialogueService`、`NpcResponseHandler`、`ThoughtInjector`、`MemoryBridge` 均为 static 类，无依赖注入，全局唯一。

### 翻译键约定

所有用户可见文本通过翻译键定义在 `Languages/*/Keyed/RimMind_Dialogue.xml` 中。

任务指令翻译键通过 `TaskInstructionBuilder.Build(keyPrefix, subKeys)` 构建，自动拼接 `{keyPrefix}.{subKey}` 并 `.Translate()`。

**注意**：翻译文件中仍有 3 个 `RimMind.Dialogue.Prompt.*` 翻译键无代码引用（System.RecipientRole、Context.DialogueHistory、Context.HistoryCompressed），可能是预留的或废弃的。详见 `docs/06-problem/RimMind-Dialogue.md` #4。

## 扩展指南

### 添加新的触发类型

1. **在 DialogueTriggerType 添加枚举值**
2. **创建新的 Patch 类**（在 `Patches/` 目录，命名空间 `RimMind.Dialogue.Patches`）
3. **在 RimMindDialogueSettings 添加开关字段**
4. **在 RimMindDialogueService.GetTriggerLabel 添加标签映射**
5. **在 Window_DialogueLog.TranslateTrigger / GetTriggerColor 添加映射**
6. **调用 HandleTrigger**

```csharp
[HarmonyPatch(typeof(SomeClass), "SomeMethod")]
public static class MyTriggerPatch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn __instance)
    {
        if (!RimMindDialogueSettings.Get().myTriggerEnabled) return;

        string context = "描述发生了什么";
        RimMindDialogueService.HandleTrigger(
            __instance, context, DialogueTriggerType.MyNewType, null);
    }
}
```

### 添加新的 Thought 标签

1. **在 ThoughtInjector.MapTagToMoodOffset 添加映射**
2. **在 ThoughtInjector.MapTagToLabel 添加标签**
3. **在翻译键 XML 添加 `RimMind.Dialogue.Thought.{TAG}` 条目**

### 自定义对话分类

在 DialogueCategory 添加新分类，在 GetCategory 方法中定义分类逻辑，在 Window_DialogueLog 的 CategoryKeys 中添加翻译键。

### 添加新的任务指令子键

在 `RimMindDialogueMod.RegisterContextProviders` 中修改 `TaskInstructionBuilder.Build` 的 subKeys 参数，并在翻译 XML 中添加对应的 `{keyPrefix}.{subKey}` 条目。

## 调试

Dev 菜单（需开启开发模式）→ RimMind-Dialogue：

- **Force Chitchat** — 强制触发闲聊对话（选中 + 目标）
- **Force Hediff Trigger** — 强制触发健康变化对话（选中）
- **Force LevelUp Trigger** — 强制触发技能升级对话（选中）
- **Force Thought Trigger** — 强制触发心情变化对话（选中）
- **Show Active Thoughts** — 查看小人的 RimMindDialogue Thought
- **Open Dialogue Window** — 打开玩家对话窗口（选中）
- **Open Dialogue Log** — 打开对话日志窗口
- **Toggle Overlay** — 切换屏幕浮窗显示
- **Show Dialogue Service State** — 显示服务内部状态
- **Clear All Dialogue Cooldowns** — 清除所有冷却计数

## 注意事项

1. **主线程调度**：ContinueWith 回调通过 `LongEventHandler.ExecuteWhenFinished()` 调度到主线程，确保 Unity/RimWorld API 在正确线程执行。
2. **并发控制**：`_pendingPawns` + `_pendingDialoguePairs` 使用 ConcurrentDictionary 防止同一小人/对话对并发请求；全局并发由 RimMind-Core 的 `maxConcurrentRequests` 控制。
3. **RimTalk 兼容**：检测到 `ModsConfig.IsActive("juicy.RimTalk")` 时自动禁用 Chitchat 拦截。
4. **存档安全**：`_pendingPawns`、`_logEntries`、`_recentTriggers`、`_activeRecipients`、`_dailyDialogueCounts` 等运行时状态不序列化；仅 CompRimMindDialogue._lastTriggerTick 通过 PostExposeData 序列化。
5. **性能考虑**：自动对话有冷却机制 + 每日计数限制；Overlay 隔帧绘制 + 脏标记保存；Pawn 缓存 600 tick 刷新；CompTick 非殖民者首行 return。
6. **MemoryBridge**：通过反射松耦合调用 RimMindMemory，不产生编译期依赖；反射失败时静默跳过；调用方额外检查 `ModsConfig.IsActive("mcocdaa.RimMindMemory")`。
7. **版本兼容**：FloatMenuPatch 使用 `#if V1_5` / `#else` 条件编译适配 1.5/1.6 API 差异。
8. **日志上限**：_logEntries（ConcurrentBag）上限 500 条，超出时重建。
9. **已知问题**：Gizmo 按钮发起的玩家对话中 `initiator=null`，导致 `NpcResponseHandler.Handle` 将其当作独白处理。FloatMenu 路径已修复。详见 `docs/06-problem/RimMind-Dialogue.md` #1。
10. **上下文格式化**：`HandleTrigger` 中根据 `DialogueTriggerType` 使用翻译键格式化 context（Context.Chitchat/Hediff/LevelUp/Thought/Auto），并通过 `GetRecipientRoleKey` 添加角色约束（Role.Prisoner/Slave/Enemy/Visitor + Context.Recipient）。PlayerInput 路径不格式化，直接使用原始 context。
