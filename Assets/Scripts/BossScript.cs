using UnityEngine;

/// <summary>
/// Biome gatekeeper boss: enters from the right after 9 pipes of a biome, hovers and
/// oscillates, lobs slow projectiles at the bird, and must be shot down by the
/// auto-turret. Defeat pays a big coin reward and unlocks the biome transition.
/// </summary>
public class BossScript : MonoBehaviour
{
    public static BossScript Active { get; private set; }

    public int Hp { get; private set; }
    public int MaxHp { get; private set; }
    public bool Dead { get; private set; }

    private SpriteRenderer body;
    private Transform hpFill;
    private float fireTimer;
    private float oscTime;
    private float targetX;
    private int biomeIndex;
    private BirdScript bird;
    private Color tint = Color.white;

    public static BossScript Spawn(int biome)
    {
        var go = new GameObject("Boss", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(BossScript));
        var boss = go.GetComponent<BossScript>();
        boss.Setup(biome);
        return boss;
    }

    private void Setup(int biome)
    {
        biomeIndex = biome;
        MaxHp = GameConfig.BossBaseHp + GameConfig.BossHpPerBiome * biome;
        Hp = MaxHp;

        var cam = Camera.main;
        float halfW = GameConfig.CameraOrthoSize * cam.aspect;
        transform.position = new Vector3(halfW + 8f, 0f, 0);
        targetX = GameConfig.BossHoverX;

        body = GetComponent<SpriteRenderer>();
        body.sprite = ProceduralSprites.Get("boss");
        body.sortingOrder = 2;
        body.transform.localScale = Vector3.one * 5.5f;
        // each biome's guardian wears that biome's colors
        var palette = BiomePalette.Get(Mathf.Abs(biome) % BiomePalette.All.Count);
        tint = Color.Lerp(Color.white, palette.accent, 0.65f);
        body.color = tint;

        var col = GetComponent<CircleCollider2D>();
        col.radius = 0.46f; // sprite-space, scaled by transform

        // HP bar above the boss
        var barGo = new GameObject("HpBar", typeof(SpriteRenderer));
        barGo.transform.SetParent(transform, false);
        var barBg = barGo.GetComponent<SpriteRenderer>();
        barBg.sprite = ProceduralSprites.Get("white");
        barBg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
        barBg.sortingOrder = 3;
        barBg.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        barBg.transform.localScale = new Vector3(1.1f, 0.12f, 1f);

        var fillGo = new GameObject("HpFill", typeof(SpriteRenderer));
        fillGo.transform.SetParent(barGo.transform, false);
        hpFill = fillGo.transform;
        var fill = fillGo.GetComponent<SpriteRenderer>();
        fill.sprite = ProceduralSprites.Get("white");
        fill.color = new Color(0.92f, 0.28f, 0.3f);
        fill.sortingOrder = 4;
        fill.transform.localPosition = new Vector3(0f, 0f, 0f);
        fill.transform.localScale = new Vector3(1.06f, 0.08f, 1f);

        bird = LogicScript.Instance != null ? LogicScript.Instance.Bird : null;
        Active = this;

        LogicScript.Instance?.AnnounceBoss(this);
        ProceduralAudio.PlaySfx("alarm", 1f);
    }

    private void OnDestroy()
    {
        if (Active == this) Active = null;
    }

    private void Update()
    {
        if (Dead) return;
        var logic = LogicScript.Instance;
        if (logic == null || !logic.IsPlaying) return;

        // enter, then hover-oscillate
        float x = Mathf.MoveTowards(transform.position.x, targetX, GameConfig.BossEnterSpeed * Time.deltaTime);
        oscTime += Time.deltaTime * GameConfig.BossOscSpeed;
        float y = Mathf.Sin(oscTime) * GameConfig.BossOscAmp;
        transform.position = new Vector3(x, y, 0f);

        // menacing pulse
        body.transform.localScale = Vector3.one * (5.5f + Mathf.Sin(oscTime * 3f) * 0.18f);

        // lob projectiles at the bird
        fireTimer -= Time.deltaTime;
        if (x < targetX + 0.5f && fireTimer <= 0f && bird != null && bird.birdIsAlive)
        {
            fireTimer = Mathf.Max(0.9f, GameConfig.BossFireInterval - 0.08f * biomeIndex);
            FireAt(bird.transform.position);
        }
    }

    private void FireAt(Vector3 target)
    {
        var dir = ((Vector2)(target - transform.position)).normalized;
        var go = new GameObject("BossBullet", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(EnemyProjectile));
        go.transform.position = transform.position + (Vector3)(dir * 3f);
        var r = go.GetComponent<SpriteRenderer>();
        r.sprite = ProceduralSprites.Get("bossBullet");
        r.sortingOrder = 12;
        go.transform.localScale = Vector3.one * 2.4f;
        var col = go.GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.22f;
        go.GetComponent<EnemyProjectile>().Launch(dir * GameConfig.BossProjectileSpeed);
        ProceduralAudio.PlaySfx("shoot", 0.5f, 0.2f);
    }

    public void RegisterHit(int damage, Vector3 at)
    {
        if (Dead) return;
        Hp -= damage;
        EffectsManager.Instance.Burst(at, new Color(1f, 0.5f, 0.3f), 6, 5f, 0.2f, 0.4f, 6f);
        ProceduralAudio.PlaySfx("hit", 0.5f);

        if (hpFill != null)
        {
            float frac = Mathf.Max(0f, (float)Hp / MaxHp);
            hpFill.localScale = new Vector3(1.06f * frac, 0.08f, 1f);
            hpFill.localPosition = new Vector3(-(1.06f * (1f - frac)) / 2f, 0f, 0f);
        }
        // hit flash
        body.color = Color.Lerp(tint, Color.red, 0.55f);
        EffectsManager.Instance?.FlashScreen(new Color(1f, 0.3f, 0.2f), 0.18f, 0.25f);
        var restoreTint = tint;
        this.Invoke(() => { if (body != null) body.color = restoreTint; }, 0.08f);

        if (Hp <= 0) Die();
    }

    private void Die()
    {
        Dead = true;
        EffectsManager.Instance.Burst(transform.position, new Color(1f, 0.6f, 0.2f), 40, 10f, 0.5f, 1.1f, 8f);
        EffectsManager.Instance.Burst(transform.position, new Color(1f, 0.9f, 0.4f), 25, 7f, 0.4f, 0.9f, 6f);
        EffectsManager.Instance.Shake(0.7f);
        ProceduralAudio.PlaySfx("explode", 1f);
        ProceduralAudio.PlaySfx("buy", 1f);
        LogicScript.Instance?.BossDefeated(this);
        Destroy(gameObject, 0.05f);
    }
}

/// <summary>Tiny inline-delay helper (avoids a Coroutine for one-shot callbacks).</summary>
public static class InvokeExtension
{
    public static void Invoke(this MonoBehaviour mb, System.Action action, float delay)
    {
        mb.StartCoroutine(Delayed(action, delay));
    }

    private static System.Collections.IEnumerator Delayed(System.Action action, float delay)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }
}
