using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates every sprite in the game procedurally (sky, clouds, pipes, coins, UI, icons).
/// Everything is cached by name so it is built exactly once per session.
/// </summary>
public static class ProceduralSprites
{
    private const int Ppu = 100;
    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    public static Sprite Get(string name)
    {
        if (Cache.TryGetValue(name, out var s) && s != null) return s;
        s = Build(name);
        Cache[name] = s;
        return s;
    }

    private static Sprite Build(string name) => name switch
    {
        "white" => Solid("white", 4, 4, Color.white),
        "sky" => SkyFor(0, new Color32(126, 206, 247, 255), new Color32(172, 226, 250, 255), new Color32(255, 236, 189, 255)),
        "cloudA" => Cloud("cloudA", 0x2E, 99481234),
        "cloudB" => Cloud("cloudB", 0x24, 77121145),
        "bushStrip" => BushStrip(),
        "cityStrip" => CityStrip(),
        "ground" => Ground(),
        "pipeBody" => PipeBody(),
        "pipeCap" => PipeCap(),
        "coin" => Coin(),
        "blocker" => BlockerPlate(),
        "target" => Target(),
        "bullet" => Bullet(),
        "shieldBubble" => ShieldBubble(),
        "boss" => Boss(),
        "bossBullet" => BossBullet(),
        "iconShield" => IconShield(),
        "iconHeart" => IconHeart(),
        "iconMagnet" => IconMagnet(),
        "iconWrench" => IconWrench(),
        "iconFeather" => IconFeather(),
        "iconStar" => IconStar(),
        "iconCrosshair" => IconCrosshair(),
        "iconCoinSmall" => IconCoin(),
        "uiPanel" => RoundedRect("uiPanel", 128, 128, 24, new Color32(255, 250, 240, 245)),
        "uiPanelDark" => RoundedRect("uiPanelDark", 128, 128, 24, new Color32(38, 43, 68, 235)),
        "uiButtonGreen" => RoundedRect("uiButtonGreen", 128, 128, 30, new Color32(76, 187, 104, 255)),
        "uiButtonAmber" => RoundedRect("uiButtonAmber", 128, 128, 30, new Color32(240, 166, 62, 255)),
        "uiButtonGrey" => RoundedRect("uiButtonGrey", 128, 128, 30, new Color32(150, 156, 175, 255)),
        "uiButtonRed" => RoundedRect("uiButtonRed", 128, 128, 30, new Color32(226, 96, 91, 255)),
        "uiCircle" => CircleTex("uiCircle", 64, new Color32(255, 255, 255, 255)),
        "vignette" => Vignette(),
        "flappyTitle" => null,
        _ => Solid(name, 4, 4, Color.white),
    };

    // ---------------------------------------------------------------- helpers

    private delegate bool Sdf(float x, float y); // true = inside, in pixel coords

