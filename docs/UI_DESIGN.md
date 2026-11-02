# 咿米的桌面小窝

2026-11-03 的管理窗口视觉重做。用户要求以美观、可爱和咿米风格为优先，并补充了包含狗爪、骨头、荷叶边、蝴蝶结、铃铛与爱心的参考图。

## 视觉方向

把管理器做成柔软的桌面小窝：粉白纸面、莓灰文字、淡蓝留白、圆角按钮、少量荷叶边和狗爪 / 骨头印记。主操作使用莓粉实色，普通操作使用白底细线，选中导航使用白色胶囊。装饰留在边角和预览区，不覆盖编辑器。

| 用途 | 颜色 |
| --- | --- |
| 背景 | `#FFF9FB` |
| 侧栏与欢迎区 | `#FFEDF4` |
| 主操作 | `#BE5881` |
| 正文 | `#5B4151` |
| 次要文字 | `#8B7181` |
| 细边框 | `#EFDAE5` |
| 蓝色纸面 | `#EEF6FD` |

首页由欢迎区、真实桌宠 / 队列 / 保存状态、快捷操作组成。桌宠页保留列表和单只设置；角色页以大预览展示原图，当前默认角色排在前面；互动页分为陪伴方式、活动参数、气泡总闸；设置页展示桌面舞台与自动保存状态。角色显示名对随包 ID 提供友好名称，存储 ID 与自定义显示名不变。

角色素材继续遵守作者手绘要求。欢迎区复用仓库已有 `Resources/icon_yimy.png`，角色预览复用原始精灵帧；没有调用 AI 绘画，没有生成 / 改写任何角色 PNG。狗爪、骨头、荷叶边和导航图标是代码内的小型 UI 几何图案。用户参考图仅作设计依据，没有切图拷入产品。未增加依赖或网络资源。

## 代码落点

- `UiTheme`：统一颜色、共享字体、按钮 / 选择器 / 数值 / 开关样式。AntdUI 的 Padding 会缩进其绘制区域；按钮留白由真实尺寸提供，避免大点击区里只画出一颗小按钮。
- `Controls/UiCard`：使用真正的 WinForms Padding 布局子控件，绘制圆角纸面、细边和可选荷叶边 / 印记。不会再把卡片绘制用的 InnerPadding 当成子控件的布局边距。
- `Controls/UiMotifs`：内置 SVG 导航图标与 GDI+ 小图案，无远程资源。
- `Controls/CompanionArt`：读取嵌入的既有角色图标，按比例展示，拥有并释放位图。
- `Controls/ThemedListView`：保留原生列表的键盘 / 选择行为，自绘柔和表头和选中行；行高 ImageList 随控件释放。
- `Controls/SettingRows`：说明与编辑器分栏、按内容高度布局，文字随可用宽度换行。
- `Controls/ManagerChrome`：自定义标题栏，拖动和边缘缩放通过集中在 NativeMethods 的 Win32 入口；最大化尊重工作区，关闭仍回到托盘。窗口控制按钮提供提示和可访问名称。
- `Controls/CompanionMenuRenderer`：托盘菜单沿用现有动作，应用统一字体与配色。

页面仍按需创建、缓存选择与滚动位置；可见时 200 ms 刷新。没有改 Core、角色动画、用户设置字段或启动逻辑。保存失败仍显示错误并提供重试；首次无设置文件只显示友好的默认提示，不隐藏真实损坏 / 恢复诊断。

## 检查与交付

```powershell
.\.tools\dotnet\dotnet.exe build tools/ManagerUiSmoke/ManagerUiSmoke.csproj -c Release -p:Platform=x64
.\tools\ManagerUiSmoke\bin\x64\Release\net8.0-windows\ManagerUiSmoke.exe --visual-review
.\tools\ManagerUiSmoke\bin\x64\Release\net8.0-windows\ManagerUiSmoke.exe --baseline
.\tools\ManagerUiSmoke\bin\x64\Release\net8.0-windows\ManagerUiSmoke.exe
.\tools\ManagerUiSmoke\bin\x64\Release\net8.0-windows\ManagerUiSmoke.exe --preview-pixels
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/desktop-input-smoke.ps1
```

`--visual-review` 使用隔离设置和正式角色副本，真实创建两只咿米后生成五页图；计数来自真实宿主，不是写死的演示数字。随后检查默认 / 最小窗口尺寸下的布局，以及标题栏、托盘还原和边缘命中。产物在 `artifacts/ui-redesign-20261103/`。效果图仅从应用自身 DrawToBitmap，不截取用户桌面。

本次当前单屏 2560×1600、150% / 144 DPI 下，默认客户区 1680×1140、最小窗口 1440×960。图像与布局检查不代替真实桌面输入；本次另外执行了完整 input smoke。真实多屏、跨屏 DPI 与 OS 缩放切换未验证，不能把预览的四档像素计算称为系统验收。

输入 smoke 另用实际鼠标拖动标题栏、拖拽右下角改变窗口大小、点击关闭回到托盘，避免只检查 WM_NCHITTEST 返回值就声称窗口操作已经通过。

版本保持维护分支的 0.3.1；UI 预览包单独输出，不覆盖正式 Release、标签或已有发布附件。最终四档结果、命令与 Git 状态见 [STATUS](STATUS.md)。
