using UnityEngine;

public class SwordVisual : MonoBehaviour
{
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Transform swordPivot; // el SwordPivot que rota

    [Header("Rotaciones por zona (eje Z)")]
    [SerializeField] private float rotationHigh = 45f;   // espada arriba
    [SerializeField] private float rotationMid  = 0f;    // espada horizontal
    [SerializeField] private float rotationLow  = -45f;  // espada abajo

    [Header("Velocidad de transición")]
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Colores por zona")]
    [SerializeField] private Color colorHigh     = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color colorMid      = new Color(0.9f, 0.9f, 0.9f);
    [SerializeField] private Color colorLow      = new Color(1f, 0.8f, 0.1f);
    [SerializeField] private Color colorAttacking = new Color(1f, 0.3f, 0.3f);

    private float targetRotation;

    private void Update()
    {
        if (playerCombat == null) return;

        // --- Color ---
        if (sr != null)
        {
            sr.color = playerCombat.IsAttacking
                ? colorAttacking
                : playerCombat.CurrentZone switch
                {
                    AttackZone.High => colorHigh,
                    AttackZone.Low  => colorLow,
                    _               => colorMid,
                };
        }

        // --- Rotación objetivo según zona ---
        if (!playerCombat.IsAttacking)
        {
            targetRotation = playerCombat.CurrentZone switch
            {
                AttackZone.High => rotationHigh,
                AttackZone.Low  => rotationLow,
                _               => rotationMid,
            };
        }

        // --- Interpolar suavemente hacia la rotación objetivo ---
        if (swordPivot != null)
        {
            float current = swordPivot.localEulerAngles.z;
            // Convertir a rango -180/180 para que Lerp funcione bien
            if (current > 180f) current -= 360f;

            float next = Mathf.Lerp(current, targetRotation, Time.deltaTime * rotationSpeed);
            swordPivot.localEulerAngles = new Vector3(0f, 0f, next);
        }
    }
}