# UI 功能组件审查与重构

日期：2026-11-03。审查基线：`9d057ba`，维护分支 `codex/bootstrap`，工作区开始时干净。范围为管理器五页、托盘、共享 UI 控件与相关宿主命令；保留简短欢迎语、粉白配色和可爱布局。

证据等级：S = 源码确认；R = 本轮完整 ManagerUiSmoke 实跑复现 / 回归。当前构建、测试和 Windows 桌面边界统一见 [STATUS](../STATUS.md) 最新条目，不能把 UI 自动化或截图作为完整桌面矩阵。

| ID | 发现与证据 | 处理 |
| --- | --- | --- |
| UI-01 | S/R：PetsPage 的“恢复默认”只调用 ResetPetBehaviorOverrides，ScaleOverride 不清空；同一详情区的缩放与其他设置语义不一致 | 增加 AppHost.ResetPetOverrides 组合已有重置能力；按钮同时重置缩放与全部行为，并立即刷新控件。保留原来的局部重置 API |
| UI-02 | S/R：CharactersPage.RefreshCharacters 清空列表后总选默认角色，刷新会丢失非默认预览目标 | 按角色 ID 保存选择与首行；缺失时再回退默认角色。刷新放到页头，预览下方只保留设为默认操作 |
| UI-03 | S/R：五组数值覆盖重复创建模式选择器、数值框、事件和同步逻辑 | 抽出 NumberOverrideEditor，改成“跟随全局”开关 + 数值。组件处理静默绑定、百分比换算、焦点与目标切换；页面只提交宿主命令 |
| UI-04 | S/R：单只设置与全局设置使用不同设置行；详情依靠逆序 Dock.Top 拼接，操作容易跟随长内容滚走 | 统一 SettingRows，增加可选状态说明；重排为横向身份 / 状态区、参数行、独立固定操作栏。无选中项隐藏设置及操作，喂食不可用时禁用 |
| UI-05 | S/R：两页重复列表配置、列宽计算和列表 / 详情分栏；按钮图标与移除操作样式散落在页面 | ThemedListView 统一列配置；UiLayout 统一内容栈、流式行与列表 / 详情布局；UiTheme.CreateButton 统一图标、可访问名称与移除操作样式 |

## 回归入口

构建 `tools/ManagerUiSmoke` 后运行完整模式：

- `INSTANCE_OVERRIDE_UI inheritance_target_switch_all_reset=PASS`：编辑缩放、活跃度、三种倾向和两个行为开关；切换桌宠后不串值，绑定不把全局值写成单只覆盖；点击实际恢复按钮后，宿主状态与控件同时恢复全局值。
- `ROLE_ACTIONS refresh_preview_selection_set_default=PASS`：选择非默认角色，点击实际刷新按钮后选择不变且默认角色不变；再点击设为默认，核对新默认及选择。
- 空列表隐藏旧设置及操作；按身份连续移除，核对宿主数量与列表一致。
- `--visual-review --output artifacts/ui-components-20261103` 检查五页默认 / 最小窗口，单只操作栏无须滚动即可访问。

不为可替换配色写镜像测试；本轮新增断言针对重置遗漏、预览选择丢失、绑定串值和固定操作栏的实际行为。

## 保留的边界

UiTheme 已统一单个基础控件，不另造一套按钮框架；预览仍由 CharacterFramePreview 持有 SurfaceLease。互动页的两组全局设置与列表 / 详情页职责不同，不强行套同一种布局。托盘的隐藏 / 显示保留显式命令：宿主当前不维护持久的全局可见状态，不能直接包装成声称始终准确的双态开关。Core、角色格式、角色 PNG、动画和设置 schema 不属于本次重构。
