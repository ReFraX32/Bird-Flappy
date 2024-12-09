using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LogicScript : MonoBehaviour
{
    public int playerScore;
    public Text scoreText;
    public GameObject gameOverScreen;

    [SerializeField] private BirdScript bird; // Reference to the BirdScript

    private int lastDisplayedScore = 0;
    private const int ScoreUpdateThreshold = 1;

    void Start()
    {
        // Find the BirdScript if it's not assigned in the inspector
        if (bird == null)
        {
            bird = FindObjectOfType<BirdScript>();
            if (bird == null)
            {
                Debug.LogError("BirdScript not found in the scene. Please ensure the bird object is present.");
            }
        }

        // Initialize the score display
        UpdateScoreDisplay();
    }

    [ContextMenu("Increase Score")]
    public void AddScore(int scoreToAdd)
    {
        // Only add score if the bird is alive
        if (bird != null && bird.birdIsAlive)
        {
            playerScore += scoreToAdd;
            if (playerScore - lastDisplayedScore >= ScoreUpdateThreshold)
            {
                UpdateScoreDisplay();
            }
        }
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = playerScore.ToString();
            lastDisplayedScore = playerScore;
        }
        else
        {
            Debug.LogWarning("Score Text is not assigned in LogicScript.");
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GameOver()
    {
        UpdateScoreDisplay(); // Ensure final score is displayed
        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Game Over Screen is not assigned in LogicScript.");
        }
    }
}