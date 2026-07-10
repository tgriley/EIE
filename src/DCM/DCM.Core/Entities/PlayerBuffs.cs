#nullable enable
using System;

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

// Stacking upgrade levels for one playthrough. All buff-tunable player stats
// live here so PlayScreen, Player, HUD and the renderer share one source.
public class PlayerBuffs
{
    public const double BaseRunSpeed   = 4.5;
    public const float  BaseStamina    = 1f;
    public const int    BaseHealth     = 100;
    public const float  BaseRegen      = 0.15f;
    public const float  BaseCooldown   = 5f;
    public const double BaseSightRange = 9.0;
    public const double BaseReveal     = 10.0;
    public const float  BaseStun       = 3f;

    public const double RunSpeedPerLevel = 0.25;
    public const float  StaminaPerLevel  = 0.25f;
    public const int    HealthPerLevel   = 15;
    public const float  RegenPerLevel    = 0.15f;
    public const float  CooldownFactor   = 0.92f;
    public const double SightPerLevel    = 0.75;
    public const float  StunPerLevel     = 0.5f;

    public static readonly BuffType[] All = (BuffType[])Enum.GetValues(typeof(BuffType));

    private readonly int[] _levels = new int[All.Length];

    public int GetLevel(BuffType type) => _levels[(int)type];

    public void Apply(BuffType type) => _levels[(int)type]++;

    public double RunSpeed       => BaseRunSpeed + RunSpeedPerLevel * GetLevel(BuffType.RunSpeed);
    public float  MaxStamina     => BaseStamina + StaminaPerLevel * GetLevel(BuffType.MaxStamina);
    public int    MaxHealth      => BaseHealth + HealthPerLevel * GetLevel(BuffType.MaxHealth);
    public float  StaminaRegen   => BaseRegen * (1f + RegenPerLevel * GetLevel(BuffType.StaminaRegen));
    public float  CameraCooldown => BaseCooldown * (float)Math.Pow(CooldownFactor, GetLevel(BuffType.CameraCooldown));
    public double SightRange     => BaseSightRange + SightPerLevel * GetLevel(BuffType.SightRange);
    public double MinimapReveal  => BaseReveal + SightPerLevel * GetLevel(BuffType.SightRange);
    public float  StunDuration   => BaseStun + StunPerLevel * GetLevel(BuffType.StunDuration);

    public static string Name(BuffType type) => type switch
    {
        BuffType.RunSpeed       => "RUN SPEED",
        BuffType.MaxStamina     => "STAMINA",
        BuffType.MaxHealth      => "MAX HEALTH",
        BuffType.StaminaRegen   => "STAMINA REGEN",
        BuffType.CameraCooldown => "FLASH COOLDOWN",
        BuffType.SightRange     => "SIGHT RANGE",
        _                       => "STUN DURATION"
    };

    public static string Description(BuffType type) => type switch
    {
        BuffType.RunSpeed       => "+0.25 sprint speed",
        BuffType.MaxStamina     => "+0.25s max stamina",
        BuffType.MaxHealth      => "+15 max health, keeps hp %",
        BuffType.StaminaRegen   => "+15% stamina regen",
        BuffType.CameraCooldown => "-8% flash cooldown",
        BuffType.SightRange     => "+0.75 tiles sight and minimap",
        _                       => "+0.5s flash stun"
    };

    public string StatValue(BuffType type) => type switch
    {
        BuffType.RunSpeed       => $"{RunSpeed:F1}",
        BuffType.MaxStamina     => $"{MaxStamina:F1}s",
        BuffType.MaxHealth      => MaxHealth.ToString(),
        BuffType.StaminaRegen   => $"{StaminaRegen:F2}/s",
        BuffType.CameraCooldown => $"{CameraCooldown:F1}s",
        BuffType.SightRange     => $"{SightRange:F1}",
        _                       => $"{StunDuration:F1}s"
    };

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
