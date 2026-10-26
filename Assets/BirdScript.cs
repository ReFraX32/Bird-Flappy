using UnityEngine;

/// <summary>
/// Player bird: flap physics + tilt, shield bubbles, invincibility blink, auto-turret
/// for blocked-pipe mini-games, and death tumble. Input supports Space / mouse / touch.
/// </summary>
public class BirdScript : MonoBehaviour
{
    [Header("Kept from original scene setup")]
    public Rigidbody2D birdRigidbody;
    public float flapStrength = GameConfig.FlapStrength;
    public LogicScript logic;
    public bool birdIsAlive = true;

    private bool shouldFlap;
    private float turretCooldown;
    private SpriteRenderer spriteRenderer;
    private GameObject bubbleVisual;
    private Vector3 menuBasePos;
    private float menuTime;
    private int shields;
    private float invincibleUntil;
    private Quaternion deathSpin;

    public int Shields => shields;
    public bool IsInvincible => Time.time < invincibleUntil;

    void Start()
    {
        if (birdRigidbody == null) birdRigidbody = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        // adopt the same default sprite material the runtime-created renderers use —
        // the scene's serialized material can be pipeline-incompatible (renders magenta)
        var probe = new GameObject("MaterialProbe").AddComponent<SpriteRenderer>();
        spriteRenderer.sharedMaterial = probe.sharedMaterial;
        Destroy(probe.gameObject);

        if (logic == null)
        {
            logic = GameObject.FindGameObjectWithTag("Logic")?.GetComponent<LogicScript>();
            if (logic == null)
                Debug.LogError("LogicScript not found. Ensure there's a GameObject with the 'Logic' tag and LogicScript attached.");
        }

        int featherLvl = SaveSystem.GetUpgradeLevel(UpgradeCatalog.IndexOf("feather"));
        birdRigidbody.gravityScale = GameConfig.GravityScale * (1f - Mathf.Min(0.35f, 0.06f * featherLvl));
        birdRigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
        birdRigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BuildBubble();
        EnterMenuMode();
    }

    private void BuildBubble()
    {
        bubbleVisual = new GameObject("ShieldBubble", typeof(SpriteRenderer));
        bubbleVisual.transform.SetParent(transform, false);
        var r = bubbleVisual.GetComponent<SpriteRenderer>();
        r.sprite = ProceduralSprites.Get("shieldBubble");
        r.sortingOrder = 5;
        r.transform.localScale = Vector3.one * 4.2f;
        bubbleVisual.SetActive(false);
    }

    // ------------------------------------------------------------ states

    public void EnterMenuMode()
    {
        birdIsAlive = true;
        shields = 0;
        birdRigidbody.simulated = false;
        birdRigidbody.linearVelocity = Vector2.zero;
        menuBasePos = new Vector3(-7.5f, 0f, 0f);
        transform.position = menuBasePos;
        transform.rotation = Quaternion.identity;
        spriteRenderer.enabled = true;
        bubbleVisual.SetActive(false);
        menuTime = 0f;
    }

    public void BeginPlay()
    {
        birdIsAlive = true;
        birdRigidbody.simulated = true;
        birdRigidbody.position = new Vector2(-7.5f, 2f);
        birdRigidbody.linearVelocity = Vector2.zero;
        transform.rotation = Quaternion.identity;
        shields = SaveSystem.GetUpgradeLevel(UpgradeCatalog.IndexOf("shield"));
        UpdateBubble();
        invincibleUntil = Time.time + 0.4f;
    }

    public void SetInvincible(float seconds) => invincibleUntil = Time.time + seconds;

    public void Revive(Vector3 pos)
    {
        birdIsAlive = true;
        birdRigidbody.simulated = true;
        birdRigidbody.position = pos;
        birdRigidbody.linearVelocity = Vector2.up * 6f;
        transform.rotation = Quaternion.identity;
        spriteRenderer.enabled = true;
        SetInvincible(GameConfig.ReviveInvincibleTime);
        UpdateBubble();

        // Phoenix revive flare & golden particle ring
        EffectsManager.Instance?.Burst(pos, new Color(1f, 0.85f, 0.3f), 30, 8f, 0.4f, 0.9f, 4f);
        EffectsManager.Instance?.Burst(pos, new Color(1f, 0.4f, 0.1f), 20, 6f, 0.3f, 0.7f, 3f);
        EffectsManager.Instance?.FlashScreen(new Color(1f, 0.9f, 0.4f), 0.35f, 0.4f);
        EffectsManager.Instance?.Shake(0.35f);

        // Pulse scale animation
        Vector3 baseScale = transform.localScale;
        transform.localScale = baseScale * 0.3f;
        Tween.Scale(transform, baseScale * 0.3f, baseScale * 1.35f, 0.18f, Tween.EaseOutQuad, () =>
        {
            if (this != null)
                Tween.Scale(transform, baseScale * 1.35f, baseScale, 0.14f, Tween.EaseInQuad);
        });
        StartCoroutine(BlinkCo(GameConfig.ReviveInvincibleTime));
    }

    private void UpdateBubble() => bubbleVisual.SetActive(birdIsAlive && shields > 0);

    // ------------------------------------------------------------ input & update

