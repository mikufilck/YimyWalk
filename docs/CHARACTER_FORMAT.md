# YimyWalk 角色包格式（M4.1）

角色包是一个目录。最小格式只有一张透明 PNG：

```text
Characters/<id>/
  sprite.png
```

`<id>` 是目录名。应用不会执行角色目录里的脚本、命令或外部资源。`sprite.png` 必须是带 Alpha 的 PNG，宽高都能被 3 整除，图像按从左到右、从上到下切成九个等大的格子。当前限制由 Core 的 `CharacterLoader` 执行：单边最多 2048 像素、总像素最多 16,777,216、PNG 最多 32 MiB，并拒绝损坏、全透明或没有 Alpha 的输入。

## 九格协议

| 索引 | 含义 | 默认使用方式 |
| ---: | --- | --- |
| 0–2 | 发呆 | 按 `idleFps` 循环 |
| 3–5 | 朝右走路 | 按 `walkFps` 循环；朝左由程序水平翻转 |
| 6 | 起跳 | 上升阶段使用 |
| 7 | 最高点附近 | 短暂使用，不是循环动画 |
| 8 | 下落 | 下降阶段使用 |

第三排不是普通循环动画。直接从屏幕上方生成或拖拽释放时可以从下落帧开始。默认脚底锚点为单格宽度的 `0.5`、单格高度的 `0.9`；配置中的锚点仍以单格比例填写（`x`/`y` 取值 0–1）。

`0–2` 与 `3–5` 的**实际播放顺序**可由 `animation.idleFrames` / `animation.walkFrames` 覆盖，见下节。

## 可选文件

```text
Characters/<id>/
  sprite.png
  character.json   # 可选
  dialogue.txt     # 可选
```

缺少 `character.json` 或 `dialogue.txt` 时使用 Core 内置默认值，因此只有 `sprite.png` 的目录可以直接运行。当前支持的配置字段如下（速度和重力单位是像素/秒及像素/秒²）：

```json
{
  "schemaVersion": 1,
  "displayName": "示例小猫",
  "scale": 1.0,
  "anchor": { "x": 0.5, "y": 0.9 },
  "animation": {
    "idleFps": 3,
    "walkFps": 8,
    "idleFrames": [0, 1, 2],
    "walkFrames": [3, 4, 5]
  },
  "movement": {
    "walkSpeed": 60,
    "jumpSpeed": 450,
    "gravity": 1500,
    "maxFallSpeed": 1500
  },
  "behavior": {
    "idleSecondsMin": 2,
    "idleSecondsMax": 8,
    "walkSecondsMin": 3,
    "walkSecondsMax": 10,
    "hungerIncreasePerMinute": 0.01,
    "speechCooldownSeconds": 45
  },
  "rendering": { "sampling": "nearest" }
}
```

缺少字段会回退默认值；`schemaVersion` 若出现必须为 `1`。当前未知 JSON 属性会被忽略，不能依赖它们改变行为。数值必须是有限数并落在 Loader 的限制内，`idleSecondsMax` 不能小于 `idleSecondsMin`。

## 播放序列与采样（0.3.0）

在默认九格协议之上，`character.json` 可以覆盖播放顺序与缩放采样方式：

```json
{
  "animation": {
    "idleFps": 3,
    "walkFps": 8,
    "idleFrames": [0],
    "walkFrames": [3, 4, 5, 4],
    "walkStridePixels": 100
  },
  "rendering": { "sampling": "smooth" }
}
```

| 字段 | 取值 | 缺省 | 行为 |
| --- | --- | --- | --- |
| `animation.idleFrames` | 1–64 个整数，每项 0–2 | `[0,1,2]` | 发呆序列，按 `idleFps` 循环 |
| `animation.walkFrames` | 1–64 个整数，每项 3–5 | `[3,4,5]` | 行走序列，按 `walkFps` 循环；朝左仍由程序镜像 |
| `animation.walkStridePixels` | 有限正数，≤ 8192 | `null` | 一个完整 `walkFrames` 循环对应的原图像素位移。设置后行走动画按**实际位移**推进而不是按时间；跨屏 wrap 只按实际位移计 |
| `rendering.sampling` | `nearest` / `smooth` | `nearest` | `smooth` 使用双三次缩放，适合手绘或抗锯齿素材；`nearest` 保留像素画锐利边缘 |

