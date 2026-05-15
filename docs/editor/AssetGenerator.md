# AssetGenerator

Editor-only static class. Generates all game sprites and audio clips programmatically from code — no external art or DAW required. Outputs to `Assets/Resources/Sprites/` and `Assets/Resources/Audio/`.

Assembly: Editor (`Assets/Editor/`)  
File: `Assets/Editor/AssetGenerator.cs`  
Menu: **IdleClicker → Generate Assets** (priority 2)

---

## When to run

Run **before** `SceneBuilder.BuildScene()` so the rounded-rect sprite is available when panels are created. Re-run any time you want to regenerate assets (e.g., after changing colour or shape parameters).

---

## Output paths

| Path | Contents |
|------|----------|
| `Assets/Resources/Sprites/` | All sprite PNG files |
| `Assets/Resources/Audio/` | All WAV audio clips |

---

## `GenerateAll()` flow

Calls each generator in order, then `AssetDatabase.Refresh()`:

1. `RoundedRect()` — `rounded_rect.png` (used by `SceneBuilder` as the UI panel sprite)
2. `Background()` — background texture
3. `Buttons()` — button sprite sheet
4. `Hero()` — player hero sprite
5. `Goblin()` — enemy sprite (sprite index 0)
6. `OrcWarrior()` — enemy sprite (sprite index 1)
7. `CaveTroll()` — enemy sprite (sprite index 2)
8. `StoneOgre()` — enemy sprite (sprite index 3)
9. `DemonKnight()` — enemy sprite (sprite index 4)
10. `VampireLord()` — enemy sprite (sprite index 5)
11. `AncientDragon()` — enemy sprite (sprite index 6)
12. `GenerateAudio()` — generates all WAV audio clips

---

## Enemy sprite index mapping

`EnemySpriteIndex` on `GameManager` maps to these sprites (multiple enemy types share a sprite):

| Index | Sprite | Enemy types that use it |
|-------|--------|------------------------|
| 0 | Goblin | Goblin, Skeleton |
| 1 | OrcWarrior | Orc Warrior |
| 2 | CaveTroll | Cave Troll, Werewolf |
| 3 | StoneOgre | Stone Ogre, Ice Giant |
| 4 | DemonKnight | Demon Knight, Dark Witch |
| 5 | VampireLord | Vampire Lord, Lich |
| 6 | AncientDragon | Ancient Dragon |

---

## Audio clips

All clips are synthesized as PCM at 44,100 Hz using simple sine waves with envelope shaping:

| File | Use |
|------|-----|
| `blood_farm.wav` | Farm Blood tap — short 1100 Hz tap |
| `enemy_kill.wav` | Normal enemy kill — descending thud |
| `boss_kill.wav` | Boss kill — distinct from normal kill |

Audio playback was removed from `GameManager` due to a Unity assembly config issue (CS0234 on `AudioClip`). The files are generated and remain in `Resources/Audio/` for future restoration.

---

## Texture generation technique

Each sprite is created as a `Texture2D`, drawn pixel-by-pixel in C#, then saved as PNG via `File.WriteAllBytes`. Import settings are applied via `AssetDatabase` after refresh to ensure sprites are recognized as `Sprite` type with the correct pivot.
