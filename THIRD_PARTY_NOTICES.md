# Third-party notices

## Adrianotiger/desktopPet (eSheep)

- Source: <https://github.com/Adrianotiger/desktopPet>
- Snapshot: `e2933305157a34818f47d238c74607593d81816d`
- Fetched: 2026-10-04
- License stated by upstream README: MIT License.
- No upstream snapshot is checked into this repository.

The upstream repository contains code, NuGet references, and many pet images/audio assets. The MIT notice applies to the upstream project as stated; individual bundled assets may have separate attribution or provenance. YimyWalk does not assume every bundled pet is redistributable. No upstream snapshot or upstream bundled asset is included in a YimyWalk release. Any future asset must be audited separately and retain its attribution.

The upstream README credits NAudio and several sprite authors. The historical audit record is in [`docs/UPSTREAM_AUDIT.md`](docs/UPSTREAM_AUDIT.md); the source is obtained only in an external temporary checkout when a new audit is needed.

## StbImageNext 1.0.0

- Package: `StbImageNext` 1.0.0, no package dependencies for net8.0.
- Source: <https://github.com/StbSharp/StbImageNext>; package repository commit `f350d0c0e6dccf155ce77f3422c485073a0b5449`.
- Authors: StbImageSharpTeam; upstream stb contributors.
- NuGet metadata declares `Unlicense OR MIT`; this distribution selects Unlicense. Full text and attribution: `licenses/StbImageNext-LICENSE.txt`.
- Use: managed PNG decoding behind Core's PNG-only input validation. Other formats supported by the dependency are not exposed as character inputs. PNG preview writing uses .NET compression and the project's own PNG chunk writer.
- Previous ImageSharp 3.1.12 was removed from product, validator and test dependency graphs on 2026-11-01. The isolated compatibility probe may reference its existing local DLL for trusted fixtures; that probe is excluded from the source solution and release package. No license validation is disabled or bypassed.

## YimyWalk sample roles

- `Characters/slime/sprite.png`, `Characters/咿米_Yimy/sprite.png`, and `Characters/咿米_Line/sprite.png` are the roles shipped in the release. `tests/Fixtures/Characters/cat/sprite.png` is retained only for multi-role regression tests.
- They are project sample assets and are not copied from the upstream snapshot.
- The two Yimy sprites were supplied by the project owner on 2026-10-13 with an explicit request to bundle them as starter roles. Their source filenames were `Yimy.jpg` and `Yimy_no.jpg`, but both files contain transparent PNG data. They are copied byte-for-byte as `sprite.png`; no image conversion or generated artwork is involved.

## Yimy speech-bubble artwork

The 2026-11-03 manager UI refresh reuses the existing project `logo.png` and character sprite frames without changing their source files. Navigation and action icons are inline SVG geometry. The decorative character panel and its embedded `icon_yimy.png` resource were removed during the layout simplification; the source file remains unchanged. No reference-sheet bitmap, new third-party media, AI-generated character artwork or additional package dependency is introduced by this refresh.

- `src/YimyWalk.App/Resources/Dialogue/yimy-bubble.png` is original project UI artwork generated with the built-in OpenAI image generation tool on 2026-11-01 under the project owner's explicit authorization.
- The owner's reference screenshot supplied the broad bubble silhouette; the new image is not a crop or reproduction of that screenshot. Existing owner-supplied Yimy sprites supplied visual context and were not modified.
- Public character facts and the blush representative color were consulted through the Moegirl entry and its readable mirror. No encyclopedia image, article text, third-party sprite or font file is bundled for this change. Runtime text uses an installed Windows font.
- Provenance, source links, file hash and the complete generation prompt are recorded in [`docs/YIMY_VISUAL_STYLE.md`](docs/YIMY_VISUAL_STYLE.md).

## AntdUI 2.4.12

- Package: `AntdUI` 2.4.12
- Source: <https://github.com/AntdUI/AntdUI>
- Fixed source tag: `v2.4.12` (`3d64564ebca42c6d2809034983822b43ac7f2e1e`)
- License: Apache-2.0
- Use in YimyWalk: WinForms management shell only (`Button`, `Switch`, `Select`, `InputNumber`, and `Panel`). Pet windows, rendering and Core do not depend on the UI package.
- The package has no additional direct package dependency. The NuGet package and license metadata are copied into the normal restore/publish process; no CDN, Node, or runtime network access is required.
