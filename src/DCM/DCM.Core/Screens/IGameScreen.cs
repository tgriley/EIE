#nullable enable
using DCM.Core.Input;
using Microsoft.Xna.Framework;
using System;

namespace DCM.Core.Screens;

// Returns this to continue, a new screen to transition, or null to exit the application.
public interface IGameScreen : IDisposable
{
    bool IsMouseVisible { get; }
    IGameScreen? Update(GameTime gameTime, InputSnapshot input);
    void Draw(GameTime gameTime);
    void OnFocusLost() { }
}
