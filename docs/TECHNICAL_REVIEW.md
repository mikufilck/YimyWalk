# YimyWalk 技术评审：全功能实现与重构必要性

> **历史快照提示（2026-11-01）**：后续核对、四项沙盒复现与待办见 [本轮审查](reviews/2026-11-01-project-review.md)，尤其 §5 的纠正。本页原判断保留供追溯，不能直接当作当前已确认缺陷或实施授权。

**评审日期**：2026-10-16
**评审对象**：`codex/bootstrap` @ `7a0fb13`（YimyWalk v0.1.13）
**评审方式**：只读静态审查（全仓 98 个 C# 文件 / 11,091 行）+ 本机实测基线
**评审范围**：`src/YimyWalk.Core`（2,561 行）、`src/YimyWalk.App`（3,832 行）、`tests`（2,637 行）、`tools`（2,061 行）

## 证据标记约定

本文件区分四种证据强度，**不要混用**：

| 标记 | 含义 |
| --- | --- |
| ✅ | 本次评审**亲自逐行核对**过源码，行号与语义可直接复查 |
| 🔎 | 静态分析结论（只读审查，已给行号，但未逐条人工复核） |
| ❓ | **疑似**：机制可追溯到代码，但用户可见影响未经实测 |
| 🚫 | **需真机验收**：只能在可见 Windows 桌面确认，本文件不作结论 |

> ⚠️ **本文件不构成任何"已验证通过"的证据。** 所有 `❓`/`🚫` 条目标注的缺陷均**未被复现**；引用本文件时禁止把 `❓` 当已确认缺陷、把静态分析当桌面验收。

---

## 0. 结论摘要

### 0.1 一句话结论

**不需要整体重写，但需要一次有边界的"缺陷修复 + 结构收敛"重构。** 分层骨架是对的、测试基线是健康的，问题集中在 **两个上帝类、一条"无上限"的资源路径、若干可追溯的正确性缺陷，以及大量重复/死代码**。

### 0.2 判定表

| 维度 | 评分 | 判定 | 说明 |
| --- | --- | --- | --- |
| 分层边界 | ★★★★☆ | 保留 | Core 不碰 Win32/WinForms 且由测试强制；接口注入干净 |
| 可测试性 | ★★★★☆ | 保留 | 时钟/随机/显示器全注入；无 `DateTime.Now`、无 `Thread.Sleep`；148 用例全绿 |
| 缺陷密度 | ★★★☆☆ | **需要修** | 有多条**可追溯**的正确性/资源缺陷（详见 §5-A） |
| 资源安全 | ★★☆☆☆ | **需要修** | 位图缓存无上限、无淘汰；GDI 句柄有泄漏窗口 |
| 结构内聚 | ★★☆☆☆ | **需要修** | `AppHost` = 24 成员 / 11 职责；`PetEngine.Tick` = 131 行 / 6 关注点 |
| 重复与死代码 | ★★☆☆☆ | **需要修** | 同一常量多处硬编码且**取值不一致**；多组零引用 API |
| 可观测性 | ★☆☆☆☆ | **需要修** | **全应用没有任何运行期日志**（只有启动失败写 `startup-error.log`）✅ |
| 功能完成度 | ★★★★☆ | 保留 | M0–M4 主功能均已实现；缺口是 `actions.png`/粒子等**已如实声明**的未实现项 |
| 桌面验收债务 | ★★☆☆☆ | **独立于本评审** | 多屏/DPI/负坐标/长时趋势等仍为 `Windows 桌面未验证` |

### 0.3 不建议做的事（防止过度重构）

- ❌ **不要重写渲染层**：一个实例一个分层窗口的方案已被 83/83 双屏矩阵验证过，换方案会让既有证据全部作废。
- ❌ **不要引入 MVVM / DI 容器 / 事件总线**：6,400 行生产代码 + 手写假实现的测试体系，收益为负。
- ❌ **不要把 UI 拆成独立项目**：`internal` 边界现在是资产。
- ❌ **不要顺手升级 .NET / 换 UI 框架**：与本次目标正交，会掩盖真实回归。

---

## 1. 本机实测基线（可复现）

| 项 | 命令 | 结果 |
| --- | --- | --- |
| SDK 前置 | `scripts/bootstrap-dotnet.ps1` | `global.json` 锁 `8.0.425`；系统 SDK 8.0.423/10.0.302 **均不满足**；本地 SDK 8.0.426 ✅ |
| 环境自检 | `scripts/verify-environment.ps1` | 退出码 0 ✅ |
| 构建 | `dotnet build YimyWalk.sln -c Release --no-restore` | 0 错误 / 10 警告（全部为既有 ImageSharp 3.1.12 NU1902/NU1903）✅ |
| Core 测试 | `dotnet test YimyWalk.sln -c Release --no-restore` | **148/148 通过**（≈0.6 s）✅ |
| 发布链路 | `scripts/build-yimywalk.ps1 -Version 0.1.13` | 退出码 0 ✅ |
| 发布包自检 | `scripts/test-release-package.ps1` | `PACKAGE PASS VERSION=0.1.13 FILES=13 CHECKSUMS=12` ✅ |
| **桌面验收** | `desktop-smoke` / `desktop-input-smoke` / `desktop-display-smoke` | **本轮未执行** 🚫 |

