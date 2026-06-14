#nullable enable
using DCM.Core.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace DCM.Core.Screens;

public class MenuScreen : IGameScreen
{
    private readonly MainMenu _menu;
    private readonly Func<IGameScreen> _createLevelSelect;
    private readonly Func<IGameScreen> _createSettings;

    public bool IsMouseVisible => true;

    public MenuScreen(SpriteBatch sb, SpriteFont font, GraphicsDevice gd,
        Func<IGameScreen> createLevelSelect, Func<IGameScreen> createSettings,
        SoundEffect clickSound)
    {
        _menu              = new MainMenu(sb, font, gd, clickSound);
        _createLevelSelect = createLevelSelect;
        _createSettings    = createSettings;
    }

    public IGameScreen? Update(GameTime gameTime, MouseState mouse, MouseState prevMouse)
    {
        var action = _menu.Update(gameTime, mouse, prevMouse);
        if (action == MenuAction.Start)    return _createLevelSelect();
        if (action == MenuAction.Settings) return _createSettings();
        if (action == MenuAction.Exit)     return null;
        return this;
    }

    public void Draw(GameTime gameTime)
    {
        _menu.Draw(gameTime);
    }

    public void Dispose()
    {
        _menu.Dispose();
    }
}
