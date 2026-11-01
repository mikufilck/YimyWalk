# 2026-11-01 审查问题修复记录

基于 [项目审查](2026-11-01-project-review.md) 的 R01–R13，本轮获授权修复与执行必要测试。产品修复版本为 **0.1.14**；工作区保留此前文档改动，未提交或推送远端。需求差距表中的图标分身、额外动作、勿扰、实例存档、社交等属于后续产品扩展，不在本轮缺陷关闭范围。

## 问题处置

| ID | 处置 | 证据与边界 |
| --- | --- | --- |
| R01 | Core 的 `PetLimits` 统一最终像素上限：单边 1024、总像素 1,048,576。缓存按实际尺寸建键，BGRA + alpha 预算 64 MiB，淘汰未使用项；窗口与预览持独立 surface handle，在用资源不被回收。预算不足保留上一帧并记录诊断，创建失败回滚 | App 测试验证预算、在用引用、淘汰、Dispose、极端缩放；真实窗口批量创建与清空验证资源回落。64 MiB 是该缓存预算，不是进程 / Windows 合成器总内存承诺 |
| R02 | 所有 GDI 获取纳入 try/finally，检查零句柄 / HGDI_ERROR，检查 UpdateLayeredWindow 结果，错误传入宿主诊断 | 五个分步失败测试；真实 GetHbitmap 失败连续执行后 GDI 计数不增；正常桌面三轮清空 GDI 回到 44。没有用耗尽整机 GDI 配额测试 |
| R03 | 仅合法主配置可轮换为备份；坏主文件替换时保留原备份，备用文件写入路径同样遵守此规则。保留整份配置验证契约 | 原回归先失败、修复后通过；原子替换与强制备用路径均验证恢复→保存→再次损坏→仍可恢复；非法保存不触碰现存文件 |
| R04 | PetSession 统一推进食物与仿真，暂停时不推进；食物只提供目标 / 占用和消费，PetEngine 单独推进位置。遵守实例走动覆盖、地面可达性、原行走速度、拖拽中断和目标超时 | Core 组合回归通过；真实输入 smoke 的创建、接近、吃食、清空流程通过。修正 smoke 的远距离旧夹具，改为在一秒行程处生成宠物，并保留“必须进入 Eat”断言 |
| R05 | 同 ID 替换前释放旧所有者，旧实例持有的引用继续有效；实例创建订阅失败回滚实例与租约 | 回归覆盖新旧定义并存、清空 / Dispose 后释放以及新增失败回滚 |
| R06 | 拒读异常归一为 CharacterLoadException；目录先做枚举快照，扫描事务失败释放未提交租约，保留上一份已发布目录 | Core 注入中途失败测试、实际 Windows ACL 拒读测试通过；拒读角色保留旧版本，其他角色继续刷新。测试只修改自己创建的临时文件 ACL，并在 finally 还原 |
| R07 | DPI 的两条 P/Invoke 移入 NativeMethods；Core 无 Windows 依赖 / P/Invoke 与 App 不导出类型 / P/Invoke 集中约束纳入自动检查 | Core / App 架构测试通过，真实当前显示器 DPI 与输入测试通过；新增 App.Tests 纳入 .sln，CI 增加 Windows 适配层测试，CI 本身未远端执行 |
| R08 | 食物与气泡使用无激活、整窗透明输入样式，保留命中消息处理；食物的普通绘制继续可见 | 独立进程测试背景窗实际收到穿过 FoodWindow / BubbleWindow 的鼠标点击，底层焦点保持，输入 smoke 返回 PASS。窗口截图没有被当作点击证据 |
| R09 | 移除 ImageSharp 3.1.12，采用 StbImageNext 1.0.0 解码，PNG 输入限定格式 / 大小 / 尺寸 / chunk CRC / 解压预算，输出使用项目 PNG writer；同步许可证与发布清单 | 原 ImageSharp 3.2.0 构建要求工作区没有的许可证，未关闭授权校验。新依赖图审计无已知易受攻击包；3 个正式 PNG + 6 个透明 / 灰度 / 调色板 / Adam7 夹具与旧解码逐像素一致；旧 DLL 仅在忽略目录内用于可信样本比较，不进入产品或测试依赖图 |
| R10 | 宿主窗口排除集合包含气泡和食物，二者不作为落脚候选或遮挡体 | 真实输入 smoke 检查实际 EnumWindows 候选中的 IsExcluded=true、IsOcclusionBlocker=false；窗口落脚 / 失去支撑后重落通过 |
| R11 | 注入单调时钟，统一帧循环、拖拽测速、显示器 / 窗口 / 前台轮询；隐藏窗口跳过渲染，相同帧与位置不重绘；日志每份最多约 1 MiB、保留一份轮换、同类错误限频。故障实例隔离，批量失败取消，帧循环故障暂停而非反复弹窗 | 日志边界与隐私测试、创建失败隔离、当前桌面生命周期通过。日志只记录稳定操作码 / 异常类型；输入 smoke 移除其他应用窗口标题采集。墙钟只用于测试环境时间标签，不推进生产物理 |
| R12 | 行走上限统一为角色契约的 2000 px/s；无聊在高值时有界缩短 Idle，继续遵守走动门控；版本统一由 Directory.Build.props 的 0.1.14 提供 | 最大行走速度、无聊门控与参数有限值回归通过；无参数 publish / 验包使用同一版本。HardLimit 与 WarningLimit 保留兼容，不机械删除公开 API / 已序列化字段 |
| R13 | AppHost 构造时注入测试设置目录，DesktopSmoke / DesktopInputSmoke / ManagerUiSmoke 均独立存储；完整宿主 teardown 放入 finally。新增 HostIsolationTests | 成功、保存、注入创建失败与错误日志均落在临时目录；测试不读取原用户偏好来决定前置状态。输入 smoke 的未验证分支不再同时打印 RESULT PASS |

