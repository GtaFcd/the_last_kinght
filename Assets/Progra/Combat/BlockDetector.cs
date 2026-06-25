using UnityEngine;

public class BlockDetector : MonoBehaviour
{
    [Header("Modo")]
    [SerializeField] private bool pvpMode = false;

    [Header("Referencias vs IA")]
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private EnemyAI enemyAI;
    [SerializeField] private PlayerDodge playerDodge;

    [Header("Referencias vs J2")]
    [SerializeField] private PlayerCombat player2Combat;
    [SerializeField] private PlayerDodge player2Dodge;

    [Header("Choque de espadas")]
    [SerializeField] private float clashWindow = 0.15f;
    [SerializeField] private float clashPushForce = 4f;
    [SerializeField] private GameObject clashVFXPrefab;

    [Header("Bloqueo")]
    [SerializeField] private float blockPushForce = 1.5f;
    [SerializeField] private GameObject blockVFXPrefab;
    [SerializeField] private GameObject blockEffectPrefab;

    [Header("SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clipClash;
    [SerializeField] private AudioClip clipBlock;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    private Rigidbody2D playerRb;
    private Rigidbody2D enemyRb;

    private float p1AttackTime = -99f;
    private float p2AttackTime = -99f;
    private bool p1WasAttacking;
    private bool p2WasAttacking;

    private void Awake()
    {
        if (playerCombat != null) playerRb = playerCombat.GetComponent<Rigidbody2D>();

        if (pvpMode)
        {
            if (player2Combat != null) enemyRb = player2Combat.GetComponent<Rigidbody2D>();
        }
        else
        {
            if (enemyAI != null) enemyRb = enemyAI.GetComponent<Rigidbody2D>();
        }
    }

    private void Update()
    {
        bool p1Attacking = playerCombat != null && playerCombat.IsAttacking;
        if (p1Attacking && !p1WasAttacking) p1AttackTime = Time.time;
        p1WasAttacking = p1Attacking;

        bool p2Attacking = pvpMode
            ? (player2Combat != null && player2Combat.IsAttacking)
            : (enemyAI != null && enemyAI.IsAttacking);

        if (p2Attacking && !p2WasAttacking) p2AttackTime = Time.time;
        p2WasAttacking = p2Attacking;
    }

    private void FixedUpdate()
    {
        if (pvpMode) FixedUpdatePvP();
        else FixedUpdateVsAI();
    }

private void FixedUpdatePvP()
{
    if (playerCombat == null || player2Combat == null) return;

    bool p1HitboxActive = playerCombat.HitboxActive;
    bool p2HitboxActive = player2Combat.HitboxActive;

    // Choque de espadas: primero, porque ambos están atacando
    if (p1HitboxActive && p2HitboxActive)
    {
        float timeDiff = Mathf.Abs(p1AttackTime - p2AttackTime);
        bool zonaIgual = playerCombat.CurrentZone == player2Combat.CurrentZone;
        bool overlap = HitboxesOverlapPvP();
        Debug.Log("[BD] AMBOS ATACAN | timeDiff=" + timeDiff.ToString("F3") + " clashWindow=" + clashWindow + " zonaIgual=" + zonaIgual + " overlap=" + overlap + " p1Zone=" + playerCombat.CurrentZone + " p2Zone=" + player2Combat.CurrentZone);

        if (timeDiff <= clashWindow && zonaIgual && overlap)
        {
            Debug.Log("[BD] ClashPvP EJECUTADO");
            ClashPvP();
            return;
        }
        else
        {
            Debug.Log("[BD] Clash NO ejecutado | razon: timeDiff=" + (timeDiff <= clashWindow) + " zona=" + zonaIgual + " overlap=" + overlap);
        }
    }

    // Player 1 ataca, Player 2 bloquea o esquiva
    if (p1HitboxActive && !p2HitboxActive)
    {
        if (player2Dodge != null && player2Dodge.IsDodging)
        {
            if (DodgeBeats(player2Combat.CurrentZone, playerCombat.CurrentZone) &&
                HitboxReachesTarget(playerCombat, player2Combat))
            {
                playerCombat.CancelHitbox();
                SpawnVFX(blockVFXPrefab, player2Combat.transform.position, playerCombat.transform.position, clipBlock);
                SpawnBlockEffect(player2Combat.transform.position, playerCombat.transform.position);
                return;
            }
        }

        if (playerCombat.CurrentZone == player2Combat.CurrentZone &&
            HitboxReachesTarget(playerCombat, player2Combat))
        {
            BlockAttack(
                attacker: playerCombat,
                attackerRb: playerRb,
                defenderPos: player2Combat.transform.position,
                attackerPos: playerCombat.transform.position
            );

            SpawnBlockEffect(player2Combat.transform.position, playerCombat.transform.position);
            return;
        }
    }

    // Player 2 ataca, Player 1 bloquea o esquiva
    if (p2HitboxActive && !p1HitboxActive)
    {
        if (playerDodge != null && playerDodge.IsDodging)
        {
            if (DodgeBeats(playerCombat.CurrentZone, player2Combat.CurrentZone) &&
                HitboxReachesTarget(player2Combat, playerCombat))
            {
                player2Combat.CancelHitbox();
                SpawnVFX(blockVFXPrefab, playerCombat.transform.position, player2Combat.transform.position, clipBlock);
                SpawnBlockEffect(playerCombat.transform.position, player2Combat.transform.position);
                return;
            }
        }

        if (player2Combat.CurrentZone == playerCombat.CurrentZone &&
            HitboxReachesTarget(player2Combat, playerCombat))
        {
            BlockAttack(
                attacker: player2Combat,
                attackerRb: enemyRb,
                defenderPos: playerCombat.transform.position,
                attackerPos: player2Combat.transform.position
            );

            SpawnBlockEffect(playerCombat.transform.position, player2Combat.transform.position);
            return;
        }
    }
}

    private void FixedUpdateVsAI()
    {
        if (playerCombat == null || enemyAI == null) return;

        if (playerCombat.HitboxActive && enemyAI.HitboxActive)
        {
            float timeDiff = Mathf.Abs(p1AttackTime - p2AttackTime);

            if (timeDiff <= clashWindow &&
                playerCombat.CurrentZone == enemyAI.CurrentZone &&
                HitboxesOverlapVsAI())
            {
                OnClashVsAI();
                return;
            }
        }

        if (enemyAI.HitboxActive && !playerCombat.IsAttacking)
        {
            if (playerDodge != null && playerDodge.IsDodging)
            {
                if (DodgeBeats(playerCombat.CurrentZone, enemyAI.CurrentZone))
                {
                    enemyAI.CancelHitbox();
                    SpawnVFX(blockVFXPrefab, playerCombat.transform.position, enemyAI.transform.position, clipBlock);
                    SpawnBlockEffect(playerCombat.transform.position, enemyAI.transform.position);
                    return;
                }
            }

            if (playerCombat.CurrentZone == enemyAI.CurrentZone && EnemyHitboxReachesPlayer())
            {
                OnPlayerBlock();
                return;
            }
        }

        if (playerCombat.HitboxActive && !enemyAI.IsAttacking)
        {
            if (playerCombat.CurrentZone == enemyAI.CurrentZone && HitboxesOverlapVsAI())
            {
                OnEnemyBlockVsAI();
                return;
            }
        }
    }

    private void BlockAttack(PlayerCombat attacker, Rigidbody2D attackerRb, Vector3 defenderPos, Vector3 attackerPos)
    {
        attacker.CancelHitbox();

        if (attackerRb != null)
        {
            float dir = attackerPos.x < defenderPos.x ? -1f : 1f;
            attackerRb.linearVelocity = Vector2.zero;
            attackerRb.AddForce(new Vector2(dir * blockPushForce, 0.3f), ForceMode2D.Impulse);
        }

        SpawnVFX(blockVFXPrefab, defenderPos, attackerPos, clipBlock);
    }

    private void ClashPvP()
    {
        playerCombat.CancelHitbox();
        player2Combat.CancelHitbox();

        if (playerRb != null)
        {
            float dir = playerCombat.transform.position.x < player2Combat.transform.position.x ? -1f : 1f;
            playerRb.AddForce(new Vector2(dir * clashPushForce, 0.5f), ForceMode2D.Impulse);
        }

        if (enemyRb != null)
        {
            float dir = player2Combat.transform.position.x < playerCombat.transform.position.x ? -1f : 1f;
            enemyRb.AddForce(new Vector2(dir * clashPushForce, 0.5f), ForceMode2D.Impulse);
        }

        SpawnVFX(clashVFXPrefab, playerCombat.transform.position, player2Combat.transform.position, clipClash);
    }

    private void OnEnemyBlockVsAI()
    {
        playerCombat.CancelHitbox();

        if (playerRb != null)
        {
            float dir = playerCombat.transform.position.x < enemyAI.transform.position.x ? -1f : 1f;
            playerRb.linearVelocity = Vector2.zero;
            playerRb.AddForce(new Vector2(dir * blockPushForce, 0.3f), ForceMode2D.Impulse);
        }

        SpawnVFX(blockVFXPrefab, playerCombat.transform.position, enemyAI.transform.position, clipBlock);
        SpawnBlockEffect(playerCombat.transform.position, enemyAI.transform.position);
    }

    public void OnPlayerBlock()
    {
        if (enemyAI == null || playerCombat == null) return;

        enemyAI.CancelHitbox();

        if (enemyRb != null)
        {
            float dir = enemyAI.transform.position.x > playerCombat.transform.position.x ? 1f : -1f;
            enemyRb.linearVelocity = Vector2.zero;
            enemyRb.AddForce(new Vector2(dir * blockPushForce, 0.3f), ForceMode2D.Impulse);
        }

        SpawnVFX(blockVFXPrefab, playerCombat.transform.position, enemyAI.transform.position, clipBlock);
        SpawnBlockEffect(playerCombat.transform.position, enemyAI.transform.position);
    }

    private void OnClashVsAI()
    {
        playerCombat.CancelHitbox();
        enemyAI.CancelHitbox();

        if (playerRb != null)
        {
            float dir = playerCombat.transform.position.x < enemyAI.transform.position.x ? -1f : 1f;
            playerRb.AddForce(new Vector2(dir * clashPushForce, 0.5f), ForceMode2D.Impulse);
        }

        if (enemyRb != null)
        {
            float dir = enemyAI.transform.position.x < playerCombat.transform.position.x ? -1f : 1f;
            enemyRb.AddForce(new Vector2(dir * clashPushForce, 0.5f), ForceMode2D.Impulse);
        }

        SpawnVFX(clashVFXPrefab, playerCombat.transform.position, enemyAI.transform.position, clipClash);
    }

    private void SpawnVFX(GameObject prefab, Vector3 posA, Vector3 posB, AudioClip clip = null)
    {
        if (prefab != null)
        {
            Vector3 spawnPos = (posA + posB) * 0.5f;
            spawnPos.z = 0f;

            GameObject effect = Instantiate(prefab, spawnPos, Quaternion.identity);

            ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>(true);
            float longestDuration = 1f;

            foreach (ParticleSystem ps in particles)
            {
                ps.gameObject.SetActive(true);
                ps.Clear(true);
                ps.Play(true);

                float duration = ps.main.duration + ps.main.startLifetime.constantMax;
                if (duration > longestDuration)
                    longestDuration = duration;
            }

            Destroy(effect, longestDuration + 0.2f);
        }

        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip, sfxVolume);
    }

    private void SpawnBlockEffect(Vector3 posA, Vector3 posB)
    {
        if (blockEffectPrefab == null)
        {
            Debug.LogWarning("[BlockEffect] Falta asignar blockEffectPrefab en el Inspector.");
            return;
        }

        Vector3 spawnPos = (posA + posB) * 0.5f;
        spawnPos.z = 0f;

        GameObject effect = Instantiate(blockEffectPrefab, spawnPos, Quaternion.identity);

        ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem ps in particles)
        {
            ps.gameObject.SetActive(true);
            ps.Clear(true);
            ps.Play(true);
        }

        Destroy(effect, 2f);
    }

