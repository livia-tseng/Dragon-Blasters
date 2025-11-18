using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance;

    public float duration = 60f;
    public TMP_Text timerText;
    public bool gameOver = false;

    private float timeRemaining;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        timeRemaining = duration;
    }

    private void Update()
    {
        if (gameOver) return;

        timeRemaining -= Time.deltaTime;

        // Update the text (optional)
        if (timerText)
        {
            timerText.text = Mathf.Ceil(timeRemaining).ToString();
        }

        // Trigger game over
        if (timeRemaining <= 0f)
        {
            GameOver();
        }
    }

    private void GameOver()
    {
        gameOver = true;
        timeRemaining = 0f;

        Debug.Log("GAME OVER");
        // TODO: Add whatever you want here:
        // - Show UI panel
        // - Stop player movement
        // - Load new scene
        // - Fade screen
    }

    public bool isGameOver()
    {
        return gameOver;
    }
}

