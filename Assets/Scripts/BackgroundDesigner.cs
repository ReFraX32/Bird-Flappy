using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the 2D backdrop (sky, clouds, city, bushes, ground) and drives the endless
/// biome cycle: every AdvanceBiome() the sky swaps and all world tints fade to the
/// next palette over a couple of seconds. Ground doubles as the kill floor.
/// </summary>
public class BackgroundDesigner : MonoBehaviour
{
    private class ScrollingLayer
    {
        public List<Transform> Tiles = new List<Transform>();
        public float Speed;
        public float Width;   // single tile width in world units
    }

    private readonly List<ScrollingLayer> layers = new List<ScrollingLayer>();
    private readonly List<Transform> groundTiles = new List<Transform>();
    private readonly List<SpriteRenderer> bushRenderers = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> cloudRenderers = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> cityRenderers = new List<SpriteRenderer>();
    private SpriteRenderer skyRenderer;

    private float groundWidth;
    private float halfHeight, halfWidth;

    public int BiomeIndex { get; private set; }
    public BiomePalette Current => BiomePalette.Get(BiomeIndex);
    public Color PipeTint => Current.pipe;

    // tint transition targets (lerped in LateUpdate)
    private Color tgtGround = Color.white, tgtBush = Color.white, tgtCloud = Color.white, tgtCity = Color.white;

