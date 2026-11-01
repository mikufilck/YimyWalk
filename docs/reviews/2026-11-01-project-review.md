# 2026-11-01 全项目审查与长期维护基线

> 本次审查后的 R01–R13 修复见 [处置记录](2026-11-01-remediation.md)。下文是修复前的证据快照，待处理状态不代表当前仍未修复。

## 1. 范围、方法与结论

基线：`codex/bootstrap` @ `7a0fb13`。开工已有未提交的 `AGENTS.md`、`docs/STATUS.md` 修改，以及未跟踪的 `docs/INDEX.md`、`docs/TECHNICAL_REVIEW.md`；本轮保留并继续整理，没有提交或推送这些既有改动。

审查覆盖产品 Core/App、测试、五个工具项目、构建发布与桌面脚本、CI、角色包、第三方声明和工程文档。方法为目录与依赖全量盘点、关键调用链静态审查、自动测试 / 编译 / 发布实跑，以及无窗口的沙盒缺陷复现；不构成逐行无遗漏审计或 Windows 桌面验收。

以 `git ls-files` 中的 `.cs` 统计，排除生成目录与临时探针：**80 个 C# 文件 / 10,874 行**（Python `splitlines()` 口径）。Core 20 / 2,493；App 30 / 3,752；Tests 23 / 2,602；Tools 7 / 2,027。此前评审的 98 / 11,091 统计口径未保留，不继承为当前源码数字。

结论：保留现有分层和窗口方案；先修已复现的恢复、资源和运动问题，再按需要拆分大类。当前主链路可构建、可测试、可发布，但不能据此称 M0–M4 全部验收通过。现有测试的通过范围不覆盖本轮新找到的组合场景。

## 2. 本轮实跑证据

环境：Windows 11 家庭版中文 x64，本地 SDK **8.0.426**；`global.json` 请求 8.0.425，允许同 feature band 的 latestPatch。日期按项目会话 2026-11-01 记录。

| 项目 | 命令 / 方法 | 结果 |
| --- | --- | --- |
| 环境 | `scripts/verify-environment.ps1` | 退出 0；本地 SDK 可用，仓库无 vendor/esheep |
| Core | `scripts/test-yimywalk.ps1` | **148 通过 / 0 失败 / 0 跳过** |
| 解决方案 | `.tools/dotnet/dotnet.exe build YimyWalk.sln -c Release --no-restore` | **0 错误 / 12 警告**，NU1902 / NU1903 均指向 ImageSharp 3.1.12 |
| 桌面工具编译 | 对 `DesktopSmoke`、`DesktopInputSmoke`、`DesktopDisplaySmoke`、`ManagerUiSmoke` 分别 `dotnet build tools/<工具>/<工具>.csproj -c Release -p:Platform=x64` | 均退出 0；只编译，无窗口与鼠标操作 |
| 发布 | `scripts/build-yimywalk.ps1 -Version 0.1.13 -PublishPath artifacts/maintenance-20261101/publish` | 退出 0；流程内 Core 148 项再次通过，生成 self-contained x64 包 |
| 验包 | `scripts/test-release-package.ps1 -Path artifacts/maintenance-20261101/publish -Version 0.1.13` | `PACKAGE PASS VERSION=0.1.13 FILES=13 CHECKSUMS=12` |
| 角色 | 对 `Characters` 三个子目录分别执行 `scripts/validate-character.ps1 -Path <角色目录>` | 三者均退出 0；1254×1254 / 单格 418×418、Alpha、9 帧、默认配置与台词 |
| 沙盒探针 | 引用现有 App/Core 的临时控制台，只调用 Core 和注入目录的 SettingsStore，不创建 AppHost | 四种预期缺陷均复现，见下表；不是功能通过或新增 Core 用例 |
| Windows 桌面 | 未运行 desktop-*、ManagerUiSmoke 程序或发布 EXE | **Windows 桌面未验证** |

本机附加证据在忽略目录 `artifacts/maintenance-20261101/`：`build.log`、四份 `<工具>-build.log`、`publish.log`、三份 `character-<角色>.log`、`probe.log`、`probe/` 源码、`inventory.json`（受控文件哈希和三角色 PNG 元数据）。这些是本机证据，克隆仓库后不保证存在；本报告保留命令、结果和复现步骤供重建。发布仅为本地复核，没有上传版本。

