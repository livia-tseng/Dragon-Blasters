using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum GameState { Idle, Gameplay, GameOver }
    public GameState State { get; private set; } = GameState.Idle;

    [Header("UI (optional)")]
    public TMP_Text p1Text;
    public TMP_Text p2Text;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip idleMusic;
    public AudioClip gameplayMusic;

    private int p1, p2;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Start in Idle State
        State = GameState.Idle;
        PlayIdleMusic();
    }

    private void Update()
    {
        // Start Game on SPACE key
        if (State == GameState.Idle && Input.GetKeyDown(KeyCode.Space))
        {
            StartGame();
        }
    }

    public void StartGame()
    {
        Debug.Log("GAME START");
        ResetAll();

        State = GameState.Gameplay;

        PlayGameplayMusic();

        GameTimer.Instance.StartTimer();
    }

    public void EndGame()
    {
        State = GameState.GameOver;

        PlayIdleMusic(); // Goes back to original audio track

        GameTimer.Instance.ShowGameOver();
    }

    // ===== SCORE =====

    public static void Add(int playerId, int delta)
    {
        if (!Instance) return;
        if (playerId == 1) Instance.p1 += delta;
        else if (playerId == 2) Instance.p2 += delta;
        Instance.RefreshUI();
    }

    public static void ResetAll()
    {
        if (!Instance) return;
        Instance.p1 = 0;
        Instance.p2 = 0;
        Instance.RefreshUI();
    }

    private void RefreshUI()
    {
        if (p1Text) p1Text.text = $"{p1}";
        if (p2Text) p2Text.text = $"{p2}";
    }

    public int Winner()
    {
        if (p1 > p2) return 1;
        if (p2 > p1) return 2;
        return 0;
    }

    // ===== AUDIO CONTROL =====

    private void PlayIdleMusic()
    {
        if (audioSource == null || idleMusic == null) return;
        audioSource.loop = true;
        audioSource.clip = idleMusic;
        audioSource.Play();
    }

    private void PlayGameplayMusic()
    {
        if (audioSource == null || gameplayMusic == null) return;
        audioSource.loop = true;
        audioSource.clip = gameplayMusic;
        audioSource.Play();
    }
}