    public static BackgroundDesigner Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        var cam = Camera.main;
        halfHeight = GameConfig.CameraOrthoSize;
        halfWidth = halfHeight * cam.aspect;
        BuildAll();
        ApplyBiome(0, true);
    }

    private void BuildAll()
    {
        int sort = -100;

        // --- sky (swapped per biome, sliced to fill the view)
        var skyGo = new GameObject("Sky", typeof(SpriteRenderer));
        skyGo.transform.SetParent(transform, false);
        skyRenderer = skyGo.GetComponent<SpriteRenderer>();
        skyRenderer.sortingOrder = sort++;
        skyRenderer.drawMode = SpriteDrawMode.Sliced;
        skyRenderer.size = new Vector2(halfWidth * 4f, halfHeight * 2.4f);
        skyGo.transform.position = new Vector3(0, halfHeight * 0.15f, 20);

        // --- drifting clouds
        CreateCloudLayer(sort++, 1.1f, -halfHeight * 0.05f, 3.0f, 0.4f, 5);
        CreateCloudLayer(sort++, 2.0f, halfHeight * 0.32f, 4.2f, 0.55f, 4);

        // --- city skyline strip
        CreateStripLayer("CityStrip", sort++, ProceduralSprites.Get("cityStrip"),
            GameConfig.GroundY + 0.7f, 0.9f, 2.4f, cityRenderers);

        // --- bush strip
        CreateStripLayer("BushStrip", sort++, ProceduralSprites.Get("bushStrip"),
            GameConfig.GroundY + 0.5f, 1.8f, 2.8f, bushRenderers);

        // --- ground (kill floor)
        var groundSprite = ProceduralSprites.Get("ground");
        groundWidth = halfWidth * 2.6f;
        for (int i = 0; i < 3; i++)
            groundTiles.Add(CreateGroundTile(sort++, groundSprite, -groundWidth + i * groundWidth));
    }

    private void CreateCloudLayer(int sort, float speed, float y, float scale, float alpha, int count)
    {
        var layer = new ScrollingLayer { Speed = speed * 0.12f };
        var holder = new GameObject("Clouds" + sort).transform;
        holder.SetParent(transform, false);

        float span = halfWidth * 2.4f;
        for (int i = 0; i < count; i++)
        {
            var cloudGo = new GameObject("Cloud" + i, typeof(SpriteRenderer));
            cloudGo.transform.SetParent(holder, false);
            var r = cloudGo.GetComponent<SpriteRenderer>();
            r.sprite = ProceduralSprites.Get(i % 2 == 0 ? "cloudA" : "cloudB");
            r.sortingOrder = sort;
            float x = -span / 2f + span * i / (count - 1) + Random.Range(-1.5f, 1.5f);
            cloudGo.transform.position = new Vector3(x, y + Random.Range(-1f, 1.5f), 15);
            cloudGo.transform.localScale = Vector3.one * scale * Random.Range(0.8f, 1.15f);
            var c = r.color; c.a = alpha * (i % 2 == 0 ? 1f : 0.85f); r.color = c;
            cloudRenderers.Add(r);
            layer.Tiles.Add(cloudGo.transform);
        }
        layers.Add(layer);
    }

    private void CreateStripLayer(string name, int sort, Sprite sprite, float y, float speed, float scale,
                                  List<SpriteRenderer> collect)
    {
        var layer = new ScrollingLayer { Speed = speed };
        var holder = new GameObject(name).transform;
        holder.SetParent(transform, false);

        float worldW = sprite.bounds.size.x * scale;
        int count = Mathf.CeilToInt(halfWidth * 2.2f / worldW) + 2;
        float startX = -halfWidth - worldW;
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject(name + "Tile" + i, typeof(SpriteRenderer));
            go.transform.SetParent(holder, false);
            var r = go.GetComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingOrder = sort;
            r.transform.localScale = new Vector3(scale, scale, 1);
            r.transform.position = new Vector3(startX + i * worldW, y, 10);
            collect.Add(r);
            layer.Tiles.Add(go.transform);
        }
        layer.Width = worldW;
        layers.Add(layer);
    }

    private Transform CreateGroundTile(int sort, Sprite sprite, float x)
    {
        var go = new GameObject("GroundTile", typeof(SpriteRenderer), typeof(BoxCollider2D));
        go.transform.SetParent(transform, false);
        var r = go.GetComponent<SpriteRenderer>();
        r.sprite = sprite;
        r.sortingOrder = sort;
        r.drawMode = SpriteDrawMode.Sliced;
        r.size = new Vector2(groundWidth, 7f);
        r.transform.position = new Vector3(x, GameConfig.GroundY - 7f, 0);

        var col = go.GetComponent<BoxCollider2D>();
        col.offset = new Vector2(groundWidth / 2f, 3.5f);
        col.size = new Vector2(groundWidth, 7f);
        return go.transform;
    }

    // ------------------------------------------------------------ biomes

    public void AdvanceBiome()
    {
        ApplyBiome(BiomeIndex + 1, false);
    }

    private void ApplyBiome(int index, bool instant)
    {
        BiomeIndex = index;
        var b = Current;
        int wrapped = Mathf.Abs(index) % BiomePalette.All.Count;
        skyRenderer.sprite = ProceduralSprites.SkyFor(wrapped, b.skyTop, b.skyMid, b.skyBot);

        tgtGround = b.ground;
        tgtBush = b.bush;
        tgtCloud = b.cloud;
        tgtCity = Color.Lerp(Color.white, b.ground, 0.5f);

        ProceduralAudio.UpdateBiomeMusic(wrapped);

        if (instant)
            SetTints(tgtGround, tgtBush, tgtCloud, tgtCity);
        // else: tints lerp toward targets in LateUpdate
    }

    private void SetTints(Color ground, Color bush, Color cloud, Color city)
    {
        foreach (var t in groundTiles) t.GetComponent<SpriteRenderer>().color = ground;
        foreach (var r in bushRenderers) r.color = bush;
        foreach (var r in cityRenderers) r.color = city;
        foreach (var r in cloudRenderers)
        {
            var c = cloud;
            c.a = r.color.a; // keep per-cloud alpha variety
            r.color = c;
        }
    }

    private void LateUpdate()
    {
        // smooth biome tint transition
        if (groundTiles.Count == 0) return;
        var g = groundTiles[0].GetComponent<SpriteRenderer>().color;
        bool transitioning = Mathf.Abs(g.r - tgtGround.r) + Mathf.Abs(g.g - tgtGround.g) + Mathf.Abs(g.b - tgtGround.b) > 0.004f;
        if (!transitioning) return;

        float k = Time.deltaTime * 1.5f;
        var bush = bushRenderers.Count > 0 ? bushRenderers[0].color : Color.white;
        var city = cityRenderers.Count > 0 ? cityRenderers[0].color : Color.white;
        var cloud = cloudRenderers.Count > 0 ? cloudRenderers[0].color : Color.white;
        var cloudFull = cloud; cloudFull.a = 1f;

        SetTints(Color.Lerp(g, tgtGround, k), Color.Lerp(bush, tgtBush, k),
                 Color.Lerp(cloudFull, tgtCloud, k), Color.Lerp(city, tgtCity, k));
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        bool playing = LogicScript.Instance != null && LogicScript.Instance.IsPlaying;

        foreach (var layer in layers)
        {
            float dx = layer.Speed * dt;
            foreach (var tile in layer.Tiles)
            {
                tile.position += Vector3.left * dx;
                if (tile.position.x < -halfWidth - layer.Width)
                    tile.position += Vector3.right * (layer.Width * layer.Tiles.Count);
            }
        }

        float gSpeed = playing
            ? GameConfig.PipeSpeed(LogicScript.Instance.PipesPassed)
            : 1.2f;
        float gdx = gSpeed * dt;
        foreach (var tile in groundTiles)
        {
            tile.position += Vector3.left * gdx;
            if (tile.position.x < -groundWidth * 1.5f)
                tile.position += Vector3.right * (groundWidth * groundTiles.Count);
        }
    }
}
