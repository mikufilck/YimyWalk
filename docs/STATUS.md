# 2026-11-03 v0.3.2 发布预检

用户要求构建并发布 0.3.2。基线 `d165970`；版本真源已升为 0.3.2，Unreleased 内容归入正式版本说明，README 的下载入口改为当前仓库。远端尚无 v0.3.2 标签或 Release，工作区原先干净；GitHub CLI 可访问当前仓库，上一提交 GitHub Actions 为 success。

**① 代码已实现**：发布包含此前 UI 重做、共享组件、单只完整重置与角色刷新选择修复，以及待机 5–35 秒 / 行走 3–20 秒默认区间。产品逻辑本轮不改；DesktopSmoke 的跨屏反射调用补齐 StartWalking 的可选时长参数，跨屏分支仅编译未实跑。

**② 构建通过**：`scripts/verify-environment.ps1`、0.3.2 预检构建与验包通过：`PACKAGE PASS VERSION=0.3.2 FILES=15 CHECKSUMS=14`。首次预检包 InformationalVersion 指向基线提交，不能将它当作最终标签产物。待提交本次发布准备后，从干净的发布提交重建，再核对 EXE 文件版本 / 提交号、ZIP 和远端附件。

**③ Core 测试通过**：`scripts/test-yimywalk.ps1` 和预检发布流程实跑 Core **187** / App **24**，0 失败、0 跳过。

**④ Windows 桌面已验证（当前单屏范围）**：0.3.2 完整 ManagerUiSmoke、五页默认 / 最小窗口与独立 desktop-input-smoke 均通过；后者 `RESULT PASS`。DesktopSmoke 首次 `-Landing` 失败：pet=285、probe=300；诊断运行发现同一区域有顶边 285 的其他窗口，随后原测试曾通过。原夹具从屏幕顶部落下，依赖其他窗口的层级 / 遮挡状态。已改为先核对探针自身的原生顶边、可见且未遮挡，再从其正上方 0.5 物理像素起落；不放宽精确落点及关闭后再次下落断言。修正后 `WINDOW LANDING TOP=300 PET=300 PASS`，再次下落通过；1/10/50/100 创建清空、缓存释放和角色重载通过。本项修正隔离测试环境，不改产品落脚规则。

**Windows 桌面未验证**：多屏、真实跨屏 DPI、OS 缩放切换、拔插 / 睡眠和长期压力。单次 100 只不是流畅性或长期资源承诺。

证据在 `artifacts/release-0.3.2/`：tests.log、publish.log、package.log、full-ui.log、visual-review.log、input-smoke.log、desktop-smoke.log；首次失败与诊断分别保留在 desktop-smoke-first-failure.log 和 desktop-smoke-diagnostic.log。本条为发布准备记录，推送标签 / Release / 附件校验结果在完成后追加。

# 2026-11-03 UI 功能组件审查、共享控件与页面重构

**基线与范围**：`9d057ba`，工作区开始时干净。用户要求检查按钮及所属功能组件是否需要重构，并统一可复用 UI。审查快照与复现入口见 [UI-01–05](reviews/2026-11-03-ui-components.md)，设计与页面图见 [UI_DESIGN](UI_DESIGN.md)。

**① 代码已实现**：修复“恢复默认”遗漏单只缩放，新增 AppHost.ResetPetOverrides 组合现有行为 / 缩放重置，保留原局部 API；操作后立即重新采样，不用旧投影更新新设置。角色刷新按 ID 保留当前预览与首行，列表同步显示名和默认标记。五组数值改用 NumberOverrideEditor，统一全局继承、百分比换算、静默绑定与焦点 / 目标切换；SettingRows 供单只与全局共用，UiLayout 供内容栈 / 操作行 / 列表详情分栏共用，ThemedListView 统一列配置，UiTheme 统一按钮图标 / 可访问名称 / 移除样式。

桌宠详情重排为横向身份区、参数行、固定底部操作栏；无选择隐藏旧设置，喂食仅在发呆 / 走动时可用并显示操作结果，恢复默认只在有覆盖时可用，移除使用独立样式。角色刷新移至页头，预览下保留设默认。首页清空操作采用同一移除样式。保留简短欢迎语、粉白圆角布局和角色原帧；Core、PNG、动画、设置 schema 和 Win32 声明未改，无新增依赖或 AI 绘画。

**② 构建通过**：ManagerUiSmoke 与 DesktopInputSmoke 构建 0 警告 / 0 错误。`scripts/build-yimywalk.ps1 -PublishPath artifacts/ui-components-20261103/publish` 和 `scripts/test-release-package.ps1 -Path artifacts/ui-components-20261103/publish` 实跑通过，`PACKAGE PASS VERSION=0.3.1 FILES=15 CHECKSUMS=14`。本地 ZIP：`artifacts/ui-components-20261103/YimyWalk-0.3.1-ui-components-win-x64.zip`，SHA-256 `377564054A7250D3AA17AF4022E346D64686A8E130427230AD8A8380BB855282`。保持 0.3.1 预览，正式 Release / 标签未改。

**③ Core 测试通过**：`scripts/test-yimywalk.ps1` 与发布流程实跑 Core **187** / App **24**，0 失败、0 跳过。未为纯样式添加镜像测试；新增有意义的 UI 回归覆盖实际修复行为。

**④ Windows 桌面已验证（本次范围）**：单屏 2560×1600、144 DPI / 150%。完整 ManagerUiSmoke 输出 `INSTANCE_OVERRIDE_UI inheritance_target_switch_all_reset=PASS`、`ROLE_ACTIONS refresh_preview_selection_set_default=PASS` 与 `SAVE_FAILURE notice_navigation_retry=PASS`；覆盖编辑缩放 / 活跃度 / 三种倾向 / 两个行为开关、切换目标不串写、实际重置按钮同时恢复宿主与控件、空选择、连续移除、刷新非默认预览再设默认。`--visual-review --output artifacts/ui-components-20261103` 五页默认 1680×1140 / 最小 1440×960 布局与固定操作栏检查通过，已查看桌宠 / 角色页实图。独立 `scripts/desktop-input-smoke.ps1` 输出 `MANAGER FRAME_REAL_DRAG_RESIZE=PASS`、`MANAGER REFRESH_BUTTON=PASS`、`RESULT PASS`，真实鼠标窗口操作、开关、食物、拖拽、穿透 / 焦点、托盘及退出通过。

**Windows 桌面未验证**：多屏、真实跨屏 DPI、OS 缩放切换、拔插 / 睡眠和长时压力。UI 布局与截图不替代这些验收。

证据：`artifacts/ui-components-20261103/` 的 `build.log`、`tests.log`、`full-ui.log`、`visual-review.log`、`input-smoke.log`、`publish.log`、`package.log` 和五页 PNG。测试使用隔离角色 / 设置目录。已同步当前记忆、索引、设计、测试入口与页面截图；按既有授权提交并正常推送维护分支，发布包及日志不纳入 Git。

# 2026-11-03 恢复简短欢迎语与可爱布局

**基线与原因**：接续 `bbc754b`。用户纠正了上一轮过度精简：要减少啰嗦，不是删掉所有欢迎语或可爱设计。已更正 MAINTENANCE 与 UI_DESIGN 中过于绝对的文案规则。

**① 代码已实现**：首页恢复一句“欢迎回来！”，粉色圆角欢迎区左侧放默认角色、实际计数与创建操作，右侧展示原始角色帧；下方白色圆角区整理常用操作。五页标题加入小狗爪，喂食 / 食物按钮恢复骨头图标，欢迎区增加细虚线边；侧栏使用“咿米的桌面”。功能说明继续简短，未恢复重复状态卡片和多段口号。全部为原生 UI 绘制和既有角色原帧，角色 PNG / Core / 设置字段未改，未调用 AI 绘画。

**② 构建通过**：ManagerUiSmoke 构建 0 警告 / 0 错误。`scripts/build-yimywalk.ps1 -PublishPath artifacts/ui-balance-20261103/publish` 与 `scripts/test-release-package.ps1 -Path artifacts/ui-balance-20261103/publish` 实跑通过，`PACKAGE PASS VERSION=0.3.1 FILES=15 CHECKSUMS=14`。本地 ZIP：`artifacts/ui-balance-20261103/YimyWalk-0.3.1-ui-balanced-win-x64.zip`，SHA-256 `2B4F43B97C457114FEB1EFC34E5C639889A1100F6E9DFFF4BB8C4E5CD0B00AD7`。仍为 0.3.1 预览，正式 Release 与标签未改。

**③ Core 测试通过**：`scripts/test-yimywalk.ps1` 与发布流程实跑 Core **187** / App **24**，0 失败、0 跳过。

**④ Windows 桌面已验证（本次范围）**：单屏 2560×1600、144 DPI / 150%。`ManagerUiSmoke --visual-review --output artifacts/ui-balance-20261103` 检查五页默认客户区 1680×1140 / 最小窗口 1440×960、标题栏与托盘还原，均通过；已查看首页和互动页实图。完整 ManagerUiSmoke 验证数量创建、单只参数、重置、删除与保存失败提示 / 跳转 / 重试通过。独立 `scripts/desktop-input-smoke.ps1` 输出 `FRAME_REAL_DRAG_RESIZE=PASS`、`RESULT PASS`，真实鼠标拖动 / 缩放 / 关闭回托盘，以及开关、食物、拖拽、透明路由、焦点与退出通过。

**Windows 桌面未验证**：多屏、真实跨屏 DPI、OS 缩放切换、拔插 / 睡眠与长时压力。没有用截图替代这些验收。

产物在 `artifacts/ui-balance-20261103/`：五页 PNG、`build.log`、`tests.log`、`visual-review.log`、`full-ui.log`、`input-smoke.log`、`publish.log`、`package.log`。文档首页图已更新；旧预览目录仅作对照。按既有授权提交并正常推送 `codex/bootstrap`，不提交预览包或测试临时数据。

# 2026-11-03 管理器文案与布局精简

**基线与范围**：接续 `6374a17`。用户反馈文案刻意、卡片与小标题过多，要求使用自然或直接的功能描述。本轮保留粉白配色和实际操作，删除装饰文案与重复信息。当前约定见 [UI_DESIGN](UI_DESIGN.md)，更新后的首页见 [ui-overview.png](assets/ui-overview.png)。

**① 代码已实现**：删除欢迎区、三张状态卡片、重复创建快捷按钮和设置页的装饰卡片 / 小标题；总览改为默认角色原帧、数量选择与创建按钮，常用操作直接排列。互动与设置使用设置行；桌宠与角色保留列表 / 详情分栏，移除重复参数数值说明、恒定“可用”列与列表标题。统一“桌宠缩放”“跳跃 / 攀爬倾向”，保留保存错误、备份恢复、重试、全局气泡开关影响与单只设置重启后恢复的说明。删除无调用的 CompanionArt、装饰绘制与图片嵌入项；原始素材不变。

**布局修正**：新页面在取得最终宽度前先按窄宽度测量，内容收缩后 ScrollableControl 仍保留旧滚动范围，导致设置 / 互动页出现空滚动条。SelectPage 完成布局后显式重算页面布局，已检查默认与最小窗口；新增“内容放得下时不显示滚动条”断言。首页新增数量 10 的真实队列创建检查，预览断言核对默认角色与原始第 0 帧。旧测试里要求互动页一定有滚动范围的断言改为检查可滚动能力与实际内容可达性。

保存失败改为所有页面共用的条件提示，点击进入设置页查看错误并重试，成功后隐藏。完整 UI smoke 在自己的临时目录制造真实写入失败，核对提示可见、跳转、重试恢复，输出 `SAVE_FAILURE notice_navigation_retry=PASS`；没有通过隐藏错误来精简界面。

**② 构建通过**：ManagerUiSmoke、DesktopInputSmoke 与发布构建 0 警告 / 0 错误。实跑 `scripts/build-yimywalk.ps1 -PublishPath artifacts/ui-simplify-20261103/publish` 与 `scripts/test-release-package.ps1 -Path artifacts/ui-simplify-20261103/publish`，输出 `PACKAGE PASS VERSION=0.3.1 FILES=15 CHECKSUMS=14`。本地 ZIP：`artifacts/ui-simplify-20261103/YimyWalk-0.3.1-ui-simplified-win-x64.zip`；SHA-256 `9942BF2E3A3F18641A8A816924B406517EEF5E1254212AAC747BBCCE46208059`。版本仍为 0.3.1，正式标签与 Release 未替换。

**③ Core 测试通过**：`scripts/test-yimywalk.ps1` 与发布流程实跑 Core **187** / App **24**，0 失败、0 跳过。Core、角色 PNG、角色动画与设置 schema 均未改。

**④ Windows 桌面已验证（本次范围）**：当前单屏 2560×1600、工作区 2560×1528、144 DPI / 150%。`ManagerUiSmoke --baseline`、完整模式、`--preview-pixels`、`--visual-review --output artifacts/ui-simplify-20261103` 通过；默认客户区 1680×1140 与最小窗口 1440×960 的五页布局检查通过，已逐页查看应用自身渲染图。数量创建、单只参数覆盖 / 重置、刷新、删除与清空检查通过。独立 `scripts/desktop-input-smoke.ps1` 返回 `RESULT PASS`：真实鼠标拖动 / 缩放 `FRAME_REAL_DRAG_RESIZE=PASS`，关闭 / 重开、角色刷新、开关、喂食 / 食物、拖拽与透明路由、跨进程气泡 / 食物穿透和焦点、托盘与退出通过。

**Windows 桌面未验证**：多屏、真实跨屏 DPI、OS 缩放切换、拔插 / 睡眠与长时压力。四档预览像素计算与应用截图不能代替这些验收。

产物：`artifacts/ui-simplify-20261103/` 下的 `build.log`、`tests.log`、`layout.log`、`full-ui.log`、`pixels.log`、`visual-review.log`、`input-smoke.log`、`publish.log`、`package.log` 与五页 PNG。测试使用隔离设置目录，不写真实设置。文案偏好已记入 MAINTENANCE；索引与架构说明已同步，架构文档旧的“窗口落脚默认关闭”更正为昨天已实现的默认开启。Git 作者仍为本地 `mikufilck <xuw05302@gmail.com>`；按既有授权提交并正常推送维护分支，实际同步状态以 Git 为准。

# 2026-11-03 咿米小窝 UI 重做与预览包

**基线与范围**：接续 `491aebc` 与昨天的 0.3.1 收尾，不回滚默认角色 / 启动生成 / 待机与行走节奏。用户要求优先美观、可爱和角色风格，并补充狗爪、骨头等参考元素。本轮重做 App 管理器与托盘 UI；未调用绘画工具，未改角色 PNG、图标源文件、动画、Core 或设置 schema。设计与组件入口见 [UI_DESIGN](UI_DESIGN.md)，首页实际渲染图见 [ui-overview.png](assets/ui-overview.png)。

**① 代码已实现**：粉白 / 莓灰 / 浅蓝主题、代码内狗爪 / 骨头 / 荷叶边、既有原画欢迎区、三项真实状态、快捷操作、柔和列表与大预览、互动双栏分组与保存状态。自定义标题栏保留拖动、缩放、最小化 / 最大化 / 还原和关闭回托盘；Win32 声明仍集中在 NativeMethods。页面缓存 / 200 ms 可见刷新保留，图库默认角色置顶，显示名更友好而 ID 不变。原生 Padding 与按内容收缩布局修正旧卡片边距和窗口变宽后残留的空白；AntdUI 按钮的绘制内缩与实际点击区保持一致。原画和原图标只复用，没有 AI 角色素材或新第三方依赖。

