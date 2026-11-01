# 咿米语言气泡

## 角色依据与设计范围

用户于 2026-11-01 明确项目主要角色为 **Yimy（咿米），粉色头发的人形犬娘**，要求参考所附聊天气泡的轮廓、完全重新绘制适合她的样式。视觉以用户说明和仓库现有 `Characters/咿米_Yimy/sprite.png` 为准。

检索入口：[萌娘百科「咿米」](https://zh.moegirl.org.cn/%E5%92%BF%E7%B1%B3)。原站返回 JavaScript 要求，本次通过[可读取的同名镜像条目](https://moegirl.icu/zh-hans/%E5%92%BF%E7%B1%B3)辅助核对（2026-11-01）：萨摩犬娘、白/粉发、蓝瞳、代表色 `#FFE4E9`，以及犬耳、蝴蝶结、铃铛元素。镜像并非角色方的一手资料；不据此推定任何图片的再分发许可，也未把百科图片或正文复制进产品。

保留参考图的白底圆角、粉色双层边框和柔和外描边；重绘为奶白犬耳与肉垫、粉白条纹蝴蝶结、金色铃铛、浅蓝小珠和左下角短尾。文字用深莓灰色，装饰集中在边角，正文留白。角色原图、正式台词和 Core 对话准入逻辑均保持原契约。

这是 **App 当前统一的默认气泡皮肤**，其他角色也使用此样式；本轮没有引入角色包主题字段或新的角色格式。

## 原画与运行时

- 原画：[`src/YimyWalk.App/Resources/Dialogue/yimy-bubble.png`](../src/YimyWalk.App/Resources/Dialogue/yimy-bubble.png)，2048×768 RGBA PNG。
- SHA-256：`38C3169210D0B8430FD5E5C3B7D11BDE7B21665483137C2B49FD0B5A2B579B5D`。
- 生成方式：内置 `image_gen` 工具，透明背景，2026-11-01；[完整提示词](artwork/yimy-bubble-prompt.txt)。原始生成文件逐字节复制入项目，保留 Alpha 与原始元数据。
- 这是按用户授权创作的项目气泡原画，不是从百科、上游 eSheep 或用户参考截图裁切的素材。
- `BubbleRenderer` 从嵌入资源读取皮肤，以 x=600/1600、y=360/510 切九宫格；边角固定比例缩放，空白中心和直线边框按正文尺寸拉伸。换原画时必须重新核对切线和正文留白。
- 文字独立渲染，采用系统 `Microsoft YaHei UI`、13.5 逻辑像素粗体，按宠物窗口 DPI 缩放；不烘焙台词到图片。支持换行，极长台词超出当前工作区时末尾省略，防止窗口溢出。
- `BubbleWindow` 使用已有 `LayeredWindowRenderer` 的逐像素 Alpha 呈现，不再使用黄色背景或整窗 Opacity。每个气泡拥有并释放自己的位图，移动时不重复解码或绘制；所在屏 DPI 或工作区大小变化才重建。
- 保留整窗点击穿透与不激活；在宠物所在屏的工作区中定位，横向/顶部越界时钳制，保留负坐标。屏幕顶部空间不足时会与宠物区域重叠，不保证始终完全在宠物上方。

## 复核入口

`scripts/test-yimywalk.ps1` 包含气泡的 Alpha、固定边角、四档渲染 DPI、长台词和负坐标定位测试。这些测试不代替真实多屏验收。

```powershell
.\.tools\dotnet\dotnet.exe build tools/ManagerUiSmoke/ManagerUiSmoke.csproj -c Release -p:Platform=x64
.\tools\ManagerUiSmoke\bin\x64\Release\net8.0-windows\ManagerUiSmoke.exe --bubble-skin
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/desktop-input-smoke.ps1
```

`--bubble-skin` 在 `artifacts/yimy-bubble-20261101/` 输出原生渲染效果图和 DPI 样张；另在当前真实桌面显示气泡，仅截取工具自己创建的纯色背景窗口，核对透明角、白色正文区与显示前后前台窗口保持。输入 smoke 点击气泡**可见正文区域**，验证跨进程穿透和焦点。

实际命令、四档状态和未验证边界见 [STATUS 最新条目](STATUS.md)。
