# Chronicles of the Lost Dungeon

A 2D level-based dungeon adventure built in Unity 6, focused on modular, extensible systems.
Five levels, three enemy types with distinct behaviours, ability-driven combat, persistent
progression, and an online leaderboard.


| Build | Link |
|---|---|
| WebGL (Unity Play) | `<link>` |
| Windows PC | https://drive.google.com/drive/folders/1FMCvPuLeBUOyxKOiWQFg0fe6EXi5NqG9?usp=sharing |
| Android APK | https://drive.google.com/drive/folders/1k6LZlMz0A4QBjoJBQLIcAZOAd0y0b6tq?usp=drive_link |
| Video walkthrough | `<link>` |

---

## Controls

| Action | PC / WebGL | Mobile |
|---|---|---|
| Move | A / D or arrow keys | On-screen left / right |
| Jump | Space | Jump button |
| Attack | Left mouse | Attack button |
| Dash (ability) | Left Shift | Dash button |
| Heavy Strike (ability) | Right mouse | Heavy Strike button |
| Interact | E | Interact button |
| Drink potion | Q | Potion button |
| Pause | Escape | Pause button |

---

## Architecture Overview

The project is organised so that gameplay systems communicate through **interfaces and
events** rather than direct references. The guiding rule: a system should be replaceable
without editing the systems that use it.

```
Assets/Scripts/
├── Core/          Plain C# - no UnityEngine dependency, fully unit testable
│   ├── PlayerStats, DamageCalculator, InventoryLogic, SaveData, Objectives
│   └── Ashfall.Core.asmdef
├── Interfaces/    IDamageable, IMovable, IInteractable, ICollectable,
│                  IAbility, IEnemyBehaviour, ISaveable, IObjective
├── Systems/       Singletons: GameManager, SaveManager, LevelManager,
│                  AudioManager, ObjectPoolManager, LeaderboardService,
│                  ObjectiveManager, GameInput, PlatformManager
├── Player/        Controller, Health, Attack, Abilities, Inventory, Interactor
├── Enemies/       Enemy + Behaviours/ (Warrior, Archer, Guardian)
├── Interactables/ Door, Chest, Switch, Collectible, KeyPickup
├── Combat/        Projectile
├── Levels/        LevelExit
└── UI/            HUD, Pause, GameOver, LevelComplete, MainMenu,
                   Settings, LevelButton, LeaderboardUI, TouchButton
```

### Why `Ashfall.Core` is a separate assembly

Rules that don't need a scene — damage maths, inventory sorting, save structure, objective
completion — live in an assembly definition with no Unity references. This makes them
testable in isolation (the test assembly references `Ashfall.Core` only) and keeps game
rules from silently coupling to `MonoBehaviour`.

---

## Design Patterns

### Singleton
`GameManager`, `SaveManager`, `LevelManager`, `AudioManager`, `ObjectPoolManager`,
`LeaderboardService`.

These are genuinely global and must survive scene loads, so each guards its own instance in
`Awake` and calls `DontDestroyOnLoad`. Duplicates destroy themselves, which matters because
the Systems prefab is placed in every scene — whichever loads first wins and the rest
quietly remove themselves.

### Observer
Used wherever one event needs to reach several unrelated systems.

`GameManager.OnGameStateChanged` is consumed by the pause menu, the game over screen and
the audio manager. None of them know about each other. `Enemy.OnAnyEnemyDeath` is a static
event so `AudioManager` and `ObjectiveManager` can both react to any enemy dying without
holding a reference to a single enemy instance.

**Concrete example — completing a level.** `LevelExit` calls
`LevelManager.CompleteLevel()`. That single call causes: the save file to be written, the
next level to unlock, the level-complete panel to appear, the completion sound to play, and
the player's coin total to be posted to the online leaderboard. `LevelManager` references
none of those systems.

### Strategy
Two applications.

**Enemy AI** — `IEnemyBehaviour` has one method, `Tick(enemy, player)`. `WarriorBehaviour`
chases and melees, `ArcherBehaviour` maintains distance and fires pooled projectiles,
`GuardianBehaviour` holds ground and telegraphs heavy attacks. `Enemy` itself contains no AI
logic; it delegates every frame. Adding a new enemy type means writing one class and adding
one line to `Enemy.CreateBehaviour` — no existing behaviour changes.

**Player abilities** — `IAbility` exposes `Activate(user)` and `StaminaCost`.
`PlayerAbilities` holds them as interface references and never knows which concrete ability
it's invoking, so the stamina check, the activation and the event broadcast are written once
regardless of how many abilities exist.

### State
`GameState` (MainMenu / Playing / Paused / LevelComplete / GameOver) is owned by
`GameManager`, which applies the correct `Time.timeScale` on every transition and broadcasts
the change. Centralising this fixed a real bug: the pause screen and the level-complete
screen used to set `Time.timeScale` independently and could leave the game frozen.