**② 构建通过**：解决方案、ManagerUiSmoke、DesktopInputSmoke 的最终构建为 0 警告 / 0 错误。`build-yimywalk.ps1 -PublishPath artifacts/ui-redesign-20261103/publish` 与 `test-release-package.ps1` 实跑通过：`PACKAGE PASS VERSION=0.3.1 FILES=15 CHECKSUMS=14`。版本保持 0.3.1，仅生成本地 UI 预览包，未改正式 tag / Release 或既有附件。ZIP：`artifacts/ui-redesign-20261103/YimyWalk-0.3.1-ui-preview-win-x64.zip`；SHA-256 `1D0DBBA1E5B285B7A1B25279496CBA9F1D3919741FCE75035CC4483CBB9B0DEF`。

**③ Core 测试通过**：`scripts/test-yimywalk.ps1` 与发布流程实跑 **Core 187 / App 24，通过，0 失败、0 跳过**。既有角色 hash 和架构约束仍通过；本轮没有为纯配色写镜像测试。

**④ Windows 桌面已验证（本次范围）**：当前单屏 2560×1600，工作区 2560×1528，144 DPI / 150%。`ManagerUiSmoke --baseline`、完整模式、`--preview-pixels`、`--visual-review` 通过；五页默认客户区 1680×1140 与最小窗口 1440×960 的文字、布局和操作组件检查通过；自定义标题栏最大化不盖任务栏，最小化 / 还原 / 关闭回托盘与边缘命中通过。`desktop-input-smoke.ps1` 最终 `RESULT PASS`，新增真实鼠标标题栏拖动 / 右下角缩放 / 关闭按钮验收（`FRAME_REAL_DRAG_RESIZE=PASS`）；角色选择与刷新、开关、喂食 / 食物、宠物透明 / 实体路由、拖拽、跨进程气泡 / 食物穿透与焦点、托盘、清空和退出均通过。

验证过程中发现既有 smoke 仍引用旧“运行级限制”等文案、旧托盘项和“生成虚拟食物”，并遗漏昨天 `StartWalking` 新增的可选时长参数；已将断言 / 反射调用同步到实际接口，未改产品动作来迎合脚本。完整 UI 模式另检查混合角色的单只参数、实时刷新与删除后的选择保持。

**Windows 桌面未验证**：真实双屏 / 跨屏 DPI、系统缩放切换、拔插、睡眠和长期压力。四档预览像素计算不是 OS DPI 验收；应用自身 DrawToBitmap 的五页效果图也不代替上述独立真实输入验收。

证据与预览位于 `artifacts/ui-redesign-20261103/`：`build.log`、`tests.log`、`layout.log`、`full-ui.log`、`pixels.log`、`visual-review.log`、`input-smoke.log`、`publish.log`、`package.log` 与五页 PNG。测试使用临时角色 / 设置目录，真实创建两只咿米后取计数，退出清理；无用户桌面截图、真实设置写入或原画改绘。源码与文档按已有授权提交维护分支，发布产物与临时日志不进入 Git。

# 2026-11-02 行为默认区间调整：待机 5–35 秒、行走 3–20 秒

**变更**：

- `CharacterLoader.ReadConfig` 的兜底 `CharacterDefaults` 从 `(2, 8, 45, 0.01, 3, 10)` 改为 `(5, 35, 45, 0.01, 3, 20)`；没有 `behavior` 段的角色（含两个随包咿米角色，slime 无 `character.json`）直接使用新默认值。
- `CharacterFormat.CharacterDefaults` 的 `WalkSecondsMax` 默认值 `10f` → `20f`。
- 角色格式契约、字段名与校验边界未变，`character.json` 里的显式值仍然优先。
- `CharacterLoaderTests.SpriteOnlyDirectoryUsesSafeDefaults` 增加四项默认区间断言，防止默认值静默漂移。
- `MaintenanceRegressionTests.BoredomShortensIdleWaitWithoutBypassingWalkDisable` 改为使用显式 `character.json`（待机 6 秒、行走 10 秒），不再依赖随包默认区间；无聊（boredom=1）仍在 3 秒后起步，未无聊的保持 6 秒。

**验证**：

- ② 构建通过：`dotnet build YimyWalk.sln -c Release` → **0 警告 / 0 错误**。
- ③ 测试通过：Core **187/187**、App **24/24**。
- ④ Windows 桌面：本轮**未在真机观察**新节奏的实际观感（待机 5–35 秒、行走 3–20 秒）；按用户要求**未重建发布包**。

**发布边界**：只推送源码与文档，`Directory.Build.props` 仍为 0.3.1，未创建新 tag / Release；这些默认值要等下一次打包才会进入发布二进制。

# 2026-11-02 界面文案整理（0.3.1）

**变更**：

- 新增 `src/YimyWalk.App/UI/UiText.cs`：`PetStateLabel` / `SaveStateLabel` 把
`PetState`、`SettingsSaveState` 转成中文，界面不再直接显示枚举名。
- 五个页面统一称谓：用「桌宠」「设置」「默认值」，去掉“实例”“运行级限制”“有效
定义”“继承来源”等术语；删除与当前操作无关的关系解释和重复的“开/关”开关文案（沿用
主题的“开启/关闭”）。
- 总览页只在有运行诊断或待创建时显示说明行，“快捷 N”改为“创建 N”，“生成虚拟
食物”改为“投放食物”。
- 桌宠页状态列与详情行改用中文状态；“恢复跟随互动默认值”改为“恢复默认”；删除
实例后的提示缩为“已删除。”。
- 角色页“有效”改为“可用”；角色信息只保留默认角色提示与走动速度。
- 托盘菜单改为“打开管理器 / 清空桌宠 / 隐藏桌宠 / 显示桌宠”。
- `AppHost` 的运行时诊断、食物上限提示、设置加载与保存状态均改为中文完整句子，
不再出现 `Settings: saved` 之类英文状态。

**验证**：

- ② 构建通过：`dotnet build YimyWalk.sln -c Release` → **0 警告 / 0 错误**。
- ③ 测试通过：Core **187/187**、App **24/24**。
- ④ Windows 桌面：本轮**未逐页人工核对**新文案在 96/120/144 DPI 下的换行与
截断，也未重跑 `ManagerUiSmoke` 截图；需启动 0.3.1 后逐页查看。本轮只改字符串与
枚举显示，不改行为。

**版本**：`Directory.Build.props` 由 0.3.2 调整为 **0.3.1**（用户指定的发布号）；
`RELEASE_NOTES.md` 把原 0.3.2 / 0.3.1 两节合并为一节 `v0.3.1`。

**发布**：提交 `150018b` 已推送 `codex/bootstrap`；标签 `v0.3.1` 已推送；GitHub
Release 已上传 `yimywalk-v0.3.1-win-x64.zip`（SHA-256 `F814568B75AEB733D2A153EF
37FA0541D26EDCC361BAB0DDF89C2C73147CD165`，71,217,774 字节）与 `SHA256SUMS.txt`；
远端 asset digest 与本地一致，`isDraft=false`。

# 2026-11-02 默认体验调整：待机循环、窗口落脚默认开启、默认角色与启动生成

**变更**：

- 两个随包咿米角色改为 `idleFps: 1` + `idleFrames: [0,1,2]`，停止时每秒切换一个待机姿势；其他角色保持自己的配置。
- `SettingsDocument.WindowLandingEnabled` 默认值改为 **true**。
- `SettingsDocument.SelectedRoleId` 默认值改为 **`咿米_Yimy`**；`EnsureSelectedRole` 的回退顺序改为 咿米 → slime → 首个可用角色。
- 生产入口 `new AppHost()` 在启动后**自动创建一只默认角色宠物**；`new AppHost(settingsRoot)`（测试/tool 隔离目录）不自动生成，避免影响 smoke 的实例计数。

**验证**：

- ② 构建通过：`dotnet build YimyWalk.sln -c Release` → **0 警告 / 0 错误**。
- ③ 测试通过：Core **187/187**、App **24/24**。
- ④ Windows 桌面：本轮**未人工验收**新默认观感（待机每秒切换、启动自动生成、默认落脚开启）；需启动 0.3.2 后观察。

**注意**：`windowLandingEnabled` 与 `selectedRoleId` 是**已持久化设置**。已有用户的 `%LocalAppData%/YimyWalk/settings.json` 里的旧值仍然优先；本次已把本机设置同步为 `true` / `咿米_Yimy`。

# 2026-11-02 文档清理：移除已完成的历史计划与旧评审

**删除**（均可从 git 历史取回）：

- `docs/superpowers/plans/`（13 篇，M1–M4 实施计划）与 `docs/superpowers/specs/`（6 篇设计稿）——对应功能均已实现，现状由 `STATUS.md` / `ARCHITECTURE.md` / `CHARACTER_FORMAT.md` 承载。
- `docs/TECHNICAL_REVIEW.md`（2026-10-16 快照）——其高风险结论已由 `docs/reviews/2026-11-01-project-review.md` 复核纠正。
- `docs/assets/cat-preview.png`、`docs/assets/slime-preview.png`——历史测试素材预览，不代表当前随包 PNG。

**保留**：`PROJECT_SPEC.md`（需求原文）、`ARCHITECTURE.md`、`CHARACTER_FORMAT.md`、`INDEX.md`、`MAINTENANCE.md`、`STATUS.md`（交接证据）、`TESTING.md`、`UPSTREAM_AUDIT.md`、`YIMY_VISUAL_STYLE.md`、`artwork/yimy-bubble-prompt.txt`、`reviews/2026-11-01-*.md`、`assets/character-grid-reference.png`。

**引用同步**：`AGENTS.md` 记忆分层去掉「历史」行；`INDEX.md` 文档地图去掉 3 条；`CHARACTER_FORMAT.md` 去掉历史预览段；`2026-11-01-project-review.md` 的旧评审说明改为「已清理，可从 git 历史取回」；`MAINTENANCE.md` 记录清理事实。

**边界**：本轮只改文档与删除历史文件，未改产品代码；构建与测试沿用最近一次 0.3.1 结果（0 警告 / 0 错误，Core 187 + App 24）。

# 2026-11-02 行为节奏修正与窗口落脚/食物核验

**根因**（均已核对源码，并用 `git show 7a0fb13` 确认不是 0.3.0 引入）：

1. `PetState.Walk` 没有任何时长出口，自主起步后会无限行走。
2. `Idle` 在达到 `idleSecondsMin` 后每个模拟帧有 75% 概率起步，约 2 秒后几乎必然出发，`idleSecondsMax` 实际失效。
3. 用户设置 `windowLandingEnabled=false`，窗口落脚默认关闭；另发现 `WindowSurfaceResolver` 把 `IsExcluded` 的系统壳层/覆盖窗口仍标记为遮挡体，可能把普通窗口全部判为被遮挡。
4. 虚拟食物对已认领目标也按 8 秒过期，远距离宠物来不及走到。

**修正**：

- 新增 `behavior.walkSecondsMin` / `walkSecondsMax`（Core `CharacterDefaults`，默认 3–10 秒）；`PetInstance.StartWalking` 接受时长，`PetEngine` 到时回到 Idle；食物跟随显式清除时长限制。
- Idle 改为进入状态时在区间内随机一次等待时长，不再逐帧重掷。
- `WindowSurfaceResolver` 的遮挡体判定排除 `unsafeSurface`。
- 食物：`Available` 8 秒、`Claimed` 60 秒；认领/释放时重置计时。

**验证**：

- ② 构建通过：`dotnet build YimyWalk.sln -c Release` → **0 警告 / 0 错误**。
- ③ 测试通过：Core **187/187**（新增 `BehaviorPacingTests` 3 项）、App **24/24**。
- ④ Windows 桌面已验证：`scripts/desktop-smoke.ps1 -Landing -Rounds 1 -Role slime` 退出码 0，输出 `WINDOW LANDING TOP=300 PET=300 PASS`、`WINDOW LANDING CLOSE_REFALL_Y=325.7 PASS`；1/10/50/100 创建与清空后均为 `ACTIVE=0 WINDOWS=0 CACHE=0`。

**未验证边界**：多显示器/DPI 矩阵、负坐标、物理拔插与长期资源趋势仍未执行；虚拟食物的实际手感未做真机主观验收。

**注意**：窗口落脚仍是**设置项**（默认关闭）。当前用户设置为 `false`，需在管理器「设置」页勾选「允许宠物落到可见窗口顶部」才会生效；这不是功能失效。

# 2026-11-01 逐角色动画播放配置与 0.3.0 发布

**代码已实现**：`character.json` 新增 `animation.idleFrames` / `animation.walkFrames` / `animation.walkStridePixels` 与 `rendering.sampling`；`CharacterAnimation` 持有可选帧序列（1–64 项，空数组/越界/非整数直接拒绝），`SpriteSampling` 贯通 Core 数据、App 渲染与预览。设置 `walkStridePixels` 后行走动画按实际位移推进，跨屏 wrap 只计真实位移；状态切换重置动画相位；`Apex` 至少保持 0.08 s；蓄力阶段使用起跳帧 6。两个随包咿米角色新增 `character.json`（`idleFrames [0]`、`walkFrames [3,4,5,4]`、`walkStridePixels 100`、`sampling smooth`、`anchor.y 1.0`），`sprite.png` 原始字节未改。新增 `AnimationRegressionTests`、`SpriteSamplingTests` 与 `ManagerUiSmoke --animation-audit`。

**构建通过**：本机工作区 SDK 8.0.426；`dotnet build YimyWalk.sln -c Release` **0 警告 / 0 错误**（5 个项目）。

**测试通过**：Core **184/184**、App **24/24**，0 失败 / 0 跳过（本轮实跑）。

**Windows 桌面**：沿用本日上一会话留下的审计产物（`artifacts/yimy-animation-20261101/`）：`ANIMATION_AUDIT PASS SINGLE_DISPLAY_ONLY`，单屏 2560×1600 / 144 DPI，两个咿米角色行走序列 `3,4,5,4,3`、`CHARGE=6 JUMP=6/7/8 LAND=0 EAT=0 DRAG_RELEASE=PASS`、`FOOT_CLIP=NONE`；`input-smoke.log` 为 `RESULT PASS`。**本轮没有重跑任何 `desktop-*` smoke**，上述为既有记录而非本轮复验。

**发布**：版本真源 `Directory.Build.props` 提升为 **0.3.0**；`scripts/build-yimywalk.ps1` 生成自包含单文件 x64 包到 `artifacts/publish/yimywalk-win-x64`；`scripts/test-release-package.ps1` 返回 `PACKAGE PASS VERSION=0.3.0 FILES=15 CHECKSUMS=14`；`YimyWalk.exe` FileVersion 为 `0.3.0.0`。

**文档同步**：`docs/CHARACTER_FORMAT.md` 新增「播放序列与采样」章节并更新示例；`examples/character-template` 的 `character.json` 与 README 同步；`docs/INDEX.md` 补登记字段、测试项目、审计入口与当前验证数字；`RELEASE_NOTES.md` 新增 0.3.0 段。

**未验证边界**：多显示器 / 负坐标 / 物理拔插 / 睡眠唤醒 / 完整 DPI 组合 / 长时间资源趋势仍为 `Windows 桌面未验证`；动画审计只覆盖单屏，`smooth` 采样在跨屏 DPI 切换下的呈现未单独验收。

> **最新相关交接优先**：当前短摘要见 [`MAINTENANCE.md`](MAINTENANCE.md)，结构导航见 [`INDEX.md`](INDEX.md)。本文件按日期记录证据；先读顶部，再按功能定位最新相关条目。旧测试结果只适用于当时的代码、环境与范围。

# 2026-11-01 咿米气泡重绘与接入

**需求与依据**：用户确认主要角色为 Yimy（咿米），粉色头发的人形犬娘，要求参考附图的气泡轮廓完全重绘，并授权绘画。读取现有角色图，检索萌娘百科（原站要求 JS，通过可读同名镜像核对），仅以粉白 / 蓝瞳 / 犬耳 / 蝴蝶结 / 铃铛 / 代表色辅助设计；不复制百科图片或正文到产品。内置 image_gen 生成 2048×768 透明 PNG 并原样保存，来源、hash、完整提示词与切图参数见 [YIMY_VISUAL_STYLE](YIMY_VISUAL_STYLE.md)。

