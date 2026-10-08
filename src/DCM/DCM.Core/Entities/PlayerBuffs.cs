#nullable enable
using DCM.Core.Localization;
using System;
using System.Linq;

namespace DCM.Core.Entities;

public enum BuffType
{
    RunSpeed,
    MaxStamina,
    MaxHealth,
    StaminaRegen,
    CameraCooldown,
    SightRange,
    StunDuration
}

// One row per buff: how its stat scales with level and how it is displayed.
// Name and description come from Resources.resx as Buff{Type} / Buff{Type}Desc.
public sealed record BuffDefinition(BuffType Type, Func<int, double> ValueAt, Func<double, string> Format)
{
    public string Name => GameText.Get($"Buff{Type}");
    public string Description => GameText.Get($"Buff{Type}Desc");
}

// Stacking upgrade levels for one playthrough. All buff-tunable player stats
// live here so PlayScreen, Player, the HUD and the renderer share one source.
public class PlayerBuffs
{
    private const double MinimapRevealBonus = 1.0;

    private static readonly BuffDefinition[] Definitions =
    {
        new(BuffType.RunSpeed,       l => 4.5 + 0.25 * l,              v => $"{v:F1}"),
        new(BuffType.MaxStamina,     l => 1.0 + 0.25 * l,              v => $"{v:F1}s"),
        new(BuffType.MaxHealth,      l => 100 + 15 * l,                v => $"{v:F0}"),
        new(BuffType.StaminaRegen,   l => 0.15 * (1 + 0.15 * l),       v => $"{v:F2}/s"),
        new(BuffType.CameraCooldown, l => 5.0 * Math.Pow(0.92, l),     v => $"{v:F1}s"),
        new(BuffType.SightRange,     l => 9.0 + 0.75 * l,              v => $"{v:F1}"),
        new(BuffType.StunDuration,   l => 3.0 + 0.5 * l,               v => $"{v:F1}s")
    };

    public static readonly BuffType[] All = Definitions.Select(d => d.Type).ToArray();

    private readonly int[] _levels = new int[Definitions.Length];

    public static BuffDefinition Definition(BuffType type) => Definitions[(int)type];

    public static double BaseValue(BuffType type) => Definition(type).ValueAt(0);

    public int GetLevel(BuffType type) => _levels[(int)type];

    public void Apply(BuffType type) => _levels[(int)type]++;

    public double Value(BuffType type) => Definition(type).ValueAt(GetLevel(type));

    public string StatValue(BuffType type) => Definition(type).Format(Value(type));

    public double RunSpeed       => Value(BuffType.RunSpeed);
    public float  MaxStamina     => (float)Value(BuffType.MaxStamina);
    public int    MaxHealth      => (int)Value(BuffType.MaxHealth);
    public float  StaminaRegen   => (float)Value(BuffType.StaminaRegen);
    public float  CameraCooldown => (float)Value(BuffType.CameraCooldown);
    public double SightRange     => Value(BuffType.SightRange);
    public double MinimapReveal  => SightRange + MinimapRevealBonus;
    public float  StunDuration   => (float)Value(BuffType.StunDuration);

    public static BuffType[] RollChoices(int count, Random rng)
    {
        var pool = (BuffType[])All.Clone();
        for (var i = pool.Length - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        var result = new BuffType[Math.Min(count, pool.Length)];
        Array.Copy(pool, result, result.Length);
        return result;
    }
}
