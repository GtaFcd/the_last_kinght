using UnityEngine;

public class EnemySwordVisual : MonoBehaviour
{
    [SerializeField] private EnemyAI enemyAI;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Transform swordPivot;

    [Header("Rotaciones por zona (eje Z)")]
    [SerializeField] private float rotationHigh = 45f;
    [SerializeField] private float rotationMid  = 0f;
    [SerializeField] private float rotationLow  = -45f;

    [Header("Velocidad de transición")]
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Colores por zona")]
    [SerializeField] private Color colorHigh      = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color colorMid       = new Color(0.9f, 0.9f, 0.9f);
    [SerializeField] private Color colorLow       = new Color(1f, 0.8f, 0.1f);
    [SerializeField] private Color colorAttacking = new Color(1f, 0.3f, 0.3f);

    private float targetRotation;

    private void Update()
    {
        if (enemyAI == null) return;

        // --- Color ---
        if (sr != null)
        {
            sr.color = enemyAI.IsAttacking
                ? colorAttacking
                : enemyAI.CurrentZone switch
                {
                    AttackZone.High => colorHigh,
                    AttackZone.Low  => colorLow,
                    _               => colorMid,
                };
        }

        // Solo actualizar la rotación objetivo si NO está atacando
        if (!enemyAI.IsAttacking)
        {
            targetRotation = enemyAI.CurrentZone switch
            {
                AttackZone.High => rotationHigh,
                AttackZone.Low  => rotationLow,
                _               => rotationMid,
            };
        }
        // --- Lerp suave ---
        if (swordPivot != null)
        {
            float current = swordPivot.localEulerAngles.z;
            if (current > 180f) current -= 360f;

            float next = Mathf.Lerp(current, targetRotation, Time.deltaTime * rotationSpeed);
            swordPivot.localEulerAngles = new Vector3(0f, 0f, next);
        }
    }
}