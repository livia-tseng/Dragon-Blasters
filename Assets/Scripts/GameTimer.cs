using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance;

    [Header("Game Objects")]
    public GameObject GameOverScreen;
    public TMP_Text WinnerText;
    public TMP_Text timerText;

    [Header("Time Duration")]
    public float duration = 60f;

    private float timeRemaining;
    private bool running = false;

    private void Awake()
    {
        Instance = this;
        GameOverScreen.SetActive(false);
    }

    public void StartTimer()
    {
        running = true;
        timeRemaining = duration;
        GameOverScreen.SetActive(false);
    }

    private void Update()
    {
        if (!running) return;

        timeRemaining -= Time.deltaTime;

        if (timerText)
            timerText.text = Mathf.Ceil(timeRemaining).ToString();

        if (timeRemaining <= 0f)
        {
            running = false;
            GameManager.Instance.EndGame();
        }
    }

    public void ShowGameOver()
    {
        GameOverScreen.SetActive(true);

        int winner = GameManager.Instance.Winner();
        if (winner == 1) WinnerText.text = "Player 1 Wins!";
        else if (winner == 2) WinnerText.text = "Player 2 Wins!";
        else WinnerText.text = "Players Tie!";
    }
}