基线健康度评价：**测试基础设施是这个项目最强的资产**——时间全部由 `Tick(pet, dt)` 显式驱动，随机源全为假实现，无 `Thread.Sleep`、无真实时钟、无跨用例共享状态。这意味着后续任何重构都能得到快速、确定的回归信号。

---

## 2. 功能实现盘点

对照 `docs/PROJECT_SPEC.md` 的 M0–M4 逐项盘点（"完成度"指**代码实现层面**，不代表桌面验收）：

| # | 功能 | 主要落点 | 实现方式 | 代码完成度 | 主要问题 |
| --- | --- | --- | --- | --- | --- |
| 1 | 角色包加载 | `Core/Characters/CharacterLoader.cs` | 手写 PNG 头解析（33 字节定长 + chunk 扫描推断 alpha）+ ImageSharp 解码 + 3×3 切帧 | 完整 | **上帝类**（8 职责 / 375 行 / 15 个无外部消费者的 public const）；`ReadConfig` 85 行 |
| 2 | 角色热刷新 | `CharacterCatalog` + `DefinitionLease` | `ConditionalWeakTable` 四元引用计数（owner/instance/renderer/released），失败保留旧定义 | 完整 | 机制正确但**复杂度偏高**；`IsReleased` 全仓零引用 |
| 3 | 透明分层窗口 | `App/Windows/PetWindow.cs` + `LayeredWindowRenderer` | 一个实例一个 `WS_EX_LAYERED\|TOOLWINDOW\|NOACTIVATE` 窗口；`UpdateLayeredWindow` 呈现；alpha<12 判穿透 | 完整 | **GDI 泄漏窗口**（§5-A1）；ULW 返回值不检查（A2） |
| 4 | 渲染缓存 | `FrameSurfaceCache` | 按 (定义,帧,镜像,缩放) 缓存 32bppPArgb 位图 + alpha 掩码 | 完整 | **无上限、无淘汰**（§5-A3）；逐像素 `Marshal.WriteByte` ×4（性能） |
| 5 | 物理与状态机 | `Core/Simulation/PetEngine.cs` | 10 状态（Idle/Walk/JumpCharge/Jump/Apex/Fall/Climb/TopWalk/Eat/Dragged）；单调 delta，上限 0.1s | 完整 | `Tick` **131 行 / 6 关注点**；**12 处绕过命名转移直接赋 `State`** |
| 6 | 自主行为 | `PetEngine` 内的自主计时器 | 随机延迟 → 自主走动 / 蓄力跳 / 攀爬 | 完整 | 行为参数散落在 `PetEngine` 的 8 个可变属性上，无法构造"已配置引擎" |
| 7 | 需求系统 | `PetInstance`（Hunger/Boredom） | Hunger 按角色配置增长；Boredom 固定速率 | **部分** | **`Boredom` 是只写状态**：全仓无人读取，也无人显示（§5-B4）✅ |
| 8 | 对话气泡 | `DialogueScheduler`（Core）+ `BubbleWindow`（App） | 全局可见上限 + 每宠冷却 + 过期回收；无激活、整窗穿透 | 完整 | 气泡窗口会被 `WindowSurfaceResolver` 当成**可落脚面/遮挡体**（§5-A5）❓ |
| 9 | 虚拟食物 | `VirtualFoodSystem`（Core）+ `FoodWindow`（App） | 程序生成目标 → 认领 → 移动 → 消耗 | 完整 | **双写者争用**：食物系统直接改 `PetInstance.FootPosition`/`State`（§5-A4）；`FoodWindow` **吞点击**（A6） |
| 10 | 窗口顶面落脚 / 跳窗 | `SurfaceSelector` + `JumpTargetPlanner` + `WindowSurfaceResolver` | 弹道推进 + 24 次二分反解起跳速度 + 最近可见顶面选择 | 完整 | 规划结果无运行期核对；24 次迭代超出 float 精度（§5-C1） |
| 11 | 多显示器 / DPI | `DisplayContracts` + `WindowsDisplayAdapter` | `Screen.AllScreens` + `GetDpiForMonitor` + PerMonitorV2；跨屏水平 wrap | 完整 | **P/Invoke 越界**：`MonitorFromPoint`/`GetDpiForMonitor` 不在 `NativeMethods.cs`（违反 `AGENTS.md` 铁律）✅ |
| 12 | 批量实例 | `BatchCreationQueue` + `BatchRequestQueue` | 1 ms 定时器每 tick `Drain(8)` | 完整 | 队列**无容量上限**；**无异常隔离**（单次失败会每 tick 重复抛） |
| 13 | 设置持久化 | `SettingsDocument`（Core）+ `SettingsStore`（App） | 原子替换 + 备份回退 + 300 ms 合并写入 | 完整 | **任一字段非法 → 整份设置回默认**，并覆盖掉备份（§5-A7）✅ |
| 14 | 管理界面 | `ManagerForm` + 5 个 `*Page` | AntdUI 五页、200 ms 刷新、关闭转托盘 | 完整 | 页面**双向直连 `AppHost`**（≈84 处 `Host.*`）；约 140 处中文硬编码字面量；35 处像素魔数 |
| 15 | 托盘 / 生命周期 | `TrayIconHost` + `AppHost.Dispose` | 打开/清空/隐藏/显示/退出；统一释放 | 完整 | `MainForm` **从未赋值** → 3 条相关分支是死代码（§5-B3）✅ |
| 16 | 启动诊断 | `StartupDiagnostics` + `StartupFailure` | 三类稳定错误码 + `startup-error.log` | 完整 | 这是**全应用唯一的日志路径** |
| 17 | 发布包 | `scripts/build-yimywalk.ps1` | 自包含单文件 x64 + manifest + SHA-256 | 完整 | 版本号默认值 `0.1.5` 与当前发布 `0.1.13` 脱节（靠 `-Version` 覆盖） |
| 18 | `actions.png` / 粒子 | — | **未实现** | 未实现 | 已在 `docs/CHARACTER_FORMAT.md` 如实标注，**不算缺陷** |

