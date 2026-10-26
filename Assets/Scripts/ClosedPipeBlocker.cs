using UnityEngine;

/// <summary>Armored plate that seals a pipe gap; the auto-turret (or precise flying) must shoot the target to open it.</summary>
public class ClosedPipeBlocker : MonoBehaviour
{
    public static ClosedPipeBlocker FirstActive { get; private set; }

    public bool IsOpen { get; private set; }
    public int Hp { get; private set; }

    private Transform plate;
    private SpriteRenderer targetRenderer;
    private BoxCollider2D solidCollider;
    private float alarmTimer;
    private Color targetBaseColor = new Color(1f, 1f, 1f, 1f);

    public void Setup(int hp)
    {
        Hp = hp;
        IsOpen = false;
        plate = transform.Find("Plate");
        var target = transform.Find("TargetHit");
        if (target != null) targetRenderer = target.GetComponent<SpriteRenderer>();
        solidCollider = plate != null ? plate.GetComponent<BoxCollider2D>() : null;
        if (plate != null)
        {
            plate.localScale = Vector3.one;
            plate.gameObject.SetActive(true);
        }
        if (targetRenderer != null) targetRenderer.color = targetBaseColor;
        FirstActive = this;
    }

    private void OnDestroy()
    {
        if (FirstActive == this) FirstActive = null;
    }

    public void Release()
    {
        if (FirstActive == this) FirstActive = null;
    }

    private void Update()
    {
        if (IsOpen) return;
        var logic = LogicScript.Instance;
        if (logic == null || !logic.IsPlaying) return;

        var bird = logic.Bird;
        if (bird == null) return;
        float dist = transform.position.x - bird.transform.position.x;
        if (dist > 0f && dist < GameConfig.BlockerWarningDist)
        {
            alarmTimer -= Time.deltaTime;
            if (alarmTimer <= 0f)
            {
                alarmTimer = 0.55f;
                ProceduralAudio.PlaySfx("alarm", 0.5f);
            }
            if (targetRenderer != null)
                targetRenderer.color = Color.Lerp(targetBaseColor, Color.red, Mathf.PingPong(Time.time * 3f, 1f) * 0.5f);
        }
    }

    /// <summary>Called by ProjectileScript when a bullet hits the target.</summary>
    public void RegisterBulletHit(Vector3 at, int damage)
    {
        if (IsOpen) return;
        Hp -= damage;
        EffectsManager.Instance.MetalBurst(at);
        ProceduralAudio.PlaySfx("hit", 0.45f);
        if (targetRenderer != null)
            targetRenderer.color = Color.Lerp(targetBaseColor, new Color(0.4f, 0.4f, 0.4f), 1f - Hp / 3f);

        if (Hp <= 0) Open();
    }

    private void Open()
    {
        IsOpen = true;
        if (solidCollider != null) solidCollider.enabled = false;
        if (FirstActive == this) FirstActive = null;
        ProceduralAudio.PlaySfx("open", 1f);
        EffectsManager.Instance.MetalBurst(transform.position);
        EffectsManager.Instance.Shake(0.25f);
        if (plate != null)
            Tween.Scale(plate, plate.localScale, new Vector3(1f, 0.02f, 1f), 0.35f, Tween.EaseInQuad,
                () => { if (plate != null) plate.gameObject.SetActive(false); });
    }

    /// <summary>Hard reset for pooling.</summary>
    public void ForceClose(int hp)
    {
        Setup(hp);
        if (plate != null) plate.gameObject.SetActive(true);
        if (solidCollider != null) solidCollider.enabled = true;
    }
}
