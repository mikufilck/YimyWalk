# Unreleased

## 管理界面

- 五页管理窗口、标题栏和托盘菜单统一粉白配色与圆角控件。
- 欢迎语简化为“欢迎回来！”，保留粉色欢迎区、白色操作区与狗爪 / 骨头点缀；删除重复口号、状态卡片和小标题。
- 首页将批量创建合并为数量选择与创建按钮，旁边展示默认角色原帧。
- 互动与设置改成设置列表；桌宠、角色页保留列表与详情两栏，删除重复数值说明和恒定的“可用”列。
- 修复首次进入页面时内容已经收缩、滚动条仍未消失的问题。
- 保存失败时在任意页面底部提示，可点击进入设置重试；正常保存不显示提示条。
- 保留创建、喂食、单只设置、自动保存、最小化 / 最大化 / 还原和关闭回托盘；角色 PNG、动画与 Core 未改。
- 设计与验证入口见 `docs/UI_DESIGN.md`。预览包不替换既有 0.3.1 正式 Release。

## 行为默认区间

- **待机更长**：`behavior.idleSecondsMin` / `idleSecondsMax` 的缺省值从 2–8 秒改为 **5–35 秒**，停下后不会频繁起步。
- **行走更长**：`behavior.walkSecondsMin` / `walkSecondsMax` 的缺省值从 3–10 秒改为 **3–20 秒**。
- 两个区间仍可被角色 `character.json` 的显式值覆盖；随包角色未写 `behavior`，直接使用新默认值。

# YimyWalk v0.3.1

## 默认体验

- **待机动作会切换**：两个随包咿米角色改为 `idleFps: 1`、`idleFrames: [0,1,2]`，停下时每秒换一个待机姿势；其他角色保持自己的配置。
- **窗口落脚默认开启**：新安装的 `windowLandingEnabled` 默认 `true`；现有设置文件里的值仍然优先。
- **默认角色为咿米**：首次启动的 `selectedRoleId` 默认为 `咿米_Yimy`，缺失时回退到 `slime` 或首个可用角色。
- **启动自动生成一只**：正常启动会创建一只当前默认角色的宠物；测试/工具使用隔离设置目录时不会自动生成，以免影响实例计数。

## 行为节奏修正

- **走路不再无限持续**：自主行走现在有持续时间（`behavior.walkSecondsMin` / `walkSecondsMax`，默认 3–10 秒），到时自动回到发呆；跟随虚拟食物不受该时长限制。此前 `Walk` 状态没有时长出口，起步后会一直走。
- **待机时长改为随机一次**：进入发呆时在 `idleSecondsMin`–`idleSecondsMax` 间取一次等待时长，而不是到达下限后每帧重掷 75% 概率（旧逻辑实际约 2 秒就出发）。
- **窗口落脚筛选修正**：被排除的系统壳层窗口/覆盖窗口不再充当遮挡体，避免它们把普通窗口全部标成“被遮挡”而导致无法落脚。
- **虚拟食物寿命**：未被认领的目标仍为 8 秒；已认领目标延长到 60 秒，并在认领/释放时重置计时，避免宠物还在路上食物就消失。

## 界面文案

- 管理器的五个页面统一用「桌宠」「设置」「默认值」等一致说法，去掉“实例”“运行级限制”“有效定义”等技术术语。
- 删去与当前操作无关的解释（如创建队列与清空顺序、全局保存状态与单只覆盖的关系），保留每项设置必要的说明与数值范围。
- 单只桌宠的状态列改为中文（发呆/走动/跳跃等），设置保存状态统一显示为“已保存 / 保存中 / 保存失败”。
- 托盘菜单改为“打开管理器 / 清空桌宠 / 隐藏桌宠 / 显示桌宠”。

验证：Release 构建 0 警告 / 0 错误；Core 187 + App 24 测试通过；`desktop-smoke -Landing` 在本机真实桌面返回 `WINDOW LANDING TOP=300 PET=300 PASS`。界面文案改动已在编译与 UI smoke 层面验证，未逐页做真机截图对照。

# YimyWalk v0.3.0

## 逐角色动画播放配置

- `character.json` 新增 `animation.idleFrames` / `animation.walkFrames`：在 0–2 与 3–5 范围内自定义播放顺序（1–64 项）。空数组、越界索引、浮点数或字符串会让角色加载失败并给出明确错误，不静默回退。
- 新增 `animation.walkStridePixels`：行走动画按实际位移推进，跨屏 wrap 只计真实位移；缺省为 `null`，旧角色继续按时间播放。
- 新增 `rendering.sampling`：`nearest`（像素画，默认）或 `smooth`（双三次，适合手绘素材）。桌面渲染与管理器预览使用同一设置。
- 状态切换会重置动画相位，继续行走不会每帧重启；最高点姿态至少保持 0.08 秒；蓄力阶段改用起跳帧 6。
- 两个随包咿米角色新增 `character.json`（`idleFrames [0]`、`walkFrames [3,4,5,4]`、`walkStridePixels 100`、`sampling smooth`、`anchor.y 1.0`），`sprite.png` 原始字节未改。
- 新增 Core `AnimationRegressionTests`、App `SpriteSamplingTests`，以及 `ManagerUiSmoke --animation-audit` 逐帧审计入口。

## 咿米语言气泡

