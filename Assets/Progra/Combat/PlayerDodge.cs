using System.Collections;
using UnityEngine;

public class PlayerDodge : MonoBehaviour
{
    [Header("Dash")]
    [SerializeField] private float dashForce    = 8f;
    [SerializeField] private float dashDuration = 0.2f;

    [Header("Agacharse")]
    [SerializeField] private float crouchDuration = 0.4f;

    [Header("Cooldown")]
    [SerializeField] private float cooldown = 0.8f;

    [Header("Refs")]
    [SerializeField] private PlayerCombat     playerCombat;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerHealth     playerHealth;
    [SerializeField] private Animator         animator;

    public bool      IsDodging        => isDodging;
    public DodgeType CurrentDodgeType { get; private set; }

    private bool  isDodging;
    private float cooldownTimer;
    private Rigidbody2D       rb;
    private PlayerInputConfig inputConfig;

    public enum DodgeType { None, DashForward, DashBack, Crouch }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (playerCombat     == null) playerCombat     = GetComponent<PlayerCombat>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (playerHealth     == null) playerHealth     = GetComponent<PlayerHealth>();
        if (animator         == null) animator         = GetComponent<Animator>();
    }

    private void Start()
    {
        inputConfig = playerController?.InputConfig;
    }

    private void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        if (isDodging)                return;
        if (playerCombat.IsAttacking) return;
        if (cooldownTimer > 0f)       return;
        if (inputConfig == null)      return;

        if (inputConfig.GetDodgeDown())
            StartDodge();
    }

    private void StartDodge()
    {
        float moveInput = inputConfig.GetMoveInput();

        if (Mathf.Abs(moveInput) > 0.1f)
        {
            float dir = moveInput > 0 ? 1f : -1f;
            bool movingForward = (moveInput > 0) == playerController.FacingRight;
            CurrentDodgeType = movingForward ? DodgeType.DashForward : DodgeType.DashBack;
            StartCoroutine(DashRoutine(dir, invincible: false));
        }
        else
        {
            CurrentDodgeType = DodgeType.Crouch;
            StartCoroutine(CrouchRoutine());
        }

        cooldownTimer = cooldown;
    }

    private IEnumerator DashRoutine(float dir, bool invincible)
    {
        GetComponent<CombatAudio>()?.PlayDodge();
        isDodging = true;
        animator?.SetBool("IsDodging", true);
        animator?.SetTrigger("Dash");

        playerController.OverrideMovement = true;
        if (invincible && playerHealth != null) playerHealth.SetInvincible(true);

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            rb.linearVelocity = new Vector2(dir * dashForce, rb.linearVelocity.y);
            elapsed += Time.deltaTime;
            yield return null;
        }

        float decelTime    = dashDuration * 0.6f;
        float decelElapsed = 0f;
        float startVelX    = dir * dashForce;
        while (decelElapsed < decelTime)
        {
            float tNorm  = decelElapsed / decelTime;
            float smooth = tNorm * tNorm * (3f - 2f * tNorm);
            rb.linearVelocity = new Vector2(Mathf.Lerp(startVelX, 0f, smooth), rb.linearVelocity.y);
            decelElapsed += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        playerController.OverrideMovement = false;
        if (invincible && playerHealth != null) playerHealth.SetInvincible(false);

        CurrentDodgeType = DodgeType.None;
        isDodging = false;
        animator?.SetBool("IsDodging", false);
    }

    private IEnumerator CrouchRoutine()
    {
        GetComponent<CombatAudio>()?.PlayCrouch();
        isDodging = true;
        animator?.SetBool("IsDodging", true);
        
        // Antes: SetTrigger("Dodge")
        // Ahora: trigger específico según la zona actual
        AttackZone zone = playerCombat != null ? playerCombat.CurrentZone : AttackZone.Mid;
        if (zone == AttackZone.Mid)
            animator?.SetTrigger("Crouch");   // <-- nuevo trigger para agacharse
        else
            animator?.SetTrigger("Dodge");

        if (playerHealth != null) playerHealth.SetInvincible(true);
        yield return new WaitForSeconds(crouchDuration);
        if (playerHealth != null) playerHealth.SetInvincible(false);

        CurrentDodgeType = DodgeType.None;
        isDodging = false;
        animator?.SetBool("IsDodging", false);
    }
}
