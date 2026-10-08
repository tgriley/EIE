using DCM.Core.Rendering;
using System;

namespace DCM.Core.Entities;

// The player's flash camera: hold to raise, wait for the raise to settle,
// then click to fire. Firing starts a cooldown and must be re-raised.
public sealed class CameraFlash
{
    private const float RaiseSmoothing = 12f;
    private const float FlashFadeRate  = 6f;

    private readonly float _raiseTime;
    private float _raiseTimer;
    private bool _fired;

    public float CooldownMax { get; }
    public float Cooldown { get; private set; }
    public bool IsRaised { get; private set; }
    public bool IsReady { get; private set; }
    public bool JustFired { get; private set; }
    public float RaiseAmount { get; private set; }
    public float FlashIntensity { get; private set; }

    public float ChargeFraction => CooldownMax > 0 ? 1f - Cooldown / CooldownMax : 1f;

    public CameraFlash(float cooldownMax, float raiseTime)
    {
        CooldownMax = cooldownMax;
        _raiseTime = raiseTime;
    }

    public void Update(float dt, bool raiseHeld, bool shootPressed)
    {
        JustFired = false;
        FlashIntensity = Math.Max(0f, FlashIntensity - dt * FlashFadeRate);
        if (Cooldown > 0) Cooldown = Math.Max(0f, Cooldown - dt);

        if (!raiseHeld) { _fired = false; _raiseTimer = 0f; }
        IsRaised = raiseHeld && Cooldown <= 0 && !_fired;

        if (IsRaised && _raiseTimer < _raiseTime) _raiseTimer += dt;
        IsReady = IsRaised && _raiseTimer >= _raiseTime;

        if (IsReady && shootPressed)
        {
            FlashIntensity = 1f;
            Cooldown = CooldownMax;
            _fired = true;
            _raiseTimer = 0f;
            JustFired = true;
        }

        var target = IsRaised ? 1f : 0f;
        RaiseAmount += (target - RaiseAmount) * Math.Min(1f, dt * RaiseSmoothing);
    }

    public WeaponPose Pose(float bobPhase) => new(RaiseAmount, bobPhase, FlashIntensity);
}
