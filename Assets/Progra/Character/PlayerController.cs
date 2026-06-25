using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed    = 6f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float deceleration = 20f;

    [Header("Salto")]
    [SerializeField] private float jumpForce             = 14f;
    [SerializeField] private float fallGravityMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier     = 2f;
    [SerializeField] private LayerMask  groundLayer;
    [SerializeField] private Transform  groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;

    [Header("Coyote Time & Jump Buffer")]
    [SerializeField] private float coyoteTime     = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Input")]
    [SerializeField] private PlayerInputConfig inputConfig;

    [Header("PvP")]
    [SerializeField] private Transform opponent;

    private Rigidbody2D  rb;
    private Animator     animator;
    private PlayerCombat combat;
    private PlayerDodge  dodge;

    private float moveInput;
    private bool  jumpPressed;
    private bool  jumpHeld;

    private bool  isGrounded;
    private bool  isJumping;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool  facingRight = true;

    public bool  IsGrounded    => isGrounded;
    public bool  FacingRight   => facingRight;
    public float MoveInput     => moveInput;
    public bool  IsMoving      => Mathf.Abs(rb.linearVelocity.x) > 0.1f;
    public bool  OverrideMovement { get; set; } = false;
    public PlayerInputConfig InputConfig => inputConfig;

    private void Awake()
    {
        rb       = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        combat   = GetComponent<PlayerCombat>();
        dodge    = GetComponent<PlayerDodge>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void Update()
    {
        ReadInput();
        CheckGround();
        HandleCoyoteTime();
        HandleJumpBuffer();
        HandleFlip();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
        ApplyGravity();
    }

    private void ReadInput()
    {
        if (inputConfig == null) return;
        bool busy = (combat != null && combat.IsAttacking) || (dodge != null && dodge.IsDodging);
        if (busy) { moveInput = 0f; jumpPressed = false; jumpHeld = false; return; }
        moveInput   = inputConfig.GetMoveInput();
        jumpPressed = inputConfig.GetJumpDown();
        jumpHeld    = inputConfig.GetJumpHeld();
    }

    private void HandleFlip()
    {
        if (opponent != null)
        {
            bool shouldFaceRight = opponent.position.x > transform.position.x;
            if (shouldFaceRight != facingRight) SetFacing(shouldFaceRight);
            return;
        }
        if (inputConfig != null && inputConfig.GetFlipDown()) SetFacing(!facingRight);
    }

    public void SetFacing(bool faceRight)
    {
        facingRight = faceRight;
        Vector3 s = transform.localScale;
        s.x = faceRight ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
        transform.localScale = s;
    }

    private void ApplyMovement()
    {
        if (OverrideMovement) return;
        float targetSpeed = moveInput * moveSpeed;
        float speedDiff   = targetSpeed - rb.linearVelocity.x;
        float rate        = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
        rb.AddForce(new Vector2(speedDiff * rate, 0f), ForceMode2D.Force);
    }

    private void ApplyGravity()
    {
        if (rb.linearVelocity.y < 0)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime;
        else if (rb.linearVelocity.y > 0 && !jumpHeld)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
    }

    private void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void HandleCoyoteTime()
    {
        if (isGrounded) coyoteTimer = coyoteTime;
        else            coyoteTimer -= Time.deltaTime;
    }

    private void HandleJumpBuffer()
    {
        if (jumpPressed) jumpBufferTimer = jumpBufferTime;
        else             jumpBufferTimer -= Time.deltaTime;
        if (jumpBufferTimer > 0f && coyoteTimer > 0f) Jump();
        if (!jumpHeld && isJumping && rb.linearVelocity.y > 0) isJumping = false;
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpBufferTimer = 0f; coyoteTimer = 0f; isJumping = true;
        animator?.SetTrigger("Jump");
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetFloat("Speed",     Mathf.Abs(rb.linearVelocity.x));
        animator.SetBool ("Grounded",  isGrounded);
        animator.SetFloat("VelocityY", rb.linearVelocity.y);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}