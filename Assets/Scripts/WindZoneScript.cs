using UnityEngine;

/// <summary>
/// Wind-zone event: a stretch of sky with a strong up/down draft. Pushes the bird while
/// she is inside the band and shows streaking wind particles. Spawns with a normal pipe.
/// </summary>
public class WindZoneScript : MonoBehaviour
{
    public float Force { get; private set; }
    private float streakTimer;
    private float halfWidth;
    private bool isTrigger;
    private BirdScript bird;

    public static WindZoneScript Spawn(Vector3 pos, float force)
    {
        var go = new GameObject("WindZone");
        go.transform.position = pos;
        var wz = go.AddComponent<WindZoneScript>();
        wz.Force = force;
        wz.halfWidth = GameConfig.WindZoneWidth * 0.5f;
        return wz;
    }

    private void Update()
    {
        var logic = LogicScript.Instance;
        if (logic == null || !logic.IsPlaying) return;
        if (bird == null && logic.Bird != null) bird = logic.Bird;

        float dx = bird != null ? bird.transform.position.x - transform.position.x : float.MaxValue;
        bool inside = Mathf.Abs(dx) < halfWidth;

        if (inside && bird != null && bird.birdIsAlive && bird.birdRigidbody != null)
            bird.birdRigidbody.AddForce(Vector2.up * (Force * Time.fixedDeltaTime * 60f), ForceMode2D.Force);

        // wind streak visuals while the zone is anywhere near the view
        streakTimer -= Time.deltaTime;
        if (streakTimer <= 0f && Mathf.Abs(transform.position.x) < 30f)
        {
            streakTimer = 0.06f;
            float y = Random.Range(-GameConfig.CameraOrthoSize + 1f, GameConfig.CameraOrthoSize - 1f);
            var fx = EffectsManager.Instance;
            if (fx != null)
                fx.Burst(new Vector3(transform.position.x + Random.Range(-halfWidth, halfWidth), y, 0),
                    new Color(1f, 1f, 1f, 0.45f), 1, 3.5f, 0.07f, 0.4f, 0f);
        }

        // drift with the world and despawn off-screen
        transform.position += Vector3.left * (logic.CurrentSpeed * Time.deltaTime);
        if (transform.position.x < -40f) Destroy(gameObject);
    }
}