`GuardianBehaviour` runs its own finite state machine (Idle → Windup → Attack → Cooldown)
with a timer per state, which is what produces its telegraphed attack rather than an instant
hit.

### Object Pooling
`ObjectPoolManager` keeps a `Dictionary<GameObject, Queue<GameObject>>` keyed by prefab.
Archer projectiles are fired frequently and die quickly — instantiating and destroying them
causes garbage collection spikes, which are especially visible on mobile and WebGL.
Projectiles return themselves to the pool on impact or when their lifetime expires.

---

## Algorithms

### 1. Nearest-neighbour search — `PlayerInteractor.TryInteract()`

**Problem:** when several interactable objects overlap the player's radius, which one should
E activate?

**Approach:** `Physics2D.OverlapCircleAll` gathers candidates, then a single linear pass
tracks the smallest squared distance and keeps the winner.

**Why this one:** physics queries return colliders in arbitrary order, so without this the
player could open a distant chest instead of the door they're standing in. At the scale
involved (rarely more than a handful of colliders) an O(n) scan is faster in practice than
any spatial structure, and it has no maintenance cost.

### 2. Comparison-based sorting — `InventoryLogic.SortByValue()` / `SortByType()`

**Problem:** inventory contents arrive in pickup order, which is useless for both display
and item selection.

**Approach:** `List.Sort` with a comparison delegate — descending by value, or grouped by
`ItemType` enum ordering.

**Why this one:** delegates let one sort method serve several orderings without duplicated
code, and `List.Sort` (introsort, O(n log n)) is well beyond what these list sizes need
while costing nothing to use. `SortByValue` has a real gameplay caller — see below.

### 3. Greedy selection — `PlayerInventory.ConsumeBestPotion()`

**Problem:** with several potions of different strengths, which should be drunk?

**Approach:** sort descending by value, take the first potion found, and refuse to drink at
full health.

**Why this one:** a player reaches for a potion when badly hurt, so the strongest potion
wastes the least healing at that moment. Greedy selection is correct here because the choice
is independent — drinking one potion doesn't change the value of the others.

### 4. Brace-depth JSON extraction — `LeaderboardService.ParseFirebaseJson()`

**Problem:** Firebase returns an object keyed by player name. `JsonUtility` cannot
deserialise a dictionary with arbitrary keys.

**Approach:** walk the response counting `{` and `}`. Depth 1 is the wrapper; each depth-2
block is one entry, extracted and handed to `JsonUtility` — which handles flat objects fine.
Results are then sorted descending by score.

**Why this one:** it avoids pulling in a third-party JSON library for one endpoint, and it's
tolerant of key names, whitespace and entry count.

---

## Data Management

### JSON save system

`SaveManager` serialises a `SaveData` object to `Application.persistentDataPath/save.json`.
Persisted: unlocked levels, completed levels, health and stamina maxima, coin total,
inventory contents, and audio settings.

**Why JSON:** it is human-readable (a real advantage when debugging progression bugs),
`JsonUtility` is built in with no dependency, and the format tolerates added fields — an old
save missing a new field deserialises with defaults rather than failing.

**How it stays decoupled:** `SaveManager.SaveAll()` finds every `ISaveable` in the scene and
lets each write its own slice of the save file before committing once. `SaveManager` has no
knowledge of what a player or an inventory is. Adding a new persistent system means
implementing the interface — `SaveManager` is never edited.

Read and write are both wrapped in try/catch, and a corrupt file falls back to a fresh save
rather than propagating nulls through the game.

### REST API — online leaderboard

`LeaderboardService` talks to Firebase Realtime Database over HTTPS. Coin totals are
submitted on level completion; the leaderboard screen fetches and displays the ranked list.

**Failure handling** (also the answer to "what if the API goes down"):
- `SendWebRequest()` is dispatched inside a guard, because it can throw *before* it yields
  (blocked insecure connection, malformed URL). A coroutine can't try/catch around a yield.
- An 8-second timeout stops a dead connection hanging the game.
- On failure the loaded-event fires with an **empty list** rather than not firing, so the UI
  shows "leaderboard unavailable" instead of waiting forever.
- Gameplay is never blocked: level completion and the local save happen regardless of
  network state.

**Known limitation:** database rules currently allow public read/write, which is acceptable
for a coursework demo but not production. The proper fix is authenticated writes via
Firebase Auth, or routing submissions through a Cloud Function that validates them
server-side.

*(This project originally used dreamlo. It was replaced because dreamlo's free tier is
HTTP-only, and browsers block mixed-content requests from an HTTPS-hosted WebGL build. The
swap required changes to `LeaderboardService` only — `LeaderboardUI` subscribes to events
and never knew the backend changed.)*

---

## Multi-platform Support

Targets: **Windows**, **WebGL**, **Android**.