**功能结论**：规格中的主功能链路（加载→渲染→物理→交互→需求→对话→食物→多屏→发布）**全部有实现且分层清晰**。缺口集中在"辅助玩法没接线"（Boredom）、"外部资源没隔离"（气泡/食物窗口）和"错误路径没兜底"。

---

## 3. 架构评估

### 3.1 值得保留的资产（重构时必须守住）

1. **分层是真的，不是纸面的**：`SmokeTests.CoreAssemblyDoesNotReferenceWindowsForms` 用反射断言 Core 不引用 WinForms/WindowsBase/PresentationCore ✅——这条断言让 Core 的 2,561 行保持平台无关。
2. **依赖注入是彻底的**：`IWorldSurface` / `IRandomSource` / `IDisplayProvider` 全部接口化；**Core 与 App 中都没有 `DateTime.Now`、`Stopwatch`（除 `AppHost` 的帧计时）或 `Thread.Sleep`** ✅。
3. **单一宿主进程 + 单宠物单窗口**：结构简单，没有为每只宠物起进程，也没有全屏覆盖层。100 只基线下的"窗口数 = 实例数"是显式且可观测的。
4. **资源生命周期有真正的设计**：`DefinitionLease` 用 `ConditionalWeakTable` 让"目录/会话/渲染缓存"三方各持一份租约、互不牵连，热刷新时不会连带释放别人正在用的图片。机制正确（锁内计数 + `Interlocked` 幂等句柄 + 下溢守卫）✅。
5. **测试基础设施**（见 §1 基线）——最有价值的资产。

### 3.2 分层被削弱/破坏的地方

| 破坏点 | 证据 | 后果 |
| --- | --- | --- |
| **P/Invoke 越界** | `MonitorFromPoint`/`GetDpiForMonitor` 声明在 `Host/WindowsDisplayAdapter.cs:76-77`，而 `AGENTS.md` 规定"Win32 P/Invoke 只放 `Windows/NativeMethods.cs`" ✅ | 铁律被自己违反，后续会持续扩散 |
| **文档分层名 ≠ 代码类型** | `PROJECT_SPEC.md` §4 与 `ARCHITECTURE.md` 的 `PetRenderer` / `DesktopIntegration` / `InteractionSystem` **在代码中不存在** | 新会话会按文档去找不存在的类型 |
| **平台知识渗进 Core 的"业务谓词"** | "着地状态"集合 `Idle or Walk` 在 `PetEngine`、`PetInstance`、`VirtualFoodSystem`、`AppHost` **四处各写一份** | 新增一个"可移动状态"要改 4 处 |
| **UI 与宿主双向直连** | 5 个页面 `ctor(AppHost)`，≈84 处 `Host.*` 直接读写属性 | 页面无法单测（App 项目零自动化覆盖） |
| **行为参数挂在引擎实例上** | `PetEngine` 的 8 个 settable 属性是"What 全局设置的影子副本" | `PetWindow` 每实例 new 一个从不配置的 `PetEngine`，一旦有人对它 `Tick`，缩放就会与真源分叉 ❓ |

---

## 4. 代码规模热点（重构成本地图）

