using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Vida")]
    [SerializeField] private float maxHp = 100f;

    [Header("Invencibilidad tras recibir daño")]
    [SerializeField] private float iFramesDuration = 0.8f;

    [Header("Sangre")]
    [SerializeField] private ParticleSystem bloodParticles;

    public float CurrentHp => currentHp;
    public float MaxHp     => maxHp;
    public bool  IsDead    => currentHp <= 0f;

    public System.Action<float, float> OnHealthChanged;
    public System.Action OnDeath;

    private float currentHp;
    private bool  isInvincible;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        currentHp = maxHp;
    }

    public void SetInvincible(bool value) => isInvincible = value;

    public void TakeDamage(float damage, Vector2 knockback, float hitStunDuration, AttackZone zone)
    {
    Debug.Log($"[{gameObject.name}] TakeDamage llamado | invincible:{isInvincible} | dead:{IsDead}");
    if (isInvincible || IsDead) return;

    currentHp = Mathf.Max(0f, currentHp - damage);
    GetComponent<CombatAudio>()?.PlayHitReceived();
    // Debug.Log($"[{gameObject.name}] HP: {currentHp:F0} | Golpe zona: {zone} | bloodParticles: {(bloodParticles != null ? bloodParticles.name : "NULL")}");

    if (bloodParticles != null)
    {
        float hitDirX = knockback.x > 0 ? -1f : 1f;
        float hitDirY = zone switch
        {
            AttackZone.High => -1f,
            AttackZone.Low  =>  1f,
            _               =>  0f,
        };

        if (zone == AttackZone.Mid)
            hitDirX = rb.linearVelocity.x >= 0 ? 1f : -1f;

        var shape = bloodParticles.shape;
        shape.rotation = new Vector3(0f, 0f,
            Mathf.Atan2(hitDirY, hitDirX) * Mathf.Rad2Deg);

        Debug.Log($"[{gameObject.name}] Llamando bloodParticles.Play() | isPlaying antes: {bloodParticles.isPlaying} | particleCount: {bloodParticles.particleCount}");
        bloodParticles.Play();
        Debug.Log($"[{gameObject.name}] Después de Play() | isPlaying: {bloodParticles.isPlaying} | particleCount: {bloodParticles.particleCount}");
    }
    else
    {
        Debug.LogWarning($"[{gameObject.name}] bloodParticles es NULL — asígnalo en el Inspector");
    }

    OnHealthChanged?.Invoke(currentHp, maxHp);

    rb.linearVelocity = Vector2.zero;
    rb.AddForce(knockback, ForceMode2D.Impulse);

    if (IsDead)
    {
        GetComponent<Animator>()?.SetTrigger("Death");
        // Desactivar física para que no se mueva
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        GetComponent<Rigidbody2D>().gravityScale = 0f;
        GetComponent<Collider2D>().enabled = false;
        var ctrl = GetComponent<PlayerController>();
        if (ctrl != null) ctrl.OverrideMovement = true;
        OnDeath?.Invoke();
        Debug.Log("[Player] Muerto.");
        return;
    }

    StopAllCoroutines();
    StartCoroutine(IFramesRoutine());
}

    private IEnumerator IFramesRoutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(iFramesDuration);
        isInvincible = false;
    }

    public void ResetHealth()
    {
        currentHp    = maxHp;
        isInvincible = false;
        OnHealthChanged?.Invoke(currentHp, maxHp);
    }
}