探针的 `REPRO` 表示观察到了问题，不能将退出 0 解读为这些功能健康：

| 问题 | 沙盒复现步骤 | 实际结果 |
| --- | --- | --- |
| 关闭走动后食物仍移动宠物 | 加载 cat fixture；地面 `PetSession` 中设 `DefaultWalkEnabled=false`；在宠物右侧 300px 生成并认领食物；按宿主顺序调用 `session.Tick(0.1)`、`food.Tick(session.Instances, 0.1)` | X 增加 **18px**，状态变 Walk |
| 暂停后食物仍移动宠物 | 上例调用 `session.Pause()`，重复同样的两次 Tick | X 再增加 **18px**；宿主 Tick 没有用暂停状态限制食物系统 |
| 同 ID 不同定义的所有者未释放 | 从同一 cat 目录加载两个不同定义对象；同一 session 先后对两者调用 `CreateMany(definition, 1)`；Dispose session | 首个定义 `IsDisposed=false`，第二个为 true；只证实释放语义异常，未测长期内存曲线 |
| 恢复后保存破坏备份 | 用工作区沙盒目录构造 SettingsStore；保存两次合法配置；主文件写入 `invalid-json`；Load 确认 `UsedBackup=true`；Save 恢复出的 Settings | `.bak` 被替换成 `invalid-json`，原可用恢复点丢失 |

## 3. 待办登记

状态均为 **待处理**。P1 为优先稳定性项，P2 为后续维护项；“源码确认”仅确认机制，“沙盒复现”只确认该输入下的行为。以后修复时在 STATUS 写明关闭的 ID；本报告保留当时快照。

