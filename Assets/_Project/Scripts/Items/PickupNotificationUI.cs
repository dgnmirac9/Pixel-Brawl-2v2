using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PickupNotificationUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private Image itemIcon;

    [SerializeField]
    private Image rarityAccent;
    
    [SerializeField]
    private TMP_Text itemNameText;

    [SerializeField]
    private TMP_Text rarityText;

    [SerializeField]
    private AudioSource audioSource;

    [Header("Audio")]
    [SerializeField]
    private AudioClip pickupSound;

    [Header("Animation")]
    [SerializeField, Min(0f)]
    private float displayDuration = 1.4f;

    [SerializeField, Min(0.01f)]
    private float fadeDuration = 0.2f;

    [SerializeField, Min(0f)]
    private float slideDistance = 18f;

    private RectTransform rectTransform;
    private Vector2 basePosition;
    private Coroutine notificationRoutine;

    private void Awake()
    {
        rectTransform =
            GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            basePosition =
                rectTransform.anchoredPosition;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (itemIcon != null)
        {
            itemIcon.preserveAspect = true;
        }
    }

    public void ShowItem(
        ItemDefinition item)
    {
        if (item == null)
            return;

        if (notificationRoutine != null)
        {
            StopCoroutine(
                notificationRoutine
            );
        }

        Color rarityColor =
            GetRarityColor(
                item.Rarity
            );

        if (itemIcon != null)
        {
            itemIcon.sprite =
                item.Icon;

            itemIcon.enabled =
                item.Icon != null;

            itemIcon.preserveAspect =
                true;
        }

        if (itemNameText != null)
        {
            itemNameText.text =
                item.DisplayName
                    .ToUpperInvariant();
        }

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

        if (audioSource != null &&
            pickupSound != null)
        {
            audioSource.PlayOneShot(
                pickupSound
            );
        }

        notificationRoutine =
            StartCoroutine(
                PlayNotification()
            );
    }

    private IEnumerator PlayNotification()
    {
        if (canvasGroup == null ||
            rectTransform == null)
        {
            notificationRoutine = null;
            yield break;
        }

        Vector2 startPosition =
            basePosition +
            Vector2.down *
            slideDistance;

        rectTransform.anchoredPosition =
            startPosition;

        canvasGroup.alpha = 0f;

        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime /
                    fadeDuration
                );

            float smoothTime =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    normalizedTime
                );

            canvasGroup.alpha =
                smoothTime;

            rectTransform.anchoredPosition =
                Vector2.Lerp(
                    startPosition,
                    basePosition,
                    smoothTime
                );

            elapsedTime +=
                Time.unscaledDeltaTime;

            yield return null;
        }

        canvasGroup.alpha = 1f;

        rectTransform.anchoredPosition =
            basePosition;

        yield return new WaitForSecondsRealtime(
            displayDuration
        );

        elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime /
                    fadeDuration
                );

            canvasGroup.alpha =
                1f - normalizedTime;

            elapsedTime +=
                Time.unscaledDeltaTime;

            yield return null;
        }

        canvasGroup.alpha = 0f;

        notificationRoutine = null;
    }

    private Color GetRarityColor(
        ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common =>
                new Color(
                    0.65f,
                    0.65f,
                    0.65f
                ),

            ItemRarity.Uncommon =>
                new Color(
                    0.35f,
                    0.78f,
                    0.42f
                ),

            ItemRarity.Rare =>
                new Color(
                    0.30f,
                    0.58f,
                    1f
                ),

            ItemRarity.Epic =>
                new Color(
                    0.72f,
                    0.37f,
                    1f
                ),

            ItemRarity.Legendary =>
                new Color(
                    1f,
                    0.68f,
                    0.22f
                ),

            _ => Color.white
        };
    }

    private void OnDisable()
    {
        if (notificationRoutine != null)
        {
            StopCoroutine(
                notificationRoutine
            );

            notificationRoutine = null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition =
                basePosition;
        }
    }
}