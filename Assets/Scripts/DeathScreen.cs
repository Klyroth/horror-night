using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// DeathScreen — Horror Night
// From tutorial video 39. When the player dies, a black overlay and
// red "YOU DIED" text fade in over 5 seconds, then the game freezes.
// The tutorial's public field name `showDeadScreen` is kept (PlayerMovement
// sets it); the private typo `targetAplha` is corrected to `targetAlpha`.
// HORROR NIGHT adaptation (2026-11-21):
//  - Singleton Instance so GameManager.OnPlayerDied() can call Show().
//  - Show(): enables the game-over panel, fills in the final score and
//    wave reached, starts the fade and sets Time.timeScale = 0 at once.
//    The fade runs on Time.unscaledDeltaTime so it still plays while
//    the game is frozen (with plain deltaTime it would stall at 0).
//  - RestartGame(): unfreezes and reloads the NightCity scene.
//  - QuitToMenu(): unfreezes and loads the MainMenu scene.
//  - Wire the panel's buttons in the Inspector to RestartGame() /
//    QuitToMenu() (serialized onClick — NEVER via editor-code AddListener).
public class DeathScreen : MonoBehaviour
{
    public static DeathScreen Instance { get { return instance; } }
    private static DeathScreen instance;

    [Header("Fade (YOU DIED overlay)")]
    public Image targetImage;
    public Text targetText;
    public float duration = 5f;
    public bool showDeadScreen = false;
    private float targetAlpha = 1f;
    private float startAlpha;
    private float elapsedTime = 0f;

    [Header("Game Over Panel")]
    public GameObject gameOverPanel;
    public Text finalScoreText;
    public Text waveReachedText;
    public Button restartButton;
    public Button quitButton;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (targetImage != null)
        {
            startAlpha = targetImage.color.a;
        }
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (showDeadScreen)
        {
            // Unscaled time: the fade must keep playing after Show()
            // sets Time.timeScale = 0.
            if (elapsedTime < duration)
            {
                float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);

                if (targetImage != null)
                {
                    Color newColor = targetImage.color;
                    newColor.a = newAlpha;
                    targetImage.color = newColor;
                }

                if (targetText != null)
                {
                    Color newTextAlpha = targetText.color;
                    newTextAlpha.a = newAlpha;
                    targetText.color = newTextAlpha;
                }

                elapsedTime += Time.unscaledDeltaTime;
            }
            else
            {
                Time.timeScale = 0;
            }
        }
    }

    // Called by GameManager.OnPlayerDied() when the player dies.
    public void Show()
    {
        showDeadScreen = true;
        elapsedTime = 0f;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (finalScoreText != null)
        {
            int score = GameManager.Instance != null ? GameManager.Instance.currentScore : 0;
            finalScoreText.text = score.ToString();
        }

        if (waveReachedText != null)
        {
            ZombieWaveSystem waveSystem = FindObjectOfType<ZombieWaveSystem>();
            int wave = waveSystem != null ? waveSystem.CurrentWave : 1;
            waveReachedText.text = wave.ToString();
        }

        Time.timeScale = 0;
    }

    // Inspector-wired: RESTART button on the game-over panel.
    public void RestartGame()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("NightCity");
    }

    // Inspector-wired: QUIT button on the game-over panel.
    public void QuitToMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("MainMenu");
    }
}