| 文件 | 行数 | 最长方法 | 判定 |
| --- | --- | --- | --- |
| `App/Host/AppHost.cs` | 589 | `Tick` 53 行（承载 6 件事） | **上帝类**：24 成员 / 11 职责 |
| `Core/Simulation/PetEngine.cs` | 483 | `Tick` **131 行** | 6 关注点混在一起 |
| `Core/Characters/CharacterLoader.cs` | 333 | `ReadConfig` 85 行 | 8 职责（IO/PNG/尺寸/像素/切帧/JSON/台词/异常归一） |
| `UI/Pages/PetsPage.cs` | 373 | 构造函数 66 行 | 16 个事件订阅 / 7 项设置 / 3 个同步标志 |
| `Display/SurfaceSelection.cs` | 238 | `TrySelect` **117 行 / 12 参数** | 6 个局部状态混在循环里 |
| `tools/DesktopInputSmoke/Program.cs` | 630 | — | 4 个 smoke 工具间约 **400–450 行重复脚手架** |

---

## 5. 关键问题清单

按"是否值得在重构中处理"排序。**A 组必须修，B 组应该修，C 组可以顺手修。**

### A 组：正确性与资源安全（必须修）

| ID | 判定 | 问题 | 证据 | 影响 |
| --- | --- | --- | --- | --- |
| **A1** | ✅ 可追溯 | **GDI 句柄泄漏窗口**：`LayeredWindowRenderer.Present` 的 `GetDC` / `CreateCompatibleDC` / `GetHbitmap` / `SelectObject` 四步**在 `try` 之外**，只有 `UpdateLayeredWindow` 在 try 内 | `LayeredWindowRenderer.cs:10-13`（try 起于 `:14`） | 任一步抛异常就泄漏 screen DC + memory DC + HBITMAP。该函数**每帧每宠调用一次**，GDI 句柄配额耗尽会导致整个桌面绘制异常 |
| **A2** | ✅ 可追溯 | **ULW 失败完全静默**：`UpdateLayeredWindow` 的 `bool` 返回值被丢弃，`SetLastError=true` 声明了但从不读 | `LayeredWindowRenderer.cs:26` ✅ / `NativeMethods.cs:30` | 白窗、"宠物不刷新"这类故障**没有任何诊断信号**（本仓又无运行期日志，见 B1） |
| **A3** | ✅ 可追溯 | **位图缓存无上限、无淘汰**：键 `(定义,帧,镜像,缩放)`，缩放只做**下**钳制 `Math.Max(0.1f, scale)`，**没有上限**；只有"该定义最后一个用户释放"时才整定义清空 | `FrameSurfaceCache.cs:41-47` ✅ | 与钳制链叠加（角色 scale × 全局 scale × DPI，运行期上限 64）后：418px 单格在**有效缩放 16** 时单张位图 ≈ **179 MB**，18 个键 ≈ 3.2 GB。**有效缩放 16 完全可达**（角色 1 × 全局 8 × DPI 2）。用户拖动缩放滑杆会不断产生新键且**旧键永不回收** ❓ |
| **A4** | ✅ 可追溯 | **`PetInstance` 有两个写者**：`VirtualFoodSystem.Tick` 直接改 `pet.FootPosition` 与 `pet.State = PetState.Walk`；而同一帧 `AppHost` 先跑 `PetEngine` 再跑食物系统 | `VirtualFoodSystem.cs:110`、`:113` ✅ | ① 宠物以 `WalkSpeed + 180px/s` 接近食物（速度语义被叠加）② **绕过 `PetEngine` 的"禁用自主走动"门控** → 关掉走动后引擎每帧改回 Idle、食物系统每帧改回 Walk → **状态抖动**❓ |
| **A5** | ❓ 疑似 | **气泡窗口被当成可落脚面/遮挡体**：`WindowSurfaceResolver` 只排除"宿主窗口"，而 `IsHostWindow` 只认宠物窗口与 ManagerForm，**不认气泡/食物窗口** | `AppHost.cs:541-546` / `WindowSurfaceResolver.cs:85-86` | 宠物可能落到自己的气泡上；气泡还会作为遮挡体影响下方真实窗口的候选判定 |
| **A6** | ✅ 可追溯 | **`FoodWindow` 吞点击**：它没有 `WndProc` 覆写，而 `BubbleWindow` 有 `WM_NCHITTEST → HTTRANSPARENT` | `FoodWindow.cs:21-46`（无 WndProc）vs `BubbleWindow.cs:35-45` ✅ | 违反 `AGENTS.md`"食物窗口不遮挡正常点击"；20×20 区域内的点击被吃掉 |
| **A7** | ✅ 可追溯 | **设置：一个字段非法 → 整份回默认**，且下一次保存会用坏文件**覆盖可能是好的备份** | `SettingsDocument.cs:82-96`（`settings = Defaults`）✅ / `SettingsStore` 的 `File.Replace` | 用户手改错一个键，同时**丢失全部偏好和恢复点** |
| **A8** | ✅ 可追溯 | **未授权异常杀死整次角色扫描**：`CharacterLoader` 的异常过滤器**不含 `UnauthorizedAccessException`**，而 `CharacterCatalog.Scan` 只捕 `CharacterLoadException` | `CharacterLoader.cs:75-81` ✅ / `CharacterCatalog.cs:44` ✅ | 一个 ACL 拒读的角色目录 → **全部角色不可用 + 启动失败弹窗**，而不是"逐角色失败并保留其余"❓ |
| **A9** | ❓ 疑似 | **宿主帧循环无异常隔离**：`AppHost.Tick`、`BatchCreationQueue.DrainBatch` 均无 try/catch，而 `PetWindow.RefreshFrame` 的 `SelectFrame` 可抛 `ObjectDisposedException`、`_cache.Get` 可抛 OOM | `AppHost.cs:435-487` / `BatchCreationQueue.cs:48-62` | 单只宠物的失败会冒到 WinForms 定时器回调（默认 `ThreadException` 模式弹模态框），并**每 16 ms 重复抛同一异常** |
| **A10** | ✅ 可追溯 | **定义租约覆盖泄漏**：`PetSession.GetOrAddLease` 用 `_definitions[definition.Id] = lease` 覆盖同键旧租约而不 `Dispose` | `PetSession.cs:128-135` ✅ | 旧 `CharacterDefinition` 的帧内存**永不释放**（owner 计数永远 ≥1）。当前 App 路径不可达（只在测试/工具路径），是埋着的雷 |

