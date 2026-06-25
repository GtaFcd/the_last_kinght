using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerCombat), typeof(PlayerController))]
public class PlayerChargedAttack : MonoBehaviour
{
    [Header("Tiempo de carga")]
    [SerializeField] private float chargeTime = 0.5f;

    [Header("Ataques cargados (ScriptableObjects)")]
    [SerializeField] private AttackData chargedHigh;
    [SerializeField] private AttackData chargedMid;
    [SerializeField] private AttackData chargedLow;

    [Header("Dash cargado")]
    [SerializeField] private float dashForce    = 14f;
    [SerializeField] private float dashDuration = 0.2f;

    [Header("Multiplicadores")]
    [SerializeField] private float damageMultiplier    = 2f;
    [SerializeField] private float knockbackMultiplier = 2.5f;

    [Header("Feedback visual (opcional)")]
    [SerializeField] private GameObject chargeVFX;
    [SerializeField] private GameObject releaseVFX;

    [Header("Hitbox")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Transform hitboxOrigin;

    public bool IsCharging  => state == ChargeState.Charging;
    public bool IsReady     => state == ChargeState.Ready;
    public bool IsExecuting => state == ChargeState.Executing;

    private enum ChargeState { Idle, Charging, Ready, Executing }
    private ChargeState state = ChargeState.Idle;
    private float holdTimer;
    private AttackZone lockedZone;
    private bool waitingForRelease = false;
    private bool usingDashAnim     = false;

    private PlayerCombat      combat;
    private PlayerController  controller;
    private PlayerInputConfig inputConfig;
    private Rigidbody2D       rb;

    private System.Collections.Generic.HashSet<Collider2D> hitThisAttack
        = new System.Collections.Generic.HashSet<Collider2D>();

    private void Awake()
    {
        combat     = GetComponent<PlayerCombat>();
        controller = GetComponent<PlayerController>();
        rb         = GetComponent<Rigidbody2D>();
        if (hitboxOrigin == null) hitboxOrigin = transform;
        if (chargeVFX    != null) chargeVFX.SetActive(false);
    }

    private void Start()
    {
        inputConfig = controller?.InputConfig;
    }

    private void Update()
    {
        if (inputConfig == null) return;

        switch (state)
        {
            case ChargeState.Idle:
                if (!inputConfig.GetAttackHeld())
                    waitingForRelease = false;

                if (!waitingForRelease && inputConfig.GetAttackDown())
                {
                    Debug.Log("[" + name + "] ATAQUE: comenzando carga");
                    state                    = ChargeState.Charging;
                    holdTimer                = 0f;
                    waitingForRelease        = true;
                    combat.blockNormalAttack = true;
                }
                break;

            case ChargeState.Charging:
                if (inputConfig.GetAttackHeld())
                {
                    holdTimer += Time.deltaTime;
                    if (chargeVFX != null)
                        chargeVFX.SetActive(holdTimer >= chargeTime * 0.5f);

                    if (holdTimer >= chargeTime)
                    {
                        Debug.Log("[" + name + "] ATAQUE CARGADO: listo!");
                        controller.OverrideMovement = true;
                        state = ChargeState.Ready;
                    }
                }
                else
                {
                    Debug.Log("[" + name + "] ATAQUE NORMAL (tap corto)");
                    state                    = ChargeState.Idle;
                    combat.blockNormalAttack = false;
                    if (chargeVFX != null) chargeVFX.SetActive(false);
                    waitingForRelease = false;
                    combat.ExecuteNormalAttack();
                }
                break;

            case ChargeState.Ready:
                float moveInput = inputConfig.GetMoveInput();
                if (Mathf.Abs(moveInput) > 0.1f)
                {
                    Debug.Log("[" + name + "] ATAQUE CARGADO CON DASH");
                    LanzarCargado(moveInput > 0f ? 1f : -1f);
                }
                else if (inputConfig.GetAttackUp())
                {
                    Debug.Log("[" + name + "] ATAQUE CARGADO ESTATICO");
                    LanzarCargado(0f);
                }
                break;

            case ChargeState.Executing:
                break;
        }
    }

    private void LanzarCargado(float dashDir)
    {
        state      = ChargeState.Executing;
        lockedZone = combat.CurrentZone;

        if (chargeVFX != null) chargeVFX.SetActive(false);

        AttackData data = GetChargedAttack();
        if (data == null)
        {
            Debug.LogWarning("[" + name + "] Falta AttackData para zona " + lockedZone);
            FinalizarAtaque();
            return;
        }

        controller.OverrideMovement = true;
        combat.blockNormalAttack    = true;

        usingDashAnim = (dashDir != 0f && lockedZone == AttackZone.Mid);
        combat.BeginChargedAnimation(lockedZone);

        GetComponent<CombatAudio>()?.PlayChargedAttack(lockedZone);
        
        if (dashDir != 0f)
            StartCoroutine(DashAttack(data, dashDir));
        else
            StartCoroutine(StandingAttack(data));
    }

    private IEnumerator StandingAttack(AttackData data)
    {
        yield return new WaitForSeconds(data.startupFrames);
        if (releaseVFX != null) Instantiate(releaseVFX, hitboxOrigin.position, Quaternion.identity);
        yield return StartCoroutine(DoHitbox(data));
        yield return new WaitForSeconds(data.recoveryFrames);
        FinalizarAtaque();
    }

    private IEnumerator DashAttack(AttackData data, float dashDir)
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            rb.linearVelocity = new Vector2(dashDir * dashForce, rb.linearVelocity.y);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!usingDashAnim)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        float postDash = data.startupFrames * 0.3f;
        yield return new WaitForSeconds(postDash);
        if (releaseVFX != null) Instantiate(releaseVFX, hitboxOrigin.position, Quaternion.identity);
        yield return StartCoroutine(DoHitbox(data));

