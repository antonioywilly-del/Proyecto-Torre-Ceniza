using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(VesperAnimator))]
public class VesperController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 16.5f;
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;

    [Header("Respawn")]
    public int maxLives = 5;
    public float outOfBoundsY = -5f;
    
    public int currentLives { get; private set; }
    public bool IsDead { get; private set; }

    private Rigidbody2D rb;
    private BoxCollider2D col;
    private VesperAnimator anim;
    
    private float moveInput;
    private bool isGrounded;
    private bool isFacingRight = true;
    private bool isDying = false; // True during death sequence
    private int jumpCount = 0;
    private const int maxJumps = 2; // Double jump
    
    private Vector3 lastSafeTilePosition;
    private Vector3 initialSpawnPosition;
    public static VesperController Instance { get; private set; }

    // Events for UI
    public System.Action<int> OnLivesChanged;

    private void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        anim = GetComponent<VesperAnimator>();
        
        // Configure Rigidbody for platforming
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Apply frictionless physics material to prevent wall sticking
        PhysicsMaterial2D noFriction = new PhysicsMaterial2D("NoFriction");
        noFriction.friction = 0f;
        noFriction.bounciness = 0f;
        col.sharedMaterial = noFriction;

        currentLives = maxLives;
        IsDead = false;
    }

    private void Start()
    {
        if (groundLayer == 0)
        {
            groundLayer = LayerMask.GetMask("Default");
        }
        
        initialSpawnPosition = transform.position;
        lastSafeTilePosition = transform.position;
        NotifyUI();

        // Create ground check at feet
        if (groundCheck == null)
        {
            GameObject gc = new GameObject("GroundCheck");
            gc.transform.SetParent(transform);
            gc.transform.localPosition = new Vector3(0, -(col.size.y / 2f) + col.offset.y, 0);
            groundCheck = gc.transform;
        }
    }

    private void Update()
    {
        if (isDying || IsDead) return;

        CheckGrounded();
        HandleInput();
        UpdateAnimations();
        CheckOutOfBounds();
    }

    private void FixedUpdate()
    {
        if (isDying || IsDead) 
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // Apply horizontal movement
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    private void CheckGrounded()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius);
        isGrounded = false;
        foreach (var hit in hits)
        {
            if (!hit.isTrigger && hit.gameObject != gameObject)
            {
                isGrounded = true;
                break;
            }
        }

        // Reset jump count when landing or running on ground
        if (isGrounded && rb.linearVelocity.y <= 0.1f)
        {
            jumpCount = 0;
        }
    }

    private void HandleInput()
    {
        if (Keyboard.current == null) 
        {
            moveInput = 0;
            return;
        }

        // Horizontal Movement Input (A/D or Left/Right arrows)
        moveInput = 0f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            moveInput = -1f;
        else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            moveInput = 1f;

        // Flip sprite based on movement direction
        if (moveInput > 0 && !isFacingRight)
            Flip();
        else if (moveInput < 0 && isFacingRight)
            Flip();

        // Jump Input — double jump (max 2 jumps)
        if ((Keyboard.current.wKey.wasPressedThisFrame || 
             Keyboard.current.upArrowKey.wasPressedThisFrame || 
             Keyboard.current.spaceKey.wasPressedThisFrame) && jumpCount < maxJumps)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpCount++;
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        // Tell the animator which direction we face; it handles the scale sign
        anim.SetFacingRight(isFacingRight);
    }

    private void UpdateAnimations()
    {
        if (!isGrounded)
        {
            anim.SetState(VesperState.Jump);
        }
        else if (Mathf.Abs(moveInput) > 0.1f)
        {
            anim.SetState(VesperState.Run);
        }
        else
        {
            anim.SetState(VesperState.Idle);
        }
    }

    private void CheckOutOfBounds()
    {
        if (transform.position.y < outOfBoundsY)
        {
            StartCoroutine(DeathSequence());
        }
    }

    /// <summary>
    /// Called when standing on a safe ground tile. Tracks last safe position.
    /// </summary>
    private void OnCollisionStay2D(Collision2D collision)
    {
        // Only track safe position when grounded on a non-trap surface
        if (isGrounded && !isDying)
        {
            lastSafeTilePosition = transform.position;
        }
    }

    /// <summary>
    /// Called by TrapZone when Vesper touches any trap. Insta-kill with 2s animation.
    /// </summary>
    public void KillFromTrap()
    {
        if (isDying || IsDead) return;
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        if (isDying) yield break;
        isDying = true;
        moveInput = 0;
        rb.linearVelocity = Vector2.zero;

        // Show hurt animation for ~1 second
        anim.SetState(VesperState.Hurt);
        yield return new WaitForSeconds(1f);

        // Show death animation for ~1 second
        anim.SetState(VesperState.Dead);
        yield return new WaitForSeconds(1f);

        // Consume a life and respawn or permanent death
        currentLives--;
        NotifyUI();

        if (currentLives > 0)
        {
            // Respawn to last safe tile
            transform.position = lastSafeTilePosition;
            rb.linearVelocity = Vector2.zero;
            isDying = false;
            anim.SetState(VesperState.Idle);
        }
        else
        {
            // Permanent death — game over
            IsDead = true;
            rb.linearVelocity = Vector2.zero;
            anim.SetState(VesperState.Dead);
            // GameManager can listen and show Game Over screen
        }
    }

    private void NotifyUI()
    {
        OnLivesChanged?.Invoke(currentLives);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
