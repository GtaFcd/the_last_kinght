using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image  fillImage;
    [SerializeField] private TMP_Text hpLabel;

    [Header("Colores")]
    [SerializeField] private Color colorFull    = new Color(0.2f, 0.85f, 0.3f);
    [SerializeField] private Color colorMid     = new Color(1f,   0.75f, 0f);
    [SerializeField] private Color colorLow     = new Color(0.9f, 0.15f, 0.15f);

    [Header("Conectar a")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private EnemyAI      enemyAI;

    private void Start()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateBar;
            UpdateBar(playerHealth.CurrentHp, playerHealth.MaxHp);
        }

        if (enemyAI != null)
        {
            enemyAI.OnHealthChanged += UpdateBar;
            UpdateBar(enemyAI.CurrentHp, enemyAI.MaxHp);
        }
    }

    private void UpdateBar(float current, float max)
    {
        float ratio = max > 0f ? current / max : 0f;

        if (fillImage != null)
        {
            fillImage.fillAmount = ratio;
            fillImage.color = ratio > 0.5f ? colorFull
                            : ratio > 0.25f ? colorMid
                            : colorLow;
        }

        if (hpLabel != null)
            hpLabel.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }
}