### B 组：结构与可维护性（应该修）

| ID | 判定 | 问题 | 证据 | 影响 |
| --- | --- | --- | --- | --- |
| **B1** | ✅ 可追溯 | **零运行期日志**：全仓无 `Trace`/`Debug.Write`/`ILogger`/`Serilog`/`EventLog`，唯一日志是启动失败的 `startup-error.log` | 全仓检索 ✅ | 配合显示器枚举失败、`EnumWindows` 失败、DPI 读取失败这些**无日志的静默 catch**，线上故障无法定位 |
| **B2** | ✅ 可追溯 | **同一常量多处硬编码且取值不一致** | 见下表 | 静默行为分歧 |
| **B3** | ✅ 可追溯 | **`MainForm` 从未赋值** → `RefreshForWindow` 分支、`IsHostWindow` 首个判断、`MainForm?.Dispose()` 三处是死代码 | `AppHost.cs:253/543/633` 只有读、无写 ✅ | 误导后续维护者 |
| **B4** | ✅ 可追溯 | **`Boredom` 是只写状态**：每帧推进，但全仓无人读取、UI 不显示、行为不使用 | `PetInstance.cs:32,128-129` 是唯一两处 ✅ | 为一个死状态付 CPU 和认知成本 |
| **B5** | ✅ 可追溯 | **`PetEngine.Tick` 131 行 / 6 关注点**：delta 消毒 + 需求 + 表面迁移 + 预测 + 表面选择 + 边界检测 + 行为门控 + 状态 dispatch | `PetEngine.cs:50-180` | 任何物理改动都要在 131 行里定位；回归测试必须整链路驱动 |
| **B6** | ✅ 可追溯 | **`AppHost` 24 成员 / 11 职责**（窗口注册表、仿真循环、托盘、管理器、设置持久化、对话、虚拟食物、前台轮询、批量创建、显示表面、角色目录） | `AppHost.cs:19-41` | 无法为任一职责单独写测试；`Tick` 是 6 件事的顺序执行 |
| **B7** | ✅ 可追溯 | **12 处绕过状态机直接赋 `State`** | `PetEngine.cs:97,108,133,139,167,249` + `PetInstance.cs:83,90,97,103,116` + `VirtualFoodSystem.cs:113` | 状态机没有单一入口；A4 正是这个模式的产物 |
| **B8** | 🔎 静态 | **`CharacterLoader` 是上帝类**（8 职责）且 15 个 `public const` **无任何外部消费者** | `CharacterLoader.cs` 全文 | 公开的限制表其实是实现细节泄漏 |
| **B9** | 🔎 静态 | **测试 fixture 重复**：`FixedRandom` 在 7 个测试文件里各写一份，`TestSurface` 5 份，合计约 90 行 | 各测试文件末尾 | 改一个假实现要改 7 处 |

**B2 取值不一致明细**（同一语义、不同数字）：

| 语义 | 取值 A | 取值 B | 结论 |
| --- | --- | --- | --- |
| 行走速度上限 | `CharacterLoader` 允许到 **2000** ✅ | `PetEngine` 运行期钳到 **1000**（**9 处**）✅ | `walkSpeed ∈ (1000,2000]` 能通过加载校验，运行期被**静默削半** |
| 缩放上限 | UI 与 `PetInstance`：**0.1–8** ✅ | `PetEngine.EffectiveScale` / `PetWindow`：**0.1–64** ✅ | 界面语义与运行期上限不一致 |
| 落地安全边距 | `SurfaceSelector`：`const 4f` | `PetEngine`：**3 处**字面量 `4f` | 同一像素量写在 4 个地方 |
| 喂食量 | `PetInstance.Feed` 默认 **0.35** ✅ | `VirtualFoodSystem` 用 **0.5** ✅ | 同一"喂食"两种剂量 |
| delta 上限 | `PetEngine.MaxDeltaSeconds = 0.1` | `VirtualFoodSystem` 字面量 `0.1` | 未复用命名常量 |
| 活动率/欲望范围 | `SettingsDocument` 常量 | `PetBehaviorOverrides` / `PetSession` 硬编码字面量 | 三处各写一份 |

