using Microsoft.Xna.Framework.Audio;

namespace DCM.Core.UI;

// Shared UI services handed to every screen: one painter (and its 1x1 pixel
// texture) for the whole game, plus the menu click sound.
public sealed record UIContext(UIPainter Painter, SoundEffect Click);
