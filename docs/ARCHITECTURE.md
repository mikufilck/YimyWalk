# YimyWalk 架构决策与实现边界

> 最近对照源码：2026-11-01，`7a0fb13`。当前验证见 [MAINTENANCE](MAINTENANCE.md)，历史阶段证据见 [STATUS](STATUS.md)。本页描述实现，不表示桌面矩阵已全部通过。

## 当前决策

1. 先建立可独立测试的核心模拟，再接入 Windows 窗口；核心不引用 `System.Windows.Forms`。
2. 第一版采用 .NET 8 Windows 桌面应用作为新代码基线，Windows 适配由 `App/Host/*` 和 `App/Windows/*` 承担，P/Invoke 按约束集中到 `Windows/NativeMethods.cs`，并由架构测试约束；这样避开上游 .NET Framework 目标包缺失，同时保留 Win32 能力。
3. 角色定义不可变且共享图片/台词；`PetInstance` 只保存位置、速度、朝向、状态、动画计时和需求数值。
4. 基础角色包只要求 `sprite.png`，固定 3×3 格和一致锚点；`character.json`、`dialogue.txt` 是已支持的可选扩展；`actions.png` 尚未实现。
5. M1 只交付屏幕工作区地面和基础拖拽/抛掷；M2 通过显式管理器开关提供可选窗口顶部落脚，多显示器 DPI 和外部应用事件仍分别验收。
6. 当前虚拟食物是程序生成的圆点，完整图标分身动画尚未实现。真实快捷方式暂存属于隔离的后续阶段，不能进入基础路径。
7. M1 采用“一个宠物实例对应一个透明分层窗口”。所有窗口由同一个 `YimyWalk.App` 宿主进程管理；没有为每只宠物启动进程，也没有预先改成全屏覆盖层。
8. `CharacterLoader` 只负责 PNG 解码、校验、RGBA 帧和帧元数据；`YimyWalk.Core` 不创建窗口、不调用 Win32、不持有 `Bitmap`。`FrameSurfaceCache` 和 `LayeredWindowRenderer` 在 App 层把共享 RGBA 转成平台绘制资源。

## 逻辑边界

```text
AppHost (组合与 WinForms 消息循环，单进程)
  CharacterCatalog -> CharacterLoader -> CharacterDefinition
  PetSession -> PetEngine -> PetInstance[]
  PetWindowRegistry -> PetWindow -> FrameSurfaceCache -> LayeredWindowRenderer
  DesktopWorldSurface -> WindowsDisplayAdapter / WindowSurfaceResolver
  DialogueScheduler -> BubbleWindow
  VirtualFoodSystem -> FoodWindow
  SettingsDocument -> SettingsStore / SettingsSaveCoordinator
  TrayIconHost -> ManagerForm -> 五个管理页面
```

Core 承载角色数据、模拟、显示器/表面契约和设置 DTO；窗口、Bitmap、桌面枚举、持久化文件位置由 App 承担。`PetRenderer`、`InteractionSystem`、`DesktopIntegration` 是早期规格中的逻辑层名，不是当前代码类型。

实例、角色定义和绘制资源的边界如下：同一角色的实例共享不可变 `CharacterDefinition` 和 RGBA 帧；位置、速度、状态和动画计时属于 `PetInstance`，窗口保留鼠标捕获与拖拽测速的输入暂态。定义替换通过 `DefinitionLease` 保持实例引用和在途渲染引用，最后一个引用释放后才释放旧资源。透明背景命中测试和实体命中测试分别在 `PetWindow.WndProc` 中处理，焦点控制实际使用 `WS_EX_NOACTIVATE`、`ShowWithoutActivation` / `ShowWindow(SW_SHOWNOACTIVATE)` 与 `WM_MOUSEACTIVATE → MA_NOACTIVATE`；生产代码没有 `SetWindowPos` 调用；这些行为仍需 Windows 桌面矩阵逐项验证。

Core 使用注入的随机源、表面和显示器契约，时间由调用方显式传入 Tick；AppHost 用 Stopwatch 计算物理 delta，Core 限制上限。App 的帧、拖拽与低频轮询共用注入的 IMonotonicClock。Windows 输入由 PetWindow 转为 Core 操作；PetSession 统一推进食物目标与仿真，只有引擎移动宠物。暂停和实例走动覆盖均由同一流程处理。

## 资源生命周期

角色扫描产生新的不可变定义；只有校验成功的定义才替换目录索引。现存实例持有旧定义的安全引用，等实例释放后旧图片才可回收。坏包刷新时保留上一份可用定义并记录错误。相同角色的 100 个实例不得重复解码同一图片。

## 当前验证边界

一个实例一个分层窗口已有实现。2026-11-01 当前单屏 150% 下已执行 UI / 输入 / 落脚 / 批量资源回归；多屏、负坐标真实拓扑、拔插、睡眠和长时趋势仍为 **Windows 桌面未验证**，详细证据见 STATUS。

## M2 settings boundary

