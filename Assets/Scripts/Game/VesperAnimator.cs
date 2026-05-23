using UnityEngine;

public enum VesperState
{
    Idle,
    Run,
    Jump,
    Hurt,
    Dead
}

/// <summary>
/// Code-driven animator for Vesper. Bypasses the complex Animator component.
/// Normalizes all sprite sizes so the character doesn't visually resize during animations.
/// Works with the base scale set by MapBuilder.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class VesperAnimator : MonoBehaviour
{
    private SpriteRenderer sr;
    private Transform visualsPivot;

    private Sprite idleSprite;
    private Sprite[] runFrames;
    private Sprite[] jumpFrames;
    private Sprite[] hurtFrames;
    private Sprite[] deadFrames;

    private VesperState currentState = VesperState.Idle;
    private float timer = 0f;
    private int currentFrame = 0;
    private float frameRate = 8f;

    // The base scale set externally (by MapBuilder). We normalize relative to this.
    private float baseScale = 1f;
    // Reference sprite bounds (from idle) for normalization
    private Vector2 referenceSize;

    private void Awake()
    {
        SpriteRenderer rootSr = GetComponent<SpriteRenderer>();
        if (rootSr != null)
        {
            rootSr.enabled = false;
            
            // Create a child object for visuals to prevent scaling the root and breaking physics
            GameObject visualsObj = new GameObject("Visuals");
            visualsObj.transform.SetParent(transform, false);
            visualsObj.transform.localPosition = Vector3.zero;
            visualsPivot = visualsObj.transform;

            sr = visualsObj.AddComponent<SpriteRenderer>();
            sr.color = rootSr.color;
            sr.sharedMaterial = rootSr.sharedMaterial;
            sr.sortingLayerID = rootSr.sortingLayerID;
            sr.sortingOrder = rootSr.sortingOrder;
        }
        else
        {
            sr = GetComponentInChildren<SpriteRenderer>();
            visualsPivot = sr.transform;
        }
    }

    public void Setup(Sprite[] movement, Sprite[] jump, Sprite[] hurt, Sprite[] death)
    {
        // Idle is 3rd frame of movement sheet (index 2)
        if (movement != null && movement.Length > 2)
        {
            idleSprite = movement[2];
        }
        else if (movement != null && movement.Length > 0)
        {
            idleSprite = movement[0];
        }

        runFrames = movement;
        jumpFrames = jump;
        hurtFrames = hurt;
        deadFrames = death;

        // Capture the reference size from idle sprite
        if (idleSprite != null)
        {
            referenceSize = idleSprite.bounds.size;
        }

        SetState(VesperState.Idle);
    }

    /// <summary>
    /// Call after setting transform.localScale from MapBuilder.
    /// Records the intended base scale so normalization works correctly.
    /// </summary>
    public void SetBaseScale(float scale)
    {
        baseScale = scale;
    }

    private bool facingRight = true;

    /// <summary>
    /// Sets which direction Vesper is facing. Used for X-scale flipping.
    /// </summary>
    public void SetFacingRight(bool right)
    {
        facingRight = right;
    }

    public void SetState(VesperState newState)
    {
        if (currentState == newState && newState != VesperState.Hurt) return;

        currentState = newState;
        currentFrame = 0;
        timer = 0f;
        ApplySprite();
    }

    public VesperState GetState() => currentState;

    private void Update()
    {
        if (currentState == VesperState.Idle)
        {
            ApplySprite();
            return;
        }

        Sprite[] activeFrames = GetActiveFrames();
        if (activeFrames == null || activeFrames.Length == 0) return;

        timer += Time.deltaTime;
        float timePerFrame = 1f / frameRate;

        if (timer >= timePerFrame)
        {
            timer -= timePerFrame;
            currentFrame++;

            if (currentFrame >= activeFrames.Length)
            {
                if (currentState == VesperState.Dead)
                {
                    currentFrame = activeFrames.Length - 1; // Freeze on last death frame
                }
                else if (currentState == VesperState.Hurt)
                {
                    currentFrame = 0; // Loop hurt until controller changes state
                }
                else
                {
                    currentFrame = 0; // Loop
                }
            }
            ApplySprite();
        }
    }

    /// <summary>
    /// Applies the current sprite and normalizes its visual size so the character
    /// doesn't visually grow/shrink between animation states.
    /// </summary>
    private void ApplySprite()
    {
        Sprite sprite = null;

        if (currentState == VesperState.Idle)
        {
            sprite = idleSprite;
        }
        else
        {
            Sprite[] activeFrames = GetActiveFrames();
            if (activeFrames != null && activeFrames.Length > 0 && currentFrame < activeFrames.Length)
            {
                sprite = activeFrames[currentFrame];
            }
        }

        if (sprite == null) return;
        
        sr.sprite = sprite;

        // Normalize: scale this sprite so it matches the reference (idle) sprite size.
        // We apply this to the child visuals pivot so we don't scale the physics collider.
        if (referenceSize.x > 0 && referenceSize.y > 0 && visualsPivot != null)
        {
            Vector2 currentSize = sprite.bounds.size;
            float normX = referenceSize.x / currentSize.x;
            float normY = referenceSize.y / currentSize.y;
            float norm = Mathf.Min(normX, normY);

            // Preserve facing direction on the visual pivot
            float signX = facingRight ? 1f : -1f;
            visualsPivot.localScale = new Vector3(signX * norm, norm, 1f);
        }
    }

    private Sprite[] GetActiveFrames()
    {
        switch (currentState)
        {
            case VesperState.Run: return runFrames;
            case VesperState.Jump: return jumpFrames;
            case VesperState.Hurt: return hurtFrames;
            case VesperState.Dead: return deadFrames;
            default: return null;
        }
    }
}