**① 代码已实现**：替换浅黄矩形气泡为原创粉白双层圆框、犬耳 / 肉垫、条纹蝴蝶结、金铃铛 / 浅蓝珠。App 的 BubbleRenderer 九宫格拉伸皮肤、独立绘制 DPI 文字并按工作区限宽 / 限高；BubbleWindow 用逐像素 Alpha、持有 / 释放位图，移动复用，DPI / 工作区变化重建。宿主创建时先定位再显示，边缘定位保留负坐标。维持整窗输入透明 / 不激活；Core 对话调度、台词、角色原图和角色格式不改。当前全部角色共用该默认皮肤，未引入角色包主题配置。

**② 构建通过**：`dotnet build YimyWalk.sln -c Release --no-restore` 为 0 警告 / 0 错误。ManagerUiSmoke 与 DesktopInputSmoke 编译通过。`build-yimywalk.ps1 -PublishPath artifacts/yimy-bubble-20261101/publish` 与 `test-release-package.ps1` 实跑通过，`PACKAGE PASS VERSION=0.1.14 FILES=13 CHECKSUMS=12`；版本仍为维护分支 0.1.14 的本地更新包，未发布新的 GitHub Release。

**③ Core 测试通过**：`scripts/test-yimywalk.ps1` 和发布流程均实跑 **Core 164 / App 22，通过，0 失败 / 0 跳过**。App 新增六个用例，覆盖 96/120/144/192 DPI 的透明边缘、正文不透明、固定边角不随正文宽度变形、换行、500 字极长台词限高省略，以及负坐标 / 偏移工作区定位。

**④ Windows 桌面已验证（当前单屏范围）**：`ManagerUiSmoke --bubble-skin` 在当前 2560×1600、144 DPI / 150% 会话显示真实分层气泡，截取工具自建纯色背景内的气泡区域，确认透明角露出背景、白色正文显示、显示前后前台 HWND 不变，顶部 / 右侧边界不超工作区。`desktop-input-smoke.ps1` 返回 `RESULT PASS`：修改探针点击位置为气泡可见正文中心，跨进程点击穿透与焦点保持通过；宠物拖拽、托盘、清空、辅助窗口落脚排除及退出也通过。工具一次初始断言错误地假设 Activate 必然使背景窗获得前台，已改为采样显示前后真实前台句柄比较后重跑通过；没有放宽气泡焦点要求。

本次 `--bubble-skin` 同时输出四档渲染 DPI 样张；它们不是 OS DPI 切换。**Windows 桌面未验证**：真实多显示器 / 负坐标布局、跨屏 DPI 变化、拔插、睡眠与长期资源趋势；未继承旧压力结果来声称新样式长期通过。

本地证据在 `artifacts/yimy-bubble-20261101/`：`tests.log`、`build.log`、`bubble-smoke.log`、`input-smoke.log`、`publish.log`、`package.log`，效果图 `bubble-preview.png`、真实窗口局部截图 `bubble-desktop.png`。便携 ZIP 为同目录 `YimyWalk-0.1.14-yimy-bubble-win-x64.zip`，SHA-256 `90380F9AB18723C85C3DDF3942708EECDB59D8180385A7A4EB74C572311A4B05`。产物 / 日志均排除在 Git 之外。

**Git 交付**：实现与原画已提交为 `2a2f3d8`（作者 `mikufilck`）。按已有授权正常推送 `origin/codex/bootstrap`，再次被 GitHub 拒绝：OAuth 缺少更新历史待推送 CI 文件的 `workflow` scope（`push.log`）。本轮没有改 CI 文件；既有待推送提交仍需该权限。未成功推送，未改写历史或绕过认证。

# 2026-11-01 提交与 GitHub 推送权限核对

用户明确授权提交并直接推送 GitHub，随后确认作者名为 `mikufilck`。本轮实现、测试与维护文档已提交为 `c447b93`（`fix: stabilize v0.1.14 and establish maintenance knowledge`，72 个文件），作者与用户确认一致；本仓库 local Git 身份已配置，沿用该作者现有邮箱，未修改全局身份。

**推送尚未成功**：本地与远端基线核对一致后，正常推送 `origin` 的 `codex/bootstrap` 被 GitHub 拒绝，原因是当前 OAuth 凭据没有更新 `.github/workflows/dotnet.yml` 所需的 `workflow` scope。现有 SSH 认证也不可用；GitHub 连接器对该仓库为只读，无法替代推送。需要用户为 `mikufilck` 重新完成 Git Credential Manager 登录并授权工作流权限，或提供本机已经具备写权限的 SSH 配置；不在聊天中提供令牌。未修改工作流内容以回避权限限制，待认证辅助进程已停止。

**四档验证**：① 代码已实现 —— 0.1.14 修复已提交；② 构建通过 —— 沿用此前 0 警告 / 0 错误与验包结果，本轮只做 Git / 文档操作未重跑；③ Core 测试通过 —— 沿用 Core 164 / App 16，通过范围不变；④ Windows 桌面已验证 —— 沿用当前单屏 150% 的 UI / 输入 / 落脚 / 创建清空记录，双屏、负坐标真实拓扑、拔插、睡眠和长时趋势仍未验证。推送受阻不影响已经保存在本地的提交与便携包。

# 2026-11-01 R01–R13 修复与 0.1.14 验证

**授权与范围**：用户要求处理审查发现的所有问题，并授权必要测试。本轮处理 R01–R13 与执行中发现的 UI / smoke 缺陷；原审查的需求扩展差距不作为已经实现的功能。详细改动与证据映射见 [`reviews/2026-11-01-remediation.md`](reviews/2026-11-01-remediation.md)。保留此前工作区改动，原始 PROJECT_SPEC 和历史设计不改，未提交 / 未推送。

**代码已实现**：设置恢复后的备份保全（含备用写入路径）；同 ID 定义与失败创建的租约清理；拒读 / 中断扫描事务；食物与物理统一推进、暂停 / 实例覆盖 / 速度 / 超时；最终像素上限与 64 MiB 缓存预算、在用 surface handle；GDI 分步失败清理与结果检查；透明食物 / 气泡和落脚排除；单调时钟、有限日志、故障隔离；统一 2000 px/s 上限、无聊有界影响 Idle、版本单一真源；所有完整宿主 smoke 的设置与日志隔离。新增 Windows App.Tests 项目和 CI Windows 适配层 job。

**依赖处理**：ImageSharp 3.1.12 的安全修复版 3.2.0 在本机实跑触发许可证缺失，未绕过校验。改用 StbImageNext 1.0.0 和受限 PNG 边界，检查格式、大小、尺寸、CRC 和解压预算；同步许可证 / manifest。三套正式素材未修改；3 个正式 PNG 与 6 个透明 / 灰度 / 调色板 / Adam7 夹具对旧解码逐像素一致（`PNG_COMPATIBILITY_PASS CASES=9`）。旧 DLL 只用于忽略目录内的可信样本比较。

**构建通过**：`dotnet build YimyWalk.sln -c Release --no-restore` 最终为 **0 警告 / 0 错误**。所有桌面工具编译通过。本地 SDK 8.0.426，版本由 Directory.Build.props 统一为 0.1.14；默认参数发布 / 验包通过，`PACKAGE PASS VERSION=0.1.14 FILES=13 CHECKSUMS=12`。`dotnet list YimyWalk.sln package --vulnerable --include-transitive` 对五个项目均报告当前源无已知易受攻击包，不代表未来风险为零。

**Core 测试通过**：`scripts/test-yimywalk.ps1` 及发布流程实跑 **Core 164 / App 16 通过，0 失败、0 跳过**。App 包括真实临时 PNG 的 ACL 拒读、真实 GetHbitmap 失败后的 GDI 回落，以及五种注入获取失败 / 缓存预算 / 设置两条写入路径 / 宿主失败取消回归。测试使用自己创建的沙盒目录，不操作用户桌面文件或偏好。

**Windows 桌面已验证（仅当前会话范围）**：Windows 11 x64、单屏 2560×1600、工作区 2560×1528、144 DPI / 150%。ManagerUiSmoke 基础、完整模式、像素模式通过；输入 smoke 最终 `RESULT PASS`，包括真实透明 / 实体路由、拖拽抛掷、焦点、托盘、食物消费与退出清理；FoodWindow / BubbleWindow 的鼠标实际穿过到独立进程且底层焦点保持。实际 EnumWindows 候选确认辅助窗口不落脚、不遮挡。

`desktop-smoke -Rounds 3 -Role slime -Targeted -Landing` 通过窗口顶面落脚、支撑关闭后重新下落，以及三轮 1/10/50/100 创建清空；三轮各档清空 GDI 均回到 44，最后 Dispose 后 GDI 为 38。另一次带缓存字节检查的 1/10/50/100 回归通过，100 实例时缓存记录 5,896,935 字节，清空缓存与在用 surface 字节均为 0。该数字是短测试当时采样，不代表最大工作集或长期性能承诺。

**Windows 桌面未验证**：多屏 inventory 和完整矩阵都实跑返回 `UNVERIFIED TWO_EXTENDED_DISPLAYS_REQUIRED`（退出 2），当前只有一块显示器。双屏 / 真实负坐标、物理拔插、睡眠唤醒、全部 OS DPI 组合、长时间资源仍未验证；UI 像素计算的 96/120/144/192 DPI 不替代 OS 切换。CI 配置已改，尚未推送执行。

**测试中纠正的附加问题**：150% DPI 详情说明横向溢出已修复，保持原布局断言；DesktopSmoke 改 STA + 生产 DPI 初始化，落脚探针保证自身可见；输入背景先通过真实点击建立可见 / 焦点前置，避免自身 TopMost 掩住宠物。食物测试夹具按真实行走速度布置，继续要求进入 Eat；未验证结果不再同时打印 PASS。Windows PowerShell 5.1 的中文脚本保留 UTF-8 BOM，修正一次验包的中文路径误读。早期失败日志保留其失败状态，不作为通过证据。

**交付与下一项**：本地便携目录 `artifacts/remediation-20261101/publish/`、完整归档 `YimyWalk-0.1.14-win-x64.zip` 及 `.zip.sha256` 位于同级任务目录；归档 13 个文件逐字节与验包目录比对通过。文档本地链接 / 锚点检查通过，原素材、原始规格和历史设计的 27 个受保护文件哈希未变。补充验证食物容量达到 32 后给出可读提示、不弹异常。R01–R13 当前代码问题已处理；后续从用户待说明的扩展要求开始，硬件验收边界另行保留，避免按修复前清单重复改动。

# 2026-11-01 全项目复核与长期维护记忆落地

**范围与基线**：`codex/bootstrap` @ `7a0fb13`；开工已有 `AGENTS.md`、本文件的未提交修改，以及未跟踪的 INDEX / TECHNICAL_REVIEW，本轮保留并继续整理。覆盖 Core / App / Tests / 五个工具 / 脚本 / CI / 角色包 / 文档与第三方声明；关键调用链静态核对配合实跑，不声称逐行无遗漏或所有需求已验收。

**记忆调整**：新增 [`MAINTENANCE.md`](MAINTENANCE.md) 当前维护摘要、[`reviews/2026-11-01-project-review.md`](reviews/2026-11-01-project-review.md) 审查证据与 R01–R13 待办；AGENTS 增加分段读取、证据分级和中断交接协议；INDEX 保留代码 / 测试 / 工具地图，移除重复的动态通过表、角色参数和旧债务正文，用链接指向各自详细落点。README、ARCHITECTURE、TESTING、CHARACTER_FORMAT 同步真实入口与已确认事实；旧 TECHNICAL_REVIEW 增加历史快照提示，正文保留供追溯。原始 PROJECT_SPEC 与历史设计 / 计划未改。

**纠正的当前行为**：实际启动管理器隐藏、不自动创建宠物；普通关闭隐藏回托盘，显式退出才结束宿主。气泡总闸实际持久化；实例覆盖 / 位置 / 数量不持久化。App 仍有 UTC 墙钟路径；帧缓存每个 surface 持有租约，不是每定义只有一个。三套随包角色都为 1254×1254 / 单格 418×418，刷新界面已有实现。原始需求中的无聊驱动、完整事件调度 / 图标分身 / actions / 勿扰 / 实例恢复 / 右键菜单等缺口已登记，未改写成“完成”。

**已复现但未修复**：无窗口工作区沙盒探针确认四种问题：① 关闭走动后食物仍将宠物移动 18px / 0.1s 并置为 Walk；② session 暂停后食物继续同样移动；③ 同 ID 新定义覆盖导致旧定义所有者未释放；④ 从备份恢复后再次保存，会把损坏的主文件覆盖到原合法备份。复现调用、预期修复与验收见审查 §2–§3；探针退出 0 表示预期缺陷已观测，不表示功能正常，也不计入 Core 用例数。

**额外发现**：DesktopSmoke 的临时 SettingsStore 只用于单独自检，没有注入随后创建的 AppHost；DesktopInputSmoke 同样使用默认用户设置，并会更改全局偏好（R13）。已补 TESTING 的隔离边界；本轮没有运行这些工具或触碰真实用户偏好。DesktopDisplaySmoke 是复用生产组件的独立 Harness，不覆盖完整 AppHost 生命周期。

**四档状态（本轮亲自实跑）**：

1. **代码已实现**：本轮落实的是维护记忆、索引与审查文档；产品源码、测试断言、脚本、依赖和素材无改动。登记问题保持待修。
2. **构建通过**：环境自检退出 0，本地 SDK 8.0.426；`dotnet build YimyWalk.sln -c Release --no-restore` 为 0 错误 / 12 警告（ImageSharp 3.1.12 NU1902 / NU1903）。四个外部 smoke 工具单独 x64 Release 编译均退出 0。`build-yimywalk.ps1 -Version 0.1.13 -PublishPath artifacts/maintenance-20261101/publish` 与验包通过，`PACKAGE PASS VERSION=0.1.13 FILES=13 CHECKSUMS=12`；三套正式角色经 `validate-character.ps1` 校验均退出 0。
3. **Core 测试通过**：`scripts/test-yimywalk.ps1` 实跑 **148 通过 / 0 失败 / 0 跳过**，发布流程中再次通过。没有通过删除或削弱断言隐藏新发现。
4. **Windows 桌面未验证**：本轮未启动 AppHost、桌面 smoke、ManagerUiSmoke 或发布 EXE；没有测点击 / 焦点 / 拖拽 / DPI / 多屏 / 长期资源。四个工具编译与无窗口探针不能替代真机验收。历史桌面记录保留原边界。

**证据位置**：本机 `artifacts/maintenance-20261101/` 保存源码哈希清单、构建 / 发布 / 角色校验日志、探针源码与结果；该目录被忽略，不保证在别的工作区存在。审查文档保留命令、结果与复现步骤。受控 `.cs` 文件重新盘点为 80 个 / 10,874 行，按 `git ls-files` 排除生成与临时文件，口径与旧评审数字不混用。

**文档与范围复核**：检查 10 份本轮文档中的 93 个本地链接 / 锚点，均可解析；Core / App / 测试的受控 C# 文件名均可在 INDEX 定位。开工清单中的 139 个产品 / 测试 / 工具 / 脚本 / 素材 / 规格与历史文件哈希保持不变，`git diff --check` 通过。辅助检查结果保存在 `docs-check.json`。

**下一项可执行任务**：R03 设置备份保全，先用注入目录建立“主文件损坏→备份恢复→保存→再次损坏仍能恢复”的回归，再修 Save 的备份来源；保持当前整份配置校验契约。随后处理 R01 缓存预算、R02 GDI 失败清理，并在对应桌面工具执行前解决 R13 设置隔离。未自动扩展到这些产品修复；本轮未提交、未推送。