### C 组：性能与体验（可以顺手修）

| ID | 判定 | 问题 | 证据 |
| --- | --- | --- | --- |
| **C1** | 🔎 静态 | **24 次二分超出 float 精度**：区间上界 5000 时 float eps ≈ 1.9e-4，约 12 次已到极限 | `SurfaceSelection.cs:181-189` |
| **C2** | ✅ 可追溯 | **每帧重复分配**：`PetSession.Instances` 每次访问都 `ToArray()`，而 `AppHost.Tick` 在**每个食物目标的循环内**读它；`VirtualFoodSystem.Tick` 每帧 `ToDictionary` + `ToArray` | `PetSession.cs:32` / `AppHost.cs:445` / `VirtualFoodSystem.cs:70-71` ✅ |
| **C3** | ✅ 可追溯 | **暂停/隐藏不减负载**：`Tick` 无条件 `RefreshFrame()`，暂停只拦仿真、隐藏只 `SW_HIDE` | `AppHost.cs:482` / `PetSession.cs:102` ✅ |
| **C4** | ✅ 可追溯 | **逐像素 `Marshal.WriteByte` ×4**（每通道一次 P/Invoke 级调用） | `FrameSurfaceCache.cs:93-96` ✅ |
| **C5** | ✅ 可追溯 | **未使用参数/恒真断言**：`CharacterFramePreview.ShowIdleFrame(definition, float scale = 2f)` 的 `scale` **被完全忽略**（体内恒传 `1f`）；`CurrentFrameIndex` **恒返回 0**，导致 `DesktopInputSmoke` 的"预览用 idle1/index 0"断言**恒真** | `CharacterFramePreview.cs:24,26` ✅ |
| **C6** | 🔎 静态 | **UI 未覆盖**：`YimyWalk.App`（含全部 UI）**零自动化测试**；架构铁律只断言 3 个程序集名，若 Core 改用 `System.Drawing.Common` 或 `DllImport` **不会红** | `SmokeTests.cs:8-11` |
| **C7** | ✅ 可追溯 | **2 个测试泄漏临时目录**：`CharacterCatalogTests` / `ResourceLifecycleTests` 未调用 `DeleteFixture` | 🔎 静态，未复核 |

---

## 6. 重构必要性判定

### 6.1 为什么要重构（证据驱动）

1. **有 10 条 A 组问题**，其中 **7 条已亲自核对为可追溯**——它们不是风格问题，是"会泄漏句柄 / 会丢用户数据 / 会整机角色失效 / 会让宠物行为抖动"这一级别的。
2. **资源路径没有边界**：A3 的"无上限缓存 + 缩放滑杆"组合是**唯一能让应用直接 OOM 的路径**，而且触发方式是完全正常的用户操作。
3. **两个上帝类让所有 A 组修复都变贵**：想给 `Tick` 加异常隔离、想把虚拟食物的运动交给引擎，都要先动 131 行的 `Tick`。
4. **可观测性为零**（B1）——没有日志，A 组的静默失败在线上无法定位。
5. **重复常量已经产生实际分歧**（B2 的行走速度 1000 vs 2000）。

### 6.2 为什么不重写

1. **分层骨架正确且有测试强制**，这是最难重建的资产。
2. **6,400 行生产代码**远未到"重写比修更便宜"的规模。
3. **148 个测试全绿、机制上无 flakiness、时间全注入**——回归成本极低。
4. **桌面验收债务尚未清**：`docs/STATUS.md` 里多屏/DPI/负坐标/长时趋势仍是未验证。重写会让已有的 83/83 双屏矩阵等证据**全部作废**，债务反而扩大。

### 6.3 最终判定

> **重构必要性：有，但必须是"有边界的手术"，不是"换引擎"。**
>
> - **必须做**：A 组 10 项（缺陷修复）。约 200–400 行改动，全部可被现有 Core 测试 + 真机 smoke 覆盖。
> - **应该做**：B 组 9 项（常量收敛、死代码清除、日志接入、两个上帝类的拆分）。其中 B5/B6 的拆分是**收益最高但风险也最高**的一步。
> - **可以不做**：C 组；以及 `actions.png`/粒子等**已声明未实现**的功能扩展（属产品决策，不属重构）。

---

## 7. 分阶段重构路线图

> 每阶段都必须遵守 `AGENTS.md` 的四档口径：**代码已实现 / 构建通过 / Core 测试通过 / Windows 桌面已验证**。任何涉及窗口、命中、DPI、缩放的改动，**桌面档位默认写 `Windows 桌面未验证`**，直到真机跑过。

