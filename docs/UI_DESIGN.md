# 管理器界面约定

最近调整：2026-11-03。用户要求保留咿米的粉白风格，同时把文案改成功能性描述，删除无实际用途的卡片和小标题。当前首页见 [实际渲染图](assets/ui-overview.png)。

## 文案与布局

- 按钮直接写动作，例如“创建桌宠”“投放食物”“刷新角色”。不用欢迎标语、陪伴口号、英文装饰标题或拟人化的保存提示。
- 每页保留一个主标题。仅在需要说明操作范围时添加说明，例如互动页的全局默认与单只设置关系；无说明时不预留高度。
- 参数名称保持一致：桌宠缩放、活跃度倍率、跳跃 / 攀爬 / 窗口跳跃倾向。说明只解释范围、影响或限制，不复述控件里已经显示的值。
- 卡片用于真实的列表 / 详情分栏。设置项直接排成行，不为状态、说明或单个按钮建立卡片。
- 待创建数量、运行诊断、保存错误与恢复提示按实际状态显示。保存失败时，任意页底部均显示可点击的提示，进入设置页查看错误并重试；成功后隐藏。真实损坏诊断不能当作冗余文案删除。

| 页面 | 内容 |
| --- | --- |
| 总览 | 运行数量、默认角色原帧、数量选择与创建按钮；下方直接排列喂食、食物、暂停、隐藏 / 显示和清空操作 |
| 桌宠 | 左侧列表，右侧单只桌宠状态、预览与设置。数值直接显示在编辑器里；走动 / 气泡保留必要的全局状态与暂停提示 |
| 角色 | 左侧名称 / ID / 默认角色列表，右侧预览与刷新 / 设为默认操作；删除恒定的“可用”列与重复列表标题 |
| 互动 | 开关与数值两栏，不再嵌套分组卡片；总开关影响全部桌宠的说明保留 |
| 设置 | 缩放、显示器、窗口落脚直接排成设置行，下方显示保存状态及必要诊断 |

## 视觉与素材

背景 `#FFF9FB`，侧栏 `#FFEDF4`，主操作 `#BE5881`，正文 `#5B4151`，次要文字 `#8B7181`，边框 `#EFDAE5`。保留圆角控件、白色选中导航与小型图标。移除大幅欢迎装饰、荷叶边和重复角色插画。

角色素材遵守作者手绘要求。首页与详情均通过 `CharacterFramePreview` 展示所选角色的原始 idle 第 0 帧；不生成或修改角色图。用户参考图未切图打包，未新增依赖或在线资源。

## 维护入口

- `UiTheme`：颜色、字体、间距与控件工厂。按钮的实际尺寸提供留白，AntdUI Padding 不用于扩大按钮。
- `UiCard`：列表 / 详情面板的圆角与细边，使用原生 Padding 布局子控件。
- `UiMotifs`：内置 SVG 图标；`ThemedListView`：原生列表选择 / 键盘行为与表头配色。
- `SettingRows`：标签 / 必要说明与编辑器分栏；不创建空说明行。
- `ManagerPage`：按实际标题 / 说明高度布局。`ManagerForm.SelectPage` 在最终布局后重算滚动范围，避免初次窄宽度测量残留空滚动条。
- `ManagerChrome` / `CompanionMenuRenderer`：窗口操作与托盘菜单。Win32 声明仍集中于 NativeMethods，关闭回托盘。
- `CharacterFramePreview`：预览持有并释放 SurfaceLease；删除的装饰控件 `CompanionArt` 不再嵌入图标位图。

页面按需创建并缓存，可见时 200 ms 刷新。创建仍经宿主队列；全局设置自动保存，单只覆盖只在本次运行生效。Core、角色格式与设置 schema 未变。

## 验证入口

```powershell
.\.tools\dotnet\dotnet.exe build tools/ManagerUiSmoke/ManagerUiSmoke.csproj -c Release -p:Platform=x64
.\tools\ManagerUiSmoke\bin\x64\Release\net8.0-windows\ManagerUiSmoke.exe --visual-review --output artifacts/ui-simplify-20261103
.\tools\ManagerUiSmoke\bin\x64\Release\net8.0-windows\ManagerUiSmoke.exe --baseline
.\tools\ManagerUiSmoke\bin\x64\Release\net8.0-windows\ManagerUiSmoke.exe
.\tools\ManagerUiSmoke\bin\x64\Release\net8.0-windows\ManagerUiSmoke.exe --preview-pixels
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/desktop-input-smoke.ps1
```

`--visual-review` 使用临时设置和角色目录，真实创建两只咿米后输出五页图，检查默认 / 最小尺寸与窗口操作。可选 `--output` 指定产物目录；默认 `artifacts/ui-review`。完整模式另验证数量选择创建、单只参数、刷新、清空与删除，并用临时设置写入失败检查提示、跳转与重试。产物只截取应用自身，不包含用户桌面。

本次单屏 2560×1600、144 DPI / 150% 下检查默认客户区 1680×1140 与最小窗口 1440×960，并独立执行真实鼠标拖动 / 缩放和完整 input smoke。截图不替代输入验收，多屏与 OS DPI 切换仍未验证。最新四档结果与本地预览包见 [STATUS](STATUS.md)。版本保持 0.3.1，未替换正式 Release。