### Input abstraction
`GameInput` is a single facade over keyboard and touch. Gameplay scripts ask *what the
player wants* (`GameInput.JumpPressed`), never *which key was pressed*. Without it, four
separate scripts would each need their own platform branches, and every future
input-consuming script would need more.

### Conditional compilation

| Location | Purpose |
|---|---|
| `GameInput` | Keyboard polling excluded from mobile builds |
| `PlatformManager` | Frame rate targets, cursor behaviour, vSync and quality per platform |
| `SaveManager` | **WebGL only:** flushes the virtual filesystem to IndexedDB after writing — without this, browser saves vanish when the tab closes |
| `TouchControlsVisibility` | On-screen controls shown on mobile, hidden on desktop, editor override for testing |
| `SaveManager`, `ObjectiveManager` | `UNITY_2022_2_OR_NEWER` guards for the `FindObjectsByType` API change |

The WebGL save flush is the clearest justification for conditional compilation in this
project: it isn't a preference or a tuning value, it's a platform behaving fundamentally
differently, and a runtime check couldn't fix it.

### UI scaling
Canvases use Scale With Screen Size against a 1920×1080 reference so layouts hold across
phone, desktop and browser. Touch targets are sized for thumbs rather than cursors.

---

## Unit Tests

14 NUnit tests under `Assets/Tests`, runnable via **Window → General → Test Runner → EditMode**.

| Suite | Covers |
|---|---|
| `PlayerStatsTests` | Damage clamping at zero, stamina spend rejection |
| `DamageCalculatorTests` | Attack-minus-defence, guaranteed minimum damage |
| `InventoryLogicTests` | Sort by value, sort by type, item removal |
| `SaveDataTests` | Fresh save unlock state, level completion recording |
| `ObjectiveTests` | Kill counting, over-count clamping, coin targets, negative clamping, exit state |

These are all possible *because* the rules live in `Ashfall.Core` with no scene dependency.

---

## Known Limitations / Future Work

- Leaderboard writes are unauthenticated (see above).
- `MainMenuController.OnContinueClicked` resumes at the last entry in `unlockedLevels`,
  which assumes the list stays ordered. Storing an explicit "furthest level" field would be
  more robust.
- Collectibles respawn on level replay — no per-level pickup state is persisted.
- Object pooling covers projectiles only; enemies and effects still instantiate.
- Enemy behaviours are stateless per instance except the Guardian; a shared behaviour
  instance per type would reduce allocations.
  Third-Party Assets & Credits

All third-party assets used in this project are listed below. Everything else — all C# source code, system architecture, level layouts, UI construction and game logic — is my own work.

Characters & Sprites

Hero Knight — Pixel Art by Sven Thole https://assetstore.unity.com/packages/2d/characters/hero-knight-pixel-art-165188 Used for the player character. Animations drive the movement, three-hit attack combo, roll (dash ability), hurt and death states. The animator parameters referenced in PlayerController and PlayerHealth (AnimState, Attack1–Attack3, Roll, Grounded, AirSpeedY, Hurt, Death, noBlood) come from this pack's controller.

Bandits — Pixel Art by Sven Thole https://assetstore.unity.com/packages/2d/characters/bandits-pixel-art-104130 Used for the Warrior and Archer enemy types.

Bringer of Death (Free) by Clembod https://assetstore.unity.com/packages/2d/characters/bringer-of-death-free-195719 Used for the Guardian enemy type — its heavier animation set suits the telegraphed wind-up attack driven by GuardianBehaviour's state machine.

Environment & Props

Platform Tile Pack by Anokolisa https://assetstore.unity.com/packages/2d/environments/platform-tile-pack-204101 Used for level geometry — ground, platforms and walls across all five levels.

Simple Gems and Items Ultimate Animated Customizable Pack by BitGem https://assetstore.unity.com/packages/3d/props/simple-gems-and-items-ultimate-animated-customizable-pack-73764 Used for collectible coins, chest contents and pickup items.

2D Bow is Bow and Arrow by Sagito Studio https://assetstore.unity.com/packages/2d/textures-materials/2d-bow-is-bow-and-arrow-174003 Used for the Archer's projectile sprite. Projectile instances are managed by ObjectPoolManager rather than instantiated per shot.

Dark Fantasy sprite assets (free) — itch.io https://itch.io/game-assets/free/tag-dark-fantasy/tag-sprites Used for level background art and environmental decoration.

Engine & Packages
Unity 6 (6000.4.7f1), Universal Render Pipeline (2D Renderer)
TextMeshPro — all UI text
Unity Test Framework (NUnit) — the 14 EditMode unit tests
Cinemachine — camera follow
Firebase Realtime Database (REST interface only, no SDK) — online leaderboard
Licensing note

All Unity Asset Store assets above are used under the Unity Asset Store EULA. The itch.io assets are used under their respective free-use licences. No third-party C# code is included in this project — all scripts under Assets/Scripts/ were written by me.