using UnityEngine;

public interface IDamageable
{
    /// <param name="damage">Puntos de daño</param>
    /// <param name="knockback">Impulso que se aplica al Rigidbody2D</param>
    /// <param name="hitStunDuration">Segundos en que el personaje no puede actuar</param>
    /// <param name="zone">Zona desde la que viene el golpe (High/Mid/Low)</param>
    void TakeDamage(float damage, Vector2 knockback, float hitStunDuration, AttackZone zone);
}