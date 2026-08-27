using UnityEngine;
using UnityEngine.UI;

public class StaminaBarUI : MonoBehaviour
{
    [SerializeField]
    private Image fillImage;
    
    public void UpdateStamina(
        float currentStamina,
        float maxStamina)
    {
        if (fillImage == null)
            return;

        float normalizedStamina =
            maxStamina > 0f
                ? currentStamina / maxStamina
                : 0f;

        fillImage.fillAmount =
            Mathf.Clamp01(
                normalizedStamina
            );
    }
}