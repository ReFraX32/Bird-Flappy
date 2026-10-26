using UnityEngine;

/// <summary>Collectible coin: bobs, spins, is pulled in by the magnet upgrade and pays out on pickup.</summary>
public class CoinScript : MonoBehaviour
{
    private SpriteRenderer rendererRef;
    private float bobTime;
    private Vector3 bobBase;
    private bool magnetActive;
    private Transform bird;

    private void Awake()
    {
        rendererRef = GetComponent<SpriteRenderer>();
    }

    public void Spawn(Vector3 pos)
    {
        bobBase = pos;
        bobTime = Random.value * Mathf.PI * 2f;
        transform.position = pos;
        magnetActive = false;
        bird = null;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        bobTime += Time.deltaTime * 3.4f;
        float spin = Mathf.PingPong(Time.time * 220f, 180f) - 90f; // fake 3D spin via x-rotation
        transform.rotation = Quaternion.Euler(0, 0, 0);
        rendererRef.transform.localScale = new Vector3(Mathf.Abs(Mathf.Sin(bobTime)) * 0.7f + 0.3f, 1f, 1f);

        var logic = LogicScript.Instance;
        if (logic == null || !logic.IsPlaying) return;

        Vector3 target = bobBase + new Vector3(0f, Mathf.Sin(bobTime) * 0.25f, 0f);

        if (!magnetActive && logic.MagnetRadius > 0f)
        {
            if (bird == null && logic.Bird != null) bird = logic.Bird.transform;
            if (bird != null && (bird.position - transform.position).magnitude < logic.MagnetRadius)
                magnetActive = true;
        }

        if (magnetActive && bird != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, bird.position,
                GameConfig.MagnetPullSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = target;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<BirdScript>() == null) return;
        var logic = LogicScript.Instance;
        if (logic == null) return;
        logic.CoinCollected(transform.position);
        EffectsManager.Instance.CoinBurst(transform.position);
        ProceduralAudio.PlaySfx("coin", 0.8f);
        logic.DespawnCoin(this);
    }
}
