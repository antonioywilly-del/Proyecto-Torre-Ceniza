using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;

public class AudioManager : MonoBehaviour
{
    // Singleton instance
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    // Audio 3D Object Pool
    [SerializeField] private int sfx3DPoolSize = 10;
    private ObjectPool<AudioSource> sfx3DPool;

    // Coroutine references para evitar race conditions
    private Coroutine currentMusicFadeCoroutine;

    private void Awake()
    {
        // Implement singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeAudioSources();
        InitializeSFX3DPool();
    }

    private void InitializeAudioSources()
    {
        // Create music source if not assigned
        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.loop = true;
        }

        // Create SFX source if not assigned
        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFXSource");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.loop = false;
        }

        // Asignar grupos de mixer si existe
        if (audioMixer != null)
        {
            AudioMixerGroup musicGroup = audioMixer.FindMatchingGroups("Music")[0];
            AudioMixerGroup sfxGroup = audioMixer.FindMatchingGroups("SFX")[0];

            if (musicGroup != null) musicSource.outputAudioMixerGroup = musicGroup;
            if (sfxGroup != null) sfxSource.outputAudioMixerGroup = sfxGroup;
        }
    }

    private void InitializeSFX3DPool()
    {
        sfx3DPool = new ObjectPool<AudioSource>(
            createFunc: CreateSFX3DSource,
            actionOnGet: OnGetSFX3D,
            actionOnRelease: OnReleaseSFX3D,
            collectionCheck: false,
            defaultCapacity: sfx3DPoolSize,
            maxSize: sfx3DPoolSize * 2
        );

        // Pre-instanciar los AudioSources del pool
        for (int i = 0; i < sfx3DPoolSize; i++)
        {
            sfx3DPool.Release(sfx3DPool.Get());
        }
    }

    private AudioSource CreateSFX3DSource()
    {
        GameObject sfxObj = new GameObject("SFX3D_Pooled");
        sfxObj.transform.SetParent(transform);
        AudioSource source = sfxObj.AddComponent<AudioSource>();
        source.spatialBlend = 1f; // Full 3D audio
        
        if (audioMixer != null)
        {
            AudioMixerGroup[] sfxGroups = audioMixer.FindMatchingGroups("SFX");
            if (sfxGroups.Length > 0)
                source.outputAudioMixerGroup = sfxGroups[0];
        }

        return source;
    }

    private void OnGetSFX3D(AudioSource source)
    {
        source.gameObject.SetActive(true);
    }

    private void OnReleaseSFX3D(AudioSource source)
    {
        source.Stop();
        source.gameObject.SetActive(false);
    }

    /// <summary>
    /// Play background music directly by AudioClip reference
    /// </summary>
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioClip is null!");
            return;
        }

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    /// <summary>
    /// Stop the background music
    /// </summary>
    public void StopMusic()
    {
        musicSource.Stop();
    }

    /// <summary>
    /// Pause the background music
    /// </summary>
    public void PauseMusic()
    {
        musicSource.Pause();
    }

    /// <summary>
    /// Resume the background music
    /// </summary>
    public void ResumeMusic()
    {
        musicSource.Play();
    }

    /// <summary>
    /// Play a sound effect directly by AudioClip reference
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioClip is null!");
            return;
        }

        sfxSource.PlayOneShot(clip, volume);
    }

    /// <summary>
    /// Play a sound effect at a specific position using Object Pool
    /// </summary>
    public void PlaySFX3D(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioClip is null!");
            return;
        }

        AudioSource source = sfx3DPool.Get();
        source.transform.position = position;
        source.clip = clip;
        source.volume = volume;
        source.Play();

        // Retornar a pool después de que el sonido termine
        StartCoroutine(ReturnSFX3DToPool(source, clip.length));
    }

    private System.Collections.IEnumerator ReturnSFX3DToPool(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        sfx3DPool.Release(source);
    }

    /// <summary>
    /// Stop all sounds
    /// </summary>
    public void StopAllSounds()
    {
        musicSource.Stop();
        sfxSource.Stop();
    }

    /// <summary>
    /// Set master volume (in decibels via AudioMixer)
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        if (audioMixer != null)
        {
            // Convertir rango 0-1 a decibelios (-80 a 0 dB)
            float dB = Mathf.Lerp(-80f, 0f, Mathf.Clamp01(volume));
            audioMixer.SetFloat("Master", dB);
        }
    }

    /// <summary>
    /// Set music volume (in decibels via AudioMixer)
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        if (audioMixer != null)
        {
            float dB = Mathf.Lerp(-80f, 0f, Mathf.Clamp01(volume));
            audioMixer.SetFloat("Music", dB);
        }
    }

    /// <summary>
    /// Set SFX volume (in decibels via AudioMixer)
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        if (audioMixer != null)
        {
            float dB = Mathf.Lerp(-80f, 0f, Mathf.Clamp01(volume));
            audioMixer.SetFloat("SFX", dB);
        }
    }

    /// <summary>
    /// Get current master volume (0-1)
    /// </summary>
    public float GetMasterVolume()
    {
        if (audioMixer != null && audioMixer.GetFloat("Master", out float dB))
        {
            return Mathf.InverseLerp(-80f, 0f, dB);
        }
        return 1f;
    }

    /// <summary>
    /// Get current music volume (0-1)
    /// </summary>
    public float GetMusicVolume()
    {
        if (audioMixer != null && audioMixer.GetFloat("Music", out float dB))
        {
            return Mathf.InverseLerp(-80f, 0f, dB);
        }
        return 1f;
    }

    /// <summary>
    /// Get current SFX volume (0-1)
    /// </summary>
    public float GetSFXVolume()
    {
        if (audioMixer != null && audioMixer.GetFloat("SFX", out float dB))
        {
            return Mathf.InverseLerp(-80f, 0f, dB);
        }
        return 1f;
    }

    /// <summary>
    /// Check if music is playing
    /// </summary>
    public bool IsMusicPlaying()
    {
        return musicSource.isPlaying;
    }

    /// <summary>
    /// Fade out music with race condition protection
    /// </summary>
    public void FadeOutMusic(float duration = 2f)
    {
        // Detener corrutina anterior si existe
        if (currentMusicFadeCoroutine != null)
        {
            StopCoroutine(currentMusicFadeCoroutine);
        }

        currentMusicFadeCoroutine = StartCoroutine(FadeOut(duration));
    }

    /// <summary>
    /// Fade in music with race condition protection
    /// </summary>
    public void FadeInMusic(float duration = 2f)
    {
        // Detener corrutina anterior si existe
        if (currentMusicFadeCoroutine != null)
        {
            StopCoroutine(currentMusicFadeCoroutine);
        }

        currentMusicFadeCoroutine = StartCoroutine(FadeIn(duration));
    }

    private System.Collections.IEnumerator FadeOut(float duration)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Stop();
        currentMusicFadeCoroutine = null;
    }

    private System.Collections.IEnumerator FadeIn(float duration)
    {
        float currentVolume = musicSource.volume;
        float targetVolume = GetMusicVolume();
        musicSource.volume = currentVolume;
        musicSource.Play();
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(currentVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        musicSource.volume = targetVolume;
        currentMusicFadeCoroutine = null;
    }
}
