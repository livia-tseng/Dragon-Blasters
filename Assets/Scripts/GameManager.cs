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
    public AudioSource idleMusicSource;
    public AudioSource gameplayMusicSource;
    public AudioClip idleMusic;
    public AudioClip gameplayMusic;
    private AudioSource currentMusic;
    private AudioSource nextMusic;

    public float crossfadeTime = 1.5f; // seconds


    private int p1, p2;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Ensure correct settings
        idleMusicSource.loop = true;
        gameplayMusicSource.loop = true;

        idleMusicSource.volume = 1f;
        gameplayMusicSource.volume = 0f; // start silent

        currentMusic = idleMusicSource;
        nextMusic = gameplayMusicSource;

        State = GameState.Idle;
        CrossfadeTo(idleMusic);
    }

    //If we have a transition from idle to start replace ts cuz it sounds buns but its better than the instant cut
    private void CrossfadeTo(AudioClip newClip)
    {
        StopAllCoroutines();
        StartCoroutine(CrossfadeCoroutine(newClip));
    }

    private System.Collections.IEnumerator CrossfadeCoroutine(AudioClip newClip)
    {
        // Set new clip on nextMusic source
        nextMusic.clip = newClip;
        nextMusic.Play();

        float t = 0f;

        float startVol = currentMusic.volume;
        float targetVol = 1f;

        while (t < crossfadeTime)
        {
            t += Time.deltaTime;
            float normalized = t / crossfadeTime;

            currentMusic.volume = Mathf.Lerp(startVol, 0f, normalized);
            nextMusic.volume = Mathf.Lerp(0f, targetVol, normalized);

            yield return null;
        }

        // Ensure values end cleanly
        currentMusic.volume = 0f;
        nextMusic.volume = 1f;

        // Stop old track
        currentMusic.Stop();

        // Swap roles
            AudioSource temp = currentMusic;
            currentMusic = nextMusic;
            nextMusic = temp;
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

        //Flush serial monitor buffer before game starts
        foreach (var blaster in FindObjectsOfType<MPUBlaster2D>())
        {
            blaster.ClearSerialBuffer();
        }

        ResetAll();

        State = GameState.Gameplay;

        CrossfadeTo(gameplayMusic);

        GameTimer.Instance.StartTimer();
    }

    public void EndGame()
    {
        State = GameState.GameOver;

        CrossfadeTo(idleMusic);

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

}