# 2026-10-16 全项目技术评审（只读，未改产品代码）

**范围**：对 `codex/bootstrap` @ `7a0fb13` 做全仓实现审查 —— 98 个 C# 文件 / 11,091 行（Core 2,561 / App 3,832 / Tests 2,637 / Tools 2,061），盘点 M0–M4 各功能实现并回答"是否需要重构"。产物见 [`docs/TECHNICAL_REVIEW.md`](TECHNICAL_REVIEW.md)。

**结论**：**不需要整体重写，需要一次有边界的"缺陷修复 + 结构收敛"重构**。分层边界、依赖注入、测试基础设施是资产；问题集中在两个上帝类（`AppHost` 24 成员/11 职责、`PetEngine.Tick` 131 行）、无上限的位图缓存、以及若干可追溯缺陷。

**本轮由评审者亲自核对为可追溯的缺陷**（其余为静态分析或疑似）：`LayeredWindowRenderer` 的 GDI 获取步骤在 `try` 之外；`UpdateLayeredWindow` 返回值被丢弃；`FrameSurfaceCache` 缩放无上限且无淘汰；`VirtualFoodSystem` 直接写 `PetInstance.FootPosition`/`State`（双写者并绕过走动门控）；`FoodWindow` 缺 `WndProc` 导致吞点击；设置文档单字段非法即整份回滚；`CharacterLoader` 异常过滤器不含 `UnauthorizedAccessException` 而 `CharacterCatalog.Scan` 只捕 `CharacterLoadException`；`PetSession.GetOrAddLease` 覆盖同键租约不释放；`MainForm` 从未赋值；`Boredom` 为只写状态；`CharacterFramePreview.ShowIdleFrame` 的 `scale` 参数被忽略、`CurrentFrameIndex` 恒为 0（使 `DesktopInputSmoke` 的对应断言恒真）；行走速度上限 1000（`PetEngine`，9 处）与 2000（`CharacterLoader`）不一致；全仓无任何运行期日志。

**验证档位**：① 代码已实现 —— 未改动，不适用；② 构建通过 / ③ Core 测试通过 —— 沿用同日早先一次实测（构建 0 错误 / 148 测试通过），本轮**未重跑**；④ **Windows 桌面未验证 —— 评审未执行任何 `desktop-*` smoke**。

**未验证边界**：评审为只读静态审查。所有标 `❓疑似` 的条目（内存曲线、点击穿透的实际观感、帧循环异常后的弹窗行为、气泡窗口被当作落脚面）**均未复现**；`TECHNICAL_REVIEW.md` 的 A3 内存数字是静态算术上界而非实测；文档与代码的类型名差异未逐类型穷举。**不要把该文档的缺陷清单当作已确认或已修复。**

**边界**：本轮只新增一份文档并登记进 `docs/INDEX.md`，**未改动任何产品代码、角色素材或构建脚本**。

# 2026-10-16 仓库索引与 Agent 记忆结构

**动机**：仓库原本没有"要改 X → 去哪个文件"的单一入口，每次新会话都要重新全仓库检索；规则又散落在 `AGENTS.md`、`docs/` 与测试断言里，容易被后来的会话漏读。

**新增**：`docs/INDEX.md` —— 仓库索引（顶层地图、依赖图、Core/App 模块地图、测试地图、工具与脚本参数表、文档地图、角色格式速查、常用命令、验证状态表、改动落点 checklist、已知维护债务 10 条）。

**重构**：`AGENTS.md` 由 11 行扩为分层入口：开工顺序 → 记忆分层表 → 硬约束（验证口径四档 / 架构铁律 / 产品边界 / 仓库卫生）→ 命令 → 改完代码后 checklist。原有约束文字全部保留。

**本轮实测（本机，非文档转抄）**：

- 环境：`global.json` 锁 SDK `8.0.425`，本机系统 SDK 为 8.0.423 / 10.0.302，**均不满足**（直接报 `A compatible .NET SDK was not found`）。运行 `scripts/bootstrap-dotnet.ps1` 装出工作区本地 SDK **8.0.426**，之后全部命令可用。此项此前未在文档中标为"必需前置"。
- `scripts/verify-environment.ps1`：退出码 0；报告 `Workspace .NET: 8.0.426`。
- `dotnet restore YimyWalk.sln`：通过（4 个项目）。
- `dotnet build YimyWalk.sln -c Release --no-restore`：**0 错误 / 10 警告**，全部为既有 NU1902 / NU1903（ImageSharp 3.1.12 安全通告）。
- `dotnet test YimyWalk.sln -c Release --no-restore`：**148/148 通过**（`Failed: 0, Skipped: 0`），与既有交接一致。
- `scripts/build-yimywalk.ps1 -Version 0.1.13 -PublishPath artifacts/publish/yimywalk-win-x64`：退出码 0，内含 148/148 测试。
- `scripts/test-release-package.ps1 -Path artifacts/publish/yimywalk-win-x64 -Version 0.1.13`：`PACKAGE PASS VERSION=0.1.13 FILES=13 CHECKSUMS=12`。
- `scripts/validate-character.ps1 -Path .\Characters\slime -PreviewPath .\artifacts\slime-preview.png`：退出码 0。

**新记录的素材事实**：`Characters/slime/sprite.png` 实测为 **1254×1254、单格 418×418、759,399 字节、SHA-256 `BC92811F…`**，与 `docs/CHARACTER_FORMAT.md` 的叙述（似乎把小尺寸示例图当作 `slime`）不符；`tools/GenerateTestCharacters.ps1` 生成的是 96×96，说明当前 `slime` 素材不是该脚本产物。两个咿米角色的哈希（`63CE5BF9…` / `EDB50E55…`）与本页 2026-10-13 记录一致。

**未验证**：本轮**未执行任何 Windows 桌面验收**（`desktop-smoke` / `desktop-input-smoke` / `desktop-display-smoke`），也未复核历史桌面结论；透明区域穿透、实体命中、焦点保持、多显示器/DPI、负坐标屏、拔插显示器、睡眠/唤醒、长期 CPU/内存/句柄趋势、权限受限会话继续保持原有未验证状态。索引第 12 节的"已知维护债务"来自本轮只读代码勘察（其中 `docs/` 与代码的类型名差异、未接线成员、空文件等已用静态检索核实），未逐条运行时复现。

**边界**：本轮只新增/改写文档（`docs/INDEX.md`、`AGENTS.md`、本条目），**未改动任何产品代码、角色素材或构建脚本**。

# 2026-10-13 管理界面标题间距修正

**根因**：`ManagerPage.AddHeading` 将所有副标题固定放在 `Y=42`，而 120 DPI 下 22pt 粗体标题的实际高度为 50px，导致首页 `YimyWalk` 标题与副标题重叠 8px。原有 `ManagerUiSmoke` 为标题面板设置了跳过条件，因此没有捕获标题面板内部的标签重叠。

**修正**：标题面板现在读取标题的首选高度，将副标题放在标题底部额外 8px 处，并根据副标题高度更新面板最小高度。`ManagerUiSmoke` 移除标题面板跳过条件，新增回归约束，避免同一标题面板内的标签再次重叠。

**验证**：修正前 smoke 按预期失败：`Overview sibling labels do not overlap: YimyWalk / 桌宠继续在桌面运行，主菜单负责管理当前状态和设置。`；修正后 `ManagerUiSmoke --baseline`、完整 ManagerUiSmoke 和 `--preview-pixels` 均通过。当前 120 DPI 截图确认首页标题与副标题分离；其他 DPI 的完整人工桌面矩阵仍未执行。

# 2026-10-13 咿米初始角色入包

**资源**：按用户要求将 `Yimy.jpg` 原样复制为 `Characters/咿米_Yimy/sprite.png`，将 `Yimy_no.jpg` 原样复制为 `Characters/咿米_Line/sprite.png`。两张源文件实际为带 Alpha 的 1254×1254 PNG，符合 3×3 格式，单格 418×418；角色 ID 与显示名使用用户指定的目录名，运动和对白使用现有默认值。现有 `slime` 保留，发布脚本复制完整 `Characters/` 并生成三角色清单。

**来源与校验**：用户明确要求作为开箱即用的初始角色随包分发。Yimy SHA-256 为 `63CE5BF9750B0ADF796A0CBB05616A4731003C01CFC23819FFD0DB435B66CA23`；Line SHA-256 为 `EDB50E55FAD02497ADB73E9FE1509A30C9FA08EA1F7E756448B4A00644EA894E`；目标文件与源文件哈希一致。根目录源文件保留在本地并忽略，Git 只提交角色目录内副本。

**验证**：`CharacterValidator` 已分别加载两个新角色，报告 1254×1254、418×418 单格、RGBA、9 帧且使用默认配置。`scripts/build-yimywalk.ps1 -Version 0.1.13 -PublishPath artifacts/publish/starter-yimy-20261013` 完成 restore、Core **148/148** 测试和 x64 self-contained 发布；`scripts/test-release-package.ps1` 修正为 UTF-8 路径读取后通过，结果为 `PACKAGE PASS VERSION=0.1.13 FILES=13 CHECKSUMS=12`，manifest 角色为 `slime`、`咿米_Line`、`咿米_Yimy`。三角色发布副本均通过运行时校验，哈希与源码角色副本一致。restore/build 仍报告既有 ImageSharp 3.1.12 的 NU1902/NU1903 安全审计警告。

**桌面边界**：本轮只验证角色格式、运行时加载和发布包内容；没有在真实 Windows 桌面逐项观察两个新角色的透明命中、动画、拖拽或长期资源行为，不能把发布包检查代替桌面验收。

# 2026-10-12 GitHub 仓库清理

**范围**：远端默认分支为 `codex/bootstrap`。完整 `vendor/esheep/` 包含 1,130 个文件，共 12,531,708 字节，当前产品源码和解决方案不依赖它。移除快照、4 个上游专用脚本、只为上游设置的 `.gitattributes` 和根目录重复需求文件。保留产品测试、开发工具、角色模板、已确认的设计规则、许可证和审计摘要。

**工程说明**：更新 `AGENTS.md`、README、第三方声明、测试入口和环境检查；忽略上游目录、本地 `.superpowers/`、重复根目录 `logo.png` 和 `.vs/`。本地未跟踪的 `.superpowers/` 与 `logo.png` 保留。旧上游文件可从历史提交恢复，后续审计在仓库外临时获取固定提交。

**验证与同步**：清理后的 staged 源码导出不含 `vendor/`、`.tools/` 或构建产物；`scripts/verify-environment.ps1` 通过。使用导出源码执行 `scripts/build-yimywalk.ps1 -Version 0.1.13`，Core 测试 **148/148** 通过，x64 self-contained 发布完成；`scripts/test-release-package.ps1` 返回 `PACKAGE PASS VERSION=0.1.13 FILES=11 CHECKSUMS=10`。restore/build 报告现有 ImageSharp 3.1.12 的 NU1902/NU1903 安全审计警告，本次清理没有改变该依赖。随后重写 `codex/bootstrap` 及 `v0.1.2–v0.1.5` 标签的路径历史，当前可达历史中的 eSheep 路径对象为 0；标签名称与发布树保留，标签对象哈希随历史重写变化。

**桌面边界**：本次只调整仓库内容与工程入口，没有新增产品桌面行为；不重新宣称历史未验证项通过。

# 当前状态（2026-10-07，桌宠详情实例大小可见性修复）

## 桌宠详情布局修复（2026-10-07）

- **真实原因**：桌宠页外层把列表/详情分栏固定在 430px 高；详情内的预览与设置编辑器又因右栏宽度不足发生换行，`实例大小`行被推到详情卡片可视区之外。控件本身和实例绑定逻辑均已存在，问题是布局测量和控件宽度组合造成的裁切。
- **代码已实现**：桌宠页分栏改为填充标题下方剩余高度；预览行调整为 128px，保留 120px idle1 预览；活跃度与实例大小编辑器调整为可在 125% DPI 右栏单行容纳的宽度。小窗口仍可使用详情卡滚动区域访问操作按钮。
- **验证通过**：新增 ManagerUiSmoke 断言，选中实例后“实例大小”、来源 Select 和数值 InputNumber 必须完整落在可视裁剪区域内；DPI 120（125%）下五页 baseline、实例大小 UI 修改/重置、自动刷新、混合实例连续删除均通过。Core 102/102、App 和 smoke 构建均通过。
- **未验证**：100%、150%、200% DPI 的真实桌面组合以及用户本机已发布 EXE 的手工操作仍需继续验证。

# 当前状态（2026-10-06，运行实例大小覆盖）

## 实例大小覆盖（2026-10-06）

- **代码已实现**：桌宠页新增“实例大小”设置，使用 AntdUI 的“跟随全局/自定义”选择和 0.1–8.0 倍 `InputNumber`。设置通过稳定 `PetInstanceId` 调用 `AppHost`，只改变选中的运行实例；同一角色的不同实例可以使用不同大小。
- **行为边界**：自定义值替代全局缩放倍率；全局缩放变化只影响仍处于“跟随全局”的实例。清除覆盖后重新读取当前全局值。覆盖保存在 `PetInstance` 运行时内存中，不写入 `SettingsDocument` 或磁盘，重启后恢复全局默认。页面同时显示来源和当前有效大小，跟随全局时禁用自定义数值输入。
- **测试通过**：Core 测试 102/102；新增 `InstanceScaleOverrideStaysIndependentFromGlobalScaleUntilReset`。`ManagerUiSmoke` 已验证混合 cat/slime 实例中单实例自定义 0.6 倍、全局改为 1.8 倍、继承实例跟随、清除后恢复，以及页面存在来源选择器和 0.1–8.0 数值控件。App 与 smoke 工具 x64 Release 构建均为 0 警告、0 错误。
- **Windows 桌面验证边界**：当前执行了 DPI 120（125%）管理界面 smoke；未重新执行 100%、150%、200% 的真实桌面矩阵，也未将截图或 ManagerUiSmoke 视为完整的 Windows 视觉验收。实例大小拖动、跨显示器 DPI 和重启后恢复需要在真实桌面继续验证。

# 当前状态（2026-10-05，M4 UI shell 最小切片）

## M4 UI shell 最小切片（2026-10-05）

- **代码已实现**：`ManagerForm` 已改为普通 WinForms 任务栏主窗口（默认 1120×720、最小 900×580、200px 侧栏、DPI 自动缩放），启动显示总览；总览、桌宠、角色、互动、设置五个 `UserControl` 页面按需创建并缓存。右上角关闭只最小化主菜单，桌宠继续运行；固定的“退出 YimyWalk”才进入宿主退出清理。托盘入口保留为兼容资源。
- **代码已实现**：桌宠页按稳定 `InstanceId` 增量维护列表和选择，删除/清空后的空状态会禁用详情操作；三态布尔覆盖和“跟随全局/自定义”活跃度覆盖均调用 `AppHost` 业务门面。详情显示单实例临时生效说明、来源和实际值。角色页预览目录中的 idle1/index 0，并且只有明确点击“设为下次创建角色”才写入下次创建默认角色；桌宠详情使用实例实际角色定义。预览复用 `FrameSurfaceCache`，刷新失败保留旧定义租约。
- **代码已实现**：互动页绑定默认主动走动、默认自动气泡、全局自动气泡运行级限制、活跃度倍率、饥饿增长和应用切换反馈；关闭外部反馈会停止专用前台轮询。设置页绑定全局缩放、窗口顶部落脚、首选显示器和保存/恢复诊断；`SettingsSaveCoordinator` 区分已应用、保存中、已保存和保存失败，并提供重试。`WarningLimit` 继续校验但当前没有对应运行时行为，因此不渲染无效控件。
- **构建通过**：
  - `.tools/dotnet/dotnet.exe build src/YimyWalk.App/YimyWalk.App.csproj --configuration Release -p:Platform=x64 --no-restore`：0 警告、0 错误。
  - `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/desktop-smoke.ps1 -Rounds 1 -Role slime`：构建通过，真实双屏会话完成 1/10/50/100 创建和清空，清空后 `ACTIVE=0 WINDOWS=0 CACHE=0`。
