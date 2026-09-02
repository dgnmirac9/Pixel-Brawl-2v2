using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Image fillImage;

    [Header("Player Colors")]
    [SerializeField]
    private Color localPlayerColor =
        new Color32(105, 169, 71, 255);

    [SerializeField]
    private Color opponentColor =
        new Color32(216, 85, 78, 255);

    public void UpdateHealthBar(
        int currentHealth,
        int maxHealth)
    {
        if (fillImage == null)
            return;

        if (maxHealth <= 0)
        {
            fillImage.fillAmount = 0f;
            return;
        }

        fillImage.fillAmount =
            Mathf.Clamp01(
                (float)currentHealth /
                maxHealth
            );
    }

    public void SetPlayerRelationship(
        bool isLocalPlayer)
    {
        if (fillImage == null)
            return;

        fillImage.color =
            isLocalPlayer
                ? localPlayerColor
                : opponentColor;
    }
}