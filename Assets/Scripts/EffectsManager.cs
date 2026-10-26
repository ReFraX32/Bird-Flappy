using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight runtime effects: sprite-burst particles, floating text, camera shake,
/// death vignette flash. Uses no ParticleSystem — pooled SpriteRenderers driven manually
/// (fully deterministic under URP 2D and mobile-friendly).
/// </summary>
public class EffectsManager : MonoBehaviour
{
    public static EffectsManager Instance { get; private set; }

    private class BurstParticle
    {
        public SpriteRenderer Renderer;
        public Vector3 Velocity;
        public float AngularVel;
        public float Life, MaxLife;
        public float StartScale, Drag, Gravity;
        public Color StartColor;
    }

    private readonly List<BurstParticle> particles = new List<BurstParticle>();
    private readonly List<SpriteRenderer> freeParticles = new List<SpriteRenderer>();

    private Camera cam;
    private Vector3 camOrigin;
    private float shakeAmount;
    private Transform shakeTarget;

    private Image vignette;

    private void Awake()
    {
        Instance = this;
        cam = Camera.main;
        shakeTarget = cam ? cam.transform : null;
        if (shakeTarget != null) camOrigin = shakeTarget.localPosition;
        BuildVignette();
    }

    private void BuildVignette()
    {
        var canvasGo = new GameObject("EffectsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 45f;
        canvas.sortingOrder = 50; // above game UI (10)
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var imgGo = new GameObject("Vignette", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imgGo.transform.SetParent(canvasGo.transform, false);
        vignette = imgGo.GetComponent<Image>();
        vignette.sprite = ProceduralSprites.Get("vignette");
        vignette.type = Image.Type.Simple;
        var rt = (RectTransform)vignette.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        vignette.raycastTarget = false;
        vignette.color = new Color(1, 1, 1, 0);
    }

    // ------------------------------------------------------------- particles

    public void Burst(Vector3 pos, Color color, int count, float speed = 6f, float size = 0.35f,
                      float life = 0.8f, float gravity = 9f, Sprite sprite = null)
    {
        for (int i = 0; i < count; i++)
        {
            var r = RentParticle(sprite);
            float ang = Random.value * Mathf.PI * 2f;
            float spd = speed * Random.Range(0.35f, 1f);
            r.transform.position = pos + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f), 0);
            r.color = color;
            r.transform.localScale = Vector3.one * size * Random.Range(0.6f, 1.3f);
            particles.Add(new BurstParticle
            {
                Renderer = r,
                Velocity = new Vector3(Mathf.Cos(ang) * spd, Mathf.Sin(ang) * spd, 0),
                AngularVel = Random.Range(-540f, 540f),
                Life = life * Random.Range(0.7f, 1.2f),
                MaxLife = life,
                StartScale = size * Random.Range(0.6f, 1.3f),
                Drag = 2.2f,
                Gravity = gravity,
                StartColor = color
            });
            var p = particles[particles.Count - 1];
            p.MaxLife = p.Life;
        }
    }

    public void FeatherBurst(Vector3 pos) =>
        Burst(pos, new Color(1f, 0.85f, 0.55f), 14, 7f, 0.3f, 0.9f, 7f);

    public void CoinBurst(Vector3 pos) =>
        Burst(pos, new Color(1f, 0.82f, 0.3f), 10, 5.5f, 0.22f, 0.6f, 8f);

    public void PopBurst(Vector3 pos) =>
        Burst(pos, new Color(0.55f, 0.9f, 1f), 12, 6f, 0.25f, 0.5f, 4f);

    public void MetalBurst(Vector3 pos) =>
        Burst(pos, new Color(0.6f, 0.65f, 0.75f), 12, 7f, 0.25f, 0.7f, 12f);

