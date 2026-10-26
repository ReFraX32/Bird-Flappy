using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds, configures, pools and spawns pipes. Chooses obstacle variants:
/// Normal, Moving (oscillating gap), Blocked (shoot-the-target mini-game) and
/// CoinRush (arc of bonus coins). Pipe geometry is generated procedurally.
/// </summary>
public class PipeSpawnerScript : MonoBehaviour
{
    [Header("Kept for scene compatibility (unused — geometry is procedural now)")]
    public GameObject pipePrefab;
    public float spawnRate = 3f;
    public float heightOffset = 10f;

    private readonly Dictionary<PipeVariant, Queue<GameObject>> pools = new Dictionary<PipeVariant, Queue<GameObject>>();
    private readonly Queue<CoinScript> coinPool = new Queue<CoinScript>();
    private readonly List<CoinScript> activeCoins = new List<CoinScript>();

    private float timer;
    private float lastGapCenter;
    private int pipesSinceEvent;
    private int nextEventIn = GameConfig.FirstEventPipe;
    private float halfWidth;

    private LogicScript Logic => LogicScript.Instance;

    void Start()
    {
        foreach (PipeVariant v in System.Enum.GetValues(typeof(PipeVariant)))
            pools[v] = new Queue<GameObject>();
        halfWidth = GameConfig.CameraOrthoSize * Camera.main.aspect;
    }

    private void Update()
    {
        if (Logic == null || !Logic.IsPlaying) return;
        if (Logic.BossActive) { timer = 0f; return; }   // world holds its breath during boss fights
        timer += Time.deltaTime;
        if (timer >= Logic.CurrentSpawnInterval)
        {
            timer = 0f;
            SpawnPipe();
        }
    }

    public void ResetForRun()
    {
        timer = Logic != null ? Logic.CurrentSpawnInterval - 1.1f : 2f;
        lastGapCenter = 0f;
        pipesSinceEvent = 0;
        nextEventIn = GameConfig.FirstEventPipe;
        DespawnAllPipes();
        DespawnAllCoins();
    }

    // ---------------------------------------------------------------- spawning

    private void SpawnPipe()
    {
        halfWidth = GameConfig.CameraOrthoSize * Camera.main.aspect;
        float spawnX = halfWidth + 3.5f;
        float gap = Logic.CurrentGap;

        PipeVariant variant = RollVariant();
        if (variant == PipeVariant.WindZone)
        {
            // wind zone: a normal pipe plus a draft band over the next stretch of sky
            WindZoneScript.Spawn(new Vector3(spawnX + GameConfig.WindZoneWidth * 0.5f, 0f, 0f),
                GameConfig.WindForce * (Random.value < 0.5f ? 1f : -1f));
            variant = PipeVariant.Normal;
        }
        bool tight = variant == PipeVariant.TightGap;
        bool coinWall = variant == PipeVariant.CoinWall;
        if (tight) gap *= 0.74f;                      // tight squeeze: much narrower gap
        if (coinWall) variant = PipeVariant.Normal;   // coin wall rides a normal pipe
        float gapCenter = PickGapCenter(variant);

        var pipe = GetPipe(variant);
        if (pipe == null) return;
        bool coinRush = variant == PipeVariant.Normal && coinRushThisSpawn;
        coinRushThisSpawn = false;

        PipeFactory.ConfigurePipe(pipe, gap, variant);
        pipe.transform.position = new Vector3(spawnX, gapCenter, 0f);
        pipe.GetComponent<PipeMovementScript>().Configure(variant, gapCenter);
        foreach (var mid in pipe.GetComponentsInChildren<PipeMiddleScript>(true))
            mid.ResetTrigger();
        pipe.SetActive(true);

        // coin placement
        if (coinRush)
            SpawnCoinArc(spawnX, gapCenter, gap);
        else if (coinWall)
            SpawnCoinWall(spawnX + 2f, gapCenter, gap);
        else if (variant == PipeVariant.Normal && Random.value < 0.32f)
            SpawnCoinColumn(spawnX + 2f, gapCenter, gap);
    }

    private bool coinRushThisSpawn;
    private int eventsSpawned;

