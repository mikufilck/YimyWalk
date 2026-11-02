# 测试与验收入口

## 自动验证

快速测试和桌面脚本使用 `.tools/dotnet/dotnet.exe`；缺失时先运行 `scripts/bootstrap-dotnet.ps1`。构建脚本也支持 `-DotnetPath` 或系统 SDK 回退，但版本必须满足 global.json。当前实跑结果见 [MAINTENANCE](MAINTENANCE.md)，此处只维护步骤与验收标准。完整发布链路执行一次即可：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build-yimywalk.ps1 `
  -Version 0.1.14 -PublishPath artifacts/publish/yimywalk-win-x64
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-release-package.ps1 `
  -Path artifacts/publish/yimywalk-win-x64 -Version 0.1.14

# 只做快速逻辑回归时：
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-yimywalk.ps1
```

`build-yimywalk.ps1` 会 restore、运行整个解决方案的核心测试，清理并发布 x64 `net8.0-windows` 应用，同时把 `Characters/`、`THIRD_PARTY_NOTICES.md` 和发布 manifest 复制到发布目录。`test-yimywalk.ps1` 在 Windows 运行 Release Core 与 App 测试，适合快速回归。`publish-yimywalk.ps1` 使用同一套流程把可复制包写到 `artifacts/publish/yimywalk-win-x64`；每次运行都会清理目标目录并生成 `PACKAGE_CHECKSUMS.txt`，两次相同输入应得到相同清单。

发布包是 self-contained x64 单文件应用，直接双击 `YimyWalk.exe` 即可运行，不需要单独安装 .NET Runtime。`run-yimywalk.ps1` 会检查发布目录、`YimyWalk.exe`、`Characters` 目录和至少一个 `sprite.png`；缺失时返回 `PUBLISH_INCOMPLETE` 或 `CHARACTERS_*` 诊断而不启动应用。脚本仍兼容旧的 framework-dependent 开发输出并保留 `RUNTIME_MISSING` 诊断。它不会修改安装目录之外的桌面文件、注册开机启动或请求管理员权限。应用启动后若角色目录全部无效，会显示 `CHARACTERS_INVALID` 并把摘要写入用户本地 `YimyWalk/startup-error.log`。

在可见 Windows 桌面会话中，`powershell -NoProfile -ExecutionPolicy Bypass -File scripts/desktop-smoke.ps1` 会使用临时 `tools/DesktopSmoke` 宿主真实创建并清空 1、10、50、100 个窗口，并输出累计处理器秒数 `CPU_S`、工作集、私有字节、托管堆、句柄、USER/GDI 基线；可追加 `-Rounds 3`（上限 20）在同一宿主进程重复多轮以观察趋势，或追加 `-Role slime` 验证另一套仅含 `sprite.png` 的角色。它不验证透明点击、拖拽或焦点行为，这些由输入矩阵单独验证。

`powershell -NoProfile -ExecutionPolicy Bypass -File scripts/desktop-input-smoke.ps1` 会在真实 Windows 窗口上执行掉落、行走、跳跃、透明/实体命中、鼠标拖拽、释放抛掷、捕获丢失、焦点和托盘生命周期检查。它会先打印生产窗口自身的 `WM_NCHITTEST` 结果；若当前会话由 `LockScreenBackstopFrame` 等安全桌面窗口截获 `WindowFromPoint`，会输出 `RESULT UNVERIFIED INPUT_ROUTING_BLOCKED`，保留未验证状态而不把环境阻塞当作功能通过。

同一个输入 smoke 还检查 M4 主菜单：普通任务栏窗口、五页导航、启动隐藏、关闭隐藏回托盘和恢复、桌宠实例列表、稳定实例选择、idle1/index 0 单帧预览、单实例覆盖、全局互动控件、设置保存入口、托盘清空队列和显式退出。它不会把关闭主菜单误判为退出；真正退出必须看到托盘和宿主资源释放。若透明穿透被当前桌面遮罩阻塞，输出仍会保留壳体和业务检查结果，但最终状态必须按 `Windows 桌面未验证` 记录。

`powershell -NoProfile -ExecutionPolicy Bypass -File scripts/desktop-display-smoke.ps1 -Rounds 3` 要求可见且已解锁的 Windows 桌面至少有两块扩展显示器。它复用生产 DPI 初始化、PerMonitorV2、表面与真实分层宠物窗口，在独立 Harness 中操作真实鼠标（不运行完整 AppHost），在每块显示器上检查显示器 bounds/工作区/DPI、cat/slime 的生成和落地、行走/跳跃、透明区域穿透、实体命中、拖拽释放、全局缩放，以及两个方向的跨屏拖拽；随后在同一宿主进程中重复创建和清空并记录 CPU、工作集、私有字节、句柄、USER/GDI。`-Rounds` 可设为 1–100，`-InventoryOnly` 只记录当前显示器元数据。该 smoke 不会改变显示器配置、关闭或移动现有用户窗口；物理拔插、睡眠/唤醒、任务栏布局变化和负坐标拓扑必须另行执行，未执行时仍标记为未验证。

当前核心测试覆盖：3×3 切帧和 0—8 协议、左向镜像、默认脚底锚点、sprite-only 默认值、坏 PNG/透明图/越界图、受限 delta、坠落和落地、行走边界、跳跃 apex、拖拽和抛掷、实例状态隔离、1/10/50/100 创建与清空、失败刷新保留旧定义、共享资源引用计数和幂等释放，以及 hunger/boredom 独立数值、配置增长率、人工喂食边界、饥饿开关、台词限流和虚拟食物认领/消费/超时。

## 桌面工具的设置隔离边界

四个工具均不在解决方案内，重构 App 后应单独编译；入口和 ManagerUiSmoke 的直接运行命令见 [INDEX §6](INDEX.md#6-工具与脚本入口)。编译成功不能记为 smoke 执行通过。

`DesktopSmoke`、`DesktopInputSmoke` 和 `ManagerUiSmoke` 都在构造 AppHost 时传入独立临时设置目录，日志与保存同步隔离，并在 teardown 清理。`DesktopDisplaySmoke` 的独立 Harness 不创建 SettingsStore。角色 fixture 另做临时复制，不能用角色隔离替代设置隔离。

R13 已修正；不要把工具改回默认用户设置，HostIsolationTests 负责验证注入保存和失败取消。输入 smoke 会启动自己的子进程背景窗，实际检查食物 / 气泡跨进程点击与焦点；所有窗口、事件与进程均由测试创建和回收。

App 文件持久化与资源逻辑可以在注入目录的无窗口测试中验证；点击穿透、焦点、拖拽与 DPI 仍需真实桌面。增加测试时应覆盖实际适配层，不只验证相同逻辑的假实现。

## Windows 桌面验收矩阵

桌面验收必须在可见 Windows 桌面会话中进行，不能用普通窗口动画或纯逻辑模拟代替。每次记录 OS、进程架构、DPI、显示器工作区、CPU、内存以及进程的适用 USER/GDI/句柄指标。

| 项目 | 证据要求 |
| --- | --- |
| 角色加载 | 只放 `sprite.png` 的 cat/slime 都能启动，换目录不改代码 |
| 运动 | 屏幕上方掉落、落地、Idle/Walk、Jump、Fall 都在真实桌面可见 |
| 交互 | 透明区域穿透、实体命中、拖拽、释放抛掷分别验证 |
| 焦点 | 点击、拖拽和刷新不激活宠物窗口 |
| 生命周期 | 托盘隐藏/恢复、清空、退出后窗口、计时器、托盘和绘制资源都释放 |
| 批量 | 1、10、50、100 真实创建与一键清空，重复运行观察 CPU、内存和句柄趋势 |
| 多显示器/DPI | 每块屏幕真实生成、落地、渲染尺寸、透明/实体命中、拖拽跨屏和释放；记录每屏 DPI 与工作区 |

100 只是基线档位，不是永久产品上限，也不能在没有测量时承诺流畅。桌面项目若未执行，必须标记为“Windows 桌面未验证”。

## 上游基线

M0 的 eSheep 审计记录保存在 [`docs/UPSTREAM_AUDIT.md`](UPSTREAM_AUDIT.md)。完整上游源码、素材和原版测试脚本不属于 YimyWalk 源码树；需要复核时，应在仓库外临时获取审计中固定的提交。上游结果只用于复用性判断，不能替代 YimyWalk 的核心测试或 Windows 桌面验收。

`desktop-smoke.ps1` output also includes `MANAGED` heap and `FINAL` host-Dispose metrics to distinguish active-window cleanup from process allocator retention; these trends are baseline evidence and not a promise of long-term smoothness at 100 instances. The current single-display landing run also verifies the manager-controlled opt-in path indirectly through the host property; the input smoke directly toggles the manager checkbox and verifies close/reopen.

## 双显示器实测记录（2026-10-05）

在 Windows 11 Home 10.0.26200、x64、16 logical CPUs、Intel Core i5-12500H 的可见桌面上，`desktop-display-smoke.ps1 -Rounds 3` 返回 `RESULT PASS CHECKS=83 FAILURES=0`。主屏 `\\.\DISPLAY1` 为 1920×1080、工作区 1920×1020、DPI 120（125%）；副屏 `\\.\DISPLAY2` 位于主屏右侧，为 2560×1440、工作区 2560×1392、DPI 96（100%）。两套 sprite-only 角色均在两块屏幕上真实生成、落地、行走、跳跃和清空；透明像素穿透、实体像素命中、焦点保持、主屏→副屏及副屏→主屏拖拽释放均通过。主屏单格实际渲染为 40×40，副屏为 32×32；global scale=2 时分别为 80×80 和 64×64。

三轮资源记录的清空值为：GDI 41、46、51；USER 均为 15；句柄为 323、324、324；活动实例和窗口均回到 0，渲染 cache 回到 0。`AutoScaleMode.Dpi` 已用于宠物窗口，以避免 WinForms 默认字体自动缩放在跨 DPI 创建/销毁时产生更大的 GDI 增长；剩余 GDI 小幅上升仍作为平台/WinForms 趋势风险保留，不能写成“无泄漏”或长期流畅性承诺。完整日志见 `artifacts/dual-display-final.log`。

## M2 window-top landing

Run `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/desktop-smoke.ps1 -Landing` only in an unlocked, visible Windows desktop session. The harness opens a temporary probe window in an unused region, creates one pet at the probe's horizontal span, verifies that the pet's foot reaches the probe top, closes the probe, and verifies re-fall to the display floor. It does not move, close, or alter existing user windows. A Core selection test or ordinary animation cannot replace this desktop check.

## 0.1.14 回归入口补充

App.Tests 只在 Windows 执行；包含设置原子 / 备用写入路径、渲染预算与 GDI 失败、单调日志限频、实际临时文件 ACL 拒读、宿主隔离。Core 在 Ubuntu / Windows 均可执行。CI 的 Windows job 运行这些适配层测试并编译四个 smoke；CI 不运行真实桌面输入。

本轮实际结果与未验证硬件边界见 [修复记录](reviews/2026-11-01-remediation.md) 和 STATUS。`--preview-pixels` 只覆盖 UI 渲染尺寸计算，不代表 OS DPI 切换已经测试。

## 咿米气泡样式复核

管理器视觉重做的 `--visual-review`、默认 / 最小窗口、标题栏和菜单检查见 [UI_DESIGN](UI_DESIGN.md)。这些检查与本节的气泡检查是不同入口；角色动作和全显示器桌面矩阵不能由 UI 效果图替代。

`ManagerUiSmoke --bubble-skin` 生成 96/120/144/192 DPI 的运行时文字样张和包含原角色的预览，在当前真实桌面显示逐像素 Alpha 气泡，并仅截取工具自己的不透明背景窗口，检查透明角、白色正文区和显示前后前台窗口未变化；另验证当前屏幕顶部 / 右侧边界。命令与素材来源见 [视觉说明](YIMY_VISUAL_STYLE.md)。

`BubbleRenderingTests` 检查 Alpha、边角不随文字长度伸缩、换行 / 极长文字限高和负坐标工作区定位；输入 smoke 点击气泡可见正文区域，验证真实跨进程穿透与焦点。绘制样张和合成负坐标测试不能代替真实多显示器与 OS DPI 切换验收。
