using UnityEngine;

public class PipeMovementScript : MonoBehaviour
{
    public float movementSpeed = 5;
    public float deadZone = -30;

    private PipeObjectPool pipePool;

    void Start()
    {
        pipePool = FindObjectOfType<PipeObjectPool>();
        if (pipePool == null)
        {
            Debug.LogError("PipeObjectPool not found in the scene.");
        }
    }

    void Update()
    {
        transform.position += Vector3.left * movementSpeed * Time.deltaTime;

        if (transform.position.x < deadZone)
        {
            if (pipePool != null)
            {
                pipePool.ReturnPipe(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