    private PipeVariant RollVariant()
    {
        pipesSinceEvent++;
        if (Logic.PipesPassed >= GameConfig.FirstEventPipe && pipesSinceEvent >= nextEventIn)
        {
            pipesSinceEvent = 0;
            nextEventIn = Random.Range(GameConfig.EventMinSpacing, GameConfig.EventMaxSpacing + 1);
            if (eventsSpawned++ == 0)
                return PipeVariant.Blocked;   // first event is always the shoot-the-target showcase
            int roll = Random.Range(0, 9);
            if (roll == 0) return PipeVariant.Blocked;
            if (roll == 1) return PipeVariant.Moving;
            if (roll == 2) return PipeVariant.DoubleGap;
            if (roll == 3) return PipeVariant.WindZone;
            if (roll == 4) return PipeVariant.SawGate;
            if (roll == 5) return PipeVariant.TightGap;
            if (roll == 6) return PipeVariant.CoinWall;
            coinRushThisSpawn = true;   // coin rush: normal pipe + bonus arc
            return PipeVariant.Normal;
        }
        return Random.value < GameConfig.RandomMovingChance ? PipeVariant.Moving : PipeVariant.Normal;
    }

    private float PickGapCenter(PipeVariant variant)
    {
        float limit = GameConfig.GapCenterLimit;
        float min = Mathf.Max(-limit, lastGapCenter - GameConfig.MaxGapCenterDelta);
        float max = Mathf.Min(limit, lastGapCenter + GameConfig.MaxGapCenterDelta);
        float y = Random.Range(min, max);
        if (variant == PipeVariant.Blocked) y = Mathf.Clamp(y, -3.5f, 3.5f);
        lastGapCenter = y;
        return y;
    }

    // ------------------------------------------------------------- coin spawns

    private void SpawnCoinColumn(float x, float gapCenter, float gap)
    {
        for (int i = -1; i <= 1; i++)
        {
            float y = Mathf.Clamp(gapCenter + i * 1.6f, gapCenter - gap / 2f + 1.2f, gapCenter + gap / 2f - 1.2f);
            SpawnCoin(new Vector3(x, y, 0f));
        }
    }

    private void SpawnCoinArc(float x, float gapCenter, float gap)
    {
        for (int i = 0; i < 8; i++)
        {
            float t = i / 7f;
            float cx = x + 3.2f - t * 6.4f;
            float cy = gapCenter + Mathf.Sin(t * Mathf.PI) * Mathf.Min(2.2f, gap / 2f - 1.4f);
            SpawnCoin(new Vector3(cx, cy, 0f));
        }
    }

    private void SpawnCoinWall(float x, float gapCenter, float gap)
    {
        int count = Mathf.Min(9, Mathf.FloorToInt(gap / 1.15f));
        for (int i = 0; i < count; i++)
        {
            float y = gapCenter + (i - (count - 1) / 2f) * 1.15f;
            SpawnCoin(new Vector3(x, y, 0f));
        }
    }

    private void SpawnCoin(Vector3 pos)
    {
        var coin = RentCoin();
        coin.Spawn(pos);
        activeCoins.Add(coin);
    }

    private CoinScript RentCoin()
    {
        while (coinPool.Count > 0)
        {
            var c = coinPool.Dequeue();
            if (c != null) return c;
        }
        var go = new GameObject("Coin", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(CoinScript));
        var r = go.GetComponent<SpriteRenderer>();
        r.sprite = ProceduralSprites.Get("coin");
        r.sortingOrder = 3;
        var col = go.GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.55f;
        return go.GetComponent<CoinScript>();
    }

    public void DespawnCoin(CoinScript coin)
    {
        activeCoins.Remove(coin);
        coin.gameObject.SetActive(false);
        coinPool.Enqueue(coin);
    }

    // ------------------------------------------------------------- pooling

    private GameObject GetPipe(PipeVariant variant)
    {
        var q = pools[variant];
        while (q.Count > 0)
        {
            var g = q.Dequeue();
            if (g != null) return g;
        }
        return PipeFactory.CreatePipe(variant);
    }

    public void DespawnPipe(PipeMovementScript mv)
    {
        if (mv == null) return;
        if (mv.Variant == PipeVariant.Blocked)
        {
            var blocker = mv.GetComponentInChildren<ClosedPipeBlocker>();
            if (blocker != null) blocker.Release();
        }
        mv.gameObject.SetActive(false);
        pools[mv.Variant].Enqueue(mv.gameObject);
    }

