#nullable enable
using Microsoft.Xna.Framework.Audio;
using System;

namespace DCM.Core.Audio;

public static class SoundSynth
{
    private const int SampleRate = 44100;

    public static SoundEffect CreateClick() => Click();

    public static PlaySounds CreatePlaySounds() => new(
        PlayerOuch(),
        PlayerDeath(),
        Pickup(),
        Win(),
        CameraShutter());

    // Renders `ms` milliseconds of mono 16-bit audio by evaluating `sample(t)`
    // once per sample in order, so generators may keep running state (phase,
    // rng) in captured locals.
    private static SoundEffect Synth(int ms, Func<double, double> sample)
    {
        var n    = SampleRate * ms / 1000;
        var data = new byte[n * 2];
        for (var i = 0; i < n; i++)
        {
            var s = (short)(Math.Clamp(sample((double)i / SampleRate), -1.0, 1.0) * short.MaxValue);
            data[i * 2]     = (byte)(s & 0xFF);
            data[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
        }
        return new SoundEffect(data, SampleRate, AudioChannels.Mono);
    }

    private static double Noise(Random rng) => rng.NextDouble() * 2 - 1;

    private static SoundEffect Click() => Synth(80, t =>
    {
        var wave = Math.Sin(2 * Math.PI * 800 * t) * 0.6
                 + Math.Sin(2 * Math.PI * 1600 * t) * 0.4;
        return Math.Exp(-t * 45) * wave * 0.8;
    });

    private static SoundEffect PlayerOuch()
    {
        var rng = new Random(1);
        return Synth(220, t =>
        {
            var freq = Math.Max(60, 200 - t * 500);
            var wave = Math.Sin(2 * Math.PI * freq * t);
            return (wave * 0.75 + Noise(rng) * 0.25) * Math.Exp(-t * 14) * 0.85;
        });
    }

    private static SoundEffect PlayerDeath()
    {
        var phase = 0.0;
        return Synth(1100, t =>
        {
            var freq = Math.Max(40, 320 * Math.Exp(-t * 2.0));
            phase   += 2 * Math.PI * freq / SampleRate;
            var vibrato = Math.Sin(2 * Math.PI * 5 * t) * 0.08;
            return Math.Sin(phase * (1 + vibrato)) * Math.Exp(-t * 1.5) * 0.85;
        });
    }

    // Two quick rising tones, a classic item-get blip.
    private static SoundEffect Pickup() => Synth(220, t =>
    {
        var freq = t < 0.07 ? 660 : 990;
        var local = t < 0.07 ? t : t - 0.07;
        var env = Math.Min(1.0, local * 200) * Math.Exp(-local * 12);
        return (Math.Sin(2 * Math.PI * freq * t) + Math.Sin(4 * Math.PI * freq * t) * 0.2) * env * 0.5;
    });

    private static SoundEffect CameraShutter()
    {
        var rng = new Random(7);
        return Synth(120, t =>
        {
            var click1 = Math.Exp(-t * 350) * (Math.Sin(2 * Math.PI * 3200 * t) * 0.6 + Noise(rng) * 0.4);
            var t2     = Math.Max(0, t - 0.04);
            var click2 = Math.Exp(-t2 * 350) * (Math.Sin(2 * Math.PI * 2600 * t2) * 0.5 + Noise(rng) * 0.35) * 0.75;
            return (click1 + click2) * 0.65;
        });
    }

    // C-E-G-C arpeggio; the last note rings out.
    private static SoundEffect Win()
    {
        double[] freqs   = { 262, 330, 392, 523 };
        double[] lengths = { 0.12, 0.12, 0.12, 0.50 };
        var phase = 0.0;
        return Synth(860, t =>
        {
            var note = 0;
            var noteStart = 0.0;
            while (note < freqs.Length - 1 && t >= noteStart + lengths[note])
                noteStart += lengths[note++];

            var local = t - noteStart;
            var env = Math.Min(1.0, local * 120);
            if (note == freqs.Length - 1) env *= Math.Exp(-local * 3.5);

            phase += 2 * Math.PI * freqs[note] / SampleRate;
            return (Math.Sin(phase) + Math.Sin(phase * 2) * 0.25) * env * 0.55;
        });
    }
}
