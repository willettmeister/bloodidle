# SceneBuilder

Editor-only static class. Builds `Assets/Scenes/MainScene.unity` entirely from code — no manual scene editing needed. Re-running it from scratch is the intended workflow; the scene is regeneratable at any time.

Assembly: Editor (`Assets/Editor/`)  
File: `Assets/Editor/SceneBuilder.cs`  
Menu: **IdleClicker → Setup Scene** (priority 1)

---

## When to run

1. After **Generate Assets** (so panels pick up `rounded_rect.png` as their sprite).
2. Any time after modifying `SceneBuilder.cs` — re-run to rebuild the scene.

---

## Coordinate system

All Y values are measured from the **top of the canvas**, increasing downward.  
Canvas reference resolution: **1080 × 1920** portrait.

```
   0–110   Header (Blood | Wave | Wood+Shards)
 120–455   Enemy card
 465–775   Army card
 785–995   Farm Blood
1005–1140  Action row (Tank | Berserker | Paladin | Heal Self)
1150–1315  Workers card
1325–1490  Barracks card
1500–1665  Fortifications card
1675–1920  Equipment card
1930–2145  Blood Ritual + Blood Pact card
2155–2295  Prestige card
2305–2505  Blood Surge card
2515–2680  Blood Bank card
2690–3105  Prestige Shop card (6 rows)
3115–3425  Soul Shard Shop card (4 rows)
3435–3535  Bottom row (Stats | Settings | Suggest)
overlay    StatsPanel, SettingsPanel, FeatureRequestOverlay (modals)
```

---

## Colour palette

| Name | Hex |
|------|-----|
| `BgBase` | `#0B0B18` |
| `Surface1` | `#161625` |
| `Crimson` | `#D32F2F` |
| `Blue` | `#1565C0` |
| `Purple` | `#6A1B9A` |
| `Green` | `#2E7D32` |
| `Brown` | `#5D4037` |
| `Gold` | `#F9A825` |
| `EHPFill` | `#C62828` |
| `EHPBg` | `#3D1010` |
| `SHPFill` | `#2E7D32` |
| `SHPBg` | `#0F2A10` |
| `TextSec` | `#B0B0C8` |
| `DeepOrange` | `#BF360C` |
| `Amber` | `#FF6F00` |
| `Teal` | `#00695C` |

---

## `BuildScene()` flow

1. Creates `Assets/Scenes/` directory.
2. Opens a new empty scene via `EditorSceneManager.NewScene`.
3. Loads `rounded_rect.png` sprite from `Assets/Resources/Sprites/`.
4. Creates `Main Camera` (solid colour `BgBase`, orthographic) + `AudioListener`.
5. Creates `EventSystem` + `StandaloneInputModule`.
6. Creates `Canvas` (`ScreenSpaceOverlay`, `CanvasScaler` with 1080×1920 reference, `GraphicRaycaster`).
7. Adds a `ScrollRect` with a `Content` RectTransform (all panels sit inside it).
8. Creates `GameManager`, `UIManager`, `ClickManager` GameObjects and adds the respective components.
9. Builds each UI section top-to-bottom, assigning every `[SerializeField]` on `UIManager` and every button listener on `ClickManager` using `UnityEventTools.AddPersistentListener`.
10. Saves the scene.

---

## Helper methods

These static methods are used throughout `BuildScene()` to reduce boilerplate:

| Method | Purpose |
|--------|---------|
| `PT(parent, name, ...)` | Creates a panel `GameObject` with a background `Image` |
| `PF(parent, name, ...)` | Creates a fill bar `Image` |
| `Panel(parent, name, color, x, y, w, h)` | Generic panel helper |
| `Btn(parent, label, color, x, y, w, h)` | Creates a `Button` + `Text` child |
| `Label(parent, text, color, x, y, w, h, align)` | Creates a `Text` label |
| `Stretch(rt)` | Sets all anchors and offsets to fill parent |
| `SetRT(rt, x, y, w, h)` | Sets anchored position and size delta |
| `CreateChild(parent, name)` | `new GameObject(name)` parented to `parent` |
| `HC(hex)` | Parses a 6-char hex string to `Color` |

---

## Adding a new section

1. Pick a Y range after the last existing section.
2. Call `PT()` or `Panel()` to create the card background.
3. Add `Label()` and `Btn()` calls for your content.
4. Assign results to the matching `UIManager` public fields (e.g., `ui.myNewText = ...`).
5. Wire button listeners with `UnityEventTools.AddPersistentListener(btn.onClick, cm.OnMyAction)`.
6. Re-run **IdleClicker → Setup Scene**.