    void Update()
    {
        if (logic == null) return;

        switch (logic.State)
        {
            case LogicScript.GameState.Menu:
                menuTime += Time.deltaTime;
                transform.position = menuBasePos + new Vector3(0f, Mathf.Sin(menuTime * 2.2f) * 0.55f, 0f);
                transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(menuTime * 2.2f + 0.6f) * 6f);
                return;

            case LogicScript.GameState.Playing:
                HandleFlapInput();
                HandleTurret();
                CheckBounds();
                UpdateTilt();
                break;

            case LogicScript.GameState.Dying:
                // tumble freely; logic monitors for game over transition
                break;
        }
    }

    private void HandleFlapInput()
    {
        bool pressed = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            pressed = true;

        if (pressed && !logic.IsPointerOverUI)
        {
            shouldFlap = true;
            logic.NotifyFlap();
        }
    }

    private void HandleTurret()
    {
        turretCooldown -= Time.deltaTime;
        int ssLvl = SaveSystem.GetUpgradeLevel(UpgradeCatalog.IndexOf("sharpshooter"));
        float rate = Mathf.Max(0.12f, GameConfig.TurretCooldown / (1f + 0.18f * ssLvl));

        // prefer an active boss; otherwise the nearest blocker target
        Transform target = null;
        if (logic.BossActive && BossScript.Active != null)
            target = BossScript.Active.transform;
        else if (logic.CurrentBlocker != null)
            target = logic.CurrentBlocker.transform;
        if (turretCooldown > 0f || target == null) return;
        float dist = target.position.x - transform.position.x;
        if (dist > 0f && dist < GameConfig.BlockerWarningDist * 1.6f)
        {
            turretCooldown = rate;
            var go = new GameObject("Bullet", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(ProjectileScript));
            go.transform.position = transform.position + Vector3.right * 2.4f; // clear of own collider (r=1.59)
            var r = go.GetComponent<SpriteRenderer>();
            r.sprite = ProceduralSprites.Get("bullet");
            r.sortingOrder = 12;
            go.GetComponent<ProjectileScript>().Launch(Vector2.right * 24f, 1 + ssLvl / 4);
            ProceduralAudio.PlaySfx("shoot", 0.7f);
        }
    }

    private void CheckBounds()
    {
        if (transform.position.y < GameConfig.GroundY + 0.8f)
        {
            Crash(true);
        }
        else if (transform.position.y > GameConfig.CeilingY)
        {
            birdRigidbody.position = new Vector2(birdRigidbody.position.x, GameConfig.CeilingY);
            birdRigidbody.linearVelocity = Vector2.zero;
        }
    }

    private void UpdateTilt()
    {
        // classic Flappy feel: snap up on a flap, but dive the nose slowly and only
        // when genuinely falling fast — small dips keep the beak near level
        float vy = birdRigidbody.linearVelocity.y;
        float target;
        float speed;
        if (vy > 0.5f)
        {
            target = GameConfig.BirdRotationUp;
            speed = GameConfig.RotationSpeed;
        }
        else
        {
            float fallT = Mathf.Clamp01((-vy - 1.5f) / (GameConfig.MaxFallSpeed * 0.95f));
            target = Mathf.Lerp(0f, GameConfig.BirdRotationDown, fallT * fallT);
            speed = GameConfig.RotationSpeed * 0.45f;   // lazy dive rotation
        }
        float current = transform.eulerAngles.z > 180 ? transform.eulerAngles.z - 360 : transform.eulerAngles.z;
        float next = Mathf.MoveTowards(current, target, speed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0, 0, next);
    }

    void FixedUpdate()
    {
        if (shouldFlap)
        {
            birdRigidbody.linearVelocity = new Vector2(0f, GameConfig.FlapStrength);
            shouldFlap = false;
        }
        // clamp terminal fall speed for a controllable classic-Flappy dive
        var v = birdRigidbody.linearVelocity;
        if (v.y < -GameConfig.MaxFallSpeed)
            v.y = -GameConfig.MaxFallSpeed;

        // soft horizontal spring back to the camera anchor — collisions and wind can
        // nudge the bird, but she never drifts off-screen
        if (logic != null && logic.State == LogicScript.GameState.Playing)
        {
            float dx = GameConfig.BirdAnchorX - birdRigidbody.position.x;
            v.x = Mathf.Clamp(dx * GameConfig.BirdAnchorSpring, -GameConfig.BirdAnchorMaxPull, GameConfig.BirdAnchorMaxPull);
        }
        else
        {
            v.x = 0f;
        }
        birdRigidbody.linearVelocity = v;
    }

    // ------------------------------------------------------------ collisions

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (logic == null || logic.State != LogicScript.GameState.Playing) return;
        Crash(false);
    }

    public void Crash(bool fromBounds)
    {
        if (!birdIsAlive || IsInvincible || logic == null) return;
        logic.BirdCrashed();
    }

    /// <summary>Called by LogicScript when a shield absorbed the hit.</summary>
    public void AbsorbHitWithShield()
    {
        shields = Mathf.Max(0, shields - 1);
        SetInvincible(GameConfig.ShieldInvincibleTime);
        UpdateBubble();
        EffectsManager.Instance.PopBurst(transform.position);
        ProceduralAudio.PlaySfx("shield", 0.9f);
        StartCoroutine(BlinkCo(GameConfig.ShieldInvincibleTime));
    }

    public void OnDeath()
    {
        birdIsAlive = false;
        bubbleVisual.SetActive(false);
        // tumble: unfreeze rotation, bounce back
        birdRigidbody.constraints = RigidbodyConstraints2D.None;
        birdRigidbody.linearVelocity = new Vector2(GameConfig.DeathBounceX, 8f);
        birdRigidbody.angularVelocity = -260f;
    }

    public void RestoreForPlay()
    {
        birdRigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
        birdRigidbody.angularVelocity = 0f;
        transform.rotation = Quaternion.identity;
    }

    private System.Collections.IEnumerator BlinkCo(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            spriteRenderer.enabled = Mathf.PingPong(t * 12f, 1f) > 0.35f;
            yield return null;
        }
        spriteRenderer.enabled = true;
    }
}
