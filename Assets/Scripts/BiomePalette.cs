using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Endless biome cycle. Every GameConfig.BiomeSize pipes the world fades to the next
/// palette: sky gradient, pipe tint, ground/bush/cloud tints. Index wraps forever.
/// </summary>
public class BiomePalette
{
    public string name;
    public Color skyTop, skyMid, skyBot;
    public Color pipe;     // multiplies grayscale pipe texture
    public Color ground, bush, cloud;
    public Color accent;   // UI / announcement color

    public static readonly List<BiomePalette> All = new List<BiomePalette>
    {
        new BiomePalette { name = "SUNNY MEADOW",                            // 0
            skyTop = Hex(0x7ECEF7), skyMid = Hex(0xACE2FA), skyBot = Hex(0xFFECBD),
            pipe = new Color(0.37f, 0.77f, 0.38f),
            ground = Color.white, bush = Color.white, cloud = Color.white, accent = Hex(0xFFC64A) },
        new BiomePalette { name = "GOLDEN SUNSET",                           // 1
            skyTop = Hex(0xFF8F6E), skyMid = Hex(0xFFB780), skyBot = Hex(0xFFD69E),
            pipe = new Color(0.95f, 0.55f, 0.35f),
            ground = new Color(1f, 0.85f, 0.7f), bush = new Color(1f, 0.72f, 0.55f),
            cloud = new Color(1f, 0.82f, 0.75f), accent = Hex(0xFF8F6E) },
        new BiomePalette { name = "MOONLIT NIGHT",                           // 2
            skyTop = Hex(0x182044), skyMid = Hex(0x2C3C6E), skyBot = Hex(0x5A6EA0),
            pipe = new Color(0.42f, 0.52f, 0.88f),
            ground = new Color(0.62f, 0.66f, 0.92f), bush = new Color(0.5f, 0.55f, 0.82f),
            cloud = new Color(0.72f, 0.78f, 1f), accent = Hex(0x9DB4FF) },
        new BiomePalette { name = "CANDY DAWN",                              // 3
            skyTop = Hex(0xFFB7D4), skyMid = Hex(0xFFD6DE), skyBot = Hex(0xFFF0D6),
            pipe = new Color(0.9f, 0.58f, 0.75f),
            ground = new Color(1f, 0.9f, 0.95f), bush = new Color(1f, 0.78f, 0.88f),
            cloud = new Color(1f, 0.92f, 0.97f), accent = Hex(0xFF9EC4) },
        new BiomePalette { name = "DEEP FOREST",                             // 4
            skyTop = Hex(0x6EBEA0), skyMid = Hex(0xA0DCB4), skyBot = Hex(0xE6F0BE),
            pipe = new Color(0.2f, 0.62f, 0.4f),
            ground = new Color(0.85f, 1f, 0.85f), bush = new Color(0.78f, 1f, 0.83f),
            cloud = new Color(0.95f, 1f, 0.95f), accent = Hex(0x7BD9A0) },
        new BiomePalette { name = "THUNDER PEAKS",                           // 5
            skyTop = Hex(0x465064), skyMid = Hex(0x646E82), skyBot = Hex(0x96A0AF),
            pipe = new Color(0.56f, 0.6f, 0.66f),
            ground = new Color(0.8f, 0.82f, 0.9f), bush = new Color(0.7f, 0.72f, 0.82f),
            cloud = new Color(0.85f, 0.87f, 0.95f), accent = Hex(0xB7C3D9) },
    };

    public static BiomePalette Get(int index) => All[Mathf.Abs(index) % All.Count];

    private static Color Hex(int rgb) => new Color(((rgb >> 16) & 0xFF) / 255f,
                                                    ((rgb >> 8) & 0xFF) / 255f,
                                                    (rgb & 0xFF) / 255f);
}
