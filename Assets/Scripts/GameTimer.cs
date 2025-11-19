using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance;

    public GameObject GameOverScreen;
    public TMP_Text WinnerText;

    public float duration = 60f;
    public TMP_Text timerText;
    public bool gameOver = false;

    private float timeRemaining;

    private void Awake()
    {
        Instance = this;
        GameOverScreen.SetActive(false);
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

        GameOverScreen.SetActive(true);

        int winner = GameManager.Instance.Winner();
        if (winner == 1) WinnerText.text = "Player 1 Wins!";
        else if (winner == 2) WinnerText.text = "Player 2 Wins!";
        else WinnerText.text = "Players Tie!";
    }

    public bool isGameOver()
    {
        return gameOver;
    }
}

