using UnityEngine;

public class BirdScript : MonoBehaviour
{
    public Rigidbody2D birdRigidbody;
    public float flapStrength;
    public LogicScript logic;
    public bool birdIsAlive = true;
    private bool shouldFlap = false;

    void Start()
    {
        if (logic == null)
        {
            logic = GameObject.FindGameObjectWithTag("Logic")?.GetComponent<LogicScript>();
            if (logic == null)
            {
                Debug.LogError("LogicScript not found. Ensure there's a GameObject with the 'Logic' tag and LogicScript attached.");
            }
        }
    }

    void Update()
    {
        // Check for input
        if (birdIsAlive && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            shouldFlap = true;
        }

        // Check for touch input
        if (birdIsAlive && Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                shouldFlap = true;
            }
        }

        // Check for out of bounds
        if (transform.position.y > 15 || transform.position.y < -15)
        {
            if (logic != null)
            {
                logic.GameOver();
            }
            birdIsAlive = false;
        }
    }

    void FixedUpdate()
    {
        // Apply flap force in FixedUpdate
        if (shouldFlap)
        {
            birdRigidbody.velocity = Vector2.up * flapStrength;
            shouldFlap = false;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (logic != null)
        {
            logic.GameOver();
        }
        birdIsAlive = false;
    }
}