`YimyWalk.Core.Settings.SettingsDocument` owns only validated, immutable data: schema version, selected role, scale, activity rate, dialogue flag, window-landing preference, preferred display identifier, and the optional warning limit. `YimyWalk.App.Settings.SettingsStore` owns the user-local JSON file, temporary file, atomic replacement, backup, and recovery diagnostics. It never places a `Form`, HWND, `Bitmap`, decoded PNG, or startup registration field in Core. AppHost loads this document before role selection, ManagerForm persists explicit selection and landing changes, and clean host disposal saves it. The preferred display is applied to subsequent spawn selection. Instance count, positions and runtime overrides are not persisted. Recovery followed by save preserves a valid backup: a corrupt primary is never rotated over that backup, including the fallback write path.

## M2 window-surface boundary

`YimyWalk.Core.Display.SurfaceSelector` receives immutable rectangles and metadata only. It rejects hidden, minimized, stale, excluded, invalid, and non-overlapping candidates, then selects the nearest top edge crossed while a pet is descending. If no candidate qualifies, `IWorldSurface` returns the display working-area floor. Core has no HWND, WinForms, `Bitmap`, `EnumWindows`, or desktop shell knowledge.

`YimyWalk.App.Host.WindowSurfaceResolver` owns the low-frequency `EnumWindows` snapshot and the platform filters for desktop shell, DWM/input, UWP child, and tiny helper windows. `DesktopWorldSurface` owns the fallback display snapshot and enables landing only when the host explicitly opts in. The existing M1 mapping remains one pet instance to one layered window, with every window managed by one `AppHost` process. Closing or losing a supporting window therefore changes only the affected pet's surface selection; it never moves or closes the user's window.

The manager's “允许宠物落到可见窗口顶部” checkbox is the user entry point. It is off by default, persisted in validated settings, and can be changed without restarting the host. The manager starts hidden. A normal close cancels the close and hides it to the tray; opening it shows a normal taskbar window. Explicit host exit disposes it. Settings including the dialogue master gate persist, while per-pet overrides remain runtime-only.

M3.1 adds `Hunger`, `Boredom`, and the short `Eat` state to each `PetInstance`. `CharacterDefaults.HungerIncreasePerMinute` is immutable role data; the mutable values stay on the instance. `PetEngine` applies bounded need deltas with a session-level enable/rate setting, and `FeedAll` is exposed by the manager without adding any desktop API to Core. Feeding is rejected while dragging, jumping, falling, or already eating, so movement/input priority remains explicit.

M3.2 keeps dialogue admission in Core's `DialogueScheduler`: it owns only pet IDs, bounded text, expiry, per-pet cooldown, and a global visible limit. `BubbleWindow` is an App-only, no-activate, hit-transparent layered WinForms surface positioned near the pet and clamped to its monitor's working area, including negative coordinates. `BubbleRenderer` draws an embedded Yimy skin with nine-slice scaling and separate DPI-aware text into a window-owned bitmap; movement reuses it, while DPI/work-area changes rebuild it. Each window and its bitmap are disposed on expiry, pet removal, clear, dialogue disable, and host exit. Core never holds a Form or drawing resource. Artwork provenance and layout details are in [YIMY_VISUAL_STYLE](YIMY_VISUAL_STYLE.md).

M3.3 uses `VirtualFoodSystem` for program-generated targets only. A target can be claimed by one grounded pet, is released when movement is interrupted, and transitions to a shrinking consumed state before disposal. Food target intent is consumed by PetEngine at the configured walk speed; PetSession pauses both systems and applies walk overrides. `FoodWindow` draws a small non-activating visual at the target position; the manager creates targets and clear/exit removes both targets and windows. No shortcut, file, shell, or desktop icon API is involved.

M3.4's optional foreground feedback is App-only and disabled by default. When enabled, `AppHost` samples only the foreground HWND's process ID at 500ms intervals, ignores the YimyWalk process, and emits the existing `program_opened` dialogue group on a process transition. It does not read window titles, paths, document content, keyboard input, or browser history; disabling the setting stops polling.

The foreground transition is not a process-open event: the existing `program_opened` dialogue group is reused for focus changes. Complete lifecycle events, timed/idle dialogue and fullscreen do-not-disturb are not implemented.

## 0.1.14 资源与失败边界

Core 的 PetLimits 对物理几何和呈现统一最终像素上限；App 缓存使用 SurfaceLease 保护在用位图，按实际输出尺寸建键，并以 BGRA + Alpha 的 64 MiB 预算淘汰未使用项。超过预算保留旧帧并给出诊断，不能释放窗口仍在命中测试 / 绘制的 surface。定义使用权和 surface handle 是两个生命周期，释放顺序见 MAINTENANCE。

AppLog 只记录操作码和异常类型，约 1 MiB × 2 文件并限频。单宠渲染故障移除该实例，批量工厂失败取消剩余请求，主循环故障暂停，避免同一失败每帧弹窗。Core 扫描事务只在成功后发布新目录，失败释放未提交租约。

PNG 解码改用 StbImageNext，Core 的 PNG-only 边界先做格式、大小、尺寸、CRC 与解压预算校验；不开放依赖支持的其他图片格式。预览 writer 使用 .NET ZLibStream，不引入平台绘图依赖。
