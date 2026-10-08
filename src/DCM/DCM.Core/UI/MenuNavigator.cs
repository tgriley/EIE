using DCM.Core.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace DCM.Core.UI;

public class MenuNavigator
{
    public int  SelectedIndex  { get; private set; }
    public bool JustConfirmed  { get; private set; }
    public bool JustCancelled  { get; private set; }

    private readonly int _itemCount;
    private readonly int _columns;
    private float _repeatTimer;
    private int   _heldDelta;

    private const float RepeatDelay     = 0.35f;
    private const float RepeatRate      = 0.12f;
    private const float StickThreshold  = 0.50f;

    public MenuNavigator(int itemCount, int columns = 1)
    {
        _itemCount = itemCount;
        _columns   = columns;
    }

    public bool IsSelected(int index) => index == SelectedIndex;
    public void Reset() => SelectedIndex = 0;

    public void Update(GameTime gameTime, InputSnapshot input)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        JustConfirmed = input.PadJustPressed(Buttons.A);
        JustCancelled = input.PadJustPressed(Buttons.B);

        var anyDir     = HasDirection(input.Pad);
        var prevAnyDir = HasDirection(input.PrevPad);
        var delta      = Delta(input.Pad);

        if (anyDir && !prevAnyDir && delta != 0)
        {
            Move(delta);
            _heldDelta   = delta;
            _repeatTimer = RepeatDelay;
        }
        else if (anyDir && delta == _heldDelta && delta != 0)
        {
            _repeatTimer -= dt;
            if (_repeatTimer <= 0)
            {
                Move(delta);
                _repeatTimer = RepeatRate;
            }
        }
        else if (!anyDir)
        {
            _heldDelta   = 0;
            _repeatTimer = 0;
        }
    }

    private void Move(int delta) => SelectedIndex = Math.Clamp(SelectedIndex + delta, 0, _itemCount - 1);

    private static bool Up(GamePadState p)    => p.DPad.Up    == ButtonState.Pressed || p.ThumbSticks.Left.Y >  StickThreshold;
    private static bool Down(GamePadState p)  => p.DPad.Down  == ButtonState.Pressed || p.ThumbSticks.Left.Y < -StickThreshold;
    private bool Left(GamePadState p)  => _columns > 1 && (p.DPad.Left  == ButtonState.Pressed || p.ThumbSticks.Left.X < -StickThreshold);
    private bool Right(GamePadState p) => _columns > 1 && (p.DPad.Right == ButtonState.Pressed || p.ThumbSticks.Left.X >  StickThreshold);

    private bool HasDirection(GamePadState p) => Up(p) || Down(p) || Left(p) || Right(p);

    private int Delta(GamePadState p)
    {
        if (Up(p))   return -_columns;
        if (Down(p)) return  _columns;
        if (Left(p)  && SelectedIndex % _columns > 0) return -1;
        if (Right(p) && SelectedIndex % _columns < _columns - 1 && SelectedIndex + 1 < _itemCount) return 1;
        return 0;
    }
}
