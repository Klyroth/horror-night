using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// GameManager — Horror Night
// From tutorial videos 34 (singleton + score), 36 (score UI).
// HORROR NIGHT adaptation (2026-11-21): weapon switching REMOVED
// entirely — one weapon (sniper) only, so the weapons array,
// SwitchWeapon, touchWeaponIndex, Alpha1-4 handling, CurrentWeaponIndex
// and WeaponCount are all gone.
// Score = kills + time survived:
//   currentScore = kills * 10 + (int)timeSurvived, recomputed every frame.
// AddScore(int) is kept for compatibility with ZombieAI /
// WaypointZombieAI, which call AddScore(1) when a zombie dies — it adds
// to kills, so one kill = +1 kill = +10 score. AddKill() does the same
// directly. There is no separate bonus-score field.
// OnPlayerDied() saves the best score to PlayerPrefs
// ("HorrorNightBestScore") and shows the death screen.
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public static GameManager Instance { get { return instance; } }

    public int kills;
    public int highScore;
    public int currentScore;

    public Text highScoreText;
    public Text currentScoreText;

    private float timeSurvived;

    void Start()
    {
        instance = this;
        if (PlayerPrefs.HasKey("HorrorNightBestScore"))
        {
            highScore = PlayerPrefs.GetInt("HorrorNightBestScore");
        }
    }

    void Update()
    {
        timeSurvived += Time.deltaTime;
        currentScore = kills * 10 + (int)timeSurvived;

        if (currentScore > highScore)
        {
            highScore = currentScore;
        }

        if (highScoreText != null)
        {
            highScoreText.text = highScore.ToString();
        }
        if (currentScoreText != null)
        {
            currentScoreText.text = currentScore.ToString();
        }
    }

    public void AddKill()
    {
        kills++;
    }

    // Kept for ZombieAI / WaypointZombieAI (they call AddScore(1) on a
    // zombie death). One call = one kill.
    public void AddScore(int amount)
    {
        kills += amount;
    }

    public void OnPlayerDied()
    {
        if (currentScore > highScore)
        {
            highScore = currentScore;
        }
        PlayerPrefs.SetInt("HorrorNightBestScore", highScore);
        PlayerPrefs.Save();

        if (DeathScreen.Instance != null)
        {
            DeathScreen.Instance.Show();
        }
    }
}
