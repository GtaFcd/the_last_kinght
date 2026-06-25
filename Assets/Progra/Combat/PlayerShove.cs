using UnityEngine;

public class PlayerShove : MonoBehaviour
{
    [Header("Empuje")]
    [SerializeField] private float shoveRange     = 1.5f;
    [SerializeField] private float shovePushForce = 8f;
    [SerializeField] private float shoveUpForce   = 0.3f;
    [SerializeField] private float cooldown       = 0.5f;

    [Header("Feedback visual (opcional)")]
    [SerializeField] private GameObject shoveVFXPrefab;

    [Header("Refs")]
    [SerializeField] private PlayerCombat     playerCombat;
    [SerializeField] private PlayerController playerController;
    [Tooltip("El otro jugador o el enemigo — cualquier GameObject con Rigidbody2D")]
    [SerializeField] private GameObject       shoveTarget;

    private float             cooldownTimer;
    private PlayerInputConfig inputConfig;

    private void Awake()
    {
        if (playerCombat     == null) playerCombat     = GetComponent<PlayerCombat>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
    }

    private void Start()
    {
        inputConfig = playerController?.InputConfig;
    }

    private void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        if (playerCombat != null && playerCombat.IsAttacking) return;
        if (cooldownTimer > 0f)  return;
        if (inputConfig   == null) return;
        if (shoveTarget   == null) return;

        if (inputConfig.GetShoveDown())
            TryShove();
    }

    private void TryShove()
    {
        float dist = Vector2.Distance(transform.position, shoveTarget.transform.position);
        if (dist > shoveRange) return;

        float   dirX  = shoveTarget.transform.position.x > transform.position.x ? 1f : -1f;
        Vector2 force = new Vector2(dirX * shovePushForce, shoveUpForce);

        // Intentar EnemyAI primero, luego Rigidbody2D genérico
        var enemyAI = shoveTarget.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.ApplyShove(force);
        }
        else
        {
            var rb = shoveTarget.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.AddForce(force, ForceMode2D.Impulse);
            }
        }

        cooldownTimer = cooldown;

        if (shoveVFXPrefab != null)
        {
            Vector3 mid = (transform.position + shoveTarget.transform.position) * 0.5f;
            Instantiate(shoveVFXPrefab, mid, Quaternion.identity);
        }

        Debug.Log($"[Shove] {gameObject.name} empujó a {shoveTarget.name}");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, shoveRange);
    }
}