| ID / 优先级 | 发现与证据 | 代码入口 | 修复后的验收要求 |
| --- | --- | --- | --- |
| R01 / P1 | **源码确认**：缓存按原始 float 缩放建键，无预算 / 淘汰；加载时单帧上限不能约束全局倍率 × DPI。未压测 OOM | [FrameSurfaceCache](../../src/YimyWalk.App/Rendering/FrameSurfaceCache.cs)、[PetWindow](../../src/YimyWalk.App/Windows/PetWindow.cs) `EffectiveScale` | 明确按像素 / 字节的联合预算与在用 surface 生命周期；缩放切换回归，真机观察资源趋势。不可只加 LRU 就释放仍被窗口引用的位图 |
| R02 / P1 | **源码确认**：GDI 获取在 try 之前；GetHbitmap 抛错时前序 DC 无 finally 清理；ULW 返回值被忽略 | [LayeredWindowRenderer](../../src/YimyWalk.App/Rendering/LayeredWindowRenderer.cs) `Present` | 覆盖分步获取失败 / 零句柄，提供失败信号；真机渲染与 GDI 回落另验。未声称日常每帧必然泄漏 |
| R03 / P1 | **沙盒复现**：从备份恢复后，保存把坏主文件变成新备份；现有 smoke 只验证到 Load，未验证再 Save | [SettingsStore](../../src/YimyWalk.App/Settings/SettingsStore.cs) `Load/Save/ReplaceWithCopyFallback` | 临时目录验证恢复→保存→再次损坏→仍可恢复，兼顾原子替换失败路径。保持合法配置校验契约 |
| R04 / P1 | **沙盒复现**：关闭走动和暂停均未阻止食物移动；引擎与食物系统双写运动状态 | [AppHost](../../src/YimyWalk.App/Host/AppHost.cs) `Tick`、[VirtualFoodSystem](../../src/YimyWalk.Core/Simulation/VirtualFoodSystem.cs) `Tick` | 组合回归覆盖暂停、实例走动覆盖、拖拽中断、运动速度及消费完成；真实食物窗口交互另验 |
| R05 / P1 | **沙盒复现**：`CreateMany` 同 ID 新定义覆盖所有者 lease；旧对象 owner 计数没有释放 | [PetSession](../../src/YimyWalk.Core/Simulation/PetSession.cs) `GetOrAddLease` | 同 ID 两定义、旧实例仍可用、清空与 Dispose 后两定义正确释放；不能仅按 ID 命中就忽略传入的新定义 |
| R06 / P1 | **源码确认**：Loader 不归一 UnauthorizedAccessException，Catalog 只捕 CharacterLoadException；枚举中途失败也没有释放已创建 nextLeases 的 finally | [CharacterLoader](../../src/YimyWalk.Core/Characters/CharacterLoader.cs) `LoadDirectory`、[CharacterCatalog](../../src/YimyWalk.Core/Characters/CharacterCatalog.cs) `Scan` | 沙盒拒读 / 锁定和枚举失败后保留已知可用角色，无半次扫描资源遗留；本轮未更改 ACL 或实际复现权限失败 |
| R07 / P2 | **源码确认**：两条 DllImport 位于 Host，违反集中声明规则；SmokeTests 只检查三个程序集引用，不约束 P/Invoke 与 App 类型可见性 | [WindowsDisplayAdapter](../../src/YimyWalk.App/Host/WindowsDisplayAdapter.cs)、[SmokeTests](../../tests/YimyWalk.Core.Tests/SmokeTests.cs) | 移至 NativeMethods 后签名不变；补适当架构检查，显示器桌面验收另记 |
| R08 / P1 | **源码确认 / 桌面待验**：FoodWindow 无透明命中处理；不能仅从缺少 WndProc 直接声称真机点击已被吞 | [FoodWindow](../../src/YimyWalk.App/Windows/FoodWindow.cs)，参照 BubbleWindow | 实际食物覆盖在其他应用上时验证点击路由与焦点；UI 截图不能替代 |
| R09 / P1 | **工具报告**：ImageSharp 3.1.12 的 NU1902/NU1903；本轮解决方案报告六条通告，经两个项目重复为 12 警告 | Core / CharacterValidator 的 csproj；`build.log` | 独立核对通告适用性与候选版本、许可，升级后跑解码 / 验证器 / 发布回归；本轮未评估每条通告的可利用性，也未修改依赖 |
| R10 / P2 | **源码确认 / 影响待验**：IsHostWindow 未包含气泡与食物；20px 食物被 tooSmall 排除，但较大气泡仍可能作为候选或遮挡体 | [AppHost](../../src/YimyWalk.App/Host/AppHost.cs) `IsHostWindow`、[WindowSurfaceResolver](../../src/YimyWalk.App/Host/WindowSurfaceResolver.cs) | 打开窗口落脚，用气泡真实验证候选及遮挡；不要把食物和气泡的筛选结果一概而论 |
| R11 / P2 | **源码确认**：拖拽测速与低频轮询依赖 UTC 墙钟；隐藏 / 暂停仍调用渲染；无持续运行日志，只有启动失败日志 | PetWindow、AppHost、DesktopWorldSurface、WindowSurfaceResolver、StartupFailure | 单调时间注入 / 故障诊断按需改；先测量再优化。日志需大小边界，避免记录用户内容 |
| R12 / P2 | **源码确认**：Loader 行走上限 2000，Engine 1000；Boredom 在生产代码只推进不驱动行为；默认版本仍为 0.1.5 | CharacterLoader、PetEngine、PetInstance、Directory.Build.props、发布脚本 | 先明确产品语义再统一，验证旧角色和设置兼容；不要因数字不同直接把所有范围或喂食剂量合并 |
| R13 / P1 | **源码确认**：DesktopSmoke 的临时设置自检没有注入后续 AppHost；DesktopInputSmoke 同样使用默认用户 SettingsStore，测试全局开关时可能改写偏好 | [DesktopSmoke](../../tools/DesktopSmoke/Program.cs)、[DesktopInputSmoke](../../tools/DesktopInputSmoke/Program.cs)，参照 ManagerUiSmoke 的临时设置注入 | 所有完整宿主 smoke 使用独立设置目录；成功 / 失败 / 异常退出均不改变原用户配置。隔离修好后再做对应桌面验收 |

**下一项**选 R03：有确定沙盒复现、影响用户偏好恢复、无需桌面即可验证文件行为。R01/R02/R08/R09 同为优先项；这一顺序不代表它们已解决或可以无限期搁置。

## 4. 需求覆盖与真实缺口

