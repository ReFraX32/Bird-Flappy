using UnityEngine;

/// <summary>Player turret bullet; damages blocker targets, sparks off everything else.</summary>
public class ProjectileScript : MonoBehaviour
{
    public int Damage { get; private set; } = 1;
    private float life;

    public void Launch(Vector2 velocity, int damage)
    {
        Damage = damage;
        life = 2.2f;
        var rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = velocity;
        var col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.16f;
        gameObject.layer = 0;
    }

    private void Update()
    {
        life -= Time.deltaTime;
        if (life <= 0f) Destroy(gameObject);
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        var blocker = other.GetComponentInParent<ClosedPipeBlocker>();
        if (blocker != null)
        {
            if (other.name == "TargetHit")
            {
                blocker.RegisterBulletHit(transform.position, Damage);
                Destroy(gameObject);
            }
            // the armored plate is ignored — bullets must reach the target behind it
            return;
        }

        // boss takes turret damage
        var boss = other.GetComponent<BossScript>();
        if (boss != null)
        {
            boss.RegisterHit(Damage, transform.position);
            Destroy(gameObject);
            return;
        }

        // never hit the shooter
        if (other.GetComponent<BirdScript>() != null) return;

        // pass through gameplay triggers (score zones, coins); spark off solid geometry only
        if (other.isTrigger) return;

        EffectsManager.Instance.Burst(transform.position, new Color(1f, 0.9f, 0.5f), 4, 3f, 0.1f, 0.3f, 6f);
        Destroy(gameObject);
    }
}
