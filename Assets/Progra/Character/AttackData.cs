using UnityEngine;

[CreateAssetMenu(fileName = "NewAttack", menuName = "Combat/AttackData")]
public class AttackData : ScriptableObject
{
    [Header("Identificación")]
    public string attackName = "Attack";
    public AttackZone zone;          // HIGH / MID / LOW

    [Header("Daño & Stun")]
    public float damage         = 20f;
    public float hitStunSeconds = 0.3f;  // tiempo que el enemigo queda en stun

    [Header("Timing (segundos)")]
    public float startupFrames  = 0.08f;  // antes de que el hitbox aparezca
    public float activeFrames   = 0.12f;  // hitbox activo
    public float recoveryFrames = 0.25f;  // después del golpe, no puede atacar

    [Header("Hitbox (local space relativo al personaje)")]
    public Vector2 hitboxOffset = new Vector2(0.6f, 0f);
    public Vector2 hitboxSize   = new Vector2(0.8f, 0.4f);

    [Header("Lanzamiento")]
    public Vector2 knockback = new Vector2(3f, 1f);  // fuerza que recibe el enemigo

    [Header("Animación")]
    [Tooltip("Nombre del trigger en el Animator")]
    public string animatorTrigger = "Attack_Mid";

    // Duración total del ataque
    public float TotalDuration => startupFrames + activeFrames + recoveryFrames;
}

public enum AttackZone
{
    High,   // Mouse por encima del umbral superior
    Mid,    // Mouse entre umbral superior e inferior
    Low     // Mouse por debajo del umbral inferior
}
