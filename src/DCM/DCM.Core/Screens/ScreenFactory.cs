using DCM.Core.Audio;
using DCM.Core.Configuration;
using DCM.Core.Entities;
using DCM.Core.Services;
using DCM.Core.UI;
using DCM.Core.World;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace DCM.Core.Screens;

// Owns the shared services and wires screen-to-screen navigation, so DCMGame
// only has to ask for the first screen.
public sealed class ScreenFactory
{
    private readonly UIContext _ui;
    private readonly GraphicsDevice _gd;
    private readonly ContentManager _content;
    private readonly GameConfiguration _config;
    private readonly GameStateStore _state;
    private readonly PlaySounds _sounds;
    private readonly SpriteFont _titleFont;
    private readonly SpriteFont _subtitleFont;
    private readonly Action<bool> _setFullscreen;
    private readonly LevelCompletionService _completion;

    public ScreenFactory(UIContext ui, GraphicsDevice gd, ContentManager content, GameConfiguration config,
        GameStateStore state, PlaySounds sounds, SpriteFont titleFont, SpriteFont subtitleFont,
        Action<bool> setFullscreen)
    {
        _ui            = ui;
        _gd            = gd;
        _content       = content;
        _config        = config;
        _state         = state;
        _sounds        = sounds;
        _titleFont     = titleFont;
        _subtitleFont  = subtitleFont;
        _setFullscreen = setFullscreen;
        _completion    = new LevelCompletionService(state.Progress, () => state.Save());
    }

    public IGameScreen Menu() => new MenuScreen(_ui, _titleFont, _subtitleFont, NewRun, Settings);

    // Each run gets a fresh base seed; stage N within a run is deterministic
    // so the same run always rebuilds the same map for the same stage.
    private IGameScreen NewRun() =>
        Stage(0, Random.Shared.Next(), (int)PlayerBuffs.BaseValue(BuffType.MaxHealth), new PlayerBuffs());

    // Completing a stage goes through a buff pick before the next stage; the
    // shared PlayerBuffs accumulates for the whole run.
    private IGameScreen Stage(int stage, int runSeed, int health, PlayerBuffs buffs)
    {
        var map = MapGenerator.Generate(stage, runSeed + stage);
        var setup = new StageSetup(stage, map, LevelAssets.Load(_content, map.TextureVariant), health, buffs);
        return new PlayScreen(_ui, _gd, setup, _sounds, _config.Gameplay, _completion, Menu,
            h => new BuffSelectScreen(_ui, buffs, h, h2 => Stage(stage + 1, runSeed, h2, buffs)));
    }

    private IGameScreen Settings() => new SettingsScreen(_ui, _state.Settings, Menu,
        () => _state.Update(s => s.Settings.ToggleMute()),
        () => _state.Update(s =>
        {
            s.Settings.ToggleFullscreen();
            _setFullscreen(s.Settings.IsFullscreen);
        }),
        () => _state.Update(s => s.Progress.Reset()));
}