- **逻辑测试通过**：`.tools/dotnet/dotnet.exe test YimyWalk.sln --configuration Release --no-restore`，**101/101** Core 测试通过；新增覆盖默认/实例行为覆盖和恢复跟随。
- **Windows 桌面已验证**：最近一次 `scripts/desktop-input-smoke.ps1` 返回 `RESULT PASS`。真实会话验证了任务栏主窗口、五页导航、关闭转最小化/恢复、实例选择、idle1 单帧、单实例 WalkOverride、互动和设置控件、虚拟食物、托盘隐藏/显示/清空、角色刷新成功/失败保留以及显式退出清理。当前运行记录包含主屏 1920×1080（工作区 1920×1020、scale 1.25）和右侧副屏 3200×1800（工作区 3200×1740、scale 1.25）；批量 smoke 另一次记录为 1536×864 + 2560×1440 双屏拓扑。
- **Windows 桌面未验证**：透明穿透依赖当前可见桌面；若其他窗口或安全桌面遮挡，输入 smoke 会输出 `RESULT UNVERIFIED INPUT_ROUTING_BLOCKED` 并返回未验证状态。物理拔插、睡眠/唤醒、负坐标/垂直排列显示器、长时间 CPU/内存曲线、系统注销/关机的真实回归和托盘最终删除决策仍未完成，不能用 Core 测试或页面截图替代。

# 当前状态（2026-10-05，v0.1.5 拖拽丢出跨屏循环）

## v0.1.5 拖拽丢出空中跨屏循环（2026-10-05）

- **代码实现**：`PetEngine` 让空中水平运动复用 `IWorldSurface.HorizontalTravelBounds`。越过全局右边界从左侧重新出现，越过全局左边界从右侧重新出现；垂直重力、水平速度和状态连续。表面选择使用循环后的投影坐标，落地仍使用实际显示器或窗口表面高度。
- **Core 测试通过**：完整 Core 测试 **96/96** 通过，新增右向和左向拖拽丢出循环回归测试。
- **构建通过**：`scripts/build-yimywalk.ps1 -Version 0.1.5` 通过，生成 self-contained single-file `YimyWalk.exe`；关闭 .NET 单文件压缩以保证重复 publish 的可复现字节；两次 clean publish 的 `YimyWalk.exe` 哈希一致，`scripts/test-release-package.ps1 -Version 0.1.5` 通过（12 个文件、11 个 SHA-256 校验项）。
- **Windows 桌面已验证**：`scripts/desktop-smoke.ps1 -Targeted -Rounds 1 -Role cat` 在当前真实双显示器拓扑通过；最大化窗口过滤、跨屏行走和新增 `CROSS-DISPLAY TOSS ... PASS` 均通过，随后角色刷新及 1/10/50/100 创建/清空、资源回落也通过。
- **Windows 桌面未验证**：本轮未重复 83/83 双屏矩阵；负坐标布局、物理拔插、睡眠唤醒、长期 CPU/内存/句柄趋势、权限受限会话及真实任务栏配置变化仍未验证。`desktop-input-smoke.ps1` 本轮因当前用户设置使气泡关闭，在“created pet shows a dialogue bubble”处提前退出，不能作为本功能通过证据。
- **发布状态**：`codex/bootstrap` 实现与发布脚本已推送（最新提交 `e4eb848`）；精简 `main` 提交 `f378757` 和 `v0.1.5` 标签已推送。源码/Windows x64 归档和 `SHA256SUMS.txt` 位于 `D:/Project/Cursor/YimyWalk-main/artifacts/release/v0.1.5/`。应用继续不联网、不自启动、不请求管理员权限、不修改真实桌面文件。

# 当前状态（2026-10-05，v0.1.4 EXE 发布包）

## v0.1.4 跨屏行走与最大化窗口修复（2026-10-05）

- **代码实现**：`DisplaySnapshot` 和 `IWorldSurface` 提供所有显示器工作区的全局水平范围；`PetEngine` 在全局左右边界循环并保留当前显示器的落脚高度，因此宠物可从一块水平排列的显示器走到另一块。窗口落脚候选新增 `IsMaximized`，Windows `IsZoomed` 或覆盖工作区的窗口会被排除，普通窗口仍可落脚。
- **构建通过**：Core、App 和 self-contained single-file `YimyWalk.exe` 的 `v0.1.4` 发布构建通过，发布包自检通过（12 个文件、11 个 SHA-256 校验项）。
- **Core 测试通过**：完整 Core 测试 **94/94** 通过，覆盖全局水平范围、左右循环、跨显示器间隙和最大化候选过滤。
- **Windows 桌面已验证**：针对性 smoke 创建真实最大化探针并确认其候选被拒绝；在当前 `DISPLAY1`（0–1536）与 `DISPLAY2`（1920–4480）拓扑下，宠物从第一块屏幕工作区右边缘走入第二块屏幕；原有角色刷新和 1/10/50/100 创建清空检查继续通过。
- **Windows 桌面未验证**：没有重复既有 83/83 双显示器矩阵；垂直排列显示器、负坐标布局、物理拔插、睡眠唤醒、更长时间资源趋势、权限受限会话和真实任务栏变化仍未验证。跨屏 smoke 只覆盖当前可见的水平排列拓扑，不能替代这些场景。
- **远程与归档**：`codex/bootstrap` 实现提交为 `0ba19a6`，`main` 发布提交为 `bea182d`，均已推送；`v0.1.4` 标签已推送。发布压缩包和 `SHA256SUMS.txt` 位于 `D:/Project/Cursor/YimyWalk-main/artifacts/release/v0.1.4/`。

## v0.1.3 应用图标修复（2026-10-05）

- **代码实现**：新增嵌入式 `YimyWalk.App.Resources.yimywalk.ico` 加载器；管理窗口 `Form.Icon` 和托盘 `NotifyIcon.Icon` 现在都使用该资源，和 EXE 文件图标保持一致；托盘图标由宿主显式释放。
- **构建通过**：Core、App 和 self-contained single-file `YimyWalk.exe` 发布均通过，包版本为 `0.1.3`，发布包自检通过（12 个文件、11 个 SHA-256 校验项）。
- **Core 测试通过**：完整 Core 测试 **89/89** 通过；图标回归断言位于 `tools/DesktopSmoke`，不把 WinForms 依赖引入 Core。
- **Windows 桌面已验证**：可见桌面 smoke 已确认嵌入资源存在，管理窗口和托盘图标逐像素匹配同一资源；发布 EXE 直接启动保持运行通过，已有创建/清空、角色刷新和资源释放检查继续通过。
- **Windows 桌面未验证**：本次没有重复双显示器 83/83 矩阵；任务栏通知区域的肉眼截图、负坐标显示器、物理拔插、睡眠唤醒、更长时间资源趋势和权限受限会话仍按原边界标记未验证。输入 smoke 因当前用户已有 `dialogueEnabled=false` 在气泡前置断言处失败，未修改用户设置，也未计为通过。
- **远程与归档**：`codex/bootstrap` 修复提交为 `bc130d5`，`main` 发布提交为 `0c9f106`，均已推送；`v0.1.3` 标签已推送。发布压缩包和 `SHA256SUMS.txt` 位于 `D:/Project/Cursor/YimyWalk-main/artifacts/release/v0.1.3/`。

## 发布分支与 v0.1.2（2026-10-05）

- **开发分支**：`codex/bootstrap` 保留完整源码、Core 测试、Desktop smoke、作者工具、上游审计快照和阶段文档。
- **标准主分支**：`main` 只保留产品源码、角色、作者格式、必要构建/启动脚本、许可证和发布说明；不包含 `tests/`、`tools/`、`vendor/`、本地 SDK、smoke 脚本、测试日志或开发计划。
- **主分支可构建性**：精简后的 `main` 使用只含 `YimyWalk.Core` 与 `YimyWalk.App` 的产品解决方案；在不复制 `.tools/` 且不含测试工程的工作树中，使用外部 .NET 8 SDK 完成 restore/build/publish，脚本会明确跳过不存在的测试项目。
- **Release 产物**：本次发布为 `v0.1.2`，入口为自包含单文件 `YimyWalk.exe`；发布包同时包含 `Characters/`、许可证、版本说明、manifest 和 SHA-256 清单。本地压缩包位于 `artifacts/release/v0.1.2/`，包含源码包、Windows x64 包和 `SHA256SUMS.txt`。
- **远程状态**：已推送 `main`、`codex/bootstrap` 和 `v0.1.2` 标签到 `origin`；没有修改系统设置、开机启动或真实桌面文件，也没有创建在线 Release 页面。

## M4.2 发布包与启动自检实际结果（当前事实来源）

- **代码已实现**：Core 新增 `StartupDiagnostics`，为角色根目录缺失、空目录和全部角色无效提供稳定错误码、原因和恢复方法；AppHost 在启动计时器前执行检查，Program 捕获启动失败并显示诊断，同时把摘要写入用户本地 `YimyWalk/startup-error.log`。`run-yimywalk.ps1` 检查 `YimyWalk.exe`、`Characters` 和至少一个 `sprite.png`；自包含发布包不依赖外部 .NET Runtime，仍保留旧 framework-dependent 开发输出的 runtime 诊断兼容。
- **发布包已实现**：`scripts/build-yimywalk.ps1` 清理目标发布目录后 restore/test/publish，生成 self-contained single-file `YimyWalk.exe`，复制 `Characters/`、许可证和版本说明，并生成 `PACKAGE_README.txt`、`PACKAGE_MANIFEST.json` 与 `PACKAGE_CHECKSUMS.txt`；`scripts/publish-yimywalk.ps1` 使用同一流程输出发布目录。
- **构建通过**：`scripts/build-yimywalk.ps1` 已通过；`YimyWalk.exe` 文件版本为 `0.1.2.0`，包清单标记 `selfContained=true`，发布目录 12 个文件、11 个 SHA-256 校验项，`scripts/test-release-package.ps1` 通过。
- **核心测试通过**：`.tools/dotnet/dotnet.exe test YimyWalk.sln --configuration Release --no-restore` 通过 **89/89**，其中新增 `StartupDiagnosticsTests` 4 项；Core 仍未引用 WinForms 或桌面绘制对象。
- **Windows 桌面已验证**：可见桌面上 `scripts/desktop-smoke.ps1 -Rounds 1 -Role slime` 通过，真实启动发布版宿主并完成 1/10/50/100 创建/清空、角色刷新成功/失败保留和资源计数回落；本轮 100 只清空后 `ACTIVE=0`、`WINDOWS=0`、`CACHE=0`、`USER=29`、`GDI=33`，Host Dispose 后 `USER=10`、`GDI=27`。另用 `run-yimywalk.ps1 -PublishPath artifacts/publish/yimywalk-win-x64` 启动发布包约 2 秒后由测试进程结束，入口预检通过。
- **Windows 桌面未验证**：本任务没有重新执行已通过的双显示器 83/83 矩阵；负坐标显示器、物理拔插、睡眠唤醒、更长时间 CPU/内存曲线、权限受限会话和更高实例压力仍保持未验证。本次输入 smoke 因当前用户已有 `dialogueEnabled=false`，在“新建宠物出现对话气泡”的前置断言处失败，未计为通过，也未修改用户设置。发布/核心/启动预检证据不能替代这些真实桌面验收。

## M4.3 稳定性回归实际结果（2026-10-05）

- **代码已实现**：未改变运行时行为；`tools/DesktopSmoke` 增加 `CPU_S` 累计处理器秒数输出，使长时资源记录同时包含 CPU、工作集、私有字节、托管堆、句柄、USER 和 GDI。输入 smoke 仍使用现有真实刷新、隐藏/恢复、清空和退出入口。
- **构建与核心测试通过**：M4.2 发布脚本在 smoke 前构建 DesktopSmoke 为 0 警告/错误；完整 Core 测试仍为 89/89。M4.3 没有引入新的生产程序集依赖。
- **Windows 桌面已验证**：`scripts/desktop-smoke.ps1 -Rounds 20 -Role slime` 在同一宿主进程完成 20 轮、160 次 CREATE 和 80 次 CLEAR；每次清空均为 `ACTIVE=0 WINDOWS=0 CACHE=0`，USER/GDI 回到 29/33，Host Dispose 后为 10/27。第 1 轮首个 CREATE 为 `CPU_S=0.438 WS=70,504,448 PRIVATE=19,070,976 HANDLES=352`，第 20 轮结束为 `CPU_S=55.234 WS=118,947,840 PRIVATE=63,967,232 HANDLES=364`。输入 smoke 在恢复默认气泡设置后连续两轮通过真实角色刷新、清空、托盘退出、气泡/食物/外部进程反馈和资源释放；原用户设置随后恢复，发布包运行前后清单哈希不变。完整趋势日志保存在 `artifacts/m43-stability-cpu.log`。
- **Windows 桌面未验证**：20 轮显示工作集和私有字节仍有明显进程分配器增长，因此不能宣称长期无泄漏或流畅；更长时间/更高压力、权限受限会话、真实快捷方式、负坐标显示器、拔插显示器和睡眠唤醒仍未验证。第一次输入 smoke 因持久化 `dialogueEnabled=false` 直接断言失败，已记录为测试前置条件问题，未把该次失败改写为通过。

## M4.2 双显示器/DPI 实际结果

- **代码已实现**：`WindowsDisplayAdapter` 为每块屏幕读取独立 DPI；应用启动声明 `PerMonitorV2`；`DesktopWorldSurface` 按宠物坐标解析显示器缩放；`PetWindow` 在目标显示器上创建/调整尺寸并使用 `AutoScaleMode.Dpi`，跨 DPI 渲染尺寸和透明命中保持一致。新增 `tools/DesktopDisplaySmoke` 与 `scripts/desktop-display-smoke.ps1`，仍沿用一个宿主进程、一个宠物一个窗口的结构。
- **构建通过**：`scripts/build-yimywalk.ps1` 通过；双屏 smoke 工具 x64 Release 构建 0 警告、0 错误。
- **核心测试通过**：`scripts/test-yimywalk.ps1` 通过 **85/85**；Core 未引用 WinForms 或桌面绘制对象。
- **Windows 桌面已验证**：在 Windows 11 Home 10.0.26200、x64、16 logical CPUs、Intel Core i5-12500H 的已解锁桌面上，`scripts/desktop-display-smoke.ps1 -Rounds 3` 返回 `RESULT PASS CHECKS=83 FAILURES=0`。`DISPLAY1` 为 1920×1080、工作区 1920×1020、DPI 120（125%）；右侧 `DISPLAY2` 为 2560×1440、工作区 2560×1392、DPI 96（100%）。cat/slime 在两块屏幕上的生成、掉落、落地、行走、跳跃、透明区域穿透、实体命中、拖拽释放、焦点保持、global scale=2，以及双向跨屏拖拽均通过；3 轮双屏创建/清空后活动实例、窗口和 cache 均归零。
- **资源趋势**：最终三轮清空 GDI 为 41、46、51，USER 为 15，句柄为 323、324、324；工作集/私有字节保留进程分配器趋势。`AutoScaleMode.Dpi` 将跨 DPI 创建/销毁的 GDI 增长从早期约每轮 +40 降到本轮约 +5，但仍不能据此宣称无泄漏或 100 只长期流畅。完整日志：`artifacts/dual-display-final.log`。
- **Windows 桌面未验证**：本次拓扑没有负坐标屏幕，因此负坐标布局未验证；物理断开/重连、睡眠/唤醒、任务栏布局变化、更长时间 CPU/内存曲线和更高实例压力未执行。它们继续保持未验证，不能用 Core 或普通窗口动画替代。

## 下一条可执行任务

