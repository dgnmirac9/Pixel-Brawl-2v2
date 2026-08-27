using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class ItemTooltipUI : MonoBehaviour
{
    [Header("Visuals")] [SerializeField] private Image itemIcon;

    [SerializeField] private Image rarityAccent;

    [Header("Texts")] [SerializeField] private TMP_Text itemNameText;

    [SerializeField] private TMP_Text rarityText;

    [SerializeField] private TMP_Text statsText;

    [SerializeField] private TMP_Text descriptionText;

    [Header("Animation")] [SerializeField, Min(0.01f)]
    private float fadeDuration = 0.12f;

    private CanvasGroup canvasGroup;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        canvasGroup =
            GetComponent<CanvasGroup>();

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        HideImmediate();
    }

    public void Show(ItemDefinition item)
    {
        if (item == null)
            return;

        gameObject.SetActive(true);

        if (itemIcon != null)
        {
            itemIcon.sprite = item.Icon;
            itemIcon.enabled =
                item.Icon != null;

            itemIcon.preserveAspect = true;
        }

        if (itemNameText != null)
        {
            itemNameText.text =
                item.DisplayName;
        }

        Color rarityColor =
            GetRarityColor(item.Rarity);

        if (rarityText != null)
        {
            rarityText.text =
                item.Rarity
                    .ToString()
                    .ToUpperInvariant();

            rarityText.color =
                rarityColor;
        }

        if (rarityAccent != null)
        {
            rarityAccent.color =
                rarityColor;
        }

        if (statsText != null)
        {
            statsText.text =
                BuildStatsText(item);
        }

        if (descriptionText != null)
        {
            descriptionText.text =
                item.Description;
        }

        StartFade(1f);
    }

    public void Hide()
    {
        if (!gameObject.activeSelf)
            return;

        StartFade(0f);
    }

    public void HideImmediate()
    {
        if (canvasGroup == null)
        {
            canvasGroup =
                GetComponent<CanvasGroup>();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        gameObject.SetActive(false);
    }

    private string BuildStatsText(
        ItemDefinition item)
    {
        StringBuilder builder =
            new StringBuilder();

        if (item.Type == ItemType.Weapon)
        {
            float attacksPerSecond =
                item.AttackCooldown > 0f
                    ? 1f / item.AttackCooldown
                    : 0f;

            builder.AppendLine(
                $"Damage: {item.AttackDamage}"
            );

            builder.AppendLine(
                $"Attack Speed: " +
                $"{attacksPerSecond:0.00}/s"
            );

            builder.AppendLine(
                $"Critical Chance: " +
                $"{item.CriticalChance * 100f:0}%"
            );

            builder.Append(
                $"Critical Damage: " +
                $"x{item.CriticalDamageMultiplier:0.00}"
            );

            return builder.ToString();
        }

        if (item.DamageReduction > 0f)
        {
            builder.AppendLine(
                $"Damage Reduction: " +
                $"{item.DamageReduction * 100f:0}%"
            );
        }

        float moveSpeedBonus =
            (item.MoveSpeedMultiplier - 1f) *
            100f;

        if (Mathf.Abs(moveSpeedBonus) > 0.01f)
        {
            builder.AppendLine(
                $"Movement Speed: " +
                $"{moveSpeedBonus:+0;-0;0}%"
            );
        }

        if (item.MaxHealthBonus != 0)
        {
            builder.AppendLine(
                $"Maximum Health: " +
                $"{item.MaxHealthBonus:+0;-0;0}"
            );
        }

        if (Mathf.Abs(
                item.MaxStaminaBonus) >
            0.01f)
        {
            builder.AppendLine(
                $"Maximum Stamina: " +
                $"{item.MaxStaminaBonus:+0;-0;0}"
            );
        }

        float dashCostChange =
        (
            item.DashStaminaCostMultiplier -
            1f
        ) * 100f;

        if (Mathf.Abs(dashCostChange) > 0.01f)
        {
            builder.AppendLine(
                $"Dash Stamina Cost: " +
                $"{dashCostChange:+0;-0;0}%"
            );
        }
        
        float dashCooldownChange =
        (
            item.DashCooldownMultiplier -
            1f
        ) * 100f;

        if (Mathf.Abs(
                dashCooldownChange) >
            0.01f)
        {
            builder.AppendLine(
                $"Dash Cooldown: " +
                $"{dashCooldownChange:+0;-0;0}%"
            );
        }
        
        float staminaRecoveryBonus =
        (
            item.StaminaRegenerationMultiplier -
            1f
        ) * 100f;

        if (Mathf.Abs(
                staminaRecoveryBonus) >
            0.01f)
        {
            builder.AppendLine(
                $"Stamina Recovery: " +
                $"{staminaRecoveryBonus:+0;-0;0}%"
            );
        }

        if (builder.Length == 0)
        {
            builder.Append(
                "No stat modifier"
            );
        }

        return builder
            .ToString()
            .TrimEnd();
    }

    private void StartFade(
        float targetAlpha)
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine =
            StartCoroutine(
                FadeRoutine(targetAlpha)
            );
    }

    private IEnumerator FadeRoutine(
        float targetAlpha)
    {
        float startAlpha =
            canvasGroup.alpha;

        float elapsedTime = 0f;

        while (elapsedTime <
               fadeDuration)
        {
            float progress =
                elapsedTime /
                fadeDuration;

            canvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    progress
                );

            elapsedTime +=
                Time.unscaledDeltaTime;

            yield return null;
        }

        canvasGroup.alpha =
            targetAlpha;

        fadeRoutine = null;

        if (targetAlpha <= 0f)
        {
            gameObject.SetActive(false);
        }
    }

    private Color GetRarityColor(
        ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common =>
                new Color32(
                    150, 154, 160, 255
                ),

            ItemRarity.Uncommon =>
                new Color32(
                    80, 180, 105, 255
                ),

            ItemRarity.Rare =>
                new Color32(
                    70, 135, 220, 255
                ),

            ItemRarity.Epic =>
                new Color32(
                    165, 90, 220, 255
                ),

            ItemRarity.Legendary =>
                new Color32(
                    235, 145, 45, 255
                ),

            _ => Color.white
        };
    }
}