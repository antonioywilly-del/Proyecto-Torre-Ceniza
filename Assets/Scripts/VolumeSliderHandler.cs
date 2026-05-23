using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controlador de slider de volumen maestro compatible con AudioManager.
/// Gestiona la sincronización entre la UI y el sistema de audio.
/// Se inicializa correctamente incluso cuando el panel padre empieza inactivo.
/// </summary>
[RequireComponent(typeof(Slider))]
public class VolumeSliderHandler : MonoBehaviour
{
    private const string VOLUME_SAVE_KEY = "MasterVolume";

    private Slider volumeSlider;
    private bool isInitialized = false;

    private void Awake()
    {
        volumeSlider = GetComponent<Slider>();
        
        // Configurar rango del slider (0-1)
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
    }

    private void OnEnable()
    {
        if (volumeSlider == null)
            volumeSlider = GetComponent<Slider>();

        // Inicializar en OnEnable para garantizar que funcione
        // aunque el objeto empiece inactivo
        if (!isInitialized)
        {
            Initialize();
        }

        // Suscribirse a cambios del slider
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    private void OnDisable()
    {
        // Desuscribirse para evitar memory leaks
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
    }

    private void Initialize()
    {
        // Cargar volumen guardado o usar valor por defecto
        float savedVolume = PlayerPrefs.GetFloat(VOLUME_SAVE_KEY, 1f);
        
        // Establecer valor del slider sin disparar el callback
        volumeSlider.SetValueWithoutNotify(savedVolume);
        
        // Aplicar volumen al AudioManager si ya existe
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(savedVolume);
        }
        
        isInitialized = true;
    }

    /// <summary>
    /// Callback ejecutado cuando cambia el valor del slider.
    /// </summary>
    private void OnVolumeChanged(float volumeValue)
    {
        // Aplicar volumen al AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(volumeValue);
        }

        // Guardar volumen en PlayerPrefs para persistencia
        PlayerPrefs.SetFloat(VOLUME_SAVE_KEY, volumeValue);
        PlayerPrefs.Save();
    }
}
