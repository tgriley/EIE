# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

"Find the Exit": a Doom-style first-person raycaster game built with MonoGame 3.8 (DesktopGL) on .NET 8, C#. Endless mode: procedurally generated stages, enemies you stun with a flash camera, and a buff pick between stages.

## Commands

Run from `src/DCM/`:

```bash
dotnet build                              # builds both projects (solution: DCM.sln; src/DCM.slnx also exists)
dotnet run --project DCM.DesktopGL       # run the game
dotnet tool restore                       # restore MGCB content tools (runs automatically on first build)
dotnet mgcb-editor DCM.Core/Content/DCM.mgcb   # open the MonoGame content pipeline editor
```

There are no tests in this repository.

## Coding Style

- **File-scoped namespaces**: `namespace DCM.Core.UI;` — no wrapping braces.
- **`var`** for all local variables; explicit types for fields.
- **Target-typed `new`** for static field initializers: `new(160, 150, 140)` not `new Color(160, 150, 140)`.
- **`#nullable enable`** at the top of any file that returns or stores nullable reference types.
- Enum members on separate lines.
- No trailing comments, no summary XML docs.
- Named constants instead of magic numbers; shared colours/sizes come from `UITheme`.

## Architecture

Two projects:

- **DCM.Core** — all game logic, rendering, content. Class library.
- **DCM.DesktopGL** — thin executable entry point (`Program.cs` just runs `DCMGame`). References Core and builds the content pipeline (`DCM.mgcb`) into XNB assets.

### Game loop and state

`DCMGame` (DCM.Core/DCMGame.cs) is the MonoGame `Game` subclass. Everything draws to a fixed 1280×720 logical render target that is scaled to the window. Each frame it builds one `InputSnapshot` (keyboard, mouse mapped to logical coords, gamepad — current and previous) and passes it to the single `IGameScreen _currentScreen`. When `Update` returns a different screen object, `DCMGame` disposes the old one and switches; when it returns `null`, the game exits. Focus loss calls `IGameScreen.OnFocusLost()`.

`Screens/ScreenFactory.cs` owns the shared services (`UIContext`, `GameStateStore`, `PlaySounds`, config) and wires all navigation: `Menu()` → `NewRun()` → `Stage(n)` → `BuffSelectScreen` → `Stage(n+1)`, plus `Settings()`. Settings changes go through `GameStateStore.Update(change)`, which applies and saves.

### Screens (`DCM.Core/Screens/`)

**`IGameScreen`** — `IsMouseVisible`, `Update(GameTime, InputSnapshot)` (returns `this`, a new screen, or `null`), `Draw(GameTime)`, default `OnFocusLost()`, `IDisposable`. Screens never poll `Mouse`/`Keyboard`/`GamePad` directly; they cache `input.MousePosition` in `Update` for hover drawing.

**`MenuScreen`** (wraps `MainMenu`), **`SettingsScreen`** (sound/fullscreen toggles, reset-save with confirm), **`BuffSelectScreen`** (three rolled buff cards + skip, stats panel).

**`PlayScreen`** — one stage. Built from a `StageSetup` (stage index, `Map`, `LevelAssets`, start health, shared `PlayerBuffs`). Coordinates `Player`, enemies, pickups, `CameraFlash`, `FogOfWar`, the renderer and the UI pieces; handles pause, win/lose, and mouse-look recentering (`Mouse.SetPosition`). Input bindings live in `Input/PlayerInputMapper`, not here.

### Rendering — software raycaster, not 3D

`Rendering/RaycasterRenderer.cs` is a classic DDA raycaster: it writes into a CPU-side `Color[]` frame buffer at `RW`=640 × `RH`=360, uploads it, and scales 2× to the 1280×720 canvas. Walls, floor and ceiling are textured per pixel with distance fog and warm tint; `IBillboard`s (enemies, pickups) are depth-tested against a per-column Z-buffer, sorted back to front. Textures are `PixelTexture` records (CPU pixel copies loaded by `LevelAssets`); the renderer never samples GPU textures. The held camera is drawn from a `WeaponPose` (raise, bob, flash) — the renderer holds no gameplay state.

### World (`DCM.Core/World/`)

- **`MapGenerator`** — seeded BSP generator. Size, enemy count and immune/sensitive mix scale with stage. The same `(stage, seed)` always produces the same map (including exit and pickup placement, which use the seeded `Random` passed to `Map`).
- **`Map`** — `int[,]` tiles (`Tile`: 0 empty, 1–3 wall variants, 9 exit), start pose, enemy spawns, texture variant, pickup spawn. Implements `IMap`.
- **`LevelAssets`** loads textures and enemy sheets; **`LevelFactory`** builds enemies and pickups from a `Map`.
- **`FogOfWar`** — tiles seen, for the minimap. **`Collision`** — shared entity radius and overlap test.

### Entities (`DCM.Core/Entities/`)

- **`Player`** (`IPlayerTarget`, `IHealable`) — movement, sprint stamina, damage cooldown, death spin. Takes `PlayerInput`; never reads devices.
- **`Enemy`** — state machine Patrol/Chase/Attack/Flee/Dazed. Raising the camera while looking at a sensitive enemy makes it flee; firing the flash dazes it (overhead countdown). Camera-immune types (`EnemyCatalog`) ignore both and move slower.
- **`CameraFlash`** — raise/settle/fire/cooldown state; exposes `IsRaised`, `IsReady`, `JustFired`, `ChargeFraction`, and the `WeaponPose`.
- **`PlayerBuffs`** — per-run stacking levels. Each `BuffType` has one `BuffDefinition` row (value-by-level + display format); name/description come from resx keys `Buff{Type}` / `Buff{Type}Desc`.
- **`HealthPickup`** (`IPickup`) — one per stage, heals 50.

### UI (`DCM.Core/UI/`)

Immediate-mode `SpriteBatch` drawing through one shared **`UIPainter`** (from `UIContext`, created once in `DCMGame`). **`UITheme`** holds screen size, button size and palette.

- **`Button`** / **`ToggleButton`** — bounds + label; `IsClicked(input)`, hover-aware `Draw`.
- **`ButtonMenu<TAction>`** — buttons bound to actions plus gamepad navigation (`MenuNavigator`) and click sound; `Update` returns the chosen action or `null`. Used by `MainMenu`, `PauseOverlay`, `EndOverlay`, `SettingsScreen`.
- In play: **`GameplayHud`** (status bars, objective, timer, crosshair, controls hint, hurt flash), **`Minimap`**, **`PauseOverlay`**, **`EndOverlay`**.

### Persistence and services

`Persistence/JsonGameSaveRepository` reads/writes `SaveData` at `%LOCALAPPDATA%/FindTheExit/save.json`. `Services/GameStateStore` holds `GameSettings` (mute, fullscreen) and `LevelProgress` (best endless stage).

### Localization

All user-visible strings go through `GameText.Get(key)`, backed by `Localization/Resources.resx` with es-ES and fr-FR satellites. Add new strings to all three .resx files. Fonts cover Latin-1 only.

### Content pipeline

Assets live in `DCM.Core/Content/` and are registered in `DCM.mgcb`. New textures/fonts must be added to the .mgcb file (manually or via mgcb-editor) before `Content.Load<T>` can find them. Source art PNGs at the repo root are working files, not pipeline inputs.