        float recoveryElapsed = 0f;
        float startVelX = rb.linearVelocity.x;
        while (recoveryElapsed < data.recoveryFrames)
        {
            if (usingDashAnim)
                rb.linearVelocity = new Vector2(
                    Mathf.Lerp(startVelX, 0f, recoveryElapsed / data.recoveryFrames),
                    rb.linearVelocity.y);
            recoveryElapsed += Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        FinalizarAtaque();
    }

    private IEnumerator DoHitbox(AttackData data)
    {
        combat.SetHitboxActive(true);
        hitThisAttack.Clear();

        float elapsed = 0f;
        bool  blocked = false;

        while (elapsed < data.activeFrames && !blocked)
        {
            float   dirX   = controller.FacingRight ? 1f : -1f;
            Vector2 origin = (Vector2)hitboxOrigin.position
                           + new Vector2(data.hitboxOffset.x * dirX, data.hitboxOffset.y);

            foreach (var col in Physics2D.OverlapBoxAll(origin, data.hitboxSize, 0f, enemyLayer))
            {
                if (hitThisAttack.Contains(col)) continue;

                var targetCombat = col.GetComponent<PlayerCombat>()
                                ?? col.GetComponentInParent<PlayerCombat>();

                if (targetCombat != null &&
                    !targetCombat.IsAttacking &&
                    targetCombat.CurrentZone == lockedZone)
                {
                    float pushDir = transform.position.x < col.transform.position.x ? -1f : 1f;
                    rb.linearVelocity = Vector2.zero;
                    rb.AddForce(new Vector2(pushDir * 4f, 0.3f), ForceMode2D.Impulse);
                    blocked = true;
                    break;
                }

                hitThisAttack.Add(col);
                var dmg = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
                if (dmg == null) continue;

                float   damage = data.damage * damageMultiplier;
                Vector2 kb     = new Vector2(data.knockback.x * dirX * knockbackMultiplier,
                                             data.knockback.y * knockbackMultiplier);
                dmg.TakeDamage(damage, kb, data.hitStunSeconds * 1.5f, lockedZone);
            }

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        combat.SetHitboxActive(false);
        hitThisAttack.Clear();
    }

    private void FinalizarAtaque()
    {
        state                       = ChargeState.Idle;
        waitingForRelease           = false;
        combat.blockNormalAttack    = false;
        combat.SetHitboxActive(false);
        controller.OverrideMovement = false;
        if (usingDashAnim)
        {
            GetComponent<Animator>()?.SetBool("IsAttacking", false);
            combat.SetIsAttacking(false);
            usingDashAnim = false;
        }
        else
        {
            combat.EndChargedAnimation();
        }
        if (chargeVFX != null) chargeVFX.SetActive(false);
    }

    private AttackData GetChargedAttack() => lockedZone switch
    {
        AttackZone.High => chargedHigh,
        AttackZone.Low  => chargedLow,
        _               => chargedMid,
    };
}
