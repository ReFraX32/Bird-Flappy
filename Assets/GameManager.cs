using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int targetFrameRate = 60;

    void Awake()
    {
        SetTargetFrameRate();
    }

    private void SetTargetFrameRate()
    {
        Application.targetFrameRate = targetFrameRate;
        QualitySettings.vSyncCount = 0; // Disable VSync
        Debug.Log($"Target frame rate set to {targetFrameRate}");
    }

    // You can add a method to change the frame rate at runtime if needed
    public void ChangeTargetFrameRate(int newTargetFrameRate)
    {
        targetFrameRate = newTargetFrameRate;
        SetTargetFrameRate();
    }
}