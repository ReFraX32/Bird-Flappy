using System;
using UnityEngine;

public class PipeSpawnerScript : MonoBehaviour
{
    public GameObject pipePrefab;
    public float spawnRate = 3;
    public float heightOffset = 10;

    private float timer = 0;
    private PipeObjectPool pipePool;

    void Start()
    {
        InitializePipePool();
        SpawnPipe();
    }

    void InitializePipePool()
    {
        pipePool = FindObjectOfType<PipeObjectPool>();
        if (pipePool == null)
        {
            UnityEngine.Debug.LogWarning("PipeObjectPool not found. Creating a new one.");
            GameObject poolObject = new GameObject("PipeObjectPool");
            pipePool = poolObject.AddComponent<PipeObjectPool>();
        }

        if (pipePool.pipePrefab == null)
        {
            pipePool.pipePrefab = pipePrefab;
        }
    }

    void Update()
    {
        if (timer < spawnRate)
        {
            timer += Time.deltaTime;
        }
        else
        {
            SpawnPipe();
            timer = 0;
        }
    }

    void SpawnPipe()
    {
        if (pipePool == null)
        {
            UnityEngine.Debug.LogError("PipeObjectPool is null. Cannot spawn pipe.");
            return;
        }

        float lowestPoint = transform.position.y - heightOffset;
        float highestPoint = transform.position.y + heightOffset;

        GameObject pipe = pipePool.GetPipe();
        if (pipe != null)
        {
            pipe.transform.position = new Vector3(transform.position.x, UnityEngine.Random.Range(lowestPoint, highestPoint), 0);
            pipe.transform.rotation = transform.rotation;
        }
        else
        {
            UnityEngine.Debug.LogError("Failed to get a pipe from the pool.");
        }
    }
}