using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controlador de slider de volumen maestro compatible con AudioManager.
/// Gestiona la sincronización entre la UI y el sistema de audio.
/// </summary>
[RequireComponent(typeof(Slider))]
public class VolumeSliderHandler : MonoBehaviour
{
    private const string VOLUME_SAVE_KEY = "MasterVolume";

    private Slider volumeSlider;
    private bool isInitializing = true;

    private void Awake()
    {
        volumeSlider = GetComponent<Slider>();
        
        // Configurar rango del slider (0-1)
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
    }

    private void Start()
    {
        // Cargar volumen guardado o usar valor por defecto
        float savedVolume = PlayerPrefs.GetFloat(VOLUME_SAVE_KEY, 1f);
        volumeSlider.value = savedVolume;
        
        // Aplicar volumen al AudioManager
        AudioManager.Instance.SetMasterVolume(savedVolume);
        
        isInitializing = false;
    }

    private void OnEnable()
    {
        // Suscribirse a cambios del slider
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    private void OnDisable()
    {
        // Desuscribirse para evitar memory leaks
        volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
    }

    /// <summary>
    /// Callback ejecutado cuando cambia el valor del slider.
    /// </summary>
    private void OnVolumeChanged(float volumeValue)
    {
        if (isInitializing)
            return;

        // Aplicar volumen al AudioManager
        AudioManager.Instance.SetMasterVolume(volumeValue);

        // Guardar volumen en PlayerPrefs para persistencia
        PlayerPrefs.SetFloat(VOLUME_SAVE_KEY, volumeValue);
        PlayerPrefs.Save();
    }
}