    private static Sprite MakeTexture(string name, int w, int h, Sdf inside, Color32 fill,
                                      Color32? outside = null, float edgeSoftness = 1f, Vector4? border = null)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var px = new Color32[w * h];
        var outCol = outside ?? new Color32(0, 0, 0, 0);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            // supersample 2x2 for smoother edges
            int hits = 0;
            for (int sy = 0; sy < 2; sy++)
            for (int sx = 0; sx < 2; sx++)
                if (inside(x + 0.25f + sx * 0.5f, y + 0.25f + sy * 0.5f)) hits++;
            float a = hits / 4f;
            px[y * w + x] = a > 0.98f ? fill : a < 0.02f ? outCol
                : Color.Lerp(outCol, fill, a);
        }
        tex.SetPixels32(px);
        tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), Ppu, 0,
            SpriteMeshType.FullRect, border ?? Vector4.zero);
        sp.name = name;
        return sp;
    }

    private static Sprite Solid(string name, int w, int h, Color c)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var px = new Color32[w * h];
        var c32 = (Color32)c;
        for (int i = 0; i < px.Length; i++) px[i] = c32;
        tex.SetPixels32(px);
        tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), Ppu, 0, SpriteMeshType.FullRect);
            sp.name = "Solid"; return sp;
    }

    private static Sdf Circle(float cx, float cy, float r) => (x, y) =>
    {
        float dx = x - cx, dy = y - cy;
        return dx * dx + dy * dy <= r * r;
    };

    private static Sdf Rounded(float cx, float cy, float hw, float hh, float r) => (x, y) =>
    {
        // rounded-rect signed test: corner arcs + straight bands (sampling stays within outer bounds)
        float dx = Mathf.Abs(x - cx) - (hw - r);
        float dy = Mathf.Abs(y - cy) - (hh - r);
        if (dx > 0 && dy > 0) return dx * dx + dy * dy <= r * r;
        return true;
    };

    // ---------------------------------------------------------------- UI

    /// <summary>Maps a cached UI sprite to the tint color its consumers should use.</summary>
    public static bool TryGetPanelColor(Sprite sprite, out Color color)
    {
        color = Color.white;
        foreach (var kv in Cache)
        {
            if (kv.Value == sprite && kv.Key.StartsWith("ui"))
            {
                color = kv.Key switch
                {
                    "uiButtonGreen" => new Color(0.30f, 0.73f, 0.41f),
                    "uiButtonAmber" => new Color(0.94f, 0.65f, 0.24f),
                    "uiButtonGrey" => new Color(0.59f, 0.61f, 0.69f),
                    "uiButtonRed" => new Color(0.88f, 0.38f, 0.36f),
                    "uiPanel" => new Color(0.99f, 0.97f, 0.92f, 0.96f),
                    "uiPanelDark" => new Color(0.15f, 0.17f, 0.27f, 0.92f),
                    _ => Color.white,
                };
                return true;
            }
        }
        return false;
    }

    /// <summary>White rounded rect at an exact pixel size (for tinted, non-sliced UI images).</summary>
    public static Sprite RoundedWhite(int w, int h, int radius)
    {
        w = Mathf.Clamp(w, 8, 640);
        h = Mathf.Clamp(h, 8, 640);
        radius = Mathf.Clamp(radius, 4, Mathf.Min(w, h) / 2 - 2);
        string key = $"rw{w}x{h}r{radius}";
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
        float hw = w / 2f, hh = h / 2f;
        var sprite = MakeTexture(key, w, h, Rounded(hw, hh, hw - 1f, hh - 1f, radius), new Color32(255, 255, 255, 255));
        Cache[key] = sprite;
        return sprite;
    }

    private static Sprite RoundedRect(string name, int w, int h, int r, Color32 fill)
    {
        float hw = w / 2f, hh = h / 2f;
        int b = r + 2; // 9-slice border keeps corners crisp when sliced
        return MakeTexture(name, w, h, Rounded(hw, hh, hw - 1f, hh - 1f, r), fill,
            border: new Vector4(b, b, b, b));
    }

    private static Sprite CircleTex(string name, int d, Color32 fill) =>
        MakeTexture(name, d, d, Circle(d / 2f, d / 2f, d / 2f - 1.5f), fill);

    private static Sprite Vignette()
    {
        int d = 256;
        var tex = new Texture2D(d, d, TextureFormat.RGBA32, false);
        var px = new Color32[d * d];
        for (int y = 0; y < d; y++)
        for (int x = 0; x < d; x++)
        {
            float dx = (x / (float)d - 0.5f) * 2f;
            float dy = (y / (float)d - 0.5f) * 2f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Clamp01((dist - 0.55f) / 0.75f);
            px[y * d + x] = new Color32(160, 20, 20, (byte)(a * 130));
        }
        tex.SetPixels32(px);
        tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0, 0, d, d), new Vector2(0.5f, 0.5f), Ppu, 0, SpriteMeshType.FullRect);
            sp.name = "Vignette"; return sp;
    }

    // ---------------------------------------------------------------- world

    public static Sprite SkyFor(int biomeIndex, Color top, Color mid, Color bot)
    {
        string key = $"sky{biomeIndex}";
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
        var sprite = BuildSky(top, mid, bot);
        sprite.name = key;
        Cache[key] = sprite;
        return sprite;
    }

    private static Sprite BuildSky(Color topC, Color midC, Color botC)
    {
        int w = 64, h = 256;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var top = (Color32)topC;
        var mid = (Color32)midC;
        var bot = (Color32)botC;
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            float t = y / (float)(h - 1);
            Color c = t < 0.62f ? Color.Lerp(top, mid, t / 0.62f) : Color.Lerp(mid, bot, (t - 0.62f) / 0.38f);
            var c32 = (Color32)c;
            for (int x = 0; x < w; x++) px[y * w + x] = c32;
        }
        tex.SetPixels32(px);
        tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), Ppu, 0, SpriteMeshType.FullRect);
            sp.name = "Sky"; return sp;
    }

    private static Sprite Cloud(string name, byte alpha, int seed)
    {
        var rng = new System.Random(seed);
        int w = 320, h = 140;
        var blobs = new List<(float cx, float cy, float r)>();
        float x = 40;
        while (x < w - 40)
        {
            float r = rng.Next(26, 44);
            blobs.Add((x, h * 0.42f + (float)rng.NextDouble() * 12f, r));
            x += r * rng.Next(11, 16) / 10f;
        }
        Sdf sdf = (px, py) =>
        {
            foreach (var b in blobs)
            {
                float dx = px - b.cx, dy = (py - b.cy) * 1.35f;
                if (dx * dx + dy * dy <= b.r * b.r) return true;
            }
            return false;
        };
        var col = new Color32(255, 255, 255, alpha);
        return MakeTexture(name, w, h, sdf, col);
    }

    private static Sprite BushStrip()
    {
        int w = 1024, h = 160;
        var rng = new System.Random(4127);
        var blobs = new List<(float cx, float cy, float r)>();
        float x = 0;
        while (x < w + 60)
        {
            float r = rng.Next(34, 64);
            blobs.Add((x, 24 + r * 0.35f, r));
            x += r * 1.1f;
        }
        Sdf sdf = (px, py) =>
        {
            foreach (var b in blobs)
            {
                float dx = px - b.cx, dy = (py - b.cy) * 1.6f;
                if (dx * dx + dy * dy <= b.r * b.r) return true;
            }
            return false;
        };
        return MakeTexture("bushStrip", w, h, sdf, new Color32(96, 190, 118, 255));
    }

    private static Sprite CityStrip()
    {
        int w = 1024, h = 200;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var px = new Color32[w * h];
        var building = new Color32(150, 178, 213, 255);
        var skyCol = new Color32(0, 0, 0, 0);
        var rng = new System.Random(991);
        int x = 0;
        while (x < w)
        {
            int bw = rng.Next(46, 110);
            int bh = rng.Next(50, 130);
            for (int bx = x; bx < Mathf.Min(x + bw, w); bx++)
            for (int by = 0; by < bh; by++)
                px[by * w + bx] = building;
            x += bw + rng.Next(6, 26);
        }
        for (int i = 0; i < px.Length; i++)
            if (px[i].a == 0) px[i] = skyCol;
        tex.SetPixels32(px);
        tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0, 0), Ppu, 0, SpriteMeshType.FullRect);
            sp.name = "CityStrip"; return sp;
    }

    private static Sprite Ground()
    {
        int w = 256, h = 160;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };
        var px = new Color32[w * h];
        var grass = new Color32(124, 200, 112, 255);
        var grassDark = new Color32(94, 172, 92, 255);
        var dirt = new Color32(178, 143, 100, 255);
        var dirtDark = new Color32(158, 124, 86, 255);
        var rng = new System.Random(7);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            // grass band at the TOP rows of the texture (visible edge of the ground)
            int d = h - 1 - y;
            Color32 c;
            if (d < 14) c = grassDark;
            else if (d < 34) c = rng.Next(0, 5) == 0 ? grassDark : grass;
            else if (d < 40) c = grass;
            else c = rng.Next(0, 7) == 0 ? dirtDark : dirt;
            px[y * w + x] = c;
        }
        tex.SetPixels32(px);
        tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0, 0), Ppu, 0, SpriteMeshType.FullRect);
            sp.name = "Ground"; return sp;
    }

    private static Sprite PipeBody()
    {
        int w = 64, h = 64;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };
        var px = new Color32[w * h];
        // grayscale shading bands — biome tint is applied via renderer color
        var baseC = new Color32(132, 132, 132, 255);
        var darkC = new Color32(94, 94, 94, 255);
        var lightC = new Color32(180, 180, 180, 255);
        var rimC = new Color32(68, 68, 68, 255);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            Color32 c;
            if (x < 3 || x >= w - 3) c = rimC;
            else if (x < 10) c = lightC;
            else if (x < 16) c = baseC;
            else if (x < w - 10) c = baseC;
            else c = darkC;
            px[y * w + x] = c;
        }
        tex.SetPixels32(px);
        tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), Ppu, 0, SpriteMeshType.FullRect);
            sp.name = "PipeBody"; return sp;
    }

    private static Sprite PipeCap()
    {
        int w = 84, h = 44;
        float hw = w / 2f, hh = h / 2f;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var px = new Color32[w * h];
        var baseC = new Color32(132, 132, 132, 255);
        var darkC = new Color32(94, 94, 94, 255);
        var lightC = new Color32(180, 180, 180, 255);
        var rimC = new Color32(68, 68, 68, 255);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            bool inside = x >= 2 && x < w - 2 && y >= 2 && y < h - 2;
            bool cornerCut = (x < 6 || x >= w - 6) && (y < 6 || y >= h - 6);
            if (!inside || cornerCut) { px[y * w + x] = new Color32(0, 0, 0, 0); continue; }
            Color32 c;
            if (x < 4 || x >= w - 4 || y < 4 || y >= h - 4) c = rimC;
            else if (x < 12) c = lightC;
            else if (x >= w - 12) c = darkC;
            else c = baseC;
            px[y * w + x] = c;
        }
        tex.SetPixels32(px);
        tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), Ppu, 0, SpriteMeshType.FullRect);
            sp.name = "PipeCap"; return sp;
    }

    private static Sprite Coin()
    {
        int d = 96;
        float c = d / 2f;
        var tex = new Texture2D(d, d, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var px = new Color32[d * d];
        var gold = new Color32(255, 200, 60, 255);
        var goldDark = new Color32(214, 148, 30, 255);
        var goldLight = new Color32(255, 236, 150, 255);
        for (int y = 0; y < d; y++)
        for (int x = 0; x < d; x++)
        {
            float dx = x - c, dy = y - c;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist > c - 1.5f) { px[y * d + x] = new Color32(0, 0, 0, 0); continue; }
            Color32 col = dist > c - 7f ? goldDark : gold;
            // inner ring
            if (dist > c * 0.52f && dist < c * 0.62f) col = goldDark;
            // highlight
            float hx = x - c * 0.72f, hy = y - c * 0.72f;
            if (hx * hx + hy * hy < 90f) col = goldLight;
            px[y * d + x] = col;
        }
        tex.SetPixels32(px);
        tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0, 0, d, d), new Vector2(0.5f, 0.5f), Ppu, 0, SpriteMeshType.FullRect);
            sp.name = "Coin"; return sp;
    }

    private static Sprite BlockerPlate()
    {
        int w = 96, h = 72;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var px = new Color32[w * h];
        var metal = new Color32(126, 134, 152, 255);
        var metalDark = new Color32(88, 95, 114, 255);
        var rivet = new Color32(62, 68, 86, 255);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (x < 2 || x >= w - 2 || y < 2 || y >= h - 2) { px[y * w + x] = new Color32(0, 0, 0, 0); continue; }
            Color32 c = y % 12 < 2 ? metalDark : metal;
            bool rivetHit = (x == 8 || x == w - 9) && (y == 10 || y == h - 11);
            if (rivetHit) c = rivet;
            px[y * w + x] = c;
        }
        tex.SetPixels32(px);
        tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), Ppu, 0, SpriteMeshType.FullRect);
            sp.name = "BlockerPlate"; return sp;
    }

    private static Sprite Target()
    {
        int d = 96;
        float c = d / 2f;
        var tex = new Texture2D(d, d, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var px = new Color32[d * d];
        var red = new Color32(232, 78, 78, 255);
        var white = new Color32(255, 255, 255, 255);
        for (int y = 0; y < d; y++)
        for (int x = 0; x < d; x++)
        {
            float dx = x - c, dy = y - c;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist > c - 1.5f) { px[y * d + x] = new Color32(0, 0, 0, 0); continue; }
            Color32 col;
            if (dist < c * 0.28f) col = red;
            else if (dist < c * 0.48f) col = white;
            else if (dist < c * 0.68f) col = red;
            else col = white;
            px[y * d + x] = col;
        }
        tex.SetPixels32(px);
        tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0, 0, d, d), new Vector2(0.5f, 0.5f), Ppu, 0, SpriteMeshType.FullRect);
            sp.name = "Target"; return sp;
    }

    private static Sprite Bullet()
    {
        int w = 28, h = 14;
        float hw = w / 2f, hh = h / 2f;
        return MakeTexture("bullet", w, h, Rounded(hw, hh, hw - 2, hh - 2, hh - 2), new Color32(255, 209, 92, 255));
    }

    private static Sprite ShieldBubble()
    {
        int d = 128;
        float c = d / 2f;
        var tex = new Texture2D(d, d, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var px = new Color32[d * d];
        var cyan = new Color32(120, 220, 255, 160);
        var cyanBright = new Color32(220, 250, 255, 220);
        for (int y = 0; y < d; y++)
        for (int x = 0; x < d; x++)
        {
            float dx = x - c, dy = y - c;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist > c - 2 || dist < c - 12) { px[y * d + x] = new Color32(0, 0, 0, 0); continue; }
            float hx = x - c * 0.7f, hy = y + c * 0.7f;
            px[y * d + x] = hx * hx + hy * hy < 80f ? cyanBright : cyan;
        }
        tex.SetPixels32(px);
        tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0, 0, d, d), new Vector2(0.5f, 0.5f), Ppu, 0, SpriteMeshType.FullRect);
            sp.name = "ShieldBubble"; return sp;
    }

    private static Sprite Boss()
    {
        int d = 160;
        float c = d / 2f;
        // grayscale shading — the active biome's accent tints the whole guardian
        var hull = new Color32(150, 150, 150, 255);
        var dark = new Color32(95, 95, 95, 255);
        var eye = new Color32(225, 225, 225, 255);
        // baked pixel loop picks hull/band/visor/pupil colors per position
        var tex = new Texture2D(d, d, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var px = new Color32[d * d];
        for (int y = 0; y < d; y++)
        for (int x = 0; x < d; x++)
        {
            float fx = x + 0.5f, fy = y + 0.5f;
            float ddx = fx - c, ddy = fy - c;
            float squashY = ddy * 0.78f;
            bool main = ddx * ddx + squashY * squashY <= 58f * 58f;
            bool finL = Mathf.Abs(fx - 14f) < 8f && Mathf.Abs(fy - c + 6f) < 14f;
            bool finR = Mathf.Abs(fx - (d - 14f)) < 8f && Mathf.Abs(fy - c + 6f) < 14f;
            bool inside = main || finL || finR;
            if (!inside) { px[y * d + x] = new Color32(0, 0, 0, 0); continue; }
            bool band = main && ddy > 8f && ddy < 26f;
            float ex = fx - c, ey = fy - (c - 8f);
            bool visor = ex * ex / (34f * 34f) + ey * ey / (12f * 12f) <= 1f;
            bool pupil = ex * ex + ey * ey <= 7.5f * 7.5f;
            Color32 col = pupil ? new Color32(45, 45, 45, 255) : visor ? eye : band ? dark : hull;
            px[y * d + x] = col;
        }
        tex.SetPixels32(px);
        tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0, 0, d, d), new Vector2(0.5f, 0.5f), Ppu, 0, SpriteMeshType.FullRect);
        sp.name = "boss";
        return sp;
    }

    private static Sprite BossBullet()
    {
        int d = 48;
        float c = d / 2f;
        var col = new Color32(196, 108, 240, 255);
        var core = new Color32(255, 224, 140, 255);
        var tex = new Texture2D(d, d, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var px = new Color32[d * d];
        for (int y = 0; y < d; y++)
        for (int x = 0; x < d; x++)
        {
            float dx = x - c, dy = y - c;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist > c - 1.5f) { px[y * d + x] = new Color32(0, 0, 0, 0); continue; }
            // spiky ball: 8 spikes
            float ang = Mathf.Atan2(dy, dx);
            float spike = Mathf.Abs(Mathf.Cos(ang * 4f));
            float r = c * (0.55f + 0.4f * spike * spike);
            px[y * d + x] = dist < c * 0.35f ? core : dist < r ? col : new Color32(0, 0, 0, 0);
        }
        tex.SetPixels32(px);
        tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0, 0, d, d), new Vector2(0.5f, 0.5f), Ppu, 0, SpriteMeshType.FullRect);
        sp.name = "bossBullet";
        return sp;
    }

    // ---------------------------------------------------------------- icons

    private static Sprite IconShield()
    {
        int d = 96;
        var body = new Color32(96, 178, 244, 255);
        var dark = new Color32(56, 124, 190, 255);
        Sdf sdf = (x, y) =>
        {
            // shield silhouette
            if (y > d - 14 || x < 10 || x > d - 10) return false;
            float t = (d - 14 - y) / (float)(d - 24);           // 0 top, 1 bottom
            float halfW = Mathf.Lerp(d / 2f - 12, d / 2f - 26, t * t);
            float cx = d / 2f;
            if (Mathf.Abs(x - cx) > halfW) return false;
            return true;
        };
        return MakeTexture("iconShield", d, d, sdf, body);
    }

    private static Sprite IconHeart()
    {
        int d = 96;
        var col = new Color32(240, 92, 92, 255);
        Sdf sdf = (x, y) =>
        {
            float nx = (x - d / 2f) / (d * 0.030f);
            float ny = (d - y - d * 0.24f) / (d * 0.030f);
            float v = Mathf.Pow(nx * nx + ny * ny - 1f, 3) - nx * nx * ny * ny * ny;
            return v <= 0;
        };
        return MakeTexture("iconHeart", d, d, sdf, col);
    }

    private static Sprite IconMagnet()
    {
        int d = 96;
        var red = new Color32(228, 90, 90, 255);
        // horseshoe: ring minus top wedge, plus two tips
        Sdf sdf = (x, y) =>
        {
            float dx = x - d / 2f, dy = y - d / 2f + 8;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            bool ring = dist < 28 && dist > 12;
            bool wedgeMissing = y > d / 2f - 4 && Mathf.Abs(dx) < 16;
            return ring && !wedgeMissing;
        };
        var s = MakeTexture("iconMagnetBase", d, d, sdf, red);
        // paint tips by re-using base; simple approach: overlay is skipped, acceptable stylization
        return s;
    }

    private static Sprite IconWrench()
    {
        int d = 96;
        var col = new Color32(140, 148, 166, 255);
        Sdf sdf = (x, y) =>
        {
            float dx = x - d / 2f, dy = y - d / 2f;
            // diagonal bar
            float u = dx * 0.707f + dy * 0.707f;
            float v = -dx * 0.707f + dy * 0.707f;
            bool bar = Mathf.Abs(u) < 34 && Mathf.Abs(v) < 8;
            bool notch = Mathf.Abs(u) > 22 && Mathf.Abs(v) < 14;
            return bar && !notch || (u < -22 && u > -36 && Mathf.Abs(v) < 20);
        };
        return MakeTexture("iconWrench", d, d, sdf, col);
    }

    private static Sprite IconFeather()
    {
        int d = 96;
        var col = new Color32(255, 224, 138, 255);
        Sdf sdf = (x, y) =>
        {
            // leaf/feather shape between two arcs
            float u = (x - 20) * 0.707f + (y - d + 20) * -0.707f; // along diagonal
            float v = -(x - 20) * 0.707f + (y - d + 20) * -0.707f;
            if (u < 0 || u > 88) return false;
            float half = 20f * Mathf.Sin(u / 88f * Mathf.PI);
            return Mathf.Abs(v) < half;
        };
        return MakeTexture("iconFeather", d, d, sdf, col);
    }

    private static Sprite IconStar()
    {
        int d = 96;
        var col = new Color32(255, 200, 60, 255);
        float c = d / 2f;
        Sdf sdf = (x, y) =>
        {
            float dx = x - c, dy = y - c;
            float ang = Mathf.Atan2(dy, dx) + Mathf.PI / 2f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float wave = Mathf.Cos(ang * 5f);
            return dist < c * (0.42f + 0.5f * wave * wave * wave * 0.5f) && dist < c - 1f;
        };
        return MakeTexture("iconStar", d, d, sdf, col);
    }

    private static Sprite IconCrosshair()
    {
        int d = 96;
        float c = d / 2f;
        var col = new Color32(120, 200, 130, 255);
        Sdf sdf = (x, y) =>
        {
            float dx = Mathf.Abs(x - c), dy = Mathf.Abs(y - c);
            bool ring = Mathf.Sqrt(dx * dx + dy * dy) < 34 && Mathf.Sqrt(dx * dx + dy * dy) > 22;
            bool cross = (dx < 4 && dy < 34) || (dy < 4 && dx < 34);
            return ring || cross;
        };
        return MakeTexture("iconCrosshair", d, d, sdf, col);
    }

    private static Sprite IconCoin()
    {
        int d = 64;
        float c = d / 2f;
        var gold = new Color32(255, 200, 60, 255);
        var goldDark = new Color32(214, 148, 30, 255);
        Sdf sdf = (x, y) =>
        {
            float dx = x - c, dy = y - c;
            return dx * dx + dy * dy <= (c - 1.5f) * (c - 1.5f);
        };
        return MakeTexture("iconCoin", d, d, sdf, gold);
    }
}