| 范围 | 已有实现 | 尚未实现 / 尚不完整 |
| --- | --- | --- |
| M0 来源与构建 | 固定上游审计、外部引用策略、SDK / 脚本 / 许可证声明 | 本轮没有重新拉取上游，也没有逐项重新核验所有素材授权 |
| M1 最小桌宠 | PNG 九格、基础状态、拖拽抛掷、单宠窗口、批量创建清空、托盘 | Windows 本轮未重新验收；当前三套正式角色尺寸均远大于早期测试小图 |
| M2 持续扩展基础 | 定义共享与刷新、DPI / 多屏、窗口落脚、全局设置备份 | 备份保全有缺陷；实例重启恢复 / 崩溃安全恢复模式未实现；负坐标真机 / 拔插 / 睡眠 / 长时趋势待验 |
| M3 趣味闭环 | 饥饿 / 无聊数值、人工喂食、简单虚拟食物、事件气泡、可关闭前台切换反馈 | 无聊未驱动行为；气泡是事件触发，并非完整自动闲聊调度；真实图标分身 / 图标位置读取、完整偷吃动画、actions.png、粒子均无实现；未见独立音量 / 静音 / 全屏勿扰机制 |
| M4 稳定化与扩展 | 五页管理 UI、实例覆盖、作者校验器、便携发布 | 未见宠物右键菜单、整宠完全穿透开关、完整应用事件 / 时间段事件、社交 / 繁殖；真实快捷方式暂存未实现，继续保持隔离边界 |

上述缺口是需求差距，不能全部当成已承诺本轮修复的 bug；也不能因历史评审写了“主功能完成”就从规格删除。内部指令有管理器等效创建 / 清空 / 暂停入口，未发现文本命令解析器。

## 5. 旧评审复核与记忆修正

`docs/TECHNICAL_REVIEW.md` 保留为 2026-10-16 历史快照。本轮对其高风险结论做了复核，以下解释在后续维护中优先：

- 单字段非法会令 `SettingsDocument.TryParse` 返回 false，这由现有测试明确约束；**SettingsStore 还会尝试合法备份**，不能直接写“单字段非法必丢全部偏好”。确认的缺陷是 R03 的后续保存破坏恢复点；逐字段容错会改变契约。
- Core 没有系统时钟依赖，但 App 多处确实使用 `DateTime.UtcNow`；“所有时钟均已注入”不符合现状。物理单调 delta 与拖拽墙钟要分开讨论。
- `MainForm` 未赋值是 ApplicationContext 独立管理消息循环的当前设计；相关冗余分支可以整理，**不能把未赋值本身当作生命周期缺陷**。
- `HardLimit` 是已有公开 Core API，WarningLimit 是已序列化字段；生产未使用并不自动允许直接删除。不同语义的喂食剂量和 UI / 有效缩放范围也不宜机械统一。
- 批量队列 `Dequeue()` 在执行工厂前发生；单个失败项目不会每 tick 原样重新入队。帧循环异常隔离确实缺失，但具体弹窗或循环失败表现仍需复现。
- UI 有 ManagerUiSmoke、输入 smoke 和像素回归；“App 零自动化覆盖”过于绝对。准确说法是：常规 xUnit / CI 不执行这些 App 检查。
- DesktopDisplaySmoke 复用生产 DPI 初始化、表面、渲染与宠物窗口，但用独立 Harness，不能说它覆盖完整 AppHost 启动与设置生命周期。完整宿主工具的用户设置隔离仍有缺口（R13）。
- 降低二分迭代次数、替换逐像素写法或大规模拆类都需要正确性 / 性能证据；本轮没有用静态行数估算承诺收益。
- 当前管理器启动隐藏、关闭隐藏回托盘；气泡总闸会持久化。已同步修正文档，历史 STATUS 条目保留当时叙述。

## 6. 本轮落地的记忆结构

`AGENTS.md` 只负责约束和读取协议；`INDEX.md` 负责导航与改动落点；新增 `MAINTENANCE.md` 作为当前短摘要；`STATUS.md` 保存带日期的交接证据；规格与格式契约独立；本目录保存有日期的审查快照。旧评审保留并链接新证据，避免推测被反复复制成事实。

本轮只改文档与记忆入口，没有修改产品实现、测试断言、依赖、脚本或素材。仓库外没有设置隐式记忆，没有建立后台维护任务。后续会话按 AGENTS 的入口读取即可继续接手。

收尾检查：本轮 10 份文档的 93 个本地链接 / 锚点全部解析成功；Core / App / 测试 C# 文件名全部进入索引；139 个受保护文件与开工哈希一致，`git diff --check` 通过。此检查只验证链接和改动范围，不证明所有文字判断或产品行为正确。
