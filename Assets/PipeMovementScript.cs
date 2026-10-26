using UnityEngine;

public enum PipeVariant { Normal, Moving, Blocked, DoubleGap, WindZone, SawGate, TightGap, CoinWall }

/// <summary>
/// Moves a pipe left at the global game speed, supports vertical oscillation
/// (Moving variant) and recycles itself into the spawner pool past the dead zone.
/// Movement pauses whenever the game is not in the Playing state.
/// </summary>
public class PipeMovementScript : MonoBehaviour
{
    public float deadZone = -30f;
    public PipeVariant Variant { get; private set; }

    [Header("Oscillation (Moving variant)")]
    public float oscillateAmplitude = 2.2f;
    public float oscillateSpeed = 1.6f;
    private float baseY;
    private float phase;
    private bool oscillating;

    private PipeSpawnerScript spawner;
    private static int spawnerSearchFrame = -1;
    private static PipeSpawnerScript cachedSpawner;

    private PipeSpawnerScript Spawner
    {
        get
        {
            if (spawner != null) return spawner;
            if (cachedSpawner == null || spawnerSearchFrame != Time.frameCount)
            {
                cachedSpawner = FindAnyObjectByType<PipeSpawnerScript>();
                spawnerSearchFrame = Time.frameCount;
            }
            spawner = cachedSpawner;
            return spawner;
        }
    }

    public void Configure(PipeVariant variant, float y)
    {
        Variant = variant;
        baseY = y;
        phase = Random.value * Mathf.PI * 2f;
        oscillating = variant == PipeVariant.Moving;
    }

    void Update()
    {
        var logic = LogicScript.Instance;
        if (logic == null || !logic.IsPlaying) return;

        float speed = logic.CurrentSpeed;
        transform.position += Vector3.left * (speed * Time.deltaTime);

        if (oscillating)
        {
            phase += oscillateSpeed * Time.deltaTime;
            transform.position = new Vector3(transform.position.x, baseY + Mathf.Sin(phase) * oscillateAmplitude, transform.position.z);
        }

        float despawnX = -(GetHalfWidth() + 4f);
        if (transform.position.x < despawnX)
        {
            if (Spawner != null) Spawner.DespawnPipe(this);
            else Destroy(gameObject);
        }
    }

    private static float GetHalfWidth()
    {
        var cam = Camera.main;
        return cam != null ? GameConfig.CameraOrthoSize * cam.aspect : 20f;
    }
}
