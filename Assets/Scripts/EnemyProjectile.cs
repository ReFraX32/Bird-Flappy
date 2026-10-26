using UnityEngine;

/// <summary>Slow homing-ish projectile lobbed by bosses; downing the bird is a normal crash (shields apply).</summary>
public class EnemyProjectile : MonoBehaviour
{
    private float life = 9f;

    public void Launch(Vector2 velocity)
    {
        var rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.linearVelocity = velocity;
    }

    private void Update()
    {
        life -= Time.deltaTime;
        transform.Rotate(0, 0, 380f * Time.deltaTime);
        if (life <= 0f) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var bird = other.GetComponent<BirdScript>();
        if (bird == null) return;   // passes through pipes/coins/world
        if (bird.IsInvincible || !bird.birdIsAlive) return;
        Destroy(gameObject);
        LogicScript.Instance?.BirdCrashed();
    }
}
