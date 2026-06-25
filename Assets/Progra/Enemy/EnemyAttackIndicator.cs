using UnityEngine;
using TMPro;

public class EnemyAttackIndicator : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private EnemyAI enemyAI;
    [SerializeField] private TMP_Text attackLabel;

    [Header("Colores por zona")]
    [SerializeField] private Color colorHigh = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color colorMid  = new Color(0.2f, 1f, 0.4f);
    [SerializeField] private Color colorLow  = new Color(1f, 0.8f, 0.1f);

    private void Update()
    {
        if (enemyAI == null || attackLabel == null) return;

        if (enemyAI.IsAttacking)
        {
            attackLabel.gameObject.SetActive(true);

            switch (enemyAI.CurrentZone)
            {
                case AttackZone.High:
                    attackLabel.text  = "HIGH";
                    attackLabel.color = colorHigh;
                    break;
                case AttackZone.Mid:
                    attackLabel.text  = "MID";
                    attackLabel.color = colorMid;
                    break;
                case AttackZone.Low:
                    attackLabel.text  = "LOW";
                    attackLabel.color = colorLow;
                    break;
            }
        }
        else
        {
            attackLabel.gameObject.SetActive(false);
        }
    }
}