    private void DespawnAllPipes()
    {
        var all = FindObjectsByType<PipeMovementScript>(FindObjectsInactive.Exclude);
        foreach (var mv in all) DespawnPipe(mv);
    }

    /// <summary>Despawns pipes and coins within `radius` of a point (used on revive).</summary>
    public void ClearPipesNear(Vector3 pos, float radius)
    {
        var all = FindObjectsByType<PipeMovementScript>(FindObjectsInactive.Exclude);
        foreach (var mv in all)
            if ((mv.transform.position - pos).sqrMagnitude < radius * radius)
                DespawnPipe(mv);
        for (int i = activeCoins.Count - 1; i >= 0; i--)
            if (activeCoins[i] != null && (activeCoins[i].transform.position - pos).sqrMagnitude < radius * radius)
                DespawnCoin(activeCoins[i]);
    }

    private void DespawnAllCoins()
    {
        for (int i = activeCoins.Count - 1; i >= 0; i--)
            if (activeCoins[i] != null) DespawnCoin(activeCoins[i]);
        activeCoins.Clear();
    }
}

/// <summary>Procedural pipe construction: two tiled columns, caps, gap trigger, optional blocker.</summary>
public static class PipeFactory
{
    private const float PipeWidth = GameConfig.PipeWidth;
    private const float ColumnLength = 36f;
    private const float CapH = 0.6f;

    public static GameObject CreatePipe(PipeVariant variant)
    {
        var root = new GameObject("Pipe", typeof(PipeMovementScript));

        BuildColumn(root.transform, "Bottom", false);
        BuildColumn(root.transform, "Top", true);

        // gap score trigger
        var middle = new GameObject("Middle", typeof(BoxCollider2D), typeof(PipeMiddleScript));
        middle.transform.SetParent(root.transform, false);
        var trig = middle.GetComponent<BoxCollider2D>();
        trig.isTrigger = true;
        trig.size = new Vector2(1.2f, 8f); // rescaled on spawn

        // saw hazard (inactive unless SawGate)
        var sawGo = new GameObject("Saw", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(SawScript));
        sawGo.transform.SetParent(root.transform, false);
        var sawR = sawGo.GetComponent<SpriteRenderer>();
        sawR.sprite = ProceduralSprites.Get("bossBullet");
        sawR.sortingOrder = 4;
        sawGo.transform.localScale = Vector3.one * 3.2f;
        var sawC = sawGo.GetComponent<CircleCollider2D>();
        sawC.radius = 0.2f;
        sawGo.SetActive(false);

        // blocker group (inactive unless Blocked)
        var blockerGo = new GameObject("Blocker", typeof(ClosedPipeBlocker));
        blockerGo.transform.SetParent(root.transform, false);
        blockerGo.SetActive(false);

        var plate = new GameObject("Plate", typeof(SpriteRenderer), typeof(BoxCollider2D));
        plate.transform.SetParent(blockerGo.transform, false);
        var plateR = plate.GetComponent<SpriteRenderer>();
        plateR.sprite = ProceduralSprites.Get("blocker");
        plateR.drawMode = SpriteDrawMode.Sliced;
        plateR.sortingOrder = -1;
        var plateC = plate.GetComponent<BoxCollider2D>();
        plateC.size = new Vector2(1f, 1f); // rescaled on spawn
        plateC.offset = Vector2.zero;

        var target = new GameObject("TargetHit", typeof(SpriteRenderer), typeof(CircleCollider2D));
        target.transform.SetParent(blockerGo.transform, false);
        var targetR = target.GetComponent<SpriteRenderer>();
        targetR.sprite = ProceduralSprites.Get("target");
        targetR.sortingOrder = 4;
        var targetC = target.GetComponent<CircleCollider2D>();
        targetC.isTrigger = true;
        targetC.radius = 0.7f;

        root.SetActive(false);
        return root;
    }

