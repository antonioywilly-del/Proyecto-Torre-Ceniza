using UnityEngine;

/// <summary>
/// Generic sprite frame animator for traps (lava, electricity, laser).
/// Cycles through an array of sprites at a configurable frame rate.
/// </summary>
public class TrapAnimator : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameRate = 6f;

    private SpriteRenderer spriteRenderer;
    private float frameTimer;
    private int currentFrame;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (frames == null || frames.Length <= 1) return;
        if (spriteRenderer == null) return;

        frameTimer += Time.deltaTime;
        if (frameTimer >= 1f / frameRate)
        {
            frameTimer = 0f;
            currentFrame = (currentFrame + 1) % frames.Length;
            spriteRenderer.sprite = frames[currentFrame];
        }
    }

    /// <summary>
    /// Set frames at runtime (used by MapBuilder).
    /// </summary>
    public void SetFrames(Sprite[] newFrames)
    {
        frames = newFrames;
        currentFrame = 0;
        frameTimer = 0f;
        if (spriteRenderer != null && frames != null && frames.Length > 0)
        {
            spriteRenderer.sprite = frames[0];
        }
    }

    /// <summary>
    /// Set the frame rate at runtime.
    /// </summary>
    public void SetFrameRate(float rate)
    {
        frameRate = rate;
    }
}
