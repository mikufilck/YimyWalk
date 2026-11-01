# YimyWalk 当前维护记忆

> 最近核对：2026-11-01；维护分支 `codex/bootstrap`，当前版本 **0.3.0**。实际提交与同步状态以 `git log -1`、`git status` 和远端分支为准。验证证据见 [STATUS](STATUS.md)，代码定位见 [INDEX](INDEX.md)，R01–R13 处置见 [修复记录](reviews/2026-11-01-remediation.md)。

## 项目与维护方向

离线 Windows x64 桌宠引擎，.NET 8 + WinForms + AntdUI。保留 Core/App 分层、单宿主与每实例一个分层窗口。本轮审查登记 R01–R13 均已实现修正并执行针对性检查；具体证据与未覆盖边界保留在修复记录，不能推广为所有 Windows 环境通过。

用户确认主要角色为 **Yimy（咿米），粉色头发的人形犬娘**。视觉优先对齐用户说明与现有咿米角色图；当前统一气泡已改为原创粉白犬耳 / 肉垫 / 蝴蝶结 / 金铃铛皮肤。[视觉依据、原画与提示词](YIMY_VISUAL_STYLE.md)是这部分的维护入口。

M0–M4 仍是完整目标。图标分身、额外动作、完整事件调度、勿扰、实例重启恢复、社交等需求差距见 [原审查 §4](reviews/2026-11-01-project-review.md#4-需求覆盖与真实缺口)，不把缺陷修复等同于所有扩展已经实现。

## 接手时必须知道的当前行为

| 主题 | 当前实现与定位 |
| --- | --- |
| 启动 / 关闭 | 启动隐藏管理器、不自动创建宠物；托盘打开管理器，普通关闭隐藏回托盘，显式退出才结束宿主 |
| 时间与诊断 | `RuntimeServices` 提供注入的单调时钟；物理 / 拖拽 / 轮询均使用它。日志仅写稳定操作码和异常类型，每份约 1 MiB、一份轮换、同类限频；帧循环故障暂停，单宠绘制故障与批量失败分别隔离 |
| 资源所有权 | 定义通过 DefinitionLease 共享；渲染窗口与预览必须持有 SurfaceLease，先释放 handle 再释放定义使用权。禁止拿到裸 surface 后丢弃 handle |
| 像素与缓存 | 最终缩放统一走 `PetLimits.EffectiveScale`，单边 ≤1024、总像素 ≤1,048,576；缓存像素 + alpha 预算 64 MiB。预算紧张保留上一帧并显示诊断，不代表整个进程内存上限 |
| 食物运动 | `PetSession.VirtualFood` 与物理同一个 Tick 推进；暂停时不推进。VirtualFoodSystem 只提供目标与消费，不写位置；PetEngine 遵守实例走动设置并按角色速度接近，不能直接在宿主另跑食物 Tick |
| 行为 | 行走上限统一 2000 px/s；高无聊值有界缩短 Idle，不绕过走动开关。全局缩放与实例覆盖的合成结果仍包含角色倍率和 DPI，超过最终像素预算会限制呈现大小 |
| 动画播放 | `character.json` 的 `animation.idleFrames` / `walkFrames` 覆盖默认序列；有 `walkStridePixels` 时按实际位移推进，否则按时间；`rendering.sampling` 控制最近邻 / 双三次。状态切换重置相位，Apex 至少 0.08 s |
| 设置 | `%LocalAppData%/YimyWalk/settings.json` + `.bak` / `.tmp`；只用合法主文件轮换备份。全局设置与气泡总闸持久化，实例覆盖 / 位置 / 数量不持久化 |
| 外部与图标边界 | 外部反馈默认关闭，读取前台进程 ID，沿用 program_opened 台词组，实际语义是前台切换。食物是程序圆点，不读取 / 移动真实图标。气泡 / 食物整窗输入透明，也从宿主落脚与遮挡集合排除 |
| 气泡外观 | App 的 BubbleRenderer 对嵌入 PNG 做九宫格缩放，台词独立按 DPI 渲染；BubbleWindow 拥有并释放位图，移动复用，按所在屏工作区定位。统一默认皮肤，未增加角色主题字段；原角色图与正式台词未改 |
| PNG 与角色 | Core 的 PngImage 只开放受限 PNG，采用 StbImageNext 1.0.0；ImageSharp 已从依赖图移除。三套随包角色仍为 1254×1254 / 单格 418×418，素材未改；cat 只作测试 fixture |
| 版本与构建 | 版本真源 Directory.Build.props = 0.1.14；构建、publish 与验包脚本从同一文件读取默认值。.sln 为 Core / App / Core.Tests / App.Tests / CharacterValidator 五个项目 |
| 测试隔离 | 完整宿主 smoke 均从构造函数注入临时设置目录，日志也随之隔离；ManagerUiSmoke 不再反射替换已构造的 store。Windows App 测试有独立项目，CI 在 Ubuntu 跑 Core、Windows 跑 Core + App 并编译 smoke，不执行桌面验收 |

## 当前验证摘要

本轮实跑范围如下，完整命令和最终输出在 STATUS；修改代码后不能直接继承这些结论。

| 档位 | 结果 |
| --- | --- |
| ① 代码已实现 | R01–R13 修正与咿米气泡皮肤 / 透明窗口接线；原始需求与历史设计不改 |
| ② 构建通过 | 本地 SDK 8.0.426；本次 Release 构建 0 警告 / 0 错误；0.3.0 自包含发布与验包通过（`PACKAGE PASS VERSION=0.3.0 FILES=15 CHECKSUMS=14`），位于 artifacts/publish/yimywalk-win-x64 |
| ③ Core 测试通过 | 本次 Core 184 / App 24 均通过，0 失败、0 跳过；含动画序列 / 采样 / 状态相位回归 |
| ④ Windows 桌面 | 单屏 2560×1600、144 DPI / 150%：气泡透明角/正文/边界/不抢焦点/跨进程点击穿透、完整 input smoke，以及动画审计（两角色行走序列 3,4,5,4,3、CHARGE=6/JUMP=6,7,8/LAND=0/EAT=0、FOOT_CLIP=NONE）均有记录。以上为 2026-11-01 既有记录；多屏、负坐标、拔插、睡眠唤醒与长期压力仍未验证 |

**Windows 桌面未验证**：至少两块扩展显示器的矩阵、真实负坐标布局、物理拔插、睡眠唤醒、完整 OS DPI 组合和长时间资源趋势。UI 渲染的 96/120/144/192 DPI 像素计算不是操作系统 DPI 切换验收。短测试清空的 GDI 回落不等同于长期无泄漏。

## 下一次工作的入口

咿米气泡与逐角色动画播放配置已实现并验证上述当前单屏范围，接收用户后续扩展要求；涉及风格先查 [视觉说明](YIMY_VISUAL_STYLE.md)。不要按历史评审重复修复已经关闭的 R01–R13。硬件相关验收在具备对应条件时继续记录，不能用模拟显示器或文档把 UNVERIFIED 改成 PASS。

保留 HardLimit 公开 API 与 WarningLimit 设置字段的兼容性；它们未在生产启用，不自动构成获准删除的理由。后续结构拆分由实际改动需要决定，不照抄旧评审的拆类数量目标。

## 记忆维护方法

用户于 2026-11-01 明确要求提交修改，并允许直接推送 GitHub。按任务提交本轮范围，沿用现有维护分支与远端，正常推送并核对同步结果；保留其他工作区改动，不强制推送。不要把本地发布包、SDK、日志或临时探针纳入 Git。

R01–R13 实现已提交为 `c447b93`，咿米气泡实现为 `2a2f3d8`，逐角色动画配置进入 0.3.0 发布。推送阻塞已定位并解决：全局 `credential.https://github.com.helper` 指向另一个项目临时目录里的 `gh.exe`，其 OAuth 令牌缺 `workflow` scope；执行 `gh auth refresh -h github.com -s workflow` 后推送恢复，不是 Git 作者身份问题。

1. 从 AGENTS 进入，用 INDEX 顶部导航定位；先读本页与 STATUS 最新相关条目，按任务读原始需求。
2. 本页维护当前结论；完整命令、失败与修复过程写带日期的 STATUS / 审查记录。旧证据保持日期和范围。
3. 改行为后更新 STATUS 四档状态，再更新本页；新增结构 / 入口同步 INDEX，格式契约变化同步作者模板。
4. 推测不能升格为事实，未执行不能写成通过。中文 Markdown 用 `Get-Content -Encoding UTF8`；含中文的 Windows PowerShell 脚本保留 UTF-8 BOM。
5. 中断前保存已改文件、实跑命令、未完成任务和四档状态；后续会话由仓库文件恢复，不依赖聊天记忆或后台承诺。
