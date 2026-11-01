# 角色作者模板

将自己的 3×3 `sprite.png` 放在本目录，或把整个目录复制到 `Characters/<id>/`。模板中的配置和对白都是可选示例；删除它们后，只有 `sprite.png` 也能运行。

用工作区校验器检查目录：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/validate-character.ps1 `
  -Path .\examples\character-template `
  -PreviewPath .\artifacts\template-preview.png
```

参考线和锚点示意位于 `docs/assets/character-grid-reference.png`。不要把参考线绘进提交给 YimyWalk 的运行时 sprite。

## 可选播放配置

模板的 `character.json` 还演示了两个可选播放字段（取值等于默认行为，不影响只有 `sprite.png` 的角色）：

- `animation.idleFrames` / `animation.walkFrames`：自定义 0–2 与 3–5 范围内的播放顺序，长度 1–64；
- `animation.walkStridePixels`：一个完整行走循环对应的原图像素位移，设置后动画按位移而不是时间推进；
- `rendering.sampling`：`nearest`（像素画，默认）或 `smooth`（双三次，适合手绘素材）。

完整字段与校验规则见 [CHARACTER_FORMAT](../../docs/CHARACTER_FORMAT.md)。

运行时最终呈现预算：角色倍率 × 全局或实例倍率 × DPI 后，单帧单边最多 1024 像素、总像素最多 1,048,576，超出时限制有效缩放。PNG 和 schema 字段不因该预算改变，完整说明见 [CHARACTER_FORMAT](../../docs/CHARACTER_FORMAT.md)。
