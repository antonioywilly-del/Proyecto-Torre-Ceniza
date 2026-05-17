# AudioManager - Guía de Configuración

## Cambios Críticos Aplicados

### 1. ✅ Código Muerto Eliminado
- Removido: `Dictionary<string, AudioClip> audioClips` (nunca se usaba)
- Removido: `LoadAudioClips()` y `Resources.LoadAll()` (antipatrón de memoria)

### 2. ✅ Referencias Directas de AudioClips
**CAMBIO**: Los métodos ahora aceptan `AudioClip` directo, no strings.

**Antes:**
```csharp
AudioManager.Instance.PlayMusic("MenuTheme");
```

**Ahora:**
```csharp
[SerializeField] private AudioClip menuThemeClip;
AudioManager.Instance.PlayMusic(menuThemeClip);
```

**Ventajas:**
- Validación en tiempo de compilación
- Sin fallos en runtime por typos
- Mejor rendimiento (sin búsquedas en diccionarios)

### 3. ✅ Object Pool para Audio 3D
**Problema anterior**: Crear/destruir GameObjects constantemente genera garbage collection spikes.

**Solución implementada**: Object Pool reutiliza AudioSources 3D.

```csharp
// Antes (crea/destruye cada vez)
PlaySFX3D("Explosion", position);

// Ahora (reutiliza del pool)
[SerializeField] private AudioClip explosionClip;
PlaySFX3D(explosionClip, position, 1f);
```

**Configuración:**
- `sfx3DPoolSize`: Cantidad de AudioSources preasignados (default: 10)
- Se crea dinámicamente si necesitas más simultáneamente

### 4. ✅ AudioMixer - Sistema de Volumen Profesional
**Problema anterior**: Multiplicar floats lineales (`0.5 * 0.8`) no refleja la percepción auditiva.

**Solución**: AudioMixer con decibelios (unidades logarítmicas reales).

**Configuración requerida:**

1. **En Unity Editor:**
   - Window → Audio → Audio Mixer
   - Click derecho en "Master" → Create submix
   - Renombra a "Music"
   - Repite para "SFX"
   - Crea parámetros expuestos:
     - Click en "Master" → Add parameter → "Master"
     - Click en "Music" → Add parameter → "Music"  
     - Click en "SFX" → Add parameter → "SFX"

2. **En tu escena:**
   ```csharp
   // Asigna el AudioMixer al AudioManager
   [SerializeField] private AudioMixer audioMixer; // Arrastra el mixer aquí
   ```

**Uso:**
```csharp
// Volumen es ahora 0-1 (internamente convierte a dB)
AudioManager.Instance.SetMasterVolume(0.8f);
AudioManager.Instance.SetMusicVolume(0.9f);
AudioManager.Instance.SetSFXVolume(0.7f);

// Obtener valores
float masterVol = AudioManager.Instance.GetMasterVolume();
```

**¿Por qué decibelios?**
- Rango: -80 dB (silencio) a 0 dB (máximo)
- La percepción auditiva es logarítmica: 0.5 en volumen se percibe como ~70% más fuerte
- AudioMixer maneja esta conversión automáticamente en C++

### 5. ✅ Race Conditions en Corrutinas Eliminadas
**Problema anterior**: Llamar `FadeOutMusic()` y luego `FadeInMusic()` generaba conflictos.

**Solución**: Guardar referencia a corrutina y detener la anterior.

```csharp
// Seguro ahora - cancela fade anterior automáticamente
AudioManager.Instance.FadeOutMusic(1.5f);
// Luego...
AudioManager.Instance.FadeInMusic(2f);  // Detiene el fade anterior
```

---

## Guía de Uso Completa

### Setup Inicial en la Escena

1. **Crea un GameObject llamado "AudioManager"**
   - Agrega el script `AudioManager.cs`

2. **Crea un AudioMixer** (ver paso 4 arriba)

3. **Asigna referencias:**
   - `Audio Mixer`: Tu mixer creado
   - `sfx3DPoolSize`: 10-20 (ajusta según disparos/explosiones simultáneas)

### Uso en Otros Scripts

```csharp
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip damageSound;

    void Jump()
    {
        // Reproducir SFX simple
        AudioManager.Instance.PlaySFX(jumpSound, 0.8f);
    }

    void TakeDamage()
    {
        // Reproducir SFX 3D en posición del enemigo
        Vector3 enemyPos = GetEnemyPosition();
        AudioManager.Instance.PlaySFX3D(damageSound, enemyPos, 1f);
    }
}

public class GameManager : MonoBehaviour
{
    [SerializeField] private AudioClip bossMusic;

    void StartBossFight()
    {
        // Fade out música actual
        AudioManager.Instance.FadeOutMusic(1f);
        
        // Esperar a que termine fade
        Invoke(nameof(PlayBossMusic), 1f);
    }

    void PlayBossMusic()
    {
        AudioManager.Instance.PlayMusic(bossMusic, true);
        AudioManager.Instance.FadeInMusic(2f);
    }
}
```

### API Completa

| Método | Descripción |
|--------|------------|
| `PlayMusic(AudioClip, bool loop)` | Reproducir música de fondo |
| `StopMusic()` | Detener música |
| `PauseMusic()` | Pausar música |
| `ResumeMusic()` | Reanudar música |
| `PlaySFX(AudioClip, float volume)` | Reproducir efecto 2D |
| `PlaySFX3D(AudioClip, Vector3 pos, float vol)` | Reproducir efecto 3D |
| `FadeOutMusic(float duration)` | Transición suave a silencio |
| `FadeInMusic(float duration)` | Transición suave desde silencio |
| `SetMasterVolume(float 0-1)` | Volumen maestro |
| `SetMusicVolume(float 0-1)` | Volumen música |
| `SetSFXVolume(float 0-1)` | Volumen efectos |
| `GetMasterVolume()` | Obtener volumen maestro |
| `GetMusicVolume()` | Obtener volumen música |
| `GetSFXVolume()` | Obtener volumen SFX |
| `IsMusicPlaying()` | ¿Música reproduciéndose? |
| `StopAllSounds()` | Silenciar todo |

---

## Ventajas Finales

✅ **Sin Memory Leaks**: Object Pool reutiliza objetos  
✅ **Sin Garbage Spikes**: No crea/destruye en runtime  
✅ **Compilación Segura**: AudioClips directos, no strings  
✅ **Audio Profesional**: AudioMixer con decibelios reales  
✅ **Corrutinas Seguras**: No hay race conditions  
✅ **Escalable**: Soporta múltiples sonidos simultáneos  

---

## Troubleshooting

**Error: "ObjectPool<T> not found"**
- Unity 2021.2+ incluye esto. Si estás en versión anterior, actualiza.

**El volumen no cambia:**
- ¿Asignaste el AudioMixer?
- ¿Creaste los parámetros "Master", "Music", "SFX"?

**No se escucha nada:**
- Verifica que `AudioSource.volume` > 0
- Checkea el volumen del sistema
- Abre Audio Mixer → ver si está muted

**3D audio no funciona:**
- Debe ser `PlaySFX3D()`, no `PlaySFX()`
- Verifica que la cámara tenga AudioListener
