#nullable enable
using Microsoft.Xna.Framework.Audio;
using System;

namespace DCM.Core.Audio;

public static class SoundSynth
{
    private const int SampleRate = 44100;

    public static SoundEffect CreateClick() => Click();

    public static PlaySounds CreatePlaySounds() => new(
        Gunshot(),
        PlayerOuch(),
        PlayerDeath(),
        EnemyOuch(),
        EnemyDeath(),
        Win(),
        CameraShutter());

    private static short ClipSample(double v) =>
        (short)(Math.Max(-1.0, Math.Min(1.0, v)) * short.MaxValue);

    private static void WriteSample(byte[] data, int i, short s)
    {
        data[i * 2]     = (byte)(s & 0xFF);
        data[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
    }

    private static SoundEffect Click()
    {
        var n    = SampleRate * 80 / 1000;
        var data = new byte[n * 2];
        for (var i = 0; i < n; i++)
        {
            var t    = (double)i / SampleRate;
            var wave = Math.Sin(2 * Math.PI * 800 * t) * 0.6
                     + Math.Sin(2 * Math.PI * 1600 * t) * 0.4;
            WriteSample(data, i, ClipSample(Math.Exp(-t * 45) * wave * 0.8));
        }
        return new SoundEffect(data, SampleRate, AudioChannels.Mono);
    }

    private static SoundEffect Gunshot()
    {
        var n    = SampleRate * 130 / 1000;
        var data = new byte[n * 2];
        var rng  = new Random(42);
        for (var i = 0; i < n; i++)
        {
            var t     = (double)i / SampleRate;
            var noise = (rng.NextDouble() * 2 - 1) * Math.Exp(-t * 80);
            var boom  = Math.Sin(2 * Math.PI * 75 * t) * Math.Exp(-t * 15);
            WriteSample(data, i, ClipSample((noise * 0.65 + boom * 0.45) * 0.9));
        }
        return new SoundEffect(data, SampleRate, AudioChannels.Mono);
    }

    private static SoundEffect PlayerOuch()
    {
        var n    = SampleRate * 220 / 1000;
        var data = new byte[n * 2];
        var rng  = new Random(1);
        for (var i = 0; i < n; i++)
        {
            var t     = (double)i / SampleRate;
            var decay = Math.Exp(-t * 14);
            var freq  = Math.Max(60, 200 - t * 500);
            var wave  = Math.Sin(2 * Math.PI * freq * t);
            var noise = (rng.NextDouble() * 2 - 1) * 0.25;
            WriteSample(data, i, ClipSample((wave * 0.75 + noise) * decay * 0.85));
        }
        return new SoundEffect(data, SampleRate, AudioChannels.Mono);
    }

    private static SoundEffect PlayerDeath()
    {
        var n     = SampleRate * 1100 / 1000;
        var data  = new byte[n * 2];
        var phase = 0.0;
        for (var i = 0; i < n; i++)
        {
            var t    = (double)i / SampleRate;
            var freq = Math.Max(40, 320 * Math.Exp(-t * 2.0));
            phase   += 2 * Math.PI * freq / SampleRate;
            var vibrato = Math.Sin(2 * Math.PI * 5 * t) * 0.08;
            WriteSample(data, i, ClipSample(Math.Sin(phase * (1 + vibrato)) * Math.Exp(-t * 1.5) * 0.85));
        }
        return new SoundEffect(data, SampleRate, AudioChannels.Mono);
    }

    private static SoundEffect EnemyOuch()
    {
        var n    = SampleRate * 160 / 1000;
        var data = new byte[n * 2];
        var rng  = new Random(3);
        for (var i = 0; i < n; i++)
        {
            var t     = (double)i / SampleRate;
            var decay = Math.Exp(-t * 22);
            var freq  = Math.Max(80, 380 - t * 1800);
            var wave  = Math.Sin(2 * Math.PI * freq * t);
            var noise = (rng.NextDouble() * 2 - 1) * 0.3;
            WriteSample(data, i, ClipSample((wave * 0.7 + noise) * decay * 0.8));
        }
        return new SoundEffect(data, SampleRate, AudioChannels.Mono);
    }

    private static SoundEffect EnemyDeath()
    {
        var n     = SampleRate * 700 / 1000;
        var data  = new byte[n * 2];
        var rng   = new Random(4);
        var phase = 0.0;
        for (var i = 0; i < n; i++)
        {
            var t     = (double)i / SampleRate;
            var freq  = Math.Max(50, 700 * Math.Exp(-t * 4.0));
            phase    += 2 * Math.PI * freq / SampleRate;
            var vibrato = Math.Sin(2 * Math.PI * 10 * t) * 0.12;
            var noise   = (rng.NextDouble() * 2 - 1) * 0.1;
            WriteSample(data, i, ClipSample((Math.Sin(phase * (1 + vibrato)) * 0.85 + noise) * Math.Exp(-t * 2.5) * 0.8));
        }
        return new SoundEffect(data, SampleRate, AudioChannels.Mono);
    }

    private static SoundEffect CameraShutter()
    {
        var n    = SampleRate * 120 / 1000;
        var data = new byte[n * 2];
        var rng  = new Random(7);
        for (var i = 0; i < n; i++)
        {
            var t = (double)i / SampleRate;
            var click1 = Math.Exp(-t * 350) * (Math.Sin(2 * Math.PI * 3200 * t) * 0.6 + (rng.NextDouble() * 2 - 1) * 0.4);
            var t2     = Math.Max(0, t - 0.04);
            var click2 = Math.Exp(-t2 * 350) * (Math.Sin(2 * Math.PI * 2600 * t2) * 0.5 + (rng.NextDouble() * 2 - 1) * 0.35) * 0.75;
            WriteSample(data, i, ClipSample((click1 + click2) * 0.65));
        }
        return new SoundEffect(data, SampleRate, AudioChannels.Mono);
    }

    private static SoundEffect Win()
    {
        double[] freqs   = { 262, 330, 392, 523 };
        double[] lengths = { 0.12, 0.12, 0.12, 0.50 };
        var totalSamples = 0;
        foreach (var l in lengths) totalSamples += (int)(l * SampleRate);
        var data  = new byte[totalSamples * 2];
        var phase = 0.0;
        var idx   = 0;
        for (var ni = 0; ni < freqs.Length; ni++)
        {
            var noteSamples = (int)(lengths[ni] * SampleRate);
            var freq        = freqs[ni];
            for (var j = 0; j < noteSamples && idx < totalSamples; j++, idx++)
            {
                var t   = (double)j / SampleRate;
                var env = ni < freqs.Length - 1
                    ? Math.Min(1.0, t * 120)
                    : Math.Min(1.0, t * 120) * Math.Exp(-t * 3.5);
                phase += 2 * Math.PI * freq / SampleRate;
                var wave = Math.Sin(phase) + Math.Sin(phase * 2) * 0.25;
                WriteSample(data, idx, ClipSample(wave * env * 0.55));
            }
        }
        return new SoundEffect(data, SampleRate, AudioChannels.Mono);
    }
}
