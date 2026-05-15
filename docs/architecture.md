# Architecture

## Component Diagram

```mermaid
graph TD
    subgraph Runtime["Runtime — GameAssembly.asmdef"]
        GM[GameManager\nSingleton · owns all state]
        UI[UIManager\nSubscribes to events · refreshes UI]
        CK[ClickManager\nButton bridge · thin forwarders]
        DN[DamageNumber\nFloating number MonoBehaviour]
        NM[NotificationManager\nStatic · push notifications]
    end

    subgraph Editor["Editor — Assets/Editor/"]
        SB[SceneBuilder\nBuilds MainScene.unity]
        AG[AssetGenerator\nGenerates sprites]
        LC[LintChecker\nCode quality checks]
    end

    subgraph Tests["Tests — Tests.EditMode.asmdef"]
        T[EditMode Tests\n13 test classes]
    end

    CK -->|"calls public methods"| GM
    GM -->|"OnStateChanged event"| UI
    GM -->|"OnDamageDealt event"| UI
    GM -->|"OnMilestoneChest event"| UI
    GM -->|"OnAchievementUnlocked event"| UI
    UI -->|"Spawn()"| DN
    GM -->|"ScheduleIdleReminder()"| NM
    SB -->|"AddComponent<GameManager>"| GM
    SB -->|"AddComponent<UIManager>"| UI
    SB -->|"AddComponent<ClickManager>"| CK
    T -->|"UNITY_INCLUDE_TESTS helpers"| GM
```

## Script Responsibilities

| Script | Assembly | Responsibility |
|--------|----------|----------------|
| `GameManager` | GameAssembly | Singleton. Owns every piece of game state. Runs `Update()` which drives the combat loop, resource accumulation, timers, and save/load. Fires `OnStateChanged` whenever state changes so UI can refresh. |
| `UIManager` | GameAssembly | Subscribes to `GameManager` events in `Start()`. Calls `Refresh()` on every `OnStateChanged` to redraw all UI elements. Holds `[SerializeField]` references to every `Text`, `Button`, `Image`, and panel `GameObject`. |
| `ClickManager` | GameAssembly | One-liner bridge between Unity button `onClick` events and `GameManager` public methods. Zero logic — pure forwarding. |
| `DamageNumber` | GameAssembly | Pooled-style floating text that rises and fades over ~1.1 s. Spawned by `UIManager.SpawnDamageNumber()` in response to `OnDamageDealt`. |
| `NotificationManager` | GameAssembly | Static class. Schedules/cancels Android and iOS push notifications. Called by `GameManager.ToggleNotifications()`. |
| `SceneBuilder` | Editor | `[MenuItem("IdleClicker/Setup Scene")]`. Builds the entire scene from scratch, wires all serialized references, and adds all button listeners using `UnityEventTools`. |
| `AssetGenerator` | Editor | `[MenuItem("IdleClicker/Generate Assets")]`. Creates `rounded_rect.png` and all enemy sprites programmatically. |
| `LintChecker` | Editor | `[MenuItem("IdleClicker/Run Lint Check")]`. Scans `Assets/Scripts/` for `Debug.Log`, hardcoded PATs, TODO markers, and naked catch blocks. |

## Event Connections

```mermaid
sequenceDiagram
    participant Player
    participant CK as ClickManager
    participant GM as GameManager
    participant UI as UIManager
    participant DN as DamageNumber

    Player->>CK: Button tap
    CK->>GM: public method call
    GM->>GM: mutate state
    GM-->>UI: OnStateChanged event
    UI->>UI: Refresh() — redraw all UI

    Note over GM,DN: Every 0.4 s during combat
    GM-->>UI: OnDamageDealt(amount, isPlayer)
    UI->>DN: DamageNumber.Spawn(...)
```

## Assembly Graph

```mermaid
graph LR
    Tests.EditMode -->|"guid: ee000003"| GameAssembly
    Tests.EditMode --> UnityEngine.TestRunner
    Tests.EditMode --> UnityEditor.TestRunner
    GameAssembly -.->|"noEngineReferences: false"| UnityEngine
    GameAssembly -.->|"autoReferenced: true"| Assembly-CSharp
```

## Save / Load

All persistent state is stored in `PlayerPrefs` (key-value strings/ints/floats). `Save()` is called automatically every 30 seconds from `Update()` and on key events. `Load()` runs once in `Awake()`. See [scripts/GameManager.md](scripts/GameManager.md) for the full key list.