    private SpriteRenderer RentParticle(Sprite sprite)
    {
        SpriteRenderer r;
        if (freeParticles.Count > 0 && freeParticles[freeParticles.Count - 1] != null)
        {
            r = freeParticles[freeParticles.Count - 1];
            freeParticles.RemoveAt(freeParticles.Count - 1);
        }
        else
        {
            var go = new GameObject("particle", typeof(SpriteRenderer));
            r = go.GetComponent<SpriteRenderer>();
            r.sortingOrder = 20;
        }
        r.sprite = sprite != null ? sprite : ProceduralSprites.Get("white");
        r.gameObject.SetActive(true);
        r.transform.rotation = Quaternion.identity;
        return r;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            var p = particles[i];
            if (p.Renderer == null) { particles.RemoveAt(i); continue; }
            p.Life -= dt;
            if (p.Life <= 0f)
            {
                p.Renderer.gameObject.SetActive(false);
                freeParticles.Add(p.Renderer);
                particles.RemoveAt(i);
                continue;
            }
            p.Velocity += Vector3.down * (p.Gravity * dt);
            p.Velocity *= Mathf.Max(0f, 1f - p.Drag * dt);
            p.Renderer.transform.position += p.Velocity * dt;
            p.Renderer.transform.Rotate(0, 0, p.AngularVel * dt);
            float a = Mathf.Clamp01(p.Life / p.MaxLife);
            var c = p.StartColor;
            c.a = a;
            p.Renderer.color = c;
            float s = Mathf.Lerp(0.2f, 1f, a);
            p.Renderer.transform.localScale = Vector3.one * p.StartScale * s;
        }

        // camera shake
        if (shakeAmount > 0.001f && shakeTarget != null)
        {
            shakeAmount = Mathf.Lerp(shakeAmount, 0f, Time.unscaledDeltaTime * 7f);
            shakeTarget.localPosition = camOrigin + (Vector3)(Random.insideUnitCircle * shakeAmount);
            if (shakeAmount < 0.01f)
            {
                shakeTarget.localPosition = camOrigin;
                shakeAmount = 0f;
            }
        }
    }

    public void Shake(float amount) => shakeAmount = Mathf.Max(shakeAmount, amount);

    // ------------------------------------------------------------- floating text

    public void FloatingText(Vector3 worldPos, string message, Color color, float size = 42f)
    {
        var go = new GameObject("FloatText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        // parent to the main game UI canvas (sorted above world)
        var mainCanvas = GameObject.Find("GameUICanvas");
        go.transform.SetParent(mainCanvas != null ? mainCanvas.transform : null, false);
        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = Mathf.RoundToInt(size);
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.text = message;
        text.raycastTarget = false;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.85f);
        outline.effectDistance = new Vector2(2, -2);

        var screen = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
        var rt = (RectTransform)go.transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mainCanvas.transform as RectTransform, screen, mainCanvas.GetComponent<Canvas>().worldCamera, out var local);
        rt.localPosition = local;

        StartCoroutine(FloatTextCo(rt, local));
    }

    private IEnumerator FloatTextCo(RectTransform rt, Vector3 localStart)
    {
        float t = 0f, dur = 0.9f;
        while (t < 1f && rt != null)
        {
            t += Time.unscaledDeltaTime / dur;
            rt.localPosition = localStart + Vector3.up * (70f * Tween.EaseOutQuad(t));
            var c = ((Text)rt.GetComponent<Text>()).color;
            c.a = 1f - t;
            ((Text)rt.GetComponent<Text>()).color = c;
            yield return null;
        }
        if (rt != null) Destroy(rt.gameObject);
    }

    // ------------------------------------------------------------- vignette flash

    public void FlashDeath()
    {
        if (vignette == null) return;
        StartCoroutine(FlashCo(new Color(1f, 1f, 1f), 0.7f, 1f));
    }

    public void FlashScreen(Color color, float duration = 0.25f, float peakAlpha = 0.35f)
    {
        if (vignette == null) return;
        StartCoroutine(FlashCo(color, duration, peakAlpha));
    }

    private IEnumerator FlashCo(Color color, float duration, float peakAlpha)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float alpha = Mathf.Lerp(0f, peakAlpha, Mathf.Pow(1f - t, 2f));
            vignette.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }
        vignette.color = new Color(1, 1, 1, 0);
    }
}
