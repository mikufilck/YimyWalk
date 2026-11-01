# YimyWalk 工程约束（Agent 入口）

> 本文件是**唯一的 Agent 入口**。目标：让任何一次新会话用最少的读取量拿到「该读什么、不能做什么、怎么跑、改完怎么交代」。

## 1. 开工顺序

1. **本文件** —— 约束与命令。
2. [`docs/INDEX.md`](docs/INDEX.md) —— **仓库索引**：先读顶部导航，按任务读对应节的代码/测试/脚本地图；不要一上来全仓库搜索。
3. [`docs/MAINTENANCE.md`](docs/MAINTENANCE.md) —— **当前维护记忆**：已核对的行为、验证摘要、下一项任务和易错点。
4. [`docs/STATUS.md`](docs/STATUS.md) —— **交接证据来源**。先读最顶部条目，再按任务定位最新相关条目；不要默认整份加载。旧记录只在原日期与场景有效。
5. [`docs/PROJECT_SPEC.md`](docs/PROJECT_SPEC.md) —— 首次接手读完整需求；后续按任务读对应章节（M0–M4 分阶段）。**不要按实现进度改写它。**
6. 需要时再读：[`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)（分层边界）、[`docs/TESTING.md`](docs/TESTING.md)（验收入口）、[`docs/CHARACTER_FORMAT.md`](docs/CHARACTER_FORMAT.md)（角色格式契约）、[`docs/UPSTREAM_AUDIT.md`](docs/UPSTREAM_AUDIT.md)（eSheep 上游审计）、[`docs/reviews/`](docs/reviews/)（带日期的审查证据）。
7. 最后再查 Git 状态与相关文件差异，保留既有未提交改动；HEAD 相同不代表工作区代码相同。

中文文档用 `Get-Content -Encoding UTF8` 读取。按索引限定目录后再用 `rg` 搜索；历史设计和评审只在需要追溯时读取。发现文档与源码不一致时，以源码确认实际行为、以实跑确认验证状态，并留下纠正记录。

## 2. 记忆分层（各司其职，别混用）

| 层 | 位置 | 放什么 | 什么时候写 |
| --- | --- | --- | --- |
| 入口/约束 | `AGENTS.md` | 不常变的规则与命令 | 规则本身变了才改 |
| 索引 | `docs/INDEX.md` | 结构地图、落点、待办入口 | 目录/项目/脚本/枢纽文件变动时 |
| 当前记忆 | `docs/MAINTENANCE.md` | 当前有效行为、验证摘要、下一项任务、易错点 | 行为/验证结论/维护优先级变化时，与 STATUS 同步 |
| 交接证据 | `docs/STATUS.md` | 带日期的交接条目（根因/修正/命令/验证/未验证边界） | **每次改行为后追加，写在最上面**；纯审查/记忆调整也登记 |
| 审查快照 | `docs/reviews/YYYY-MM-DD-*.md` | 带基线、证据等级、稳定 ID 的发现和复现步骤 | 审查时新增；修复结论写 STATUS，链接原 ID |
| 规格 | `docs/PROJECT_SPEC.md`、`docs/ARCHITECTURE.md`、`docs/CHARACTER_FORMAT.md` | 需求与契约 | 契约变化时（需求原文不改） |
| 历史 | `docs/superpowers/{specs,plans}/` | 当时的设计稿与实施计划 | 只追加，不回改 |

一份事实只指定一个详细落点，其他层用摘要和链接引用。疑似问题、旧计划和历史测试通过不得自动提升为当前事实或获准实施的方案；不要把当前验证表重复复制进索引。会话中断前把未完成任务、已跑命令与四档状态写入 STATUS，下一会话由仓库文件恢复。

## 3. 硬约束

**验证口径（最重要）**：任何交接必须把状态**分成四档**分别陈述 —— ① 代码已实现 ② 构建通过 ③ Core 测试通过 ④ Windows 桌面已验证。禁止合并成「完成」。

- **不能用文档或模拟测试冒充通过**。Core 测试、普通窗口动画、UI 截图、发布包自检都不能替代真机桌面验收。
- 未做真机验收的桌面项一律写 `Windows 桌面未验证`；环境阻塞（如安全桌面遮罩导致 `RESULT UNVERIFIED INPUT_ROUTING_BLOCKED`）如实记未验证。
- 禁止用空实现、演示按钮或伪造测试结果来声称功能完成。
- 声称数字（测试数、性能、资源趋势）前必须实际跑出来。

**架构铁律**：

- `YimyWalk.Core` **不得引用** `System.Windows.Forms`、`WindowsBase`、`PresentationCore`；不得出现 HWND、`Form`、`Bitmap`、`EnumWindows`、桌面 shell 知识。`tests/YimyWalk.Core.Tests/SmokeTests.cs` 检查程序集依赖与 P/Invoke；App.Tests 另检查 App 可见性和 P/Invoke 集中声明。
- `YimyWalk.App` 内所有类型都是 `internal`，入口只有 `Program.Main`。
- 一个宠物实例 = 一个透明分层窗口，全部窗口由**同一个 `AppHost` 进程**管理；不要为每只宠物启动进程。
- 角色定义不可变且多实例共享；可变状态（位置/速度/饥饿/动画计时）只放在 `PetInstance`。
- 时钟、随机源、显示器、世界表面一律接口注入；物理用单调时钟 + 有上限的 delta。
- Win32 P/Invoke 只放 `src/YimyWalk.App/Windows/NativeMethods.cs`。

**产品与数据边界**：

- 默认**离线**：不联网、不注册开机启动、不要管理员权限；不移动/删除/改名/修改用户的真实桌面文件。
- 角色包只含数据与媒体：不加载 DLL、脚本、Shell 命令或远程资源；解析上游 XML 时禁用外部实体与外部资源解析。
- 气泡/食物窗口不抢焦点、不遮挡正常点击。
- 100 只只是**压力基线**，不是永久上限，也不是流畅性承诺。

**仓库卫生**：

- 不提交密钥、真实桌面路径、用户文件内容或未经审计的第三方素材。
- `.tools/`（本地 SDK）、`artifacts/`、`bin/`、`obj/`、`.vs/`、`vendor/esheep/` 均已 gitignore；`scripts/verify-environment.ps1` 会在仓库里出现 `vendor/esheep` 时**故意报错**。
- eSheep 只作为外部审计来源，不再把完整上游快照提交到本仓库；审计摘要见 [`docs/UPSTREAM_AUDIT.md`](docs/UPSTREAM_AUDIT.md)。需要复核上游时，在**仓库外的临时目录**获取审计中固定的提交，不要把上游源码、素材或二进制复制回仓库。
- 未经批准不要 push 到任何远程。

## 4. 命令

```powershell
# 一次性前置：global.json 锁 SDK 8.0.425，系统 SDK 不满足时必须先装工作区本地 SDK
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/bootstrap-dotnet.ps1

# 环境自检
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-environment.ps1

# Core 与 Windows App 适配层测试（脚本内硬编码 .tools\dotnet\dotnet.exe）
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-yimywalk.ps1
.\.tools\dotnet\dotnet.exe test YimyWalk.sln --configuration Release --no-restore

# 构建 + 自包含单文件 x64 发布包
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build-yimywalk.ps1 `
  -Version 0.1.14 -PublishPath artifacts/publish/yimywalk-win-x64
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-release-package.ps1 `
  -Path artifacts/publish/yimywalk-win-x64 -Version 0.1.14

# 角色校验 + 预览图
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/validate-character.ps1 `
  -Path .\Characters\slime -PreviewPath .\artifacts\slime-preview.png
```

桌面验收只在**可见且已解锁**的 Windows 会话中执行，入口见 `scripts/desktop-smoke.ps1`、`desktop-input-smoke.ps1`、`desktop-display-smoke.ps1`（多显示器矩阵要求 ≥2 块扩展显示器，否则输出 `UNVERIFIED`）。CI（`.github/workflows/dotnet.yml`）在 Ubuntu 跑 Core、Windows 跑 Core/App 并编译 smoke，**不含任何桌面验收**。

完整命令表、参数与退出码见 [`docs/INDEX.md`](docs/INDEX.md) §6 与 §9。

## 5. 技术基线

当前目标代码使用本地 .NET 8 SDK（`.tools/dotnet/`，不提交二进制；缺失时运行 `scripts/bootstrap-dotnet.ps1`）。Windows API 适配必须与可测试的核心逻辑分层；新增功能先落到 `YimyWalk.Core` 并用假实现测试，再在 `YimyWalk.App` 接线。`docs/` 保存规格、审计、架构、角色格式、测试、状态交接与索引文档；`scripts/` 保存可复现的环境检查和构建入口。

## 6. 改完代码后

1. 跑核心测试（`scripts/test-yimywalk.ps1`）；
2. 在 [`docs/STATUS.md`](docs/STATUS.md) **最上方**追加一条带日期的条目，按四档写清验证状态与未验证边界；同步 [`docs/MAINTENANCE.md`](docs/MAINTENANCE.md) 受影响的当前结论和下一项任务；
3. 若结构/命令/入口有变，同步 [`docs/INDEX.md`](docs/INDEX.md)；
4. 若角色格式或对外契约有变，同步 [`docs/CHARACTER_FORMAT.md`](docs/CHARACTER_FORMAT.md) 与 `examples/character-template`；
5. 涉及第三方依赖或许可时同步 `THIRD_PARTY_NOTICES.md` 与 `licenses/`。
6. 按任务范围提交修改；已有用户推送授权时，直接正常推送现有维护分支并核对远端同步状态。排除本地产物与临时数据，不混入其他未知改动，不修改全局 Git 身份。