    private static void BuildColumn(Transform parent, string name, bool top)
    {
        var col = new GameObject(name, typeof(BoxCollider2D));
        col.transform.SetParent(parent, false);
        var sign = top ? 1f : -1f;

        var bodyGo = new GameObject("Body", typeof(SpriteRenderer));
        bodyGo.transform.SetParent(col.transform, false);
        var body = bodyGo.GetComponent<SpriteRenderer>();
        body.sprite = ProceduralSprites.Get("pipeBody");
        body.drawMode = SpriteDrawMode.Tiled;
        body.tileMode = SpriteTileMode.Continuous;
        body.sortingOrder = -2;

        var capGo = new GameObject("Cap", typeof(SpriteRenderer));
        capGo.transform.SetParent(col.transform, false);
        var cap = capGo.GetComponent<SpriteRenderer>();
        cap.sprite = ProceduralSprites.Get("pipeCap");
        cap.sortingOrder = -1;
        cap.transform.localScale = new Vector3(2.75f, top ? -1.4f : 1.4f, 1f);
    }

    /// <summary>Re-lays out pipe geometry for the given gap size.</summary>
    public static void ConfigurePipe(GameObject pipe, float gap, PipeVariant variant)
    {
        Color tint = BackgroundDesigner.Instance != null ? BackgroundDesigner.Instance.PipeTint : Color.white;
        foreach (var name in new[] { "Bottom", "Top" })
        {
            bool top = name == "Top";
            var col = pipe.transform.Find(name);
            float edge = (gap / 2f) * (top ? 1f : -1f);
            float length = ColumnLength - gap / 2f;
            float bodyCenter = edge + sign(top) * (length / 2f);

            var body = col.Find("Body").GetComponent<SpriteRenderer>();
            body.size = new Vector2(PipeWidth, length);
            body.color = tint;
            body.transform.localPosition = new Vector3(0f, bodyCenter, 0f);

            var cap = col.Find("Cap");
            cap.localPosition = new Vector3(0f, edge - sign(top) * (CapH * 0.5f), 0f);
            cap.GetComponent<SpriteRenderer>().color = tint;

            var box = col.GetComponent<BoxCollider2D>();
            box.offset = new Vector2(0f, bodyCenter);
            box.size = new Vector2(PipeWidth, length);
        }

        if (variant == PipeVariant.DoubleGap)
        {
            ConfigureDoubleGap(pipe, gap, tint);
            return;
        }

        var middle = pipe.transform.Find("Middle").GetComponent<BoxCollider2D>();
        middle.size = new Vector2(1.2f, Mathf.Max(1f, gap - 1.0f));
        middle.transform.localPosition = Vector3.zero;
        pipe.transform.Find("Middle2")?.gameObject.SetActive(false);
        pipe.transform.Find("Divider")?.gameObject.SetActive(false);

        // saw gate: normal gap geometry plus an oscillating blade
        var saw = pipe.transform.Find("Saw");
        bool sawGate = variant == PipeVariant.SawGate;
        if (saw != null)
        {
            saw.gameObject.SetActive(sawGate);
            if (sawGate) saw.GetComponent<SawScript>().Setup(gap);
        }

        // tight squeeze: hazard-striped tint on the columns
        if (variant == PipeVariant.TightGap)
        {
            var warn = Color.Lerp(tint, new Color(1f, 0.25f, 0.2f), 0.6f);
            foreach (var colName in new[] { "Bottom", "Top" })
            {
                var col = pipe.transform.Find(colName);
                col.Find("Body").GetComponent<SpriteRenderer>().color = warn;
                col.Find("Cap").GetComponent<SpriteRenderer>().color = warn;
            }
        }

        var blockerGo = pipe.transform.Find("Blocker").gameObject;
        bool blocked = variant == PipeVariant.Blocked;
        blockerGo.SetActive(blocked);
        if (blocked)
        {
            blockerGo.transform.localPosition = Vector3.zero;
            var plate = blockerGo.transform.Find("Plate");
            plate.localScale = Vector3.one;
            plate.GetComponent<SpriteRenderer>().size = new Vector2(PipeWidth + 0.6f, gap - 0.8f);
            var plateCol = plate.GetComponent<BoxCollider2D>();
            plateCol.size = new Vector2(PipeWidth + 0.6f, gap - 0.8f);
            plateCol.offset = Vector2.zero;

            var target = blockerGo.transform.Find("TargetHit");
            target.localPosition = Vector3.zero;
            target.localScale = Vector3.one * 1.15f;

            blockerGo.GetComponent<ClosedPipeBlocker>().ForceClose(GameConfig.BlockerHp);
        }
    }

