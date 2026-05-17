using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Configuración de Escenas")]
    [Tooltip("Nombre exacto de la escena del juego a cargar.")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Paneles de la Interfaz (UI)")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject creditsPanel;

    private void Start()
    {
        // Estado inicial: Asegurar que solo el menú principal esté visible
        ShowMainMenu();
    }

    /// <summary>
    /// Carga la escena principal del juego. Vinculado al botón "Comenzar".
    /// </summary>
    public void StartGame()
    {
        // Validación de seguridad para evitar errores de referencia nula en runtime
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("MainMenuManager: El nombre de la escena del juego no ha sido asignado en el Inspector.");
        }
    }

    /// <summary>
    /// Muestra el panel de Opciones y oculta los demás. Vinculado al botón "Opciones".
    /// </summary>
    public void ShowOptions()
    {
        TogglePanels(showMain: false, showOptions: true, showCredits: false);
    }

    /// <summary>
    /// Muestra el panel de Créditos y oculta los demás. Vinculado al botón "Créditos".
    /// </summary>
    public void ShowCredits()
    {
        TogglePanels(showMain: false, showOptions: false, showCredits: true);
    }

    /// <summary>
    /// Vuelve al menú principal. Se puede usar para botones de "Atrás" en Opciones/Créditos.
    /// </summary>
    public void ShowMainMenu()
    {
        TogglePanels(showMain: true, showOptions: false, showCredits: false);
    }

    /// <summary>
    /// Método conveniente para vincular a botones "Atrás" en Opciones/Créditos.
    /// Simplemente muestra el menú principal (equivalente a `ShowMainMenu`).
    /// </summary>
    public void BackToMain()
    {
        ShowMainMenu();
    }

    /// <summary>
    /// Cierra la aplicación. Vinculado al botón "Salir".
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Cerrando la aplicación...");

        // Directivas de preprocesador: Application.Quit() no funciona en el Editor de Unity.
        // Esto asegura que el botón "Salir" funcione tanto en la build final como al probar en el Editor.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Método auxiliar privado para gestionar el estado de los paneles, 
    /// evitando repetición de código (principio DRY).
    /// </summary>
    private void TogglePanels(bool showMain, bool showOptions, bool showCredits)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(showMain);
        if (optionsPanel != null) optionsPanel.SetActive(showOptions);
        if (creditsPanel != null) creditsPanel.SetActive(showCredits);
    }
}