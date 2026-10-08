#nullable enable
using DCM.Core.Input;
using DCM.Core.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace DCM.Core.Screens;

public class MenuScreen : IGameScreen
{
    private readonly MainMenu _menu;
    private readonly Func<IGameScreen> _createPlay;
    private readonly Func<IGameScreen> _createSettings;

    public bool IsMouseVisible => true;

    public MenuScreen(UIContext ui, SpriteFont titleFont, SpriteFont subtitleFont,
        Func<IGameScreen> createPlay, Func<IGameScreen> createSettings)
    {
        _menu           = new MainMenu(ui, titleFont, subtitleFont);
        _createPlay     = createPlay;
        _createSettings = createSettings;
    }

    public IGameScreen? Update(GameTime gameTime, InputSnapshot input) => _menu.Update(gameTime, input) switch
    {
        MenuAction.Start    => _createPlay(),
        MenuAction.Settings => _createSettings(),
        MenuAction.Exit     => null,
        _                   => this
    };

    public void Draw(GameTime gameTime) => _menu.Draw();

    public void Dispose()
    {
    }
}