### P0 — 缺陷修复（不改变任何对外行为）

| 任务 | 落点 | 验收 |
| --- | --- | --- |
| A1 把四个 GDI 获取步骤移入 `try`，并判 `CreateCompatibleDC`/`GetDC` 是否为 0 | `LayeredWindowRenderer.cs` | 构建 + 现有 smoke；GDI 计数需真机 smoke 🚫 |
| A2 检查 `UpdateLayeredWindow` 返回值（至少 `Debug.Assert` 或计数） | 同上 | 同上 |
| A6 给 `FoodWindow` 补 `WndProc`（`WM_NCHITTEST → HTTRANSPARENT` + `MA_NOACTIVATE`） | `FoodWindow.cs` | **必须**真机验证点击穿透 🚫 |
| A7 设置改为**逐字段回退**，失败字段用默认值并保留诊断；保存前先校验后再 `File.Replace` | `SettingsDocument` + `SettingsStore` | 新增 Core 测试（字段级回退 + 备份不被坏文件覆盖） |
| A8 异常过滤器加入 `UnauthorizedAccessException`/`SecurityException`；`Scan` 逐目录兜底 | `CharacterLoader.cs:75-81` + `CharacterCatalog.cs:44` | 新增 Core 测试（置只读/拒读目录 → 其余角色仍可用） |
| A10 `GetOrAddLease` 改为命中即 `Dispose` 旧租约，或按 Id 查找 | `PetSession.cs:128` | 新增 Core 测试（同 Id 不同对象 → 旧定义被释放） |
| A9 给 `Tick` 与 `DrainBatch` 加最小异常兜底（记录 + 跳过该实例，不终止循环） | `AppHost.cs` / `BatchCreationQueue.cs` | Core 无法覆盖，需 smoke 🚫 |
| A3 给缩放链加**联合上限**并让缓存可淘汰（至少：`effectiveScale` 上限收敛为单一常量 + 按字节预算 LRU） | `CharacterLoader` / `PetEngine` / `PetWindow` / `FrameSurfaceCache` | 新增 Core 测试（缩放乘积被钳制）+ 真机观察内存 🚫 |
| A4 让 `VirtualFoodSystem` **只设置目标点/朝向**，不再写 `FootPosition`/`State` | `VirtualFoodSystem.cs` + `PetEngine` 一个新入参 | 新增 Core 测试：① 关闭走动时食物不驱动 Walk ② 靠近速度 = WalkSpeed |

**P0 完成判据**：148 + N 个 Core 测试全绿；A6/A9 有真机记录；`docs/STATUS.md` 按四档追加条目。

### P1 — 收敛与清理（纯重构，零行为变化）

1. **常量单一真源**：建 `PetLimits`（或复用 `CharacterLoader` 常量）承载"行走速度上限 / 落地边距 / 缩放上限 / delta 上限 / 喂食量"，消除 §5-B2 的 6 处分歧。
2. **删死代码**：`SettingsDocument.WarningLimit`、`PetSession.HardLimit`、`BatchCreationQueue.Changed`、`PetListProjection.Add/Remove/Changed`、`ScaleForWindow` 整链、`NativeMethods` 的 `SetWindowPos`/`GetWindowLongPtr`/`SetWindowLongPtr`/`SWP_*`/`GWL_EXSTYLE`、`CharacterFramePreview` 的 `scale` 死参数、`MainForm` 三处死分支。
3. **`Boredom` 决策**：接进行为/UI，或停止推进（二选一，不要留着只写状态）。
4. **把越界 P/Invoke 移回 `NativeMethods.cs`**（恢复 `AGENTS.md` 铁律）。
5. **测试 fixture 收敛**为 `tests/.../Fakes.cs`；补齐 2 处临时目录清理。
6. **引入最小日志**：一个 `IAppLog`（文件追加 + 内存环形缓冲），替换显示器枚举/窗口枚举/DPI/保存这些路径上的静默 catch。

**P1 完成判据**：净删代码；148 测试仍全绿；**无任何行为差异**（真机 smoke 结论应逐字相同）。

### P2 — 结构拆分（收益最高、风险最高，务必独立提交）

1. **拆 `PetEngine`**（目标：`Tick` 从 131 行降到 ~35 行编排）：
   - `PetMotion`（行走/空中/蓄力/攀爬/顶走积分）
   - `PetSurfaceQuery`（预测 + 表面选择 + 附件校验）
   - `PetStateMachine`（dispatch + 命名转移，消灭 §5-B7 的 12 处直接赋值）
   - `PetAutonomy`（延迟/决策/蓄力入口）
   - `PetEngineSettings`（把 8 个 settable 属性变成注入的不可变配置）
2. **拆 `AppHost`**（目标：只剩组合根 + `ApplicationContext`，~60 行）：
   `PetWindowController` / `FrameClock` / `DialoguePresenter` / `VirtualFoodPresenter` / `ForegroundMonitor` / `CharacterLibrary` / `SettingsSession` / `TrayShell`。