继续处理 M4.3 残余验证债务：获取更长时段 CPU/内存/句柄曲线和权限受限会话证据；若再次取得负坐标或拓扑变化环境，再运行对应真实桌面矩阵。真实快捷方式读写、自启动和系统设置修改仍不在当前范围。

## M4.1 实际结果

- **代码已实现**：新增 `tools/CharacterValidator`（net8.0），直接引用 `YimyWalk.Core.CharacterLoader`；提供角色诊断、3×3 预览 PNG 和网格/默认脚底锚点参考 PNG。Core、作者工具和绘制适配层都可使用 ImageSharp 解码/转换像素；Core 仍不持有窗口、Bitmap 或其他平台绘制对象。
- **作者材料已实现**：`docs/CHARACTER_FORMAT.md` 已按当前实现区分 supported/planned；`examples/character-template` 提供配置和 UTF-8 对白示例；`Characters/cat`、`Characters/slime` 保持 sprite-only，可直接加载；`docs/assets/` 保存可重复生成的 cat/slime 预览和网格参考图。
- **入口已实现**：`scripts/validate-character.ps1` 支持空格/中文路径、可选预览和参考图，验证失败返回非零；不会修改输入角色文件。未知 JSON 属性当前会忽略，`actions.png` 尚未实现，文档已明确说明。
- **构建通过**：`.tools/dotnet/dotnet.exe test YimyWalk.sln --configuration Release --no-restore` 通过 85/85；`scripts/build-yimywalk.ps1` 通过并生成 x64 framework-dependent 发布目录。
- **Core/工具验证通过**：已运行 cat、slime、template、含空格和中文路径的角色；坏 PNG 返回 1；预览/参考图尺寸、参考图透明区域与锚点断言通过；输入 sprite 的 SHA-256 前后不变。
- **Windows 桌面状态**：本任务没有新增桌面行为，M4.1 的作者工具不需要 GUI 验收。既有单显示器桌面证据仍按历史交接保留；第二显示器/DPI 矩阵继续标记为 **Windows 桌面未验证（用户已暂缓）**。核心测试或普通窗口预览不能替代该验收。

## 仍需单独处理的验证债务

历史 M3 交接中的“代码已实现”不等于所有需求已验收：无聊状态尚未驱动完整行为决策，走路自然结束/高级动作仍有限；外部应用 smoke 的台词断言不能证明真实事件；气泡点击穿透、虚拟食物窗口层级/目标过期与路径约束仍需独立回归；长时 CPU/内存/句柄趋势和权限受限场景未完成。后续稳定性任务必须针对这些证据补强，不能用文档或模拟测试改写为通过。

## 下一条可执行任务

执行 M4.2：完善可重复 publish/package、第三方许可和启动自检诊断；保持不联网、不自启动、不修改真实桌面文件，并在完成后再次区分代码、构建、Core 测试和 Windows 桌面验证状态。

# YimyWalk 状态交接

更新时间：2026-10-05
当前分支：`codex/bootstrap`
Current target: M4 stabilization planning. M2 foundation and M3 core fun loop are implemented; available single-display Windows evidence is recorded below, while second-display/DPI remains explicitly deferred.

## M0 上游决策证据

- 上游仓库：`https://github.com/Adrianotiger/desktopPet`
- 固定提交：`e2933305157a34818f47d238c74607593d81816d`
- 快照路径：`vendor/esheep/`
- 原版 `DesktopPet_Portable.csproj` 在未适配环境时先因缺少 .NET Framework 4.7.2 引用程序集失败；补齐工作区引用后，原版仍因未使用的 `stdole` COMReference 报 MSB4181/MSB3283。隔离构建脚本只在临时副本移除这条未使用引用，x64/x86 构建和已有运动、显示器、WinForms runtime 检查通过。
- 复用依据：参考 `src/dotNet/FormPet.cs`、`FormPet.Motion.cs`、`Animations.cs` 的窗口/运动/缓存思路，以及 `tests/` 的运行时检查；没有直接采用上游 XML 角色协议，也没有改写快照。
- YimyWalk 新代码选择 `net8.0` Core 与 `net8.0-windows` App，原因是隔离可测试物理和 Windows API，避开上游 .NET Framework 目标包限制；没有把上游整体迁移到新运行时。

## 已完成证据

### 代码已实现

- `YimyWalk.Core`：PNG 解码、3×3 帧定义、默认配置、共享定义目录索引、独立实例物理、工作区地面、跳跃/坠落、拖拽/抛掷、定义和渲染引用计数。
- M2 Task 1 is implemented in Core: `PetSession.RemoveDefinition` lets a refresh stop new instances for a removed role while existing instances retain their leased definition; the new refresh regression test brings the core suite to 42 passing tests.
- M2 Task 2 is implemented: `AppHost.ReloadCharacters()` atomically rescans roles, keeps old definitions leased by existing pets, removes deleted roles from new-instance creation, and reports failures; the manager exposes a working `????` button and status count. Real smoke covered valid reload, invalid reload retention, and the button entry point.
- `YimyWalk.App`：一个实例一个透明分层窗口；同一宿主进程管理所有窗口；托盘菜单、管理器、1/10/50/100 按钮和分批创建队列；窗口透明区域和实体命中分别走 `WM_NCHITTEST`；窗口使用 `WS_EX_NOACTIVATE`。
- 角色包：`Characters/cat/sprite.png`、`Characters/slime/sprite.png`，均只包含 `sprite.png`。

### 构建通过

- `dotnet restore YimyWalk.sln`：通过。
- `dotnet test YimyWalk.sln --configuration Release --no-restore`：通过，41 个核心测试。
- `dotnet build src/YimyWalk.App/YimyWalk.App.csproj --configuration Release -p:Platform=x64`：通过。
- `scripts/build-yimywalk.ps1`：通过，发布到 `src/YimyWalk.App/bin/x64/Release/net8.0-windows/win-x64/publish/`，并复制角色目录。

### 核心测试通过

- 角色格式、加载校验、物理状态和边界、共享资源生命周期、实例创建/清空均通过。
- `YimyWalk.Core` 程序集没有引用 WinForms、WindowsBase 或 PresentationCore。

### Windows 桌面已验证

- 在本机 Windows 11 Home 10.0.26200、x64、DPI 120、12th Gen Intel Core i5-12500H、约 15.7 GB 可见内存环境中，发布版启动后保持运行至少 3 秒，随后由测试进程结束；启动时进程约 61.1 MB 工作集、333 个句柄、无主窗口句柄（应用以托盘/宠物窗口生命周期运行）。
- 通过 `scripts/desktop-smoke.ps1` 在已解锁桌面实际创建并清空 1、10、50、100 个分层窗口；每档输出的活动实例/窗口数分别为 1/1、10/10、50/50、100/100，清空后每档均为 0/0。`cat` 和 `slime` 两套只含 `sprite.png` 的角色均完成真实启动/创建 smoke。本轮单轮 `cat` 日志 CREATE 工作集/句柄约为 1=57.8 MB/336、10=59.5 MB/337、50=64.7 MB/338、100=70.3 MB/338；CLEAR 后约为 58.4 MB/337、60.7 MB/337、65.4 MB/338、71.4 MB/338。该结果是当前基线和资源趋势记录，不是流畅性承诺；绝对值会受会话和加载状态影响。
- `scripts/desktop-input-smoke.ps1` 在已解锁桌面通过：真实屏幕上方掉落、落地、Walk、Jump/Apex/Fall；透明像素 `WindowFromPoint` 穿透到探针；实体像素命中宠物；拖拽移动、释放抛掷、捕获丢失；鼠标点击/拖拽不改变前台窗口；隐藏/恢复不抢焦点；清空和退出释放托盘图标。期间修复了 `WM_MOUSEACTIVATE -> MA_NOACTIVATE` 和无激活恢复窗口路径。 Direct real-mouse clicks on `NotifyIcon.ContextMenuStrip` items (open manager, hide, show, clear, exit) also passed.
- 同一进程五轮 1/10/50/100 创建/清空并在清空后强制 GC：活动实例/窗口始终归零，USER/GDI 回到约 23/24，句柄约 333—348；本次 cat 运行工作集约从 58.1 MB 增至 75.0 MB，私有字节约从 16.5 MB 增至 28.0 MB。原生窗口资源没有随轮次持续增长，但托管/进程分配趋势仍是风险，不能据此承诺长期流畅。
- 20-round same-process 1/10/50/100 baseline also exited 0: round 1 first CREATE was about WS 61.6 MB and PRIVATE 18.0 MB; round 20 final CLEAR was about WS 107.5 MB and PRIVATE 58.5 MB. Active instances/windows returned to zero every round, USER/GDI returned to 23/24, and handles stayed around 333 to 343. Shared images, window disposal, and handle cleanup are in place; 100 is a pressure tier, not a daily-count or long-term smoothness promise.

### Windows 桌面未验证

以下项目仍未完成可见桌面逐项验收，不能据此宣布 M1 全部验收通过：

- Tray menu item display and real mouse clicks are verified. The smoke directly displays `NotifyIcon.ContextMenuStrip` and clicks its items; invoking the menu from the system notification-area icon itself, and double-clicking that icon to open the manager, remain unverified.
- DEFERRED: the second-display/DPI desktop matrix is postponed because this session exposes only `DISPLAY1` (bounds 1536x864, working area 1536x816). It remains `Windows desktop unverified`, not a product failure or a reason to block M2 core work.
- 更长时间/更多轮次的资源趋势、CPU 曲线和 100 只流畅性仍未验证；当前数据只作为 M1 基线，不是性能承诺。

此前安全桌面遮罩导致的 `LockScreenBackstopFrame` 阻塞已消失；本轮输入矩阵通过的项目已移到“Windows 桌面已验证”。剩余项目仍保持未验证，不通过模拟或文档替代。

## 下一条可执行任务

Previous next-task note: implement M2 Task 3, validated user settings persistence and atomic recovery; that task is now complete. Keep the second-display/DPI matrix explicitly deferred.

## M2 hardening handoff (2026-10-05)

本轮补齐并验证了此前审查指出的 M2 缺口。Core 的 `PetEngine` 现在用单调时间戳计算受上限保护的 delta；窗口表面选择使用投影后的下一步位置；从实际窗口顶边走出会保持行走水平速度进入坠落；显示表面失效时会按当前可用工作区安全迁移。`WindowSurfaceResolver` 仍只读枚举窗口矩形，保守排除被更高窗口遮挡的顶边，不移动或关闭用户窗口。

管理器新增“允许宠物落到可见窗口顶部”真实开关，默认关闭并写入 `SettingsDocument.WindowLandingEnabled`；关闭管理器时隐藏而不销毁，托盘可重新打开，退出时由宿主统一释放。绘制缓存按角色定义引用计数，最后一个窗口释放后清理该定义的 Bitmap 和缓存项。窗口物理刷新使用单调时钟而不是固定 16ms 假定。

## M2 当前证据

**代码已实现**：角色刷新保留旧实例定义；共享定义、RGBA 与平台 Bitmap 生命周期分层；显示工作区/窗口顶部选择、管理器开关、设置恢复、实例清空和退出清理路径已实现。

**构建通过**：`scripts/build-yimywalk.ps1` 于 2026-10-05 通过，Core 测试、App x64 Release 编译和 publish 均为 0 警告/错误，发布目录为 `src/YimyWalk.App/bin/x64/Release/net8.0-windows/win-x64/publish/`。

**核心测试通过**：`dotnet test YimyWalk.sln --configuration Release --no-restore`，67/67 通过。新增覆盖上方生成不跳顶、投影坠落、走出窗口支撑、显示表面失效迁移、遮挡候选过滤和设置开关默认值。

**Windows 桌面已验证**：

- `scripts/desktop-input-smoke.ps1` 在解锁的 Windows 11 Home 10.0.26200、x64、DPI 120、Intel Core i5-12500H、单 `DISPLAY1`（1920×1080，工作区 1920×1020，窗口 DPI scale 1.25）通过真实掉落、Walk、Jump/Apex/Fall、透明穿透、实体命中、拖拽/抛掷、捕获丢失、焦点保持、托盘隐藏/显示/清空/退出、角色刷新成功/失败保留、管理器刷新按钮、落脚开关和关闭/重新打开管理器。
- `scripts/desktop-smoke.ps1 -Landing -Rounds 1 -Role slime` 在另一实际 DPI 会话（单 `DISPLAY1` 1536×864，工作区 1536×816，scale 1）通过真实 probe 窗口顶部 Y=300 落脚，关闭 probe 后继续坠落（Y=322.2），并通过 slime sprite-only 角色以及 1/10/50/100 创建/清空。100 只 CREATE 记录 WS 80,084,992、PRIVATE 23,703,552、HANDLES 375、USER 430、GDI 630；CLEAR 后 CACHE=0、USER 31、GDI 30；Host Dispose 后 WS 79,552,512、PRIVATE 23,613,440、HANDLES 365、USER 12、GDI 24。

**Windows 桌面未验证**：第二显示器、负坐标跨屏拖动、每显示器 DPI 元数据、显示器断开/重连和更长时间 CPU/内存曲线仍未验证。`WindowsDisplayAdapter` 当前的 `DisplayInfo.Scale` 是宿主窗口/系统 DPI 快照，不能解释为每个显示器已实测；单窗口 `GetDpiForWindow` 只用于窗口渲染缩放。100 只是压力基线，不是永久上限或流畅性承诺。

## 下一条可执行任务（M3）

执行 `docs/superpowers/plans/2026-10-05-m2-hardening-and-m3.md` 的 M3.1：在不引入窗口或桌面 API 的 Core 中实现 hunger/boredom 独立实例数值、增长率和人工喂食闭环；随后再做受限气泡和虚拟食物。外部应用反馈、真实快捷方式操作和第二显示器继续保持后续/暂缓状态。

## M2 Task 3 handoff (2026-10-04)

Task 3 is implemented in the working tree. `SettingsDocument` is a validated schema-v1 Core record; `SettingsStore` writes in the user-local `YimyWalk` directory through `settings.json.tmp`, flushes before replacement, keeps `settings.json.bak`, and reports whether defaults or the backup were used. The store never writes to the install directory and startup registration remains disabled.

The host loads settings before role selection, persists the selected role on manager changes and clean exit, applies the validated global scale to new pet windows, and shows settings recovery/save status in the manager. Preferred display is retained as validated data and is intentionally not applied until the display adapter task.

Evidence: `dotnet test YimyWalk.sln --configuration Release --no-restore` passed 53/53 Core tests; `scripts/build-yimywalk.ps1` passed with a publish output; `scripts/desktop-input-smoke.ps1` passed transparent hit-through, entity hit, drag/toss, focus, tray lifecycle, and manager reload entry; `scripts/desktop-smoke.ps1 -Rounds 1 -Role slime` passed settings default/save/backup recovery, reload success/failure retention, and real 1/10/50/100 create/clear. The current single-display Windows baseline reported final active instances/windows at zero and USER/GDI back at 10/24 after host disposal.

Status distinctions: code implemented; build passed; Core tests passed; Windows desktop settings and lifecycle smoke passed. The second-display/DPI matrix remains deferred/unverified (`DISPLAY1` only), and window-top landing is still unimplemented. Next executable task: M2 Task 4 display/work-area contracts and DPI-safe positioning.

## M2 Task 4 handoff (2026-10-04)

Task 4 display contracts and the Windows adapter are implemented. Core now owns immutable `DisplayId`, `DisplayInfo`, `IDisplayProvider`, and `DisplaySnapshot` values plus point-to-display resolution; `IWorldSurface` keeps a single-display fallback while allowing physics to use the working area for the display containing each pet. Negative coordinates remain signed throughout the Core geometry and App window positioning path.

The App adapter reads `Screen.AllScreens`, bounds, working areas, and device scale. It uses `GetDpiForWindow` when a host window handle exists, falls back to `GetDpiForSystem`, and returns a deterministic 1280x720 primary fallback if monitor enumeration fails. Snapshots are refreshed at most once per second during normal simulation. Desktop smoke output now records display ID, bounds, working area, scale, and primary status.

