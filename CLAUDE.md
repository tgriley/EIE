# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

"Find the Exit": a Doom-style first-person raycaster game built with MonoGame 3.8 (DesktopGL) on .NET 8, C#.

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

## Architecture

Two projects:

- **DCM.Core** — all game logic, rendering, content. Class library.
- **DCM.DesktopGL** — thin executable entry point (`Program.cs` just runs `DCMGame`). References Core and builds the content pipeline (`DCM.mgcb`) into XNB assets.

### Game loop and state

`DCMGame` (DCM.Core/DCMGame.cs) is the MonoGame `Game` subclass. It holds a single `IGameScreen _currentScreen` and delegates every `Update`/`Draw` call to it. When `Update` returns a different screen object, `DCMGame` disposes the old one and switches; when it returns `null`, the game exits.

Screen transitions are wired via factory lambdas in `LoadContent` using mutually-recursive local functions:

```csharp
IGameScreen CreateMenu() => new MenuScreen(_spriteBatch, font, GraphicsDevice, CreatePlay);
IGameScreen CreatePlay() => new PlayScreen(_spriteBatch, font, GraphicsDevice, Content, CreateMenu);
```

### Screens (`DCM.Core/Screens/`)

**`IGameScreen`** — contract all screens implement:
- `bool IsMouseVisible { get; }`
- `IGameScreen? Update(GameTime, MouseState mouse, MouseState prevMouse)` — returns `this` to stay, a new screen to transition, or `null` to exit.
- `void Draw(GameTime)`
- `IDisposable`

**`MenuScreen`** — wraps `MainMenu`, returns a new `PlayScreen` on Start, `null` on Exit.

**`PlayScreen`** — owns the full gameplay loop: loads content, manages `Player`, `List<Enemy>`, `RaycasterRenderer`, and `HUD`. Handles ESC-toggle pause, shooting (LMB), and end conditions (`_gameOver` / `_won`). Mouse centering (`Mouse.SetPosition`) happens here, not in `Player`. Accepts `Func<IGameScreen> toMenu` to navigate back to the menu from the end overlay.

### Rendering — software raycaster, not 3D

`Rendering/RaycasterRenderer.cs` is a classic DDA raycaster (Wolfenstein/Doom style): it writes pixels into a CPU-side `Color[]` frame buffer at a fixed internal resolution (`RW`=640 × `RH`=360), uploads it to a texture, and scales 2× to 1280×720. Walls/floor/ceiling are textured per-pixel with distance fog; enemies are billboard sprites depth-tested against a per-column Z-buffer. Textures are loaded via the content pipeline in `PlayScreen`; pixel data is extracted to `Color[]` arrays and passed to the renderer constructor — the renderer never samples GPU textures. Window size is derived from `RW`/`RH` constants.

### World

`World/Map.cs` defines levels as hard-coded 16×16 `int[,]` tile grids (`Tile` constants: 0 empty, 1–3 wall variants, 9 exit), plus player start, enemy spawns, and torch positions. Positions use tile coordinates with doubles for sub-tile precision; entities spawn at tile center (+0.5). Implements `IMap`; `IsValidSpawn(x, y)` returns false for walls/exits and tiles fully enclosed by walls on all four sides.

### Entities

**`Player`** implements `ICamera` and `IDamageable`. Holds position, direction vector + camera plane (FOV), health, and attack/damage cooldowns. `Update(float dt, IMap map, PlayerInput input)` — takes a `PlayerInput` readonly struct; does not read MonoGame input directly.

**`Enemy`** is a state machine (Patrol/Chase/Attack/Dead) with spritesheet animation. Constructor takes `EnemySpriteSheet` — per-enemy texture data. The renderer sets `Enemy.DistSq` for back-to-front sprite sorting. `Update(GameTime, IDamageable target, IMap map)`.

**`EnemySpriteSheet`** (`DCM.Core/Entities/EnemySpriteSheet.cs`) — holds `Color[] Pixels`, `Width`, `Height`, `FrameCount`, and computed `FrameWidth`. Five sheets (`SpritesheetEnemy0`–`4`) are loaded in `PlayScreen` and assigned round-robin to valid spawns.

### Interfaces (`DCM.Core/Entities/`, `DCM.Core/World/`, `DCM.Core/Screens/`)

| Interface | Purpose |
|---|---|
| `IMap` | `GetTile`, `IsWall`, `IsExit`, `IsValidSpawn`, `Width`, `Height` |
| `ICamera` | Read-only position + direction + camera plane (`PosX/Y`, `DirX/Y`, `PlaneX/Y`) |
| `IDamageable` | `PosX`, `PosY`, `TakeDamage(int)` |
| `IGameScreen` | Screen state machine contract (see Screens section) |

### Input

`Input/PlayerInput.cs` — readonly struct with booleans for each action (`MoveForward`, `MoveBack`, `StrafeLeft`, `StrafeRight`, `TurnLeft`, `TurnRight`, `Running`) and `int MouseDeltaX`. Built in `PlayScreen.Update` from `Keyboard.GetState()` + raw mouse delta; passed into `Player.Update`.

### UI

`UI/MainMenu.cs`, `UI/HUD.cs`, `UI/Button.cs`, `UI/UIPainter.cs` — all immediate-mode drawing with `SpriteBatch`.

**`UIPainter`** owns the shared 1×1 white pixel texture and wraps SpriteBatch Begin/End. Provides `DrawRect`, `DrawTextShadow`, `DrawLine`, `Measure`. All UI classes take a `UIPainter` instead of owning their own SpriteBatch/font/pixel.

**`Button`** — `Rectangle` bounds + label. `IsClicked(mouse, prevMouse)` — mouse-released transition within bounds. `Draw(Point mousePos)` — hover-aware colours.

**`HUD`** — `UpdatePause(mouse, prevMouse)` returns `Resume`/`Quit`; `UpdateEnd(mouse, prevMouse)` returns `MainMenu`. `Draw(...)` branches on `paused`/`gameOver`/`won` to show the appropriate overlay. The two Update methods are separate to prevent button-area overlap between the pause overlay (Resume + Quit) and the end overlay (Main Menu).

**`MainMenu`** — `Update` returns `MenuAction`; `Draw` draws title + Start + Exit buttons.

### Localization

`Localization/` uses .resx resources (`Resources.resx` + es-ES, fr-FR satellites) with `LocalizationManager` for culture discovery/switching. Add new strings to all three .resx files.

### Content pipeline

Assets live in `DCM.Core/Content/` and are registered in `DCM.mgcb`. New textures/fonts must be added to the .mgcb file (manually or via mgcb-editor) before `Content.Load<T>` can find them. Source art PNGs at the repo root are working files, not pipeline inputs.