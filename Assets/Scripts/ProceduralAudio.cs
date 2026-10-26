using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All audio is synthesized at runtime — no asset files needed.
/// Play with PlaySfx("flap") etc. Background music is a soft generated arpeggio loop.
/// </summary>
public static class ProceduralAudio
{
    private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();
    private static AudioSource sfxSource;
    private static AudioSource musicSource;
    private static Transform owner;

    public static void Initialize(Transform parent)
    {
        owner = parent;
        EnsureSources();
    }

    private static void EnsureSources()
    {
        if (sfxSource == null)
        {
            var go = new GameObject("AudioSources");
            go.transform.SetParent(owner, false);
            sfxSource = go.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            musicSource = go.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = 0.10f;
        }
        ApplyMuteSetting();
    }

    public static void ApplyMuteSetting()
    {
        if (sfxSource == null) return;
        bool on = SaveSystem.Data.soundEnabled;
        sfxSource.mute = !on;
        musicSource.mute = !on;
    }

    public static void ToggleSound()
    {
        SaveSystem.Data.soundEnabled = !SaveSystem.Data.soundEnabled;
        SaveSystem.Save();
        ApplyMuteSetting();
    }

    public static AudioClip Get(string name)
    {
        if (Cache.TryGetValue(name, out var clip) && clip != null) return clip;
        clip = Build(name);
        Cache[name] = clip;
        return clip;
    }

    public static void PlaySfx(string name, float volume = 1f, float pitchJitter = 0.05f)
    {
        if (sfxSource == null) return;
        var clip = Get(name);
        if (clip == null) return;
        sfxSource.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        sfxSource.PlayOneShot(clip, volume);
        sfxSource.pitch = 1f;
    }

    public static void PlayMusic(int biomeIndex = 0)
    {
        if (musicSource == null) EnsureSources();
        string key = "music_biome_" + (Mathf.Abs(biomeIndex) % 6);
        var clip = Get(key);
        if (musicSource.isPlaying && musicSource.clip == clip) return;
        musicSource.clip = clip;
        musicSource.Play();
    }

    public static void UpdateBiomeMusic(int biomeIndex)
    {
        if (musicSource == null || !musicSource.isPlaying) return;
        string key = "music_biome_" + (Mathf.Abs(biomeIndex) % 6);
        var clip = Get(key);
        if (musicSource.clip == clip) return;
        float time = musicSource.time;
        musicSource.clip = clip;
        musicSource.time = time % clip.length;
        musicSource.Play();
    }