Evidence: `DisplayContractTests` and the full Core suite pass 56/56; `scripts/build-yimywalk.ps1` passes; `scripts/desktop-smoke.ps1 -Rounds 1 -Role cat` and `scripts/desktop-input-smoke.ps1` pass with display snapshots, fall/walk/jump, transparent/entity hit tests, drag/toss, focus, tray lifecycle, role reload, and final cleanup. The publish smoke saw one `DISPLAY1` at 1536x864 with a 1536x816 work area and scale 1; the input smoke saw one active `DISPLAY1` at 1920x1080 with a 1920x1020 work area and per-window scale 1.25. These are separate real sessions, so both values are recorded rather than merged.

Status distinctions: display contracts and adapter code are implemented; build and Core tests pass; single-display Windows desktop behavior is verified. Negative-coordinate and cross-display behavior is Core-tested but Windows desktop unverified because this session exposes no second display. Window-top landing remains unimplemented. Next executable task: M2 Task 5 conservative window-surface landing with floor fallback.

## M2 Task 5 handoff (2026-10-04)

Task 5 is implemented. Core now has `WindowSurfaceCandidate`, `SurfaceSelection`, and `SurfaceSelector`; the selector chooses the nearest visible top edge crossed during descent and falls back to the display working-area floor. The App layer owns `WindowSurfaceResolver`, which refreshes top-level window rectangles at 250 ms or slower, excludes the host and manager windows, ignores desktop shell/system overlays and impractically small helpers, and never moves or closes user windows. Landing is opt-in and remains disabled for the normal M1 path.

Evidence: `SurfaceSelectionTests` and the full Core suite pass 59/59; `scripts/build-yimywalk.ps1` passes; `scripts/desktop-smoke.ps1 -Landing` passed on the unlocked Windows desktop with a real probe window (pet landed at top 300, then re-fell after probe close); `scripts/desktop-smoke.ps1 -Rounds 1 -Role cat` passed real 1/10/50/100 creation and clear with active windows returning to zero; `scripts/desktop-input-smoke.ps1` passed fall, walk, jump, transparent/entity hit testing, drag/toss, focus, tray, reload, and cleanup. The landing harness was placed at an unused right-side region and did not move or close existing user windows.

Status distinctions: Task 5 code is implemented, the publish build passes, Core tests pass, and single-display Windows landing is verified. The second-display/DPI matrix remains deferred and Windows-unverified because this session exposes only `DISPLAY1`; no M2 claim treats that matrix as passed. Next executable task: maintain the M2 baseline and add a second-display/DPI desktop run when such hardware is available.

说明：文档中较早的 M2 Task 3–5 handoff 保留为历史交接记录；本节的 2026-10-05 hardening handoff 是当前事实来源，已覆盖其中“窗口顶部落脚未实现”等过时描述。

## M3.1 handoff (2026-10-05)

**代码已实现**：`CharacterDefaults.HungerIncreasePerMinute` 现在真正保存在角色定义中，sprite-only 角色使用 0.01/分钟默认值；`PetInstance` 为每只宠物独立保存 Hunger、Boredom 和短暂 Eat 状态；`PetEngine` 使用受限 delta 更新数值，`PetSession` 提供 hunger 开关、0–4 倍增长率和 `FeedAll`。拖拽、跳跃、坠落或已有 Eat 状态时拒绝喂食，缺少专用动作图时继续使用基础帧。管理器提供真实的“启用饥饿增长”和“喂食全部”入口，设置会保存。

**构建与核心测试通过**：`scripts/build-yimywalk.ps1` 通过；`dotnet test YimyWalk.sln --configuration Release --no-restore` 通过 71/71。`PetNeedsTests` 覆盖默认值、配置加载、实例隔离、边界和开关。

**Windows 桌面已验证**：输入 smoke 真实点击并验证饥饿开关、喂食全部进入 Eat、管理器关闭/重开；批量 smoke 以 slime sprite-only 角色重新完成 1/10/50/100 创建和清空，100 CREATE 约 WS 78,209,024、PRIVATE 23,691,264、HANDLES 350、USER 428、GDI 632，CLEAR 后 ACTIVE/WINDOWS=0、CACHE=0、USER 29、GDI 32。

**范围边界（当时交接）**：气泡限流、虚拟食物目标和外部应用事件尚未实现；后续 M3.2/M3.3 已分别补齐前两项，第二显示器/DPI 真实矩阵仍暂缓。

## M3.2 handoff (2026-10-05)

**代码已实现**：Core `DialogueScheduler` 处理角色台词组回退、单宠物冷却、全局同时显示上限、过期和清空；App 的 `BubbleWindow` 使用非激活、工具窗口和 `HTTRANSPARENT`，由宿主随创建/点击/拖拽/喂食事件发布并在宠物删除、清空、关闭气泡或退出时释放。管理器新增“启用自动气泡”开关，默认开启并持久化。

**构建与核心测试通过**：`scripts/build-yimywalk.ps1` 通过；Core 74/74 通过。`DialogueSchedulerTests` 覆盖缺失组回退、冷却、全局上限、过期和清空。

**Windows 桌面已验证**：输入 smoke 真实观察创建气泡，切换自动气泡开关，且透明区域/实体命中、拖拽、焦点、托盘和管理器生命周期仍通过；批量 smoke 在加入气泡后重新完成 1/10/50/100 创建/清空。100 只 CREATE 约 WS 79,818,752、PRIVATE 24,182,784、HANDLES 350、USER 443、GDI 651；CLEAR 后 ACTIVE/WINDOWS=0、CACHE=0、USER 29、GDI 33。

**范围边界**：外部应用事件尚未实现；第二显示器/DPI 真实矩阵仍暂缓，不能标记为 Windows 桌面通过。

## M3.3 handoff (2026-10-05)

**代码已实现**：Core `VirtualFoodSystem` 只管理程序生成目标，限制目标数、单宠认领和 8 秒过期；中断移动会释放认领，接近后进入 Consumed、缩小并喂食实例。App 的 `FoodWindow` 只绘制小型非激活目标，管理器提供“生成虚拟食物”，一键清空和退出同时清理目标与窗口。

**构建与核心测试通过**：`scripts/build-yimywalk.ps1` 通过；Core 77/77 通过。`VirtualFoodTests` 覆盖单一认领、消费缩小、打断释放、目标上限和无使用者超时。

**Windows 桌面已验证**：输入 smoke 真实生成虚拟食物，创建宠物后完成认领/消费并进入 Eat，随后清空目标；透明穿透、实体命中、拖拽、焦点、气泡和托盘生命周期仍通过。批量 smoke 在虚拟食物代码加入后重新完成 slime 1/10/50/100 创建/清空。

**范围边界（M3.3 交接时）**：真实快捷方式操作仍属于后续阶段，外部应用事件尚未实现；随后已补上 M3.4 的可选轮询代码，但真实事件矩阵仍未验证；第二显示器/DPI 真实矩阵仍暂缓。

## M3.4 handoff (2026-10-05)

**代码已实现**：应用切换反馈设置默认关闭；启用后 AppHost 每 500ms 只读取前台窗口所属进程 ID，排除自身进程并在进程切换时复用 `program_opened` 台词组。没有读取窗口标题、路径、文档正文、键盘输入或浏览历史；关闭开关后停止轮询。

**构建与核心测试通过**：`scripts/build-yimywalk.ps1` 通过；Core 77/77 通过，设置 round-trip 覆盖默认关闭和启用保存。

**Windows 桌面已验证**：输入 smoke 真实切换“启用应用切换反馈”开关，并启动可关闭的 Notepad 临时进程验证前台进程切换产生台词气泡；测试只使用进程 ID，结束后关闭该进程，未读取标题、路径或文件内容。其他 M3 输入、气泡、虚拟食物和生命周期检查通过。

**Windows 桌面未验证**：更长时间、多进程快速切换和权限受限会话仍未验证；第二显示器/DPI 仍暂缓。

## 下一条可执行任务

执行 M3.5 回归交接：重复完整核心/构建/输入/批量 smoke，整理资源和权限边界；外部事件的基础单进程切换 smoke 已通过，更长时间与权限矩阵继续保留未验证。

## M3.5 regression handoff (2026-10-05)

**代码已实现**：M3.1 饥饿/无聊/人工喂食、M3.2 限流气泡、M3.3 虚拟食物、M3.4 可选前台进程反馈均已保留在 Core/App 分层；清空、角色刷新和退出路径覆盖了新增事件、食物和气泡资源。

**构建与核心测试通过**：`dotnet test YimyWalk.sln -c Release --no-restore` 77/77；`scripts/build-yimywalk.ps1` 通过并生成 x64 self-contained publish；`git diff --check` 无内容错误（仅换行格式提示）。

**Windows 桌面已验证**：在本轮解锁桌面上，`scripts/desktop-input-smoke.ps1` 通过真实掉落/行走/跳跃/坠落、透明穿透/实体命中、拖拽抛掷、焦点、托盘、角色刷新、管理器开关、创建气泡、虚拟食物认领消费进入 Eat、Notepad 进程切换反馈和退出；`scripts/desktop-smoke.ps1 -Rounds 1 -Role slime` 通过 sprite-only slime、1/10/50/100 创建清空、CACHE=0 和资源计数回落。最近一轮 100 只 CREATE 为 WS 80,900,096、PRIVATE 25,075,712、HANDLES 350、USER 443、GDI 651；CLEAR 为 ACTIVE/WINDOWS=0、CACHE=0、USER 29、GDI 33；Host Dispose 后 USER 10、GDI 27。

**Windows 桌面未验证**：第二显示器/负坐标跨屏/DPI 切换/断开重连、长时间 CPU/内存曲线、权限受限外部事件和更高实例压力仍未验证；100 仍只是压力档位，不是永久上限或流畅性承诺。真实快捷方式读写、移动、隐藏和恢复没有实现，也没有触碰用户桌面文件。

## 下一阶段

已写入 `docs/superpowers/plans/2026-10-05-m4-stabilization.md`。下一条可执行任务是 M4.1：整理角色作者材料、校验入口和发布包自检；随后再处理真实验收中发现的稳定性问题。第二显示器和真实快捷方式继续作为单独的未验证/后续项目。

## M4.1 UI/托盘修复交接（2026-10-06）

**真实原因**：连续删除异常已取得堆栈，根因是 `PetsPage.ApplyProjectionToList` 在 `ListView.Items.Remove` 触发同步选择变化后重入，外层继续操作陈旧内部索引；布局测量显示原 Dock 左侧栏与 Fill 内容都从 `X=0` 开始，内容宿主实际侵入侧栏。状态不更新的直接原因是管理页只有实例/队列事件刷新，没有可见窗口的周期采样。托盘旧实现由 `ApplicationContext.MainForm` 和 FormClosing 最小化绑定，且 NotifyIcon 只处理双击，不符合新入口要求。

**代码已实现**：根布局改为 DPI 缩放两列；品牌移至底部，移除侧栏退出按钮；`PetsPage` 以复制快照和 200ms Timer 增量更新，按稳定 ID 恢复选择/滚动并抑制绑定重入；详情显示运行状态、饥饿和位置。托盘左键打开，管理器启动隐藏，×/Alt+F4 隐藏到托盘，右键有“打开主菜单/退出 YimyWalk”；退出仍走统一清理路径，清空先取消待创建队列。

**验证**：`ManagerUiSmoke` 构建通过并实测 120 DPI 默认环境五页内容宿主均从 `X=200` 开始；混合 cat/slime 30 实例按头/中/尾删除，目标 ID 和核心/列表数量一致，未再出现异常；可见详情 200ms 采样在无输入时发生变化。Core/应用构建和测试待本轮最终串行执行。真实通知区域左键、Alt+F4、100 实例可见列表压力、100/125/150/200 DPI 及最大化/最小尺寸尚未完成，均保持“Windows 桌面未验证”。

**操作**：启动发布的 `YimyWalk.exe` 后只留托盘；左键托盘图标打开主菜单；窗口 × 或 Alt+F4 隐藏到托盘；右键托盘选择“退出 YimyWalk”彻底结束进程。

## M4.1 最终验证记录（2026-10-06）

- **代码已实现**：根两列布局、底部品牌区、200ms 可见窗口采样、稳定 ID 增量行更新、删除重入保护、托盘左键打开/右键退出、启动隐藏和 × 隐藏到托盘均已写入源码。`tools/ManagerUiSmoke` 是可重复的 WinForms 集成回归，包含五页几何、20 次打开/隐藏、队列清空、无输入状态变化和 30 只混合角色头/中/尾删除。
- **构建通过**：`dotnet build src/YimyWalk.App/YimyWalk.App.csproj -c Release -p:Platform=x64`；`dotnet test YimyWalk.sln --configuration Release --no-restore` 101/101；ManagerUiSmoke 构建通过。
- **逻辑/集成测试通过**：ManagerUiSmoke 在当前 Windows 会话 DPI 120 下报告五页内容宿主 `X=250`、队列清空后活动/待创建均为 0、约 200ms 采样更新详情、删除目标 ID 与列表/Core 数量一致，无 InvalidArgument 异常。生成的检查图为 `artifacts/ui-smoke/overview.png` 和 `artifacts/ui-smoke/pets.png`。
- **Windows 桌面已验证**：更新后的输入 smoke 完成启动隐藏、托盘菜单打开、管理器 × 隐藏后重开、角色刷新、设置、虚拟食物和退出清理；托盘菜单项通过真实 `NotifyIcon.ContextMenuStrip` 菜单路径调用。当前会话有第二显示器（DISPLAY1/2）且 DPI 120，未进行 100/125/150/200 切换矩阵。
- **Windows 桌面未验证**：通知区域图标本身的物理左键、Alt+F4、最小尺寸/最大化还原和 100/125/150/200 DPI 组合未逐项完成；输入 smoke 的透明穿透检查受当前前台桌面窗口阻挡而返回 exit code 2，不能把它记为全部桌面验收通过。100 实例可见列表刷新压力和长时间资源曲线仍未验证。

## M4.2 AntdUI 管理界面重构（2026-10-06）

**真实原因与修改**：角色页双标题来自 ManagerForm 宿主标题与页面标题同时绘制；页面裁切来自 AntdUI.Panel 不向外层 TableLayoutPanel 提供子控件首选高度，导致卡片高度为 0 或固定内容落在可视区域之外。`UiTheme` 现在集中管理 AntdUI 2.4.12、颜色、字体、圆角和控件尺寸；五页改用 AntdUI Button、Switch、Select、InputNumber、Panel，并保留原生布局与滚动容器。卡片按实际内容设置可审阅高度，长说明换行，桌宠详情保留滚动，角色预览使用缓存的 idle1/index 0 单帧。总览增加“当前默认角色 + 数量 Select + 创建桌宠”组合，退出入口仍只在托盘菜单。

**代码已实现**：`YimyWalk.App.csproj` 固定 AntdUI 2.4.12；`UiTheme.cs` 提供主题和控件工厂；`ManagerPage.cs` 保持单页标题与纵向滚动；`OverviewPage`、`PetsPage`、`CharactersPage`、`InteractionsPage`、`SettingsPage` 完成控件和布局重构。`ManagerUiSmoke` 增加关键文本可见性、父级裁切、滚动末尾、AntdUI 控件存在性和角色预览尺寸断言；`DesktopInputSmoke` 已适配 Select/Switch/InputNumber，并继续验证真实 AppHost 入口。

**构建与逻辑测试**：应用、ManagerUiSmoke、DesktopInputSmoke 均以 x64 Release 构建通过；`dotnet test YimyWalk.sln --configuration Release --no-restore` 通过 101/101；ManagerUiSmoke 在当前 120 DPI 会话中通过五页截图、布局断言、200ms 状态刷新、创建队列清空和 30 只混合 cat/slime 稳定 ID 删除回归。

