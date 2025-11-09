// GameState.cs
using UnityEngine;
using System;

public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }
    public bool playerHasAxlePin = false;
    public static event Action<string,string> OnGlobalEvent; // (key, shortValue)

    public string timeOfDay = "night-1";
    public string weather = "rain";
    public int repairProgress = 0;
    public int campMorale = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void UpdateRepairProgress(int p) {
        repairProgress = p;
        OnGlobalEvent?.Invoke("repair_progress", p.ToString());
    }
    public void SetWeather(string w) {
        weather = w;
        OnGlobalEvent?.Invoke("weather", w);
    }

    string Truncate(string s, int m) => s.Length <= m ? s : s.Substring(0,m-3) + "...";
}