    public static void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying) musicSource.Stop();
    }

    // ------------------------------------------------------------- synthesis

    private static AudioClip Build(string name)
    {
        if (name.StartsWith("music_biome_"))
        {
            int b = 0;
            int.TryParse(name.Substring("music_biome_".Length), out b);
            return MusicLoopForBiome(b);
        }

        switch (name)
        {
            case "flap": return Whoosh(0.18f, 520f, 240f, 0.5f);
            case "coin": return CoinDing();
            case "score": return Blip(0.12f, 660f, 880f, 0.35f);
            case "hit": return Thud();
            case "click": return Blip(0.06f, 900f, 700f, 0.25f);
            case "buy": return Chime(new[] { 523f, 659f, 784f }, 0.09f);
            case "shield": return Chime(new[] { 392f, 523f, 659f, 784f }, 0.07f);
            case "shoot": return Zap();
            case "explode": return Explosion();
            case "alarm": return AlarmBlip();
            case "open": return Chime(new[] { 659f, 880f, 1175f }, 0.08f);
            case "revive": return Chime(new[] { 523f, 659f, 784f, 1047f }, 0.1f);
            case "music": return MusicLoopForBiome(0);
            default: return null;
        }
    }

    private static AudioClip Create(string n, float seconds, System.Func<float, float> gen)
    {
        int rate = 44100;
        int samples = Mathf.CeilToInt(seconds * rate);
        var data = new float[samples];
        for (int i = 0; i < samples; i++) data[i] = Mathf.Clamp(gen(i / (float)rate), -1f, 1f);
        var clip = AudioClip.Create(n, samples, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip Whoosh(float dur, float fStart, float fEnd, float vol)
    {
        float phase = 0f;
        return Create("flap", dur, t =>
        {
            float f = Mathf.Lerp(fStart, fEnd, t / dur);
            phase += 2f * Mathf.PI * f / 44100f;
            float env = Mathf.Sin(Mathf.PI * t / dur);
            float noise = (Random.value * 2f - 1f) * 0.4f;
            return (Mathf.Sin(phase) * 0.6f + noise) * env * vol;
        });
    }

    private static AudioClip Blip(float dur, float fStart, float fEnd, float vol)
    {
        float phase = 0f;
        return Create("blip", dur, t =>
        {
            float f = Mathf.Lerp(fStart, fEnd, t / dur);
            phase += 2f * Mathf.PI * f / 44100f;
            float env = Mathf.Pow(1f - t / dur, 1.5f);
            return Mathf.Sin(phase) * env * vol;
        });
    }

    private static AudioClip CoinDing()
    {
        return Create("coin", 0.22f, t =>
        {
            float env = Mathf.Pow(1f - t / 0.22f, 2f);
            float v = Mathf.Sin(2f * Mathf.PI * 1319f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 1976f * t) * 0.4f;
            return v * env * 0.45f;
        });
    }

    private static AudioClip Thud()
    {
        float phase = 0f;
        return Create("hit", 0.28f, t =>
        {
            float f = Mathf.Lerp(160f, 48f, t / 0.28f);
            phase += 2f * Mathf.PI * f / 44100f;
            float env = Mathf.Pow(1f - t / 0.28f, 2f);
            float noise = (Random.value * 2f - 1f) * Mathf.Pow(1f - t / 0.28f, 3f) * 0.5f;
            return (Mathf.Sin(phase) * 0.8f + noise) * env * 0.8f;
        });
    }

    private static AudioClip Zap()
    {
        float phase = 0f;
        return Create("shoot", 0.1f, t =>
        {
            float f = Mathf.Lerp(1400f, 300f, t / 0.1f);
            phase += 2f * Mathf.PI * f / 44100f;
            float env = Mathf.Pow(1f - t / 0.1f, 2f);
            return (Mathf.Sin(phase) * 0.5f + Mathf.Sign(Mathf.Sin(phase * 0.5f)) * 0.2f) * env * 0.5f;
        });
    }

    private static AudioClip Explosion()
    {
        return Create("explode", 0.4f, t =>
        {
            float env = Mathf.Pow(1f - t / 0.4f, 2.2f);
            float noise = (Random.value * 2f - 1f);
            float low = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(120f, 40f, t / 0.4f) * t);
            return (noise * 0.7f + low * 0.5f) * env * 0.8f;
        });
    }

    private static AudioClip AlarmBlip()
    {
        return Create("alarm", 0.16f, t =>
        {
            float f = t < 0.08f ? 880f : 660f;
            float env = Mathf.Pow(1f - t / 0.16f, 1f);
            return Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.35f;
        });
    }

    private static AudioClip Chime(float[] freqs, float noteDur)
    {
        float dur = noteDur * freqs.Length + 0.15f;
        return Create("chime", dur, t =>
        {
            int idx = Mathf.Min((int)(t / noteDur), freqs.Length - 1);
            float local = t - idx * noteDur;
            float env = Mathf.Exp(-local * 9f);
            return Mathf.Sin(2f * Mathf.PI * freqs[idx] * local) * env * 0.4f;
        });
    }

    // Soft pentatonic/modal arpeggio loops per biome (~7.5s, ambient square+sine pad)
    private static AudioClip MusicLoopForBiome(int biome)
    {
        // 6 distinct scales/feels for the 6 biomes:
        // 0: Meadow (C Major pentatonic)
        // 1: Sunset (A Minor / C Major chill)
        // 2: Moonlit (D Dorian / mysterious)
        // 3: Candy Dawn (F Lydian / bright & airy)
        // 4: Deep Forest (E Minor pentatonic / mystical)
        // 5: Thunder Peaks (G Mixolydian / energetic)
        float[][] biomeNotes = new float[][]
        {
            new[] { 261.6f, 329.6f, 392.0f, 440.0f, 392.0f, 329.6f, 293.7f, 246.9f, 261.6f, 392.0f, 523.3f, 440.0f, 392.0f, 329.6f, 293.7f, 246.9f },
            new[] { 220.0f, 261.6f, 329.6f, 392.0f, 329.6f, 261.6f, 220.0f, 196.0f, 220.0f, 329.6f, 440.0f, 392.0f, 329.6f, 261.6f, 196.0f, 220.0f },
            new[] { 293.7f, 349.2f, 440.0f, 493.9f, 440.0f, 349.2f, 293.7f, 261.6f, 293.7f, 440.0f, 587.3f, 493.9f, 440.0f, 349.2f, 261.6f, 293.7f },
            new[] { 349.2f, 440.0f, 523.3f, 659.3f, 523.3f, 440.0f, 392.0f, 329.6f, 349.2f, 523.3f, 698.5f, 659.3f, 523.3f, 440.0f, 392.0f, 349.2f },
            new[] { 164.8f, 196.0f, 246.9f, 293.7f, 246.9f, 196.0f, 164.8f, 146.8f, 164.8f, 246.9f, 329.6f, 293.7f, 246.9f, 196.0f, 146.8f, 164.8f },
            new[] { 196.0f, 246.9f, 293.7f, 349.2f, 293.7f, 246.9f, 196.0f, 174.6f, 196.0f, 293.7f, 392.0f, 349.2f, 293.7f, 246.9f, 174.6f, 196.0f }
        };

        float[] notes = biomeNotes[Mathf.Abs(biome) % biomeNotes.Length];
        float noteDur = 0.47f;
        float dur = notes.Length * noteDur;
        return Create("music_biome_" + (Mathf.Abs(biome) % biomeNotes.Length), dur, t =>
        {
            float tt = Mathf.Repeat(t, dur);
            int idx = Mathf.Min((int)(tt / noteDur), notes.Length - 1);
            float local = tt - idx * noteDur;
            float env = Mathf.Exp(-local * 2.2f) * 0.5f + 0.12f;
            float f = notes[idx];
            float v = Mathf.Sin(2f * Mathf.PI * f * local) * 0.7f
                    + Mathf.Sin(2f * Mathf.PI * f * 2f * local) * 0.15f
                    + Mathf.Sin(2f * Mathf.PI * f * 0.5f * local) * 0.25f;
            // fade the very end to make the loop seamless
            float loopFade = Mathf.Min(1f, (dur - tt) / 0.15f);
            return v * env * loopFade * 0.16f;
        });
    }
}