序列必须是整数数组：空数组、超过 64 项、越界索引、浮点数或字符串都会让角色加载失败并给出明确错误。`sampling` 只接受两个小写取值。

### 行走与待机节奏

- `behavior.idleSecondsMin` / `idleSecondsMax`：发呆等待时长在区间内**随机取一次**，而不是每帧重掷；缺省 2–8 秒。
- `behavior.walkSecondsMin` / `walkSecondsMax`：一次自主行走的持续时间在区间内随机，到时自动回到发呆；缺省 **3–10 秒**。跟随虚拟食物时不受该时长限制。
- `walkSecondsMax` 不能小于 `walkSecondsMin`，两者都会除以全局/实例活跃度倍率。

随包的两个咿米角色使用 `idleFps: 1`、`idleFrames: [0,1,2]`（每秒换一个待机姿势）、`walkFrames: [3,4,5,4]`、`walkStridePixels: 100`、`sampling: smooth` 与 `anchor.y: 1.0`（脚底贴单元格下沿）；`sprite.png` 原始字节未改。只有 `sprite.png` 的角色仍然完全兼容，行为与 0.1.x 一致。

`dialogue.txt` 使用 UTF-8。空行以及以 `#` 或 `;` 开头的行会忽略；用 `[idle]`、`[walk]`、`[jump]` 等标题切换分组。缺少分组会回退内置台词。每个角色最多 1000 行，每行最多 500 个字符，文件最多 256 KiB。

## 校验和预览

验证器直接调用运行时使用的 `YimyWalk.Core.CharacterLoader`，不会生成窗口，也不会修改角色文件。工作区 SDK 已准备好后运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/validate-character.ps1 `
  -Path .\Characters\slime `
  -PreviewPath .\artifacts\slime-preview.png `
  -ReferencePath .\docs\assets\character-grid-reference.png
```

成功返回 0，并报告尺寸、单格尺寸、Alpha、九帧映射、配置来源、对白来源、缩放和锚点。角色无效返回 1 并保留上一条可读错误；参数或工具使用错误返回 2。输入目录可以包含空格和中文。预览是由九个已解码帧重新组合的 PNG，参考图只包含 3×3 网格和默认锚点示意；两者都不写回 `sprite.png`。校验失败时不会创建或替换预览输出，若目标已有文件则保持原样并打印提示。

仓库中的 `Characters/slime`、`Characters/咿米_Yimy` 和 `Characters/咿米_Line` 是随发布包提供的 sprite-only 初始角色。三者当前文件均为整图 1254×1254、单格 418×418（2026-11-01 复核）。后两者分别来自用户提供的 `Yimy.jpg` 和 `Yimy_no.jpg`，文件实际为透明 PNG，原样保存为 `sprite.png`。`tools/GenerateTestCharacters.ps1` 生成的小尺寸测试图不能当作当前随包素材的来源或尺寸。`cat` 仅保留在 `tests/Fixtures/Characters/cat` 供测试使用。`examples/character-template` 提供带配置和对白的作者模板；`docs/assets/character-grid-reference.png` 是不应放进运行时 sprite 的参考图。

## 最终呈现预算

角色倍率、全局 / 实例倍率与 DPI 合成后的呈现，单帧单边最多 1024 像素、总像素最多 1,048,576；超出时统一限制有效缩放，不修改原图与配置。渲染缓存预算为 64 MiB，紧张时保留上一帧并提供诊断。PNG-only 读取现在使用 StbImageNext，并检查 chunk CRC 与解压预算；有效 3×3 协议、锚点与配置字段不变。

## 当前边界

本阶段支持 3×3 基础动作、可选配置和对白，以及工具生成的静态预览。`actions.png`、额外动作映射、逐角色用户覆盖层、脚本和远程资源尚未实现；把它们放进目录不会启用这些功能。管理器的“角色”页已提供刷新入口；失败保留旧定义、旧绘制资源在无实例和无在途引用后释放的生命周期约束由宿主负责。当前资源与异常路径的待办见 [项目审查](reviews/2026-11-01-project-review.md)。
