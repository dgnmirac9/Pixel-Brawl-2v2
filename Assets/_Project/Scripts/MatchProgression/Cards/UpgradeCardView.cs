using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeCardView :
    MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField]
    private Button purchaseButton;

    [SerializeField]
    private CanvasGroup canvasGroup;

    [Header("Visuals")]
    [SerializeField]
    private Image rarityFrame;

    [SerializeField]
    private Image cardIcon;

    [SerializeField]
    private TMP_Text cardNameText;

    [SerializeField]
    private TMP_Text rarityText;

    [SerializeField]
    private TMP_Text descriptionText;

    [SerializeField]
    private TMP_Text costText;

    private UpgradeCardDefinition card;
    private Action<int> clickedCallback;
    private int slotIndex;
    private bool affordable;
    private bool interactionAllowed;

    public UpgradeCardDefinition Card =>
        card;

    private void Awake()
    {
        if (purchaseButton == null)
        {
            purchaseButton =
                GetComponent<Button>();
        }

        if (canvasGroup == null)
        {
            canvasGroup =
                GetComponent<CanvasGroup>();
        }

        if (purchaseButton != null)
        {
            purchaseButton.onClick.AddListener(
                HandleClicked
            );
        }
    }

    public void Configure(
        int newSlotIndex,
        UpgradeCardDefinition newCard,
        int currentGold,
        Action<int> onClicked)
    {
        slotIndex = newSlotIndex;
        card = newCard;
        clickedCallback = onClicked;

        bool hasCard =
            card != null;

        gameObject.SetActive(
            hasCard
        );

        if (!hasCard)
            return;

        if (cardNameText != null)
        {
            cardNameText.text =
                card.DisplayName;
        }

        if (rarityText != null)
        {
            rarityText.text =
                card.Rarity
                    .ToString()
                    .ToUpperInvariant();

            rarityText.color =
                GetRarityColor(
                    card.Rarity
                );
        }

        if (descriptionText != null)
        {
            descriptionText.text =
                card.Description;
        }

        if (cardIcon != null)
        {
            cardIcon.sprite =
                card.Icon;

            cardIcon.enabled =
                card.Icon != null;
        }

        if (rarityFrame != null)
        {
            rarityFrame.color =
                GetRarityColor(
                    card.Rarity
                );
        }

        RefreshAffordability(
            currentGold
        );
    }

    public void RefreshAffordability(
        int currentGold)
    {
        if (card == null)
            return;

        affordable =
            currentGold >=
            card.GoldCost;

        if (costText != null)
        {
            costText.text =
                $"{card.GoldCost} GOLD";

            costText.color =
                affordable
                    ? new Color32(
                        232, 199, 102, 255
                    )
                    : new Color32(
                        229, 107, 93, 255
                    );
        }

        RefreshButtonState();
    }

    public void SetInteractionAllowed(
        bool allowed)
    {
        interactionAllowed =
            allowed;

        RefreshButtonState();
    }

    public void SetRevealProgress(
        float progress)
    {
        progress =
            Mathf.Clamp01(progress);

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                progress;
        }

        transform.localScale =
            Vector3.one *
            Mathf.Lerp(
                0.82f,
                1f,
                progress
            );
    }

    private void RefreshButtonState()
    {
        if (purchaseButton == null)
            return;

        purchaseButton.interactable =
            interactionAllowed &&
            affordable &&
            card != null;
    }

    private void HandleClicked()
    {
        if (!interactionAllowed ||
            !affordable ||
            card == null)
        {
            return;
        }

        clickedCallback?.Invoke(
            slotIndex
        );
    }

    private Color GetRarityColor(
        ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common =>
                new Color32(
                    210, 210, 210, 255
                ),

            ItemRarity.Uncommon =>
                new Color32(
                    105, 205, 115, 255
                ),

            ItemRarity.Rare =>
                new Color32(
                    95, 145, 235, 255
                ),

            ItemRarity.Epic =>
                new Color32(
                    175, 95, 225, 255
                ),

            ItemRarity.Legendary =>
                new Color32(
                    242, 184, 65, 255
                ),

            _ => Color.white
        };
    }

    private void OnDestroy()
    {
        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveListener(
                HandleClicked
            );
        }
    }
}