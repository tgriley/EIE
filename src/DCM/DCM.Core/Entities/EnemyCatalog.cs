#nullable enable
namespace DCM.Core.Entities;

// Single source of truth for enemy types. The level JSON "type" field indexes
// into this table. Camera-immune types ignore the camera entirely (no flee, no
// daze) and so have no "_hide" sprite sheet; those are the first HideSpriteCount
// types, kept contiguous so the sheet arrays stay simple.
public static class EnemyCatalog
{
    private static readonly bool[] CameraImmuneByType =
    {
        false, false, false, false, false, true, true
    };

    public static int SpriteCount => CameraImmuneByType.Length;

    public static int HideSpriteCount => System.Array.IndexOf(CameraImmuneByType, true);

    public static bool IsCameraImmune(int type) => CameraImmuneByType[type % SpriteCount];
}
