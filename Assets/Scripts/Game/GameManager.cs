using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private LoreIntroManager loreIntro;

    // State
    private bool gameStarted;
    private bool isPaused;

    public bool IsGameStarted => gameStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Game starts paused until lore intro is done
        gameStarted = false;

        // Start lore intro if available
        if (loreIntro != null)
        {
            loreIntro.StartIntro(OnLoreIntroDone);
        }
        else
        {
            OnLoreIntroDone();
        }
    }

    private void Update()
    {
        // Pause menu
        if (gameStarted && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    private void OnLoreIntroDone()
    {
        gameStarted = true;
        Debug.Log("[GameManager] Lore intro complete. Game started.");
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        Debug.Log(isPaused ? "[GameManager] Game Paused" : "[GameManager] Game Resumed");
    }

    // --- Button Callbacks ---

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuPrueba");
    }
}