## 额外修正

- 当前 150% DPI 下详情说明会横向溢出：按可用宽度约束换行，保留原布局断言，修正后 ManagerUiSmoke 基础、完整和像素回归通过。
- 批量队列改存压缩请求，单次大数量不再预分配同等数量的委托；对请求段数与计数溢出设边界，取消操作不创建未执行实例。
- 补齐 CharacterCatalogTests 与 ResourceLifecycleTests 的临时目录 / 目录所有者清理，保留原回归断言。
- 预览帧编号从实际 surface 读取，移除被忽略的 scale 参数；像素回归继续覆盖方 / 宽 / 高图及 96/120/144/192 DPI 的 UI 渲染计算。
- DesktopSmoke 使用生产 DPI 初始化与 STA，落脚探针只调整测试自身窗口的层级；输入探针通过真实鼠标建立自己可见的背景，避免将普通应用遮挡误当产品失败。安全桌面或缺少显示器时仍保留 UNVERIFIED，不修改用户显示器配置。

## 验证记录与复现入口

本机证据目录：`artifacts/remediation-20261101/`，不提交日志、二进制或临时夹具。便携产物在该目录的 `publish/`。

| 验证 | 命令 / 证据 |
| --- | --- |
| Core + App 自动回归 | `scripts/test-yimywalk.ps1`；`tests-final.log` |
| 默认版本发布 | `scripts/publish-yimywalk.ps1 -OutputPath artifacts/remediation-20261101/publish`；`publish.log` |
| 验包 | `scripts/test-release-package.ps1 -Path artifacts/remediation-20261101/publish` |
| 依赖审计 | `dotnet list YimyWalk.sln package --vulnerable --include-transitive` |
| UI | ManagerUiSmoke 的 `--baseline`、无参数完整模式、`--preview-pixels`；对应 manager 日志 |
| 输入与跨进程被动窗 | `scripts/desktop-input-smoke.ps1`；`desktop-input-cross-process.log` 及最终输入日志 |
| 真实窗口资源 | `scripts/desktop-smoke.ps1 -Rounds 3 -Role slime -Targeted -Landing`；`desktop-stress-after.log`，以及缓存字节检查记录 |
| 多屏 | `scripts/desktop-display-smoke.ps1`；当前只有一块扩展显示器可用，返回 `UNVERIFIED TWO_EXTENDED_DISPLAYS_REQUIRED` |

四档最终结果以 [STATUS 最新条目](../STATUS.md) 为准。已经验证的桌面范围是当前单屏 2560×1600、工作区 2560×1528、144 DPI / 150% 的会话。多屏完整入口实跑也返回 `UNVERIFIED TWO_EXTENDED_DISPLAYS_REQUIRED`（退出 2）。双屏 / 负坐标真实拓扑、物理拔插、睡眠唤醒与长时间运行没有实际条件或尚未执行，不能由代码修复和短测试替代。

## 依赖决策来源

ImageSharp [3.2.0 发布记录](https://github.com/SixLabors/ImageSharp/releases/tag/v3.2.0) 包含安全回移与授权校验变化；本轮授权缺失由本地实际构建错误确认。StbImageNext 的 [官方说明](https://github.com/StbSharp/StbImageNext) 与 NuGet 1.0.0 包元数据用于核对 API、来源提交和 `Unlicense OR MIT`；本项目分发选择 Unlicense，完整文本见 `licenses/StbImageNext-LICENSE.txt`。没有推断为所有未来漏洞风险已消除。
