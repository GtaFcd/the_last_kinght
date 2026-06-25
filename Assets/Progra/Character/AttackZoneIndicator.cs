using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AttackZoneIndicator : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private TMP_Text zoneLabel;
    [SerializeField] private Image    highIndicator;
    [SerializeField] private Image    midIndicator;
    [SerializeField] private Image    lowIndicator;

    [Header("Colores")]
    [SerializeField] private Color activeColor   = new Color(1f, 0.8f, 0f);  // dorado
    [SerializeField] private Color inactiveColor = new Color(1f, 1f, 1f, 0.3f);

    [Header("Jugador")]
    [SerializeField] private PlayerCombat playerCombat;

    private void Update()
    {
        if (playerCombat == null) return;

        AttackZone zone = playerCombat.CurrentZone;

        // Label
        if (zoneLabel != null)
            zoneLabel.text = zone.ToString().ToUpper();

        // Indicators
        SetIndicatorColor(highIndicator, zone == AttackZone.High);
        SetIndicatorColor(midIndicator,  zone == AttackZone.Mid);
        SetIndicatorColor(lowIndicator,  zone == AttackZone.Low);
    }

    private void SetIndicatorColor(Image img, bool active)
    {
        if (img == null) return;
        img.color = active ? activeColor : inactiveColor;
    }
}
