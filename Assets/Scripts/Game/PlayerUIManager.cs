using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the player HUD — shows only respawns remaining.
/// </summary>
public class PlayerUIManager : MonoBehaviour
{
    [SerializeField] private Text respawnText;

    public void Setup(Text text)
    {
        respawnText = text;
    }

    private void Start()
    {
        if (VesperController.Instance != null)
        {
            VesperController.Instance.OnLivesChanged += UpdateRespawns;
            UpdateRespawns(VesperController.Instance.currentLives);
        }
    }

    private void OnDestroy()
    {
        if (VesperController.Instance != null)
        {
            VesperController.Instance.OnLivesChanged -= UpdateRespawns;
        }
    }

    private void UpdateRespawns(int currentLives)
    {
        if (respawnText != null)
        {
            respawnText.text = $"Respawns: {currentLives}";
            // Color feedback
            if (currentLives <= 1)
                respawnText.color = Color.red;
            else if (currentLives <= 2)
                respawnText.color = Color.yellow;
            else
                respawnText.color = Color.white;
        }
    }
}
