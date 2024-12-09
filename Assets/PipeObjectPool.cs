using System.Collections.Generic;
using UnityEngine;

public class PipeObjectPool : MonoBehaviour
{
    public GameObject pipePrefab;
    public int poolSize = 5;

    private Queue<GameObject> pipePool = new Queue<GameObject>();

    void Awake()
    {
        InitializePool();
    }

    void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            CreateNewPipe();
        }
    }

    void CreateNewPipe()
    {
        if (pipePrefab == null)
        {
            Debug.LogError("Pipe prefab is not set in PipeObjectPool.");
            return;
        }

        GameObject pipe = Instantiate(pipePrefab);
        pipe.SetActive(false);
        pipePool.Enqueue(pipe);
    }

    public GameObject GetPipe()
    {
        if (pipePool.Count == 0)
        {
            CreateNewPipe();
        }

        GameObject pipe = pipePool.Dequeue();
        pipe.SetActive(true);
        return pipe;
    }

    public void ReturnPipe(GameObject pipe)
    {
        pipe.SetActive(false);
        pipePool.Enqueue(pipe);
    }
}