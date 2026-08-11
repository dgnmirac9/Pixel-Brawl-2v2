using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Image fillImage; // HealthBar_Fill görseli

    // Can değerini 0.0 ile 1.0 arasında bir orana dönüştürüp barı günceller
    public void UpdateHealthBar(
        int currentHealth,
        int maxHealth)
    {
        if (fillImage == null)
            return;

        if (maxHealth <= 0)
            return;

        float healthRatio =
            (float)currentHealth / maxHealth;

        fillImage.fillAmount =
            Mathf.Clamp01(healthRatio);
    }
}