    private bool DodgeBeats(AttackZone dodgeZone, AttackZone attackZone)
    {
        return dodgeZone switch
        {
            AttackZone.High => attackZone == AttackZone.High || attackZone == AttackZone.Low,
            AttackZone.Mid => attackZone == AttackZone.Mid,
            AttackZone.Low => attackZone == AttackZone.Mid,
            _ => false,
        };
    }

    private bool HitboxReachesTarget(PlayerCombat attacker, PlayerCombat defender)
    {
        if (attacker == null || defender == null) return false;

        AttackData atk = attacker.CurrentAttackData;

        if (atk == null)
        {
            Debug.LogWarning($"[HitboxReachesTarget] {attacker.name} no tiene CurrentAttackData.");
            return false;
        }

        float dirX = defender.transform.position.x >= attacker.transform.position.x ? 1f : -1f;

        Vector2 center = (Vector2)attacker.transform.position
                       + new Vector2(Mathf.Abs(atk.hitboxOffset.x) * dirX, atk.hitboxOffset.y);

        Rect attackRect = new Rect(center - atk.hitboxSize * 0.5f, atk.hitboxSize);

        Collider2D defenderCol = defender.GetComponent<Collider2D>();

        if (defenderCol != null)
        {
            Bounds b = defenderCol.bounds;
            Rect defenderRect = new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
            return attackRect.Overlaps(defenderRect);
        }

        return attackRect.Contains((Vector2)defender.transform.position);
    }

private bool HitboxesOverlapPvP()
{
    AttackData p1Atk = playerCombat.CurrentAttackData;
    AttackData p2Atk = player2Combat.CurrentAttackData;

    if (p1Atk == null || p2Atk == null) return true;

    float p1DirX = player2Combat.transform.position.x >= playerCombat.transform.position.x ? 1f : -1f;
    float p2DirX = playerCombat.transform.position.x >= player2Combat.transform.position.x ? 1f : -1f;

    Vector2 p1Center = (Vector2)playerCombat.transform.position
                     + new Vector2(Mathf.Abs(p1Atk.hitboxOffset.x) * p1DirX, p1Atk.hitboxOffset.y);

    Vector2 p2Center = (Vector2)player2Combat.transform.position
                     + new Vector2(Mathf.Abs(p2Atk.hitboxOffset.x) * p2DirX, p2Atk.hitboxOffset.y);

    Rect p1Rect = new Rect(p1Center - p1Atk.hitboxSize * 0.5f, p1Atk.hitboxSize);
    Rect p2Rect = new Rect(p2Center - p2Atk.hitboxSize * 0.5f, p2Atk.hitboxSize);

    return p1Rect.Overlaps(p2Rect);
}
    private bool EnemyHitboxReachesPlayer()
    {
        AttackData eAtk = enemyAI.CurrentAttack;
        if (eAtk == null) return false;

        float eDirX = enemyAI.transform.localScale.x > 0f ? 1f : -1f;

        Vector2 eCenter = (Vector2)enemyAI.transform.position
                        + new Vector2(eAtk.hitboxOffset.x * eDirX, eAtk.hitboxOffset.y);

        Rect eRect = new Rect(eCenter - eAtk.hitboxSize * 0.5f, eAtk.hitboxSize);

        Collider2D playerCol = playerCombat.GetComponent<Collider2D>();

        if (playerCol != null)
        {
            Bounds b = playerCol.bounds;
            Rect pRect = new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
            return eRect.Overlaps(pRect);
        }

        return eRect.Contains((Vector2)playerCombat.transform.position);
    }

    private bool HitboxesOverlapVsAI()
    {
        PlayerController controller = playerCombat.GetComponent<PlayerController>();
        if (controller == null) return false;

        bool pFacing = controller.FacingRight;

        AttackData pAtk = playerCombat.CurrentAttackData;
        if (pAtk == null) return false;

        Vector2 pCenter = (Vector2)playerCombat.transform.position
                        + new Vector2(pAtk.hitboxOffset.x * (pFacing ? 1f : -1f), pAtk.hitboxOffset.y);

        AttackData eAtk = enemyAI.CurrentAttack;
        if (eAtk == null) return false;

        float eDirX = enemyAI.transform.localScale.x > 0f ? 1f : -1f;

        Vector2 eCenter = (Vector2)enemyAI.transform.position
                        + new Vector2(eAtk.hitboxOffset.x * eDirX, eAtk.hitboxOffset.y);

        Rect pRect = new Rect(pCenter - pAtk.hitboxSize * 0.5f, pAtk.hitboxSize);
        Rect eRect = new Rect(eCenter - eAtk.hitboxSize * 0.5f, eAtk.hitboxSize);

        return pRect.Overlaps(eRect);
    }
}
