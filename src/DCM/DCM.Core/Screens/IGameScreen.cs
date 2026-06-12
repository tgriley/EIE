#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace DCM.Core.Screens
{
    // Returns this to continue, a new screen to transition, or null to exit the application.
    public interface IGameScreen : IDisposable
    {
        bool IsMouseVisible { get; }
        IGameScreen? Update(GameTime gameTime, MouseState mouse, MouseState prevMouse);
        void Draw(GameTime gameTime);
    }
}
