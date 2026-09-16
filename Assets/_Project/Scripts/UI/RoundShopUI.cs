using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class RoundShopUI :
    MonoBehaviour
{
    [Header("Panel")]
    [SerializeField]
    private GameObject shopPanel;

    [SerializeField]
    private RectTransform shopWindow;

    [Header("Texts")]
    [SerializeField]
    private TMP_Text shopTimerText;

    [SerializeField]
    private TMP_Text currentGoldText;

    [SerializeField]
    private TMP_Text statusText;

    [Header("Cards")]
    [SerializeField]
    private UpgradeCardView[] cardViews;

    [Header("Controls")]
    [SerializeField]
    private Button skipButton;

    [Header("Animation")]
    [SerializeField, Min(0.01f)]
    private float windowAnimationDuration =
        0.20f;

    [SerializeField, Min(0.01f)]
    private float cardAnimationDuration =
        0.18f;

    [SerializeField, Min(0f)]
    private float cardRevealDelay =
        0.10f;

    private RoundShopManager shopManager;

    private PlayerMatchProgression
        localProgression;

    private Coroutine initializationRoutine;
    private Coroutine revealRoutine;

    private bool selectionResolved;

    private void OnEnable()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(
                HandleSkipClicked
            );
        }

        initializationRoutine =
            StartCoroutine(
                Initialize()
            );
    }

    private IEnumerator Initialize()
    {
        while (RoundShopManager.Instance == null ||
               !RoundShopManager.Instance.IsSpawned)
        {
            yield return null;
        }

        shopManager =
            RoundShopManager.Instance;

        while (NetworkManager.Singleton == null ||
               NetworkManager.Singleton.LocalClient ==
               null ||
               NetworkManager.Singleton.LocalClient
                   .PlayerObject == null)
        {
            yield return null;
        }

        NetworkObject localPlayer =
            NetworkManager.Singleton
                .LocalClient
                .PlayerObject;

        localProgression =
            localPlayer.GetComponent<
                PlayerMatchProgression>();

        if (localProgression == null)
        {
            localProgression =
                localPlayer.GetComponentInChildren<
                    PlayerMatchProgression>();
        }

        if (localProgression == null)
        {
            Debug.LogError(
                "[RoundShopUI] Yerel oyuncuda " +
                "PlayerMatchProgression bulunamadı."
            );

            initializationRoutine = null;
            yield break;
        }

        shopManager.LocalShopOpened +=
            HandleShopOpened;

        shopManager.LocalShopClosed +=
            HandleShopClosed;

        shopManager.ShopTimeChanged +=
            HandleShopTimeChanged;

        shopManager.LocalPurchaseSucceeded +=
            HandlePurchaseSucceeded;

        shopManager.LocalPurchaseFailed +=
            HandlePurchaseFailed;

        localProgression.GoldChanged +=
            HandleGoldChanged;

        HandleShopTimeChanged(
            shopManager.ShopTimeRemaining
        );

        SetGoldText(
            localProgression.Gold
        );

        // RPC, UI abone olmadan geldiyse
        // manager'ın önbelleğini kullanır.
        if (shopManager.HasLocalOffers)
        {
            HandleShopOpened();
        }

        initializationRoutine = null;
    }

    private void HandleShopOpened()
    {
        if (shopManager == null ||
            localProgression == null)
        {
            return;
        }

        selectionResolved = false;

        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
        }

        if (statusText != null)
        {
            statusText.text =
                "SELECT ONE CARD";
        }

        SetGoldText(
            localProgression.Gold
        );

        for (int index = 0;
             index < cardViews.Length;
             index++)
        {
            UpgradeCardDefinition offer =
                shopManager.GetLocalOffer(
                    index
                );

            cardViews[index].Configure(
                index,
                offer,
                localProgression.Gold,
                HandleCardClicked
            );

            cardViews[index]
                .SetInteractionAllowed(false);

            cardViews[index]
                .SetRevealProgress(0f);
        }

        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
        }

        revealRoutine =
            StartCoroutine(
                RevealShop()
            );
    }

    private IEnumerator RevealShop()
    {
        if (shopWindow != null)
        {
            float elapsed = 0f;

            while (elapsed <
                   windowAnimationDuration)
            {
                elapsed +=
                    Time.unscaledDeltaTime;

                float progress =
                    Mathf.Clamp01(
                        elapsed /
                        windowAnimationDuration
                    );

                shopWindow.localScale =
                    Vector3.one *
                    Mathf.Lerp(
                        0.90f,
                        1f,
                        progress
                    );

                yield return null;
            }

            shopWindow.localScale =
                Vector3.one;
        }

        for (int index = 0;
             index < cardViews.Length;
             index++)
        {
            UpgradeCardView view =
                cardViews[index];

            if (!view.gameObject.activeSelf)
                continue;

            float elapsed = 0f;

            while (elapsed <
                   cardAnimationDuration)
            {
                elapsed +=
                    Time.unscaledDeltaTime;

                float progress =
                    Mathf.Clamp01(
                        elapsed /
                        cardAnimationDuration
                    );

                view.SetRevealProgress(
                    progress
                );

                yield return null;
            }

            view.SetRevealProgress(1f);

            if (cardRevealDelay > 0f)
            {
                yield return
                    new WaitForSecondsRealtime(
                        cardRevealDelay
                    );
            }
        }

        RefreshCardInteractions();

        revealRoutine = null;
    }

    private void HandleCardClicked(
        int slotIndex)
    {
        if (selectionResolved ||
            shopManager == null)
        {
            return;
        }

        SetAllCardInteractions(false);

        if (skipButton != null)
        {
            skipButton.interactable =
                false;
        }

        if (statusText != null)
        {
            statusText.text =
                "PURCHASING...";
        }

        shopManager.RequestPurchase(
            slotIndex
        );
    }

    private void HandleSkipClicked()
    {
        if (selectionResolved ||
            shopManager == null)
        {
            return;
        }

        selectionResolved = true;

        SetAllCardInteractions(false);

        if (skipButton != null)
        {
            skipButton.interactable =
                false;
        }

        if (statusText != null)
        {
            statusText.text =
                "GOLD SAVED - WAITING...";
        }

        shopManager.RequestSkip();
    }

    private void HandlePurchaseSucceeded(
        UpgradeCardDefinition card)
    {
        selectionResolved = true;

        SetAllCardInteractions(false);

        if (skipButton != null)
        {
            skipButton.interactable =
                false;
        }

        if (statusText != null)
        {
            statusText.text =
                card != null
                    ? $"PURCHASED: {card.DisplayName}"
                    : "PURCHASE COMPLETE";
        }
    }

    private void HandlePurchaseFailed(
        string message)
    {
        selectionResolved = false;

        if (statusText != null)
        {
            statusText.text =
                message;
        }

        if (skipButton != null)
        {
            skipButton.interactable =
                true;
        }

        RefreshCardInteractions();
    }

    private void HandleShopClosed()
    {
        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
            revealRoutine = null;
        }

        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        selectionResolved = false;
    }

    private void HandleShopTimeChanged(
        int seconds)
    {
        if (shopTimerText != null)
        {
            shopTimerText.text =
                seconds.ToString();
        }
    }

    private void HandleGoldChanged(
        int previousGold,
        int newGold)
    {
        SetGoldText(newGold);

        if (!selectionResolved)
        {
            RefreshCardInteractions();
        }
    }

    private void SetGoldText(
        int gold)
    {
        if (currentGoldText != null)
        {
            currentGoldText.text =
                $"{gold} GOLD";
        }
    }

    private void RefreshCardInteractions()
    {
        if (localProgression == null ||
            selectionResolved)
        {
            return;
        }

        foreach (UpgradeCardView view
                 in cardViews)
        {
            if (view == null ||
                !view.gameObject.activeSelf)
            {
                continue;
            }

            view.RefreshAffordability(
                localProgression.Gold
            );

            view.SetInteractionAllowed(
                true
            );
        }
    }

    private void SetAllCardInteractions(
        bool allowed)
    {
        foreach (UpgradeCardView view
                 in cardViews)
        {
            if (view != null)
            {
                view.SetInteractionAllowed(
                    allowed
                );
            }
        }
    }

    private void OnDisable()
    {
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(
                HandleSkipClicked
            );
        }

        if (initializationRoutine != null)
        {
            StopCoroutine(
                initializationRoutine
            );

            initializationRoutine = null;
        }

        if (revealRoutine != null)
        {
            StopCoroutine(
                revealRoutine
            );

            revealRoutine = null;
        }

        if (shopManager != null)
        {
            shopManager.LocalShopOpened -=
                HandleShopOpened;

            shopManager.LocalShopClosed -=
                HandleShopClosed;

            shopManager.ShopTimeChanged -=
                HandleShopTimeChanged;

            shopManager.LocalPurchaseSucceeded -=
                HandlePurchaseSucceeded;

            shopManager.LocalPurchaseFailed -=
                HandlePurchaseFailed;
        }

        if (localProgression != null)
        {
            localProgression.GoldChanged -=
                HandleGoldChanged;
        }

        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        shopManager = null;
        localProgression = null;
    }
}