    private static float sign(bool top) => top ? 1f : -1f;

    /// <summary>Two stacked gaps with a dividing column — the player picks a lane.</summary>
    private static void ConfigureDoubleGap(GameObject pipe, float gap, Color tint)
    {
        const float LaneOffset = 4.2f;   // gap centers at ±LaneOffset
        float laneGap = Mathf.Min(6.4f, gap * 0.78f);
        float halfLane = laneGap * 0.5f;
        float innerEdge = LaneOffset + halfLane;    // 7.4-ish: where the columns stop

        // bottom column: floor → bottom lane; top column: top lane → ceiling
        LayoutColumn(pipe.transform.Find("Bottom"), tint, -ColumnLength, -innerEdge, capAtTop: true);
        LayoutColumn(pipe.transform.Find("Top"), tint, innerEdge, ColumnLength, capAtTop: false);

        // middle divider (created on first use, pooled with the pipe afterwards)
        var div = pipe.transform.Find("Divider");
        if (div == null)
        {
            var divGo = new GameObject("Divider", typeof(SpriteRenderer), typeof(BoxCollider2D));
            divGo.transform.SetParent(pipe.transform, false);
            var dr = divGo.GetComponent<SpriteRenderer>();
            dr.sprite = ProceduralSprites.Get("pipeBody");
            dr.drawMode = SpriteDrawMode.Tiled;
            dr.sortingOrder = -2;
            div = divGo.transform;
        }
        float divHalf = LaneOffset - halfLane;
        LayoutDivider(div, tint, -divHalf, divHalf);

        // score triggers — one per lane
        var m1 = pipe.transform.Find("Middle").GetComponent<BoxCollider2D>();
        m1.size = new Vector2(1.2f, laneGap - 1.2f);
        m1.transform.localPosition = new Vector3(0f, -LaneOffset, 0f);
        pipe.transform.Find("Middle").gameObject.SetActive(true);

        var m2Go = pipe.transform.Find("Middle2");
        if (m2Go == null)
        {
            var go = new GameObject("Middle2", typeof(BoxCollider2D), typeof(PipeMiddleScript));
            go.transform.SetParent(pipe.transform, false);
            m2Go = go.transform;
        }
        m2Go.gameObject.SetActive(true);
        var m2 = m2Go.GetComponent<BoxCollider2D>();
        m2.isTrigger = true;
        m2.size = new Vector2(1.2f, laneGap - 1.2f);
        m2Go.localPosition = new Vector3(0f, LaneOffset, 0f);

        // blocker group unused here
        pipe.transform.Find("Blocker")?.gameObject.SetActive(false);
    }

    private static void LayoutColumn(Transform col, Color tint, float from, float to, bool capAtTop)
    {
        float length = to - from;
        float center = (from + to) / 2f;
        var body = col.Find("Body").GetComponent<SpriteRenderer>();
        body.size = new Vector2(PipeWidth, length);
        body.color = tint;
        body.transform.localPosition = new Vector3(0f, center, 0f);
        var cap = col.Find("Cap");
        float capEdge = capAtTop ? to : from;
        cap.localPosition = new Vector3(0f, capEdge - sign(capAtTop) * 0.3f, 0f);
        cap.GetComponent<SpriteRenderer>().color = tint;
        var box = col.GetComponent<BoxCollider2D>();
        box.offset = new Vector2(0f, center);
        box.size = new Vector2(PipeWidth, length);
    }

    private static void LayoutDivider(Transform div, Color tint, float from, float to)
    {
        float length = to - from;
        float center = (from + to) / 2f;
        var body = div.GetComponent<SpriteRenderer>();
        body.size = new Vector2(PipeWidth, length);
        body.color = tint;
        div.localPosition = new Vector3(0f, center, 0f);
        var box = div.GetComponent<BoxCollider2D>();
        box.offset = new Vector2(0f, center);
        box.size = new Vector2(PipeWidth, length);
    }
}
