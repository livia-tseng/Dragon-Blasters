using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; 

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI (optional)")]
    public TMP_Text p1Text;
    public TMP_Text p2Text;

    private int p1, p2;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

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
        Instance.p1 = 0; Instance.p2 = 0;
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