**Windows 桌面已验证**：更新后的 `scripts/desktop-input-smoke.ps1` 通过真实管理窗口生命周期、托盘菜单打开/隐藏/重开/退出、角色浏览与显式设为默认、AntdUI 设置/互动控件、idle1 预览、气泡、虚拟食物和资源清理。当前会话报告 DISPLAY1/2 均为 DPI 120（125%）。真实截图来自 `artifacts/ui-smoke/antd-overview.png`、`antd-pets.png`、`antd-characters.png`、`antd-interactions.png`、`antd-settings.png`。

**尚未验证**：100%、150%、200% DPI 切换；最小支持尺寸、最大化还原和真实通知区域左键/Alt+F4 的逐项桌面矩阵；100 实例下管理列表的长时间可见刷新压力；桌面审美验收。截图/布局 smoke 不替代这些 Windows 交互验收。

## M4.2 发布验证补充（2026-10-06）

- `scripts/build-yimywalk.ps1 -Version 0.1.8` 完成 restore、101/101 Core 测试和 x64 self-contained 发布；`scripts/test-release-package.ps1 -Path src/YimyWalk.App/bin/x64/Release/net8.0-windows/win-x64/publish -Version 0.1.8` 返回 `PACKAGE PASS VERSION=0.1.8 FILES=12 CHECKSUMS=11`。
- 发布入口：`src/YimyWalk.App/bin/x64/Release/net8.0-windows/win-x64/publish/YimyWalk.exe`，文件版本 `0.1.8.0`。发布目录已带 AntdUI、角色资源、第三方声明和校验清单。
- 五张布局图由本次 Release UI smoke 生成：`artifacts/ui-smoke/antd-overview.png`、`antd-pets.png`、`antd-characters.png`、`antd-interactions.png`、`antd-settings.png`。它们验证了实际 WinForms 控件布局；未将其当作通知区域左键、Alt+F4 或其他未执行的桌面矩阵验收。

- 角色页最终采用稳定角色 ID 的 `ListView` 列表（名称、ID、有效状态、默认标记）与右侧 idle1 详情，而不是下拉选择；查看行不会改变创建默认，只有“设为默认角色”按钮会调用 `AppHost.SetSelectedRole`。
- 角色列表和桌宠实例列表同时存在于缓存页面时，smoke 按当前可见列表选择，避免测试工具把两类列表混淆；本轮 DesktopInputSmoke 已重新通过。
- 全局和应用级布尔项的 AntdUI Switch 旁显示“开启/关闭”文字状态，且状态标签由控件事件同步；这保证状态不只依赖颜色或滑块位置。
# 2026-10-07 窗口蓄力跳跃修正（当前交接）

## 100% 窗口跳跃低跳问题（2026-10-07）

**根因**：`WindowJumpDesire=100%` 只保证进入窗口目标判定，不保证任意高度都能被选择。上一版把窗口轨迹和普通蓄力跳共用 `1.75x` 垂直速度上限；默认 `JumpSpeed=450、Gravity=1500` 时最高上升约 `206.7px`。底部到常见窗口顶部的高度差超过该值时，`JumpTargetPlanner` 正确判定不可达，代码按既有回退规则执行一次普通低跳，所以用户看到的是原地低跳而不是蓄力轨迹。

**修正**：普通关注跳仍保持 `1.75x` 上限；窗口目标使用文档化的、有限的 `6x` 目标专用上限，并继续受真实水平距离、下降交会时间、缩放后帧边距和落脚区间约束。窗口目标蓄力仍播放第七帧并冻结位置，起跳后按统一弹道积分平滑飞行；在窗口上再次尝试时排除当前支撑窗口，避免 100% 欲望反复跳回原窗口。弹道预测和 Tick 积分现使用同一加速度/终端速度分段公式。

**可重复验证**：Core `148/148` 通过，覆盖 1080/1740 像素屏幕高度、16/33/100ms Tick、窗口专用速度上限、水平速度上限、当前支撑面排除、目标失效后继续普通物理和旧有物理回归。

**桌面验证边界**：本次尚未在真实桌面上重新观察“100% 窗口欲望从底部蓄力跃迁到任意真实窗口顶部”的视觉路径；以下旧有托盘、拖拽、状态刷新、资源释放和显示器 smoke 仍作为回归依据，不能替代该项人工观察。新的发布包为 `artifacts/publish/charged-window-jump-fix`，版本 `0.1.11`；`scripts/test-release-package.ps1` 通过，11 个文件/10 条 SHA-256 清单，角色清单只有 `slime`。
# 2026-10-07 主动运动与预览（实施中）

用户已确认总体方案和七项补充规则；当前任务为 M4 改进。详见 `superpowers/specs/2026-10-07-yimywalk-autonomous-movement-design.md` 的最终补充规则和 `superpowers/plans/2026-10-07-autonomous-movement.md`。全局欲望位于互动页，实例覆盖位于桌宠页；窗口跳跃受真实弹道/限速约束，顶部释放保留窗口落脚；cat 清理仅限内置包和新发布产物。Core/UI 实现及可重复验证已完成；主动跳跃、侧边攀爬、顶部行走等新增桌面路径仍按下文标记为未验证。

## 2026-10-07 主动运动与预览实施交接

**代码与资源**：默认新安装配置使用 `slime`；仓库内置 `Characters/cat` 已移出，旧猫咪仅保留在 `tests/Fixtures/Characters/cat` 供多角色回归。优化后的史莱姆 PNG 已通过 `CharacterValidator`（1254×1254、单格 418×418、RGBA、9 帧），发布包来源为用户提供的已优化发布素材，SHA-256 为 `BC92811F4B7149AE948A07A16E1A72662E175EC6DCB5BBC398B4138993527AF1`。测试 fixture 中旧 cat SHA-256 为 `22C9F2469645A34A64FB8CA8C63ABAA0A3D984191701BC11F5B984E8A21E94BD`。

**主动行为**：Core 已实现每只实例独立的跳跃 8–16 秒等待、攀爬 10–20 秒等待、失败重置、暂停冻结、边缘 12px 去重、右侧登顶向左/左侧登顶向右、顶部端点普通 Fall、显示器/缩放变化解除攀附，以及带水平速度、下降交会和有效落脚区间检查的窗口目标规划。失效或移动目标继续当前物理，不瞬移、不吸附、不突破速度上限。欲望 0 只禁止新行为；拖拽、Eat、空中、失去支撑和暂停优先级保持不变。

**验证**：`dotnet test YimyWalk.sln -c Release --no-restore` 通过 **123/123**；App x64 Release 构建 0 警告/0 错误。`ManagerUiSmoke` 完整回归通过，包含清空队列、实例大小、全局欲望 35%/65% 映射、单只 70%/80% 覆盖与重置、200ms 状态刷新、托盘和混合角色删除；`--preview-pixels` 通过方形/宽图/高图、不同源尺寸与 96/120/144/192 控件尺寸的实际像素检查；baseline 页面布局 smoke 通过。预览只显示 idle1/index 0，等比缩放不会改变宠物实际大小。发布构建 `0.1.9` 与 `scripts/test-release-package.ps1` 通过（11 个文件、10 条 SHA-256 清单，角色清单只有 `slime`）。

**桌面验证边界**：本轮 Windows smoke 实测了双显示器快照、窗口顶部落脚/关闭后普通下落和角色刷新保留；批量 1/10 已输出并通过，50/100 批量阶段因大尺寸多窗口耗时过长而中止，因此本轮不把 50/100 重新标记为通过。主动跳跃、侧边攀爬、顶部行走端点、目标移动/失效、暂停恢复和真实 DPI/显示器拓扑变化尚未在真实桌面上逐项验证；Core/UI 可重复测试不能替代这些桌面验证。既有删除、状态刷新、托盘、拖拽和资源释放回归仍保留在测试工具中。
补充：`DesktopInputSmoke` 在当前双显示器会话中完整通过（透明/实体命中、拖拽抛掷、托盘隐藏/显示/清空、角色刷新成功/失败保留、设置开关、虚拟食物、外部事件和退出释放）；`desktop-display-smoke.ps1 -InventoryOnly` 通过双显示器 PerMonitorV2/DPI 元数据检查。该结果仍不等于主动跳跃、攀爬和显示器变化运行路径已实测。
补充验证：`desktop-display-smoke.ps1 -Rounds 1` 在当前双显示器会话通过，结果为 `CHECKS=79 FAILURES=0`，覆盖两台显示器的 DPI/缩放、slime 与 fixture cat 的真实渲染/跳跃/拖拽、跨屏拖拽、资源轮次和清理。该 smoke 明确仍将物理断开重连、睡眠恢复、任务栏配置变化和负坐标布局列为未验证；它也不覆盖主动攀爬路径。

## 2026-10-07 autonomous movement correction handoff

The approved correction is implemented. Edge climbing now renders the static `Apex` frame (frame 7); top walking keeps the direction-specific walk frame. A new `WindowJumpDesire` is available as a global interaction setting and as a per-pet inherit/override/reset setting. Values are entered as percentages and normalized to `[0,1]`; invalid or non-finite values fall back safely. `0%` prevents new window-jump attempts without interrupting an existing motion, while `100%` prioritizes a reachable visible-window target on every eligible attempt and otherwise uses the ordinary jump path. Existing activity, pause, drag, landing, and speed-limit priorities remain in force.

Autonomous attempt timers use an initial randomized delay and an 8-16 second retry interval for jumps; failed probability checks consume the current attempt and schedule the next one. Climb attempts use their own 10-20 second interval with edge/display de-duplication. Pause/resume does not replay overdue actions. Window target planning checks horizontal speed/distance, descending intersection time, and the valid landing interval; moving or invalid targets fall back to ordinary physics without snapping or exceeding speed limits.

Verification completed:
- `dotnet test YimyWalk.sln -c Release --no-restore`: 131/131.
- App x64 Release build: 0 warnings, 0 errors.
- `ManagerUiSmoke`: global 35/65/55% mapping, per-pet 70/80/90% override/reset, state refresh, tray/list regressions passed.
- `ManagerUiSmoke --preview-pixels`: square, wide, tall frames at 96/120/144/192 control sizes passed; this checks rendered pixels and aspect ratio, not only `Zoom`.
- `validate-character.ps1 -Path Characters/slime`: optimized 1254x1254 RGBA sprite, 9 frames, passed.
- Release package `artifacts/publish/window-jump-corrections`, version 0.1.9: package test passed; only `slime` is shipped. Sprite SHA-256: `BC92811F4B7149AE948A07A16E1A72662E175EC6DCB5BBC398B4138993527AF1`.
- `scripts/desktop-input-smoke.ps1` passed tray, drag/toss, focus, resource cleanup, and settings regressions. `scripts/desktop-display-smoke.ps1 -Rounds 1` passed 79 checks across two displays (DPI 120 and 96), per-display scale, rendering, and cross-screen drag; physical disconnect/reconnect, sleep/resume, and taskbar topology remain unverified.

The repository sample and release output contain only `slime`; the `cat` test fixture remains under `tests/Fixtures/Characters/cat` for multi-role regression coverage. Real desktop observation of proactive window jumping, Apex edge climbing, top walking endpoints, target movement/invalidations, and pause recovery remains unverified. The physical disconnect/reconnect, sleep/resume, and taskbar topology cases in the full DPI/display-change matrix also remain unverified; passed two-display smoke checks must not be inferred to cover them.
## 2026-10-07 charged jump trajectories

The approved charged-jump correction is implemented in Core. Ordinary autonomous jump attempts now make one mode decision after the existing retry gate: they either launch immediately at the role's base jump speed or enter `JumpCharge` for a sampled `0.15–0.45 s` interval. While charging, the pet remains stationary, retains its horizontal velocity, and renders the seventh (`FrameKind.Apex`) frame. The sampled duration maps to a finite launch speed capped at `1.75x` the role's base jump speed. A zero ordinary jump desire prevents new charges and jumps without interrupting an active charge or airborne motion. Pause remains a session-level freeze and drag cancels the charge through the existing priority path.

Window-jump planning now evaluates the descending intersection, scaled-frame landing margins, horizontal distance, and the bounded launch-speed range. A reachable candidate above the base apex enters a planned charge with the required launch speed, up to the explicit `6x` window-only cap; ordinary jumps remain capped at `1.75x`, and horizontal speed remains bounded by the existing walking-speed cap. The arc is integrated by the existing Core physics path. Candidate movement or disappearance does not snap, attach, or replan the active pet; normal surface selection continues to decide any later landing.

Repeatable verification:

- `dotnet test YimyWalk.sln -c Release --no-restore`: **140/140** Core tests passed, including charge-frame/stationary behavior, randomized duration mapping, zero-desire and pause rules, high-window reachability, speed/landing bounds, and target invalidation fallback.
- `dotnet build src/YimyWalk.App/YimyWalk.App.csproj -c Release -r win-x64 --no-restore`: **0 warnings, 0 errors**.
- `ManagerUiSmoke --preview-pixels`: preview pixel/aspect checks passed at 96/120/144/192 control sizes; existing settings, tray, list refresh, drag, and resource-release regressions remain covered.
- `scripts/desktop-input-smoke.ps1`: **PASS**.
- `scripts/desktop-display-smoke.ps1 -Rounds 1`: **PASS**, `CHECKS=79 FAILURES=0`; the run also covered the existing cat fixture and shipped slime at both displays/DPI values.

The new charged ordinary-jump and high visible-window trajectory paths have not been visually observed in a live desktop session yet. The latest display smoke confirms the existing manual upward jump path and rendering, but it is not evidence of autonomous charge timing or high-window landing. Physical display disconnect/reconnect, sleep/resume, and taskbar-topology cases remain unverified as recorded above.
- `scripts/build-yimywalk.ps1 -Version 0.1.10 -PublishPath artifacts/publish/charged-jump-trajectories` and `scripts/test-release-package.ps1`: **PACKAGE PASS**, version `0.1.10`, 11 files/10 checksums; manifest contains only `slime`. The shipped optimized slime sprite SHA-256 remains `BC92811F4B7149AE948A07A16E1A72662E175EC6DCB5BBC398B4138993527AF1`.

## 2026-10-09 UI Logo

**代码与资源**：将用户提供的透明 Logo 复制到 `src/YimyWalk.App/Resources/logo.png`，作为 `YimyWalk.App.Resources.logo.png` 嵌入资源。管理器左下角 Footer 使用 `PictureBox` 的 `Zoom` 等比缩放显示，保留透明背景，并在窗体释放时释放 Bitmap。来源文件和嵌入副本 SHA-256 均为 `E3F8A5C0CC3936C3DDC828C93523A5CE7D00DA7ECE1D2146F0ABEA526BFA1197`。

**验证**：`ManagerUiSmoke --baseline` 通过，新增断言确认 BrandLogo 有嵌入图像且完整位于 Footer（当前 Windows 会话 DPI 120）；`ManagerUiSmoke` 完整回归通过；`--preview-pixels` 通过；Core `148/148` 测试通过；App x64 Release 构建 0 警告/0 错误。截图 `artifacts/ui-smoke/antd-overview.png` 可见左下角 Logo。

**桌面验证边界**：已在当前 125% DPI 会话完成管理器布局和渲染 smoke。100/150/200% DPI、最小尺寸/最大化还原及真实桌面视觉验收仍未逐项实测；预览与 smoke 不替代这些人工验收。根目录临时上传文件 `logo.png` 未纳入提交，避免重复资源。

**后续 UI 修正（2026-10-09）**：按用户要求删除 Logo 上方的“点击托盘图标打开主菜单”提示文字，Footer 高度同步收紧，Logo 仍保持完整可见。`ManagerUiSmoke --baseline` 与完整 smoke 已确认提示不存在且 Logo 仍在 Footer 内。
