using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CrosshairUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private RectTransform crosshairDot;
    [SerializeField] private TMP_Text zoneText;

    [Header("Colores por zona")]
    [SerializeField] private Color colorHigh = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color colorMid  = new Color(0.2f, 1f, 0.4f);
    [SerializeField] private Color colorLow  = new Color(1f, 0.8f, 0.1f);

    private Image dotImage;

    private void Awake()
    {
        dotImage = crosshairDot.GetComponent<Image>();
        Cursor.visible = false;
    }

    private void Update()
    {
        // Validar que la posición del mouse sea finita antes de asignar
        Vector3 mousePos = Input.mousePosition;
        if (!float.IsInfinity(mousePos.x) && !float.IsInfinity(mousePos.y))
            crosshairDot.position = mousePos;

        if (playerCombat == null) return;

        AttackZone zone = playerCombat.CurrentZone;
        Color c = zone switch
        {
            AttackZone.High => colorHigh,
            AttackZone.Low  => colorLow,
            _               => colorMid,
        };

        if (dotImage != null) dotImage.color = c;

        if (zoneText != null)
        {
            zoneText.text  = zone.ToString().ToUpper();
            zoneText.color = c;
        }
    }

    private void OnDestroy()
    {
        Cursor.visible = true;
    }
}