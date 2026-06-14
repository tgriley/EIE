#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace DCM.Core.UI;

public class MenuNavigator
{
    public int  SelectedIndex  { get; private set; }
    public bool JustConfirmed  { get; private set; }
    public bool JustCancelled  { get; private set; }

    private int _itemCount;
    private readonly int _columns;
    private GamePadState _prevPad;
    private float _repeatTimer;
    private int   _heldDelta;
    private bool  _firstUpdate = true;

    private const float RepeatDelay     = 0.35f;
    private const float RepeatRate      = 0.12f;
    private const float StickThreshold  = 0.50f;

    public int ItemCount
    {
        get => _itemCount;
        set { _itemCount = value; SelectedIndex = Math.Clamp(SelectedIndex, 0, _itemCount - 1); }
    }

    public MenuNavigator(int itemCount, int columns = 1)
    {
        _itemCount = itemCount;
        _columns   = columns;
    }

    public bool IsSelected(int index) => index == SelectedIndex;
    public void Reset() => SelectedIndex = 0;

    public void Update(GameTime gameTime)
    {
        var dt  = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var pad = GamePad.GetState(PlayerIndex.One, GamePadDeadZone.None);

        if (_firstUpdate)
        {
            _prevPad     = pad;
            _firstUpdate = false;
            JustConfirmed = false;
            JustCancelled = false;
            return;
        }

        JustConfirmed = pad.Buttons.A == ButtonState.Pressed && _prevPad.Buttons.A != ButtonState.Pressed;
        JustCancelled = pad.Buttons.B == ButtonState.Pressed && _prevPad.Buttons.B != ButtonState.Pressed;

        var up    = pad.DPad.Up    == ButtonState.Pressed || pad.ThumbSticks.Left.Y >  StickThreshold;
        var down  = pad.DPad.Down  == ButtonState.Pressed || pad.ThumbSticks.Left.Y < -StickThreshold;
        var left  = _columns > 1 && (pad.DPad.Left  == ButtonState.Pressed || pad.ThumbSticks.Left.X < -StickThreshold);
        var right = _columns > 1 && (pad.DPad.Right == ButtonState.Pressed || pad.ThumbSticks.Left.X >  StickThreshold);

        var prevUp    = _prevPad.DPad.Up    == ButtonState.Pressed || _prevPad.ThumbSticks.Left.Y >  StickThreshold;
        var prevDown  = _prevPad.DPad.Down  == ButtonState.Pressed || _prevPad.ThumbSticks.Left.Y < -StickThreshold;
        var prevLeft  = _columns > 1 && (_prevPad.DPad.Left  == ButtonState.Pressed || _prevPad.ThumbSticks.Left.X < -StickThreshold);
        var prevRight = _columns > 1 && (_prevPad.DPad.Right == ButtonState.Pressed || _prevPad.ThumbSticks.Left.X >  StickThreshold);

        var anyDir     = up || down || left || right;
        var prevAnyDir = prevUp || prevDown || prevLeft || prevRight;

        int delta = 0;
        if      (up)    delta = -_columns;
        else if (down)  delta =  _columns;
        else if (left  && SelectedIndex % _columns > 0)                                                        delta = -1;
        else if (right && SelectedIndex % _columns < _columns - 1 && SelectedIndex + 1 < _itemCount)          delta =  1;

        if (anyDir && !prevAnyDir && delta != 0)
        {
            SelectedIndex = Math.Clamp(SelectedIndex + delta, 0, _itemCount - 1);
            _heldDelta    = delta;
            _repeatTimer  = RepeatDelay;
        }
        else if (anyDir && delta == _heldDelta && delta != 0)
        {
            _repeatTimer -= dt;
            if (_repeatTimer <= 0)
            {
                SelectedIndex = Math.Clamp(SelectedIndex + delta, 0, _itemCount - 1);
                _repeatTimer  = RepeatRate;
            }
        }
        else if (!anyDir)
        {
            _heldDelta   = 0;
            _repeatTimer = 0;
        }

        _prevPad = pad;
    }
}
