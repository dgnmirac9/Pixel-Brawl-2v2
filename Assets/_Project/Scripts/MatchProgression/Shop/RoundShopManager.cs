using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class RoundShopManager :
    NetworkBehaviour
{
    public static RoundShopManager Instance
    {
        get;
        private set;
    }

    private const int OfferCount = 3;

    [Header("Card Database")]
    [SerializeField]
    private UpgradeCardCatalog cardCatalog;

    [Header("Shop Settings")]
    [SerializeField, Min(1)]
    private int shopDurationSeconds = 10;

    private readonly NetworkVariable<int>
        shopTimeRemaining = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private readonly Dictionary<
        ulong,
        UpgradeCardId[]> offersByClient =
            new();

    private readonly HashSet<ulong>
        participantClientIds = new();

    private readonly HashSet<ulong>
        resolvedClientIds = new();

    private readonly UpgradeCardDefinition[]
        localOffers =
            new UpgradeCardDefinition[OfferCount];

    private Coroutine shopRoutine;
    private bool shopRunning;
    private bool hasLocalOffers;

    public int ShopTimeRemaining =>
        shopTimeRemaining.Value;

    public bool IsShopRunning =>
        shopRunning;

    public bool HasLocalOffers =>
        hasLocalOffers;

    public event Action LocalShopOpened;
    public event Action LocalShopClosed;

    public event Action<int>
        ShopTimeChanged;

    public event Action<
        UpgradeCardDefinition>
        LocalPurchaseSucceeded;

    public event Action<string>
        LocalPurchaseFailed;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Debug.LogError(
                "[RoundShop] Sahnede birden fazla " +
                "RoundShopManager bulundu."
            );

            enabled = false;
            return;
        }

        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        shopTimeRemaining.OnValueChanged +=
            HandleShopTimeChanged;

        ShopTimeChanged?.Invoke(
            shopTimeRemaining.Value
        );
    }

    public override void OnNetworkDespawn()
    {
        shopTimeRemaining.OnValueChanged -=
            HandleShopTimeChanged;

        if (shopRoutine != null)
        {
            StopCoroutine(shopRoutine);
            shopRoutine = null;
        }

        ClearServerShopState();
        ClearLocalOffers();
    }

    public UpgradeCardDefinition
        GetLocalOffer(int slotIndex)
    {
        if (slotIndex < 0 ||
            slotIndex >= localOffers.Length)
        {
            return null;
        }

        return localOffers[slotIndex];
    }

    public void ServerBeginShop(
        int completedRoundNumber)
    {
        if (!IsServer ||
            shopRunning)
        {
            return;
        }

        if (cardCatalog == null)
        {
            Debug.LogError(
                "[RoundShop] UpgradeCardCatalog atanmamış."
            );

            return;
        }

        ClearServerShopState();

        shopRunning = true;

        foreach (ulong clientId in
                 NetworkManager.ConnectedClientsIds)
        {
            if (!TryGetProgression(
                    clientId,
                    out PlayerMatchProgression
                        progression))
            {
                Debug.LogWarning(
                    "[RoundShop] Oyuncu progression'ı " +
                    "bulunamadı. " +
                    $"ClientId: {clientId}"
                );

                continue;
            }

            participantClientIds.Add(
                clientId
            );

            UpgradeCardId[] offers =
                GenerateOffers(
                    progression,
                    completedRoundNumber
                );

            offersByClient[clientId] =
                offers;

            ShowShopClientRpc(
                offers[0],
                offers[1],
                offers[2],
                CreateTargetParams(clientId)
            );

            Debug.Log(
                "[RoundShop] Teklif oluşturuldu. " +
                $"ClientId: {clientId} | " +
                $"Round: {completedRoundNumber} | " +
                $"Kartlar: {offers[0]}, " +
                $"{offers[1]}, {offers[2]}"
            );
        }

        shopRoutine =
            StartCoroutine(
                RunShopTimer()
            );
    }

    public void RequestPurchase(
        int slotIndex)
    {
        if (!IsSpawned ||
            slotIndex < 0 ||
            slotIndex >= OfferCount)
        {
            return;
        }

        RequestPurchaseServerRpc(
            slotIndex
        );
    }

    public void RequestSkip()
    {
        if (!IsSpawned)
            return;

        RequestSkipServerRpc();
    }

    public void ServerCancelShop()
    {
        if (!IsServer)
            return;

        if (shopRoutine != null)
        {
            StopCoroutine(shopRoutine);
            shopRoutine = null;
        }

        FinishShopOnServer();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPurchaseServerRpc(
        int slotIndex,
        ServerRpcParams rpcParams = default)
    {
        ulong clientId =
            rpcParams.Receive.SenderClientId;

        if (!shopRunning ||
            resolvedClientIds.Contains(clientId))
        {
            return;
        }

        if (slotIndex < 0 ||
            slotIndex >= OfferCount)
        {
            SendPurchaseFailed(
                clientId,
                "INVALID CARD"
            );

            return;
        }

        if (!offersByClient.TryGetValue(
                clientId,
                out UpgradeCardId[] offers))
        {
            SendPurchaseFailed(
                clientId,
                "OFFERS NOT FOUND"
            );

            return;
        }

        UpgradeCardId selectedCardId =
            offers[slotIndex];

        if (selectedCardId ==
            UpgradeCardId.None)
        {
            SendPurchaseFailed(
                clientId,
                "CARD NOT AVAILABLE"
            );

            return;
        }

        UpgradeCardDefinition card =
            cardCatalog.GetCard(
                selectedCardId
            );

        if (card == null)
        {
            SendPurchaseFailed(
                clientId,
                "CARD DATA NOT FOUND"
            );

            return;
        }

        if (!TryGetProgression(
                clientId,
                out PlayerMatchProgression
                    progression))
        {
            SendPurchaseFailed(
                clientId,
                "PLAYER NOT FOUND"
            );

            return;
        }

        if (!progression
                .ServerTryPurchaseCard(card))
        {
            SendPurchaseFailed(
                clientId,
                "NOT ENOUGH GOLD OR MAXED"
            );

            return;
        }

        resolvedClientIds.Add(clientId);

        PurchaseSucceededClientRpc(
            selectedCardId,
            CreateTargetParams(clientId)
        );

        Debug.Log(
            "[RoundShop] Satın alma başarılı. " +
            $"ClientId: {clientId} | " +
            $"Kart: {selectedCardId}"
        );
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSkipServerRpc(
        ServerRpcParams rpcParams = default)
    {
        ulong clientId =
            rpcParams.Receive.SenderClientId;

        if (!shopRunning ||
            !participantClientIds.Contains(clientId) ||
            resolvedClientIds.Contains(clientId))
        {
            return;
        }

        resolvedClientIds.Add(clientId);

        Debug.Log(
            "[RoundShop] Oyuncu seçimi geçti. " +
            $"ClientId: {clientId}"
        );
    }

    private IEnumerator RunShopTimer()
    {
        shopTimeRemaining.Value =
            shopDurationSeconds;

        while (shopTimeRemaining.Value > 0)
        {
            RemoveDisconnectedParticipants();

            if (AllParticipantsResolved())
                break;

            yield return new WaitForSecondsRealtime(
                1f
            );

            shopTimeRemaining.Value =
                Mathf.Max(
                    0,
                    shopTimeRemaining.Value - 1
                );
        }

        FinishShopOnServer();
        shopRoutine = null;
    }

    private void FinishShopOnServer()
    {
        if (!IsServer ||
            !shopRunning)
        {
            return;
        }

        shopRunning = false;
        shopTimeRemaining.Value = 0;

        CloseShopClientRpc();

        Debug.Log(
            "[RoundShop] Shop tamamlandı."
        );
    }

    private UpgradeCardId[] GenerateOffers(
        PlayerMatchProgression progression,
        int completedRoundNumber)
    {
        List<UpgradeCardDefinition>
            availableCards = new();

        foreach (UpgradeCardDefinition card
                 in cardCatalog.Cards)
        {
            if (card == null ||
                card.Id == UpgradeCardId.None)
            {
                continue;
            }

            if (card.MinimumShopRound >
                completedRoundNumber)
            {
                continue;
            }

            int purchaseCount =
                progression
                    .ServerGetPurchaseCount(
                        card.Id
                    );

            if (purchaseCount >=
                card.MaximumPurchasesPerMatch)
            {
                continue;
            }

            availableCards.Add(card);
        }

        UpgradeCardId[] generatedOffers =
            new UpgradeCardId[OfferCount];

        HashSet<UpgradeCardId>
            selectedIds = new();

        // İlk slotta mümkün olduğunca
        // satın alınabilir bir kart garanti eder.
        UpgradeCardDefinition firstCard =
            SelectAffordableCard(
                availableCards,
                selectedIds,
                progression.Gold
            );

        generatedOffers[0] =
            firstCard != null
                ? firstCard.Id
                : UpgradeCardId.None;

        if (firstCard != null)
            selectedIds.Add(firstCard.Id);

        for (int index = 1;
             index < OfferCount;
             index++)
        {
            UpgradeCardDefinition card =
                SelectWeightedCard(
                    availableCards,
                    selectedIds
                );

            generatedOffers[index] =
                card != null
                    ? card.Id
                    : UpgradeCardId.None;

            if (card != null)
                selectedIds.Add(card.Id);
        }

        return generatedOffers;
    }

    private UpgradeCardDefinition
        SelectAffordableCard(
            List<UpgradeCardDefinition> cards,
            HashSet<UpgradeCardId> excludedIds,
            int availableGold)
    {
        List<UpgradeCardDefinition>
            affordableCards = new();

        foreach (UpgradeCardDefinition card
                 in cards)
        {
            if (excludedIds.Contains(card.Id) ||
                card.GoldCost > availableGold)
            {
                continue;
            }

            affordableCards.Add(card);
        }

        if (affordableCards.Count == 0)
        {
            return SelectWeightedCard(
                cards,
                excludedIds
            );
        }

        return SelectWeightedCard(
            affordableCards,
            excludedIds
        );
    }

    private UpgradeCardDefinition
        SelectWeightedCard(
            List<UpgradeCardDefinition> cards,
            HashSet<UpgradeCardId> excludedIds)
    {
        int totalWeight = 0;

        foreach (UpgradeCardDefinition card
                 in cards)
        {
            if (excludedIds.Contains(card.Id))
                continue;

            totalWeight +=
                GetRarityWeight(card.Rarity);
        }

        if (totalWeight <= 0)
            return null;

        int roll =
            UnityEngine.Random.Range(
                0,
                totalWeight
            );

        foreach (UpgradeCardDefinition card
                 in cards)
        {
            if (excludedIds.Contains(card.Id))
                continue;

            roll -=
                GetRarityWeight(card.Rarity);

            if (roll < 0)
                return card;
        }

        return null;
    }

    private int GetRarityWeight(
        ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => 50,
            ItemRarity.Uncommon => 28,
            ItemRarity.Rare => 14,
            ItemRarity.Epic => 6,
            ItemRarity.Legendary => 2,
            _ => 0
        };
    }

    private bool TryGetProgression(
        ulong clientId,
        out PlayerMatchProgression progression)
    {
        progression = null;

        if (!NetworkManager.ConnectedClients
                .TryGetValue(
                    clientId,
                    out NetworkClient client))
        {
            return false;
        }

        NetworkObject playerObject =
            client.PlayerObject;

        if (playerObject == null)
            return false;

        progression =
            playerObject.GetComponent<
                PlayerMatchProgression>();

        if (progression == null)
        {
            progression =
                playerObject.GetComponentInChildren<
                    PlayerMatchProgression>();
        }

        return progression != null;
    }

    private bool AllParticipantsResolved()
    {
        if (participantClientIds.Count == 0)
            return true;

        foreach (ulong clientId in
                 participantClientIds)
        {
            if (!resolvedClientIds.Contains(
                    clientId))
            {
                return false;
            }
        }

        return true;
    }

    private void RemoveDisconnectedParticipants()
    {
        participantClientIds.RemoveWhere(
            clientId =>
                !NetworkManager.ConnectedClients
                    .ContainsKey(clientId)
        );
    }

    private ClientRpcParams CreateTargetParams(
        ulong clientId)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds =
                    new[] { clientId }
            }
        };
    }

    private void SendPurchaseFailed(
        ulong clientId,
        string message)
    {
        PurchaseFailedClientRpc(
            message,
            CreateTargetParams(clientId)
        );
    }

    [ClientRpc]
    private void ShowShopClientRpc(
        UpgradeCardId card0,
        UpgradeCardId card1,
        UpgradeCardId card2,
        ClientRpcParams rpcParams = default)
    {
        localOffers[0] =
            cardCatalog.GetCard(card0);

        localOffers[1] =
            cardCatalog.GetCard(card1);

        localOffers[2] =
            cardCatalog.GetCard(card2);

        hasLocalOffers = true;

        LocalShopOpened?.Invoke();

        Debug.Log(
            "[RoundShop] Yerel teklifler: " +
            $"{card0}, {card1}, {card2}"
        );
    }

    [ClientRpc]
    private void PurchaseSucceededClientRpc(
        UpgradeCardId purchasedCardId,
        ClientRpcParams rpcParams = default)
    {
        UpgradeCardDefinition card =
            cardCatalog.GetCard(
                purchasedCardId
            );

        LocalPurchaseSucceeded?.Invoke(
            card
        );
    }

    [ClientRpc]
    private void PurchaseFailedClientRpc(
        string message,
        ClientRpcParams rpcParams = default)
    {
        LocalPurchaseFailed?.Invoke(
            message
        );
    }

    [ClientRpc]
    private void CloseShopClientRpc()
    {
        ClearLocalOffers();
        LocalShopClosed?.Invoke();
    }

    private void HandleShopTimeChanged(
        int previousValue,
        int newValue)
    {
        ShopTimeChanged?.Invoke(
            newValue
        );
    }

    private void ClearServerShopState()
    {
        offersByClient.Clear();
        participantClientIds.Clear();
        resolvedClientIds.Clear();
        shopRunning = false;
    }

    private void ClearLocalOffers()
    {
        hasLocalOffers = false;

        for (int index = 0;
             index < localOffers.Length;
             index++)
        {
            localOffers[index] = null;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}