3. **拆 `CharacterLoader`**：`PngHeaderReader` / `SpriteFrameExtractor` / `CharacterConfigReader` / `DialogueReader`；`ReadConfig` 降到 ~20 行/方法。
4. **拆 `JumpTargetPlanner.TrySelect`**（117 行 / 12 参数 → 5 个纯函数 + 10 行循环）。

**P2 的验收关键**：必须在**每一步之后**跑一遍完整 Core 测试 + 一次真机 `desktop-input-smoke`，因为 `PetEngine` 与 `AppHost` 的行为无法被 Core 测试完全覆盖 🚫。

### P3 — 体验与性能（可延后）

- UI 去重（估算省 ≈130–160 行）：`ManagerPage.CreateSectionLabel/AddCard`、统一 `AddSettingRow`、`PetsPage` 设置项**表驱动**（把 5 个 `Apply*Override` + 16 个事件订阅折成表 + 2 个循环）。
- C2 的每帧分配：`Instances` 改为版本化快照，`VirtualFoodSystem.Tick` 复用传入集合。
- C3 暂停/隐藏时跳过重绘。
- C4 用 `Span<byte>` 覆盖 `Scan0` 行区间替代逐像素 `Marshal.WriteByte`。
- C1 二分迭代降到 12–14 次。
- 4 个 smoke 工具抽共享库（估算删 ≈400–450 行），统一 `Pump` 粒度与断言/退出码语义。

---

## 8. 风险登记

| 风险 | 说明 | 缓解 |
| --- | --- | --- |
| **P0-A6/A9 无法用 Core 测试覆盖** | 点击穿透、帧循环异常隔离只能真机看 | 明确在 `docs/STATUS.md` 标 `Windows 桌面未验证`，不得用构建通过代替 |
| **P0-A3 的缩放钳制会改变现有观感** | 收紧上限后，极端缩放下宠物会比现在小 | 需产品确认上限值；改动前后各跑一次双屏矩阵 |
| **P2 拆分可能引入行为漂移** | `PetEngine` 的时序（vibe-decay、重试用例）很敏感 | 每步独立提交 + 全量 Core 测试 + 真机 smoke；不要与 P0/P1 混在一个提交 |
| **`DefinitionLease` 静态 `ConditionalWeakTable` 使测试顺序敏感** | 共享同一 definition 实例的测试会互相影响生命周期 | 拆分时不要改变租约语义；如需重构，先加一组"释放后可重新注册"的测试 |
| **工具靠私有字段名反射** | `_manager`/`_tray`/`CharacterRootOverride` 等被 smoke 工具反射访问，且工具不在 `.sln` 内 | 重命名字段时必须**手动**跑一次 4 个 smoke 脚本 |

---

## 9. 未验证边界（引用本文件时不得跨过）

1. **本评审未执行任何桌面验收**——`desktop-smoke` / `desktop-input-smoke` / `desktop-display-smoke` 本轮全部未运行。
2. 所有 `❓` 条目（A5、A8 的用户可见影响、A3 的实际内存曲线、A9 的弹窗行为）**均未复现**。
3. `🔎` 标记的条目来自只读静态审查，行号已给但**未经本评审逐条复核**。
4. A3 的内存数字是**静态算术上界**（418px × 有效缩放 16 × 4 字节 × 18 键），不是实测。真实曲线需真机长时观测。
5. §3.2 的"文档分层名 ≠ 代码类型"来自对 `PROJECT_SPEC.md` / `ARCHITECTURE.md` 与全仓源码的比对，**未逐类型穷举验证**。
6. 本文件**未修改任何代码**，因此按 `AGENTS.md` §6 的口径，`docs/STATUS.md` 只需记录"做过这次评审"这一事实，**不得**把本文件的缺陷清单写成"已修复"或"已验证"。

---

## 附录 A：一句话结论对照表（给决策用）

| 问题 | 回答 |
| --- | --- |
| 需要整体重构吗？ | **不需要**。分层与测试是资产，重写会让桌面验收债务扩大。 |
| 需要重构吗？ | **需要**，且应尽快做 A 组（10 项缺陷）。 |
| 最该先修哪一项？ | **A1**（GDI 泄漏窗口，每帧每宠调用）与 **A7**（设置整份回滚 + 覆盖备份）。 |
| 最贵的一项？ | **P2 拆 `PetEngine` / `AppHost`**，需独立提交并逐次真机验收。 |
| 最容易做且有收益的？ | **P1 的常量收敛 + 删死代码 + 接最小日志**（净删代码、零行为变化）。 |
| 最不该做的？ | 重写渲染层、引入 MVVM/DI 容器、把 UI 拆成独立项目。 |
| 重构的验收标准是什么？ | **148+N Core 测试全绿 + 真机 smoke 逐项通过 + `docs/STATUS.md` 四档记录**。 |
