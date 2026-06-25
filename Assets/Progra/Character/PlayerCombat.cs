using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Ataques (ScriptableObjects)")]
    [SerializeField] private AttackData attackHigh;
    [SerializeField] private AttackData attackMid;
    [SerializeField] private AttackData attackLow;

    [Header("Hitbox")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Transform hitboxOrigin;

    [Header("Refs")]
    [SerializeField] private Camera cam;
    [SerializeField] private Animator animator;

    public bool       IsAttacking      => isAttacking;
    public bool       HitboxActive     => hitboxActive;
    public AttackZone CurrentZone      { get; private set; }
    public AttackData CurrentAttackData => currentAttack;

    [HideInInspector] public bool blockNormalAttack = false;

    private bool       isAttacking;
    private bool       hitboxActive;
    private AttackData currentAttack;
    private PlayerController controller;
    private PlayerInputConfig inputConfig;
    private float blockPushTimer;

    private System.Collections.Generic.HashSet<Collider2D> hitThisAttack
        = new System.Collections.Generic.HashSet<Collider2D>();

    private void Awake()
    {
        controller  = GetComponent<PlayerController>();
        if (cam          == null) cam          = Camera.main;
        if (animator     == null) animator     = GetComponent<Animator>();
        if (hitboxOrigin == null) hitboxOrigin = transform;
        if (enemyLayer.value == 0)
        {
            int idx = LayerMask.NameToLayer("Enemy");
            if (idx >= 0) enemyLayer = 1 << idx;
        }
    }

    private void Start()
    {
        inputConfig = controller?.InputConfig;
    }

    private void Update()
    {
        UpdateZone();
        if (blockPushTimer > 0f)
        {
            blockPushTimer -= Time.deltaTime;
            if (controller != null) controller.OverrideMovement = blockPushTimer > 0f;
        }
    }

    private void FixedUpdate()
    {
        if (hitboxActive) CheckHits();
    }

    private void UpdateZone()
    {
        if (inputConfig == null) return;

        if (inputConfig.UsesManualZone)
        {
            if (inputConfig.GetZoneUpDown())
                CurrentZone = CurrentZone switch
                {
                    AttackZone.Low => AttackZone.Mid,
                    AttackZone.Mid => AttackZone.High,
                    _              => AttackZone.High,
                };
            else if (inputConfig.GetZoneDownDown())
                CurrentZone = CurrentZone switch
                {
                    AttackZone.High => AttackZone.Mid,
                    AttackZone.Mid  => AttackZone.Low,
                    _               => AttackZone.Low,
                };
            return;
        }

        if (cam == null) return;
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        float deltaY = mouseWorld.y - transform.position.y;
        if      (deltaY >  inputConfig.highThreshold) CurrentZone = AttackZone.High;
        else if (deltaY <  inputConfig.lowThreshold)  CurrentZone = AttackZone.Low;
        else                                          CurrentZone = AttackZone.Mid;
    }

    public void ExecuteNormalAttack()
    {
        if (isAttacking) return;
        currentAttack = CurrentZone switch
        {
            AttackZone.High => attackHigh,
            AttackZone.Low  => attackLow,
            _               => attackMid,
        };
        if (currentAttack == null) return;
        Debug.Log("[" + name + "] Ataque NORMAL zona=" + CurrentZone);
        isAttacking = true;
        GetComponent<CombatAudio>()?.PlayAttack(CurrentZone);
        StartCoroutine(NormalAttackRoutine(currentAttack));
    }

    public void BeginChargedAnimation(AttackZone zone)
    {
        isAttacking = true;
        string triggerName = ZoneToTrigger(zone);
        Debug.Log("[" + name + "] BeginChargedAnimation zona=" + zone + " trigger=" + triggerName);
        animator?.SetBool("IsAttacking", true);
        animator?.SetTrigger(triggerName);
    }

    public void EndChargedAnimation()
    {
        Debug.Log("[" + name + "] EndChargedAnimation");
        isAttacking = false;
        animator?.SetBool("IsAttacking", false);
    }

    private IEnumerator NormalAttackRoutine(AttackData data)
    {
        hitboxActive = false;
        hitThisAttack.Clear();

        string triggerName = ZoneToTrigger(data.zone);
        animator?.SetBool("IsAttacking", true);
        animator?.SetTrigger(triggerName);

        controller.OverrideMovement = true;

        yield return new WaitForSeconds(data.startupFrames);
        hitboxActive = true;
        yield return new WaitForSeconds(data.activeFrames);
        hitboxActive = false;
        yield return new WaitForSeconds(data.recoveryFrames);

        controller.OverrideMovement = false;
        animator?.SetBool("IsAttacking", false);
        isAttacking = false;
    }

    private void CheckHits()
    {
        if (currentAttack == null) return;
        float   dirX   = controller != null ? (controller.FacingRight ? 1f : -1f) : 1f;
        Vector2 origin = (Vector2)hitboxOrigin.position
                       + new Vector2(currentAttack.hitboxOffset.x * dirX, currentAttack.hitboxOffset.y);

        foreach (var col in Physics2D.OverlapBoxAll(origin, currentAttack.hitboxSize, 0f, enemyLayer))
        {
            if (hitThisAttack.Contains(col)) continue;
            hitThisAttack.Add(col);

            var targetCombat = col.GetComponent<PlayerCombat>() ?? col.GetComponentInParent<PlayerCombat>();
            if (targetCombat != null && !targetCombat.IsAttacking && targetCombat.CurrentZone == CurrentZone)
            {
                var rb = GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    float pushDir = transform.position.x < col.transform.position.x ? -1f : 1f;
                    rb.linearVelocity = Vector2.zero;
                    rb.AddForce(new Vector2(pushDir * 4f, 0.3f), ForceMode2D.Impulse);
                    blockPushTimer = 0.2f;
                    controller.OverrideMovement = true;
                }
                CancelHitbox();
                return;
            }

            var dmg = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (dmg != null)
            {
                Vector2 kb = new Vector2(currentAttack.knockback.x * dirX, currentAttack.knockback.y);
                dmg.TakeDamage(currentAttack.damage, kb, currentAttack.hitStunSeconds, CurrentZone);
            }
        }
    }

    public void CancelHitbox()
    {
        hitboxActive = false;
        hitThisAttack.Clear();
    }

    public void SetHitboxActive(bool value)
    {
        hitboxActive = value;
        if (!value) hitThisAttack.Clear();
    }

    public void SetCurrentAttack(AttackData data) { currentAttack = data; }
    public void SetIsAttacking(bool value)         { isAttacking = value; }

    public void ForceStopAttack()
    {
        StopAllCoroutines();
        isAttacking  = false;
        hitboxActive = false;
        hitThisAttack.Clear();
        if (controller != null) controller.OverrideMovement = false;
        animator?.SetBool("IsAttacking", false);
    }

    private string ZoneToTrigger(AttackZone zone) => zone switch
    {
        AttackZone.High => "Attack_High",
        AttackZone.Low  => "Attack_Low",
        _               => "Attack_Mid",
    };

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = CurrentZone switch
        {
            AttackZone.High => Color.cyan,
            AttackZone.Low  => Color.yellow,
            _               => Color.green,
        };
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, new Vector3(0.15f, 0.15f, 0f));
        if (hitboxActive && currentAttack != null)
        {
            float   dirX = controller != null ? (controller.FacingRight ? 1f : -1f) : 1f;
            Vector2 pos  = (Vector2)hitboxOrigin.position
                         + new Vector2(currentAttack.hitboxOffset.x * dirX, currentAttack.hitboxOffset.y);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(pos, currentAttack.hitboxSize);
        }
    }
}