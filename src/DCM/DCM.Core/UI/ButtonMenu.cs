#nullable enable
using DCM.Core.Input;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace DCM.Core.UI;

// A vertical list of buttons, each bound to an action, driven by both mouse
// and gamepad. Binding the action to the button removes index bookkeeping
// from callers; B/cancel maps to an optional cancel action.
public sealed class ButtonMenu<TAction> where TAction : struct
{
    private readonly UIContext _ui;
    private readonly List<(Button Button, TAction Action)> _items;
    private readonly TAction? _cancelAction;
    private readonly MenuNavigator _nav;

    public ButtonMenu(UIContext ui, TAction? cancelAction, params (Button, TAction)[] items)
    {
        _ui           = ui;
        _items        = new(items);
        _cancelAction = cancelAction;
        _nav          = new MenuNavigator(items.Length);
    }

    public void ResetSelection() => _nav.Reset();

    public TAction? Update(GameTime gameTime, InputSnapshot input)
    {
        _nav.Update(gameTime, input);

        foreach (var (button, action) in _items)
            if (button.IsClicked(input))
                return Choose(action);

        if (_nav.JustConfirmed) return Choose(_items[_nav.SelectedIndex].Action);
        if (_nav.JustCancelled && _cancelAction is { } cancel) return Choose(cancel);

        return null;
    }

    public void Draw(Point mousePos)
    {
        for (var i = 0; i < _items.Count; i++)
            _items[i].Button.Draw(mousePos, _nav.IsSelected(i));
    }

    private TAction Choose(TAction action)
    {
        _ui.Click.Play();
        return action;
    }
}