- 默认语言气泡重绘为咿米粉白风格：双层圆框、犬耳与肉垫、条纹蝴蝶结、金铃铛和浅蓝点缀。
- 逐像素透明皮肤采用九宫格伸缩，文字独立绘制，支持多行和按 DPI 缩放；极长文字限制在所在屏工作区内。
- 保留不抢焦点与整窗点击穿透；修正屏幕边缘定位对负坐标工作区的处理。
- 素材来源与生成提示词见 `docs/YIMY_VISUAL_STYLE.md`；桌面验收范围见 `docs/STATUS.md`。

## 0.1.14 — 稳定性修正

- 修正设置恢复后的备份保全、同 ID 定义租约释放、角色扫描失败时的资源清理。
- 食物目标交由统一物理引擎推进，遵守暂停和单实例走动设置；食物窗口与气泡不参与宿主落脚面。
- 渲染加入像素与内存预算、在用 surface 引用保护、GDI 分步清理与失败诊断。
- 使用单调时钟、有限日志和独立 smoke 设置目录；补充 Windows 适配层测试。
- 移除存在审计告警的 ImageSharp 依赖，采用 PNG 专用边界与 StbImageNext；角色格式与原始素材保持兼容。

验证证据与桌面覆盖范围见 `docs/STATUS.md` 最新条目。

## Manager heading spacing

- Fixed page headings so the large title and subtitle use the title's measured font height instead of a fixed vertical offset. This prevents the subtitle from overlapping the title at the normal 120 DPI layout.

## Starter roles

- Bundle the supplied color and line-art Yimy sprite sheets as `Characters/咿米_Yimy/sprite.png` and `Characters/咿米_Line/sprite.png` alongside `slime`.
- Both supplied files are transparent 1254×1254 PNGs despite their `.jpg` source filenames; the packaged sprites retain their original bytes.

## UI Logo

- Added the supplied YimyWalk logo to the lower-left of the manager sidebar.
- The transparent PNG is embedded in the application, rendered with proportional `Zoom` scaling, and disposed with the manager form.
- Added a ManagerUiSmoke regression check for the embedded image and its visible footer bounds.
- Removed the tray instruction text from the footer while keeping the logo anchored at the lower-left.

## YimyWalk v0.1.11

- Fixed 100% visible-window jump attempts falling back to a low ordinary jump when the target was above the previous 1.75x ceiling.
- Window trajectories now use a finite 6x target-only vertical ceiling, retain scaled landing/speed checks, exclude the current supporting window, and use the same ballistic integration as prediction.
- Ordinary attention-seeking jumps retain the randomized 0.15–0.45 second charge and 1.75x cap.

## Per-instance pet size

- The Pets page now lets each running instance follow the global scale or use a custom 0.1–8.0× size.
- Custom instance size is runtime-only. Changing the global scale affects only instances that follow it; clearing an override restores inheritance, and restarting restores the global default.
- Added Core and ManagerUiSmoke regression coverage for mixed-role instances, UI controls, inheritance, and reset behavior.
- Fixed the Pets detail layout so the instance-size source selector and numeric input remain visible after selecting a pet at the normal 125% DPI window size.

# YimyWalk v0.1.5

## Included

- Dragged pets now wrap horizontally across the global display span while airborne. A toss that crosses the right edge re-enters from the left, and the reverse direction behaves the same while preserving flight velocity and vertical motion.
- Surface selection uses the wrapped projected position so a cross-display toss can complete its landing on the destination display floor.
- Core regression tests cover both wrap directions; the targeted Windows smoke validates the behavior against the current two-display topology.

## Requirements and boundaries

- Windows x64. The release executable embeds the .NET 8 runtime.
- No network access, startup registration, administrator privileges, or real desktop-file operations.
- Negative-coordinate monitors, physical monitor hot-plug, sleep/wake, restricted-permission sessions, and long-duration resource trends remain unverified.

# YimyWalk v0.1.4

## Included

- Offline Windows x64 desktop pet application.
- Data-driven 3×3 PNG roles; `cat` and `slime` are included.
- Dragging, throwing, movement, falling, optional window-top landing, hunger, dialogue bubbles, virtual food, and opt-in foreground feedback.
- Startup diagnostics for missing runtimes, incomplete packages, and invalid role directories.
- Application icon derived from `icon_yimy.png`, normalized to 512×512 and packaged as a multi-size Windows icon. The same embedded icon is used by the executable, manager taskbar window, and notification-area tray icon.
- Pets can walk across horizontally arranged displays and wrap from the global virtual-desktop edge to the opposite edge; maximized windows are excluded from window-top landing.
- Release entry is the self-contained single-file `YimyWalk.exe`; double-clicking it starts the app without a separate .NET runtime installation.

## Requirements and boundaries

- Windows x64. The release executable embeds the .NET 8 runtime.
- No network access, startup registration, administrator privileges, or real desktop-file operations.
- The package is self-contained and offline; no separate runtime installation is required.
- Negative-coordinate monitors, physical monitor hot-plug, sleep/wake, restricted-permission sessions, and long-duration resource trends remain unverified.

# YimyWalk v0.1.8

## Included

- AntdUI 2.4.12 unified the WinForms management shell across overview, pets, characters, interactions, and settings.
- Fixed card measurement and page-internal clipping; added content-driven spacing, scrolling, idle1 previews, modern Switch/Select/InputNumber controls, and a default-role plus creation-count overview action.
- Preserved the tray-first lifecycle, stable instance deletion, 200 ms visible manager sampling, global/instance setting semantics, and offline self-contained Windows x64 packaging.

## Verification boundary

- Core tests, ManagerUiSmoke layout assertions, application builds, and the Windows input smoke passed in the current Windows session (DPI 120 / 125%).
- 100%, 150%, 200% DPI switching, minimum-size/maximize matrix, and desktop visual acceptance remain unverified.
