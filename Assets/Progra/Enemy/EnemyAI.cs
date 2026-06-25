using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour, IDamageable
{
    // ───────────────────────────────────────────
    // CONFIGURACIÓN
    // ───────────────────────────────────────────
    [Header("Personalidad")]
    [SerializeField] private AIPersonality personality = AIPersonality.Offensive;

    [Header("Ataques normales")]
    [SerializeField] private AttackData attackHigh;
    [SerializeField] private AttackData attackMid;
    [SerializeField] private AttackData attackLow;

    [Header("Ataques cargados")]
    [SerializeField] private AttackData chargedHigh;
    [SerializeField] private AttackData chargedMid;
    [SerializeField] private AttackData chargedLow;
    [SerializeField] private float chargedDamageMultiplier    = 2f;
    [SerializeField] private float chargedKnockbackMultiplier = 2.5f;

    [Header("Probabilidades de acción (suman ~1)")]
    [SerializeField] private float chanceNormalAttack  = 0.50f;
    [SerializeField] private float chanceChargedAttack = 0.20f;
    [SerializeField] private float chanceDodge         = 0.15f;
    [SerializeField] private float chanceShove         = 0.15f;

    [Header("Combate")]
    [SerializeField] private float attackRange      = 2.5f;
    [SerializeField] private float approachRange    = 5f;
    [SerializeField] private float minAttackDelay   = 0.8f;
    [SerializeField] private float maxAttackDelay   = 2.2f;
    [Tooltip("Tiempo que el enemigo queda en stun tras recibir un bloqueo")]
    [SerializeField] private float blockStunDuration = 0.8f;

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed    = 3f;
    [SerializeField] private float stopDistance = 6.0f;
    [SerializeField] private float retreatDuration = 1.4f;

    [Header("Esquive")]
    [SerializeField] private float dodgeDashForce    = 10f;
    [SerializeField] private float dodgeDashDuration = 0.2f;

    [Header("Ataque cargado dash")]
    [SerializeField] private float chargedDashForce    = 14f;
    [SerializeField] private float chargedDashDuration = 0.2f;
    [Tooltip("Probabilidad de hacer el dash vs quieto en el ataque cargado")]
    [SerializeField] private float chargedDashChance = 0.5f;

    [Header("Shove")]
    [SerializeField] private float shovePushForce = 8f;
    [SerializeField] private float shoveRange     = 1.8f;

    [Header("IA Defensiva")]
    [SerializeField] private float waitForPlayerTime = 1.5f;

    [Header("IA Predator")]
    [SerializeField] private float predatorMinDelay  = 1.0f;
    [SerializeField] private float predatorMaxDelay  = 1.8f;
    [SerializeField] private float fintaChance       = 0.15f;
    [SerializeField] private float comboChance       = 0.18f;
    // [SerializeField] private float counterWindowSecs = 0.4f;
    [SerializeField] private float predatorMoveSpeed = 3.0f;

    [Header("HP")]
    [SerializeField] private float maxHp = 100f;

    [Header("Feedback Visual")]
    [SerializeField] private Color hitColor  = Color.red;
    [SerializeField] private float flashTime = 0.12f;

    [Header("Refs")]
    [SerializeField] private Transform    player;
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private LayerMask    playerLayer;

    // ── Propiedades públicas ──
    public bool       IsAttacking  => isAttacking;
    public bool       HitboxActive => hitboxActive;
    public AttackZone CurrentZone  { get; private set; }
    public AttackData CurrentAttack => currentAttack;
    public float      CurrentHp    => currentHp;
    public float      MaxHp        => maxHp;
    public bool       IsDodging    => isDodging;

    public System.Action<float, float> OnHealthChanged;

    // ── Estado interno ──
    private enum AIState { Idle, Approach, Action, Retreat, WaitAndWatch }
    private AIState state = AIState.Idle;

    private Rigidbody2D   rb;
    private SpriteRenderer sr;
    private Animator      animator;
    private Color         originalColor;

    private float currentHp;
    private bool  isAttacking;
    private bool  hitboxActive;
    private bool  isDodging;
    private bool  isShoved;
    private AttackData currentAttack;
    private float attackTimer;
    private bool  facingRight    = false;
    private bool  isBlockStunned = false;

    private float watchTimer;
    private bool  playerJustAttacked;

    // ── Estado Predator ──
    private int        comboHitsLeft        = 0;
    private float      predatorPressureTimer = 0f;
    private bool       fintaDone            = false;
    private AttackZone lastPlayerZone       = AttackZone.Mid;

    private System.Collections.Generic.HashSet<Collider2D> hitThisAttack
        = new System.Collections.Generic.HashSet<Collider2D>();

    // ───────────────────────────────────────────
    // INIT
    // ───────────────────────────────────────────
    private void Awake()
    {
        rb       = GetComponent<Rigidbody2D>();
        sr       = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (sr != null) originalColor = sr.color;
        currentHp   = maxHp;
        attackTimer = Random.Range(minAttackDelay, maxAttackDelay);
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                if (playerCombat == null)
                    playerCombat = p.GetComponent<PlayerCombat>();
            }
        }
        if (playerLayer.value == 0)
        {
            int idx = LayerMask.NameToLayer("Player");
            if (idx >= 0) playerLayer = 1 << idx;
        }
    }

    // ───────────────────────────────────────────
    // UPDATE
    // ───────────────────────────────────────────
    private void Update()
    {
        if (player == null || currentHp <= 0f) return;
        if (isShoved || isDodging || isBlockStunned) return;

        float dist = Vector2.Distance(transform.position, player.position);
        FacePlayer();

        if (playerCombat != null && playerCombat.IsAttacking)
            playerJustAttacked = true;

        switch (personality)
        {
            case AIPersonality.Offensive: UpdateOffensive(dist); break;
            case AIPersonality.Defensive: UpdateDefensive(dist); break;
            case AIPersonality.Predator:  UpdatePredator(dist);  break;
        }
    }

    private void FixedUpdate()
    {
        if (hitboxActive) CheckHits();
    }

    // ───────────────────────────────────────────
    // IA OFENSIVA
    // ───────────────────────────────────────────
    private void UpdateOffensive(float dist)
    {
        switch (state)
        {
            case AIState.Idle:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                if (dist <= approachRange) state = AIState.Approach;
                break;

            case AIState.Approach:
                if (isAttacking) { rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); return; }

                if (dist > stopDistance)
                {
                    float dir = player.position.x > transform.position.x ? 1f : -1f;
                    rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
                }
                else
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                    attackTimer -= Time.deltaTime;
                    if (attackTimer <= 0f)
                    {
                        state = AIState.Action;
                        StartCoroutine(ChooseAndExecuteAction(dist));
                        attackTimer = Random.Range(minAttackDelay, maxAttackDelay);
                    }
                }
                break;

            case AIState.Retreat:
            case AIState.Action:
                break;
        }
    }

    // ───────────────────────────────────────────
    // IA DEFENSIVA
    // ───────────────────────────────────────────
    private void UpdateDefensive(float dist)
    {
        switch (state)
        {
            case AIState.Idle:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                if (dist <= approachRange) state = AIState.Approach;
                break;

            case AIState.Approach:
                if (isAttacking) { rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); return; }

                if (dist > stopDistance)
                {
                    float dir = player.position.x > transform.position.x ? 1f : -1f;
                    rb.linearVelocity = new Vector2(dir * moveSpeed * 0.7f, rb.linearVelocity.y);
                }
                else
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                    state      = AIState.WaitAndWatch;
                    watchTimer = waitForPlayerTime;
                    playerJustAttacked = false;
                }
                break;

            case AIState.WaitAndWatch:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                watchTimer -= Time.deltaTime;

                // El jugador atacó → reaccionar: esquivar o contraatacar
                if (playerJustAttacked && !playerCombat.IsAttacking)
                {
                    playerJustAttacked = false;
                    state = AIState.Action;
                    StartCoroutine(DefensiveReactionRoutine());
                    return;
                }

                if (watchTimer <= 0f)
                {
                    state = AIState.Action;
                    StartCoroutine(ChooseAndExecuteAction(Vector2.Distance(transform.position, player.position)));
                    watchTimer = waitForPlayerTime;
                }
                break;

            case AIState.Action:
                break;
        }
    }

    // ───────────────────────────────────────────
    // IA PREDATOR
    // ───────────────────────────────────────────
    private void UpdatePredator(float dist)
    {
        if (isAttacking || isDodging || isShoved) return;

        // Registrar cambio de zona del jugador (para detectar fallos)
        AttackZone currentPlayerZone = playerCombat != null ? playerCombat.CurrentZone : AttackZone.Mid;
        bool playerChangedZone = currentPlayerZone != lastPlayerZone;
        lastPlayerZone = currentPlayerZone;

        // Siempre aproximarse al jugador a velocidad Predator
        if (dist > stopDistance)
        {
            float dir = player.position.x > transform.position.x ? 1f : -1f;
            rb.linearVelocity = new Vector2(dir * predatorMoveSpeed, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        // Contraataque inmediato si el jugador falló (atacó y no conectó)
        if (playerCombat != null && playerJustAttacked && !playerCombat.IsAttacking)
        {
            playerJustAttacked = false;
            if (dist <= attackRange * 1.3f)
            {
                state = AIState.Action;
                // Contraatacar en la zona opuesta a la que usó el jugador
                CurrentZone = GetCounterZone(currentPlayerZone);
                StartCoroutine(PredatorAttackRoutine(combo: false, forceZone: true));
                return;
            }
        }

        // Presión continua: timer controla el ritmo de ataque
        predatorPressureTimer -= Time.deltaTime;
        if (predatorPressureTimer > 0f || dist > attackRange) return;

        state = AIState.Action;
        fintaDone = false;
        comboHitsLeft = Random.value < comboChance ? 1 : 0;
        StartCoroutine(PredatorAttackRoutine(combo: comboHitsLeft > 0, forceZone: false));
        predatorPressureTimer = Random.Range(predatorMinDelay, predatorMaxDelay);
    }

    // Elige la zona que el jugador NO puede bloquear desde su posición actual
    private AttackZone GetCounterZone(AttackZone playerZone)
    {
        // Atacar la zona que NO es la del jugador — fuerza un error de bloqueo
        return playerZone switch
        {
            AttackZone.High => Random.value < 0.5f ? AttackZone.Mid : AttackZone.Low,
            AttackZone.Mid  => Random.value < 0.5f ? AttackZone.High : AttackZone.Low,
            AttackZone.Low  => Random.value < 0.5f ? AttackZone.High : AttackZone.Mid,
            _               => AttackZone.Mid,
        };
    }

    private IEnumerator PredatorAttackRoutine(bool combo, bool forceZone)
    {
        // Finta: mostrar una zona, luego cambiar antes del golpe real
        if (!forceZone && !fintaDone && Random.value < fintaChance)
        {
            fintaDone = true;
            AttackZone playerZone = playerCombat != null ? playerCombat.CurrentZone : AttackZone.Mid;
            // Mostrar la zona del jugador (como si fuéramos a atacar ahí)
            CurrentZone = playerZone;
            yield return new WaitForSeconds(0.30f); // ventana visible para el jugador
            // Cambiar a la zona opuesta — el jugador ya movió su guardia
            CurrentZone = GetCounterZone(playerZone);
        }
        else if (!forceZone)
        {
            // Sin finta: atacar directamente la zona opuesta al jugador
            AttackZone playerZone = playerCombat != null ? playerCombat.CurrentZone : AttackZone.Mid;
            CurrentZone = GetCounterZone(playerZone);
        }

        // Primer golpe
        yield return StartCoroutine(AttackRoutine(forceZone: true, charged: false));

        // Combo: segundo golpe en zona diferente sin pausa de retreat
        if (combo && !isShoved)
        {
            yield return new WaitForSeconds(0.55f); // pausa entre golpes del combo (un ciclo de ataque del player)
            AttackZone playerZone = playerCombat != null ? playerCombat.CurrentZone : AttackZone.Mid;
            CurrentZone = GetCounterZone(playerZone);
            yield return StartCoroutine(AttackRoutine(forceZone: true, charged: false));
        }

        // Sin retreat largo — Predator mantiene la presión
        float shortRetreat = 1.2f;
        float retreatDir = transform.position.x < player.position.x ? -1f : 1f;
        float elapsed = 0f;
        while (elapsed < shortRetreat)
        {
            rb.linearVelocity = new Vector2(retreatDir * predatorMoveSpeed, rb.linearVelocity.y);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (state == AIState.Action) state = AIState.Approach;
    }

    // ───────────────────────────────────────────
    // ELEGIR Y EJECUTAR ACCIÓN
    // ───────────────────────────────────────────
    private IEnumerator ChooseAndExecuteAction(float dist)
    {
        float roll = Random.value;
        float cumulative = 0f;

        cumulative += chanceNormalAttack;
        if (roll < cumulative)
        {
            yield return StartCoroutine(OffensiveAttackRoutine(charged: false));
            yield break;
        }

        cumulative += chanceChargedAttack;
        if (roll < cumulative)
        {
            yield return StartCoroutine(OffensiveAttackRoutine(charged: true));
            yield break;
        }

        cumulative += chanceDodge;
        if (roll < cumulative)
        {
            yield return StartCoroutine(DodgeRoutine());
            yield break;
        }

        cumulative += chanceShove;
        if (roll < cumulative)
        {
            if (dist <= shoveRange)
                yield return StartCoroutine(ShoveRoutine());
            else
                yield return StartCoroutine(OffensiveAttackRoutine(charged: false));
            yield break;
        }

        // Fallback
        yield return StartCoroutine(OffensiveAttackRoutine(charged: false));
    }

    // ───────────────────────────────────────────
    // REACCIÓN DEFENSIVA al ataque del jugador
    // ───────────────────────────────────────────
    private IEnumerator DefensiveReactionRoutine()
    {
        float roll = Random.value;

        // 50% esquivar, 50% contraatacar
        if (roll < 0.5f)
        {
            yield return StartCoroutine(DodgeRoutine());
        }
        else
        {
            yield return new WaitForSeconds(0.15f);
            if (playerCombat != null) CurrentZone = playerCombat.CurrentZone;
            yield return StartCoroutine(AttackRoutine(forceZone: true, charged: false));
        }

        state      = AIState.WaitAndWatch;
        watchTimer = waitForPlayerTime;
    }

    // ───────────────────────────────────────────
    // ATAQUE OFENSIVO (normal o cargado)
    // ───────────────────────────────────────────
    private IEnumerator OffensiveAttackRoutine(bool charged)
    {
        yield return StartCoroutine(AttackRoutine(forceZone: false, charged: charged));

        // Retroceder tras golpear
        state = AIState.Retreat;
        float retreatDir = transform.position.x < player.position.x ? -1f : 1f;
        float elapsed = 0f;
        while (elapsed < retreatDuration)
        {
            rb.linearVelocity = new Vector2(retreatDir * moveSpeed, rb.linearVelocity.y);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        state = AIState.Approach;
    }

    // ───────────────────────────────────────────
    // RUTINA BASE DE ATAQUE
    // ───────────────────────────────────────────
    private IEnumerator AttackRoutine(bool forceZone, bool charged)
    {
        isAttacking  = true;
        hitboxActive = false;
        hitThisAttack.Clear();

        // Elegir zona y AttackData
        if (!forceZone) ChooseZone();
        AttackData data = charged ? GetChargedAttack(CurrentZone) : GetAttackForZone(CurrentZone);

        if (data == null)
        {
            // Fallback al ataque normal si no hay charged asignado
            data = GetAttackForZone(CurrentZone);
            charged = false;
        }
        if (data == null)
        {
            isAttacking = false;
            state = AIState.Approach;
            yield break;
        }

        currentAttack = data;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // Ataque cargado: pequeña pausa de "carga" visual
        if (charged)
            yield return new WaitForSeconds(data.startupFrames * 2f);
        else
            yield return new WaitForSeconds(data.startupFrames);

        // Verificar bloqueo del jugador
        if (playerCombat != null &&
            playerCombat.CurrentZone == CurrentZone &&
            !playerCombat.IsAttacking)
        {
            Debug.Log($"[Block] Jugador bloqueó {CurrentZone}");
            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                float pushDir = player.position.x < transform.position.x ? -1f : 1f;
                playerRb.linearVelocity = Vector2.zero;
                                playerRb.AddForce(new Vector2(pushDir * 5f, 0.5f), ForceMode2D.Impulse);
            }
            isAttacking = false;
            StartCoroutine(BlockStunRoutine());
            yield break;
        }

        // Ataque cargado con dash
        if (charged && Random.value < chargedDashChance)
        {
            float dashDir = player.position.x > transform.position.x ? 1f : -1f;
            yield return StartCoroutine(ChargedDashRoutine(dashDir));
        }

        // Ejecutar hitbox
        hitboxActive = true;
        float activeTime = data.activeFrames;
        float elapsed    = 0f;
        while (elapsed < activeTime)
        {
            CheckHits(charged);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        hitboxActive = false;

        yield return new WaitForSeconds(data.recoveryFrames);
        isAttacking = false;
        if (state == AIState.Action) state = AIState.Approach;
    }

    // ───────────────────────────────────────────
    // DASH DEL ATAQUE CARGADO
    // ───────────────────────────────────────────
    private IEnumerator ChargedDashRoutine(float dashDir)
    {
        float elapsed = 0f;
        while (elapsed < chargedDashDuration)
        {
            rb.linearVelocity = new Vector2(dashDir * chargedDashForce, rb.linearVelocity.y);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        yield return new WaitForSeconds(0.05f);
    }

    // ───────────────────────────────────────────
    // ESQUIVE CON DASH
    // ───────────────────────────────────────────
    private IEnumerator DodgeRoutine()
    {
        isDodging = true;

        // Siempre esquivar alejándose del jugador
        float dodgeDir = transform.position.x < player.position.x ? -1f : 1f;

        float elapsed = 0f;
        while (elapsed < dodgeDashDuration)
        {
            rb.linearVelocity = new Vector2(dodgeDir * dodgeDashForce, rb.linearVelocity.y);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        isDodging = false;
        state = AIState.Approach;
    }

    // ───────────────────────────────────────────
    // SHOVE (EMPUJE AL JUGADOR)
    // ───────────────────────────────────────────
    private IEnumerator ShoveRoutine()
    {
        // Pequeña anticipación
        yield return new WaitForSeconds(0.1f);

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= shoveRange)
        {
            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                float pushDir = player.position.x > transform.position.x ? 1f : -1f;
                playerRb.linearVelocity = Vector2.zero;
                playerRb.AddForce(new Vector2(pushDir * shovePushForce, 0.3f), ForceMode2D.Impulse);
                Debug.Log("[EnemyShove] Enemigo empujó al jugador");
            }
        }

        yield return new WaitForSeconds(0.3f);
        state = AIState.Approach;
    }

    // ───────────────────────────────────────────
    // SHOVE RECIBIDO (del jugador)
    // ───────────────────────────────────────────
    public void ApplyShove(Vector2 force)
    {
        StopAllCoroutines();
        isAttacking  = false;
        hitboxActive = false;
        isDodging    = false;
        hitThisAttack.Clear();
        state = AIState.Idle;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(force, ForceMode2D.Impulse);

        StartCoroutine(ShoveRecovery());
    }

    private IEnumerator BlockStunRoutine()
    {
        isBlockStunned = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        yield return new WaitForSeconds(blockStunDuration);
        isBlockStunned = false;
        if (state == AIState.Action) state = AIState.Approach;
        attackTimer = Random.Range(minAttackDelay * 0.5f, minAttackDelay);
    }

    private IEnumerator ShoveRecovery()
    {
        isShoved = true;
        yield return new WaitForSeconds(0.35f);
        isShoved = false;
        state    = AIState.Approach;
    }

    // ───────────────────────────────────────────
    // DETECCIÓN DE HITS
    // ───────────────────────────────────────────
    private void CheckHits(bool charged = false)
    {
        if (currentAttack == null) return;

        float   dirX   = facingRight ? 1f : -1f;
        Vector2 origin = (Vector2)transform.position
                       + new Vector2(currentAttack.hitboxOffset.x * dirX,
                                     currentAttack.hitboxOffset.y);

        Collider2D[] hits = Physics2D.OverlapBoxAll(origin, currentAttack.hitboxSize, 0f, playerLayer);

        foreach (var col in hits)
        {
            if (hitThisAttack.Contains(col)) continue;

            // FIX: verificar bloqueo DENTRO del loop, contra el collider que recibió
            // el overlap físico real. Esto evita que a distancia corta el hitbox
            // conecte en el mismo frame en que la verificación de zona pasa.
            var targetCombat = col.GetComponent<PlayerCombat>() ?? col.GetComponentInParent<PlayerCombat>();
            if (targetCombat != null &&
                targetCombat.CurrentZone == CurrentZone &&
                !targetCombat.IsAttacking)
            {
                Debug.Log($"[Block] Jugador bloqueó en CheckHits zona={CurrentZone}");
                hitboxActive = false;
                Rigidbody2D playerRb = col.GetComponent<Rigidbody2D>() ?? col.GetComponentInParent<Rigidbody2D>();
                if (playerRb != null)
                {
                    float pushDir = player.position.x < transform.position.x ? -1f : 1f;
                    playerRb.linearVelocity = Vector2.zero;
                    playerRb.AddForce(new Vector2(pushDir * 3f, 0.3f), ForceMode2D.Impulse);
                }
                isAttacking = false;
                StartCoroutine(BlockStunRoutine());
                return;
            }

            hitThisAttack.Add(col);
            var damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                float   dmg    = currentAttack.damage * (charged ? chargedDamageMultiplier : 1f);
                float   kbMult = charged ? chargedKnockbackMultiplier : 1f;
                Vector2 kb     = new Vector2(currentAttack.knockback.x * dirX * kbMult,
                                             currentAttack.knockback.y * kbMult);
                damageable.TakeDamage(dmg, kb, currentAttack.hitStunSeconds, CurrentZone);
                hitboxActive = false;
            }
        }
    }

    // ───────────────────────────────────────────
    // DAÑO RECIBIDO
    // ───────────────────────────────────────────
    public void TakeDamage(float damage, Vector2 knockback, float hitStunDuration, AttackZone zone)
    {
        if (currentHp <= 0f) return;

        currentHp = Mathf.Max(0f, currentHp - damage);
        OnHealthChanged?.Invoke(currentHp, maxHp);

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockback, ForceMode2D.Impulse);

        if (personality == AIPersonality.Defensive && !isAttacking)
            playerJustAttacked = true;

        if (sr != null) StartCoroutine(FlashRoutine());
        if (currentHp <= 0f) OnDeath();
    }

    // ───────────────────────────────────────────
    // HELPERS
    // ───────────────────────────────────────────
    private void ChooseZone()
    {
        float roll = Random.value;
        if      (roll < 0.25f) CurrentZone = AttackZone.High;
        else if (roll < 0.75f) CurrentZone = AttackZone.Mid;
        else                   CurrentZone = AttackZone.Low;
    }

    private AttackData GetAttackForZone(AttackZone zone) => zone switch
    {
        AttackZone.High => attackHigh,
        AttackZone.Low  => attackLow,
        _               => attackMid,
    };

    private AttackData GetChargedAttack(AttackZone zone) => zone switch
    {
        AttackZone.High => chargedHigh != null ? chargedHigh : attackHigh,
        AttackZone.Low  => chargedLow  != null ? chargedLow  : attackLow,
        _               => chargedMid  != null ? chargedMid  : attackMid,
    };

    public void CancelHitbox()
    {
        hitboxActive = false;
        hitThisAttack.Clear();
    }

    public void InterruptAttack()
    {
        StopAllCoroutines();
        isAttacking  = false;
        hitboxActive = false;
        hitThisAttack.Clear();
        state = AIState.Approach;
    }

    private void FacePlayer()
    {
        if (player == null) return;
        bool shouldFaceRight = player.position.x > transform.position.x;
        if (shouldFaceRight == facingRight) return;
        facingRight = shouldFaceRight;
        Vector3 scale = transform.localScale;
        scale.x = facingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    private IEnumerator FlashRoutine()
    {
        if (sr == null) yield break;
        sr.color = hitColor;
        yield return new WaitForSeconds(flashTime);
        sr.color = originalColor;
    }

    private void OnDeath()
    {
        StopAllCoroutines();
        isAttacking  = false;
        hitboxActive = false;
        state        = AIState.Idle;
        Destroy(gameObject, 0.3f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, approachRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, shoveRange);
    }
}

public enum AIPersonality { Offensive, Defensive, Predator }
