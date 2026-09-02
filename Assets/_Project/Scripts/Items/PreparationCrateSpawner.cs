using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PreparationCrateSpawner : MonoBehaviour
{
    public static PreparationCrateSpawner Instance
    {
        get;
        private set;
    }

    [Header("Crate Prefab")]
    [SerializeField]
    private PreparationLootCrate preparationCratePrefab;

    [Header("Item Database")]
    [SerializeField]
    private ItemCatalog itemCatalog;

    [Header("Rarity Weights")]
    [SerializeField, Min(0f)]
    private float commonWeight = 50f;

    [SerializeField, Min(0f)]
    private float uncommonWeight = 28f;

    [SerializeField, Min(0f)]
    private float rareWeight = 14f;

    [SerializeField, Min(0f)]
    private float epicWeight = 6f;

    [SerializeField, Min(0f)]
    private float legendaryWeight = 2f;
    
    [Header("Player 1 Room")]
    [SerializeField]
    private Transform[] room0SpawnPoints;

    [Header("Player 2 Room")]
    [SerializeField]
    private Transform[] room1SpawnPoints;

    [Header("Weapon Loot")]
    [SerializeField]
    private ItemId[] weaponLoot =
    {
        ItemId.BalancedSword,
        ItemId.DuelistSword,
        ItemId.HeavySword
    };

    [Header("Passive Loot")]
    [SerializeField]
    private ItemId[] passiveLoot =
    {
        ItemId.IronShield,
        ItemId.SwiftBoots,
        ItemId.VitalityRuby
    };

    private readonly List<NetworkObject>
        spawnedCrates = new();

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Debug.LogError(
                "Birden fazla PreparationCrateSpawner bulundu."
            );

            enabled = false;
            return;
        }

        Instance = this;
    }

    public void SpawnCratesOnServer()
    {
        if (!CanRunOnServer())
            return;

        CleanupPreparationObjectsOnServer();

        if (preparationCratePrefab == null)
        {
            Debug.LogError(
                "PreparationCrate prefabı atanmamış."
            );

            return;
        }
        
        if (itemCatalog == null)
        {
            Debug.LogError(
                "PreparationCrateSpawner: " +
                "ItemCatalog atanmamış."
            );

            return;
        }

        if (room0SpawnPoints == null ||
            room1SpawnPoints == null ||
            room0SpawnPoints.Length == 0 ||
            room1SpawnPoints.Length == 0)
        {
            Debug.LogError(
                "Preparation sandık noktaları atanmamış."
            );

            return;
        }

        if (room0SpawnPoints.Length !=
            room1SpawnPoints.Length)
        {
            Debug.LogError(
                "İki hazırlık odasındaki sandık " +
                "noktası sayısı eşit olmalı."
            );

            return;
        }

        List<ItemId> room0LootPlan =
            CreateFairLootPlan(
                room0SpawnPoints.Length
            );

        List<ItemId> room1LootPlan =
            CreateFairLootPlan(
                room1SpawnPoints.Length
            );

        // İki odaya tamamen aynı set gelirse
        // birkaç kez yeniden üretmeyi dene.
        int rerollAttempts = 0;

        while (HaveSameItems(
                   room0LootPlan,
                   room1LootPlan) &&
               rerollAttempts < 10)
        {
            room1LootPlan =
                CreateFairLootPlan(
                    room1SpawnPoints.Length
                );

            rerollAttempts++;
        }

        SpawnRoomCrates(
            room0SpawnPoints,
            room0LootPlan
        );

        SpawnRoomCrates(
            room1SpawnPoints,
            room1LootPlan
        );
    }

    public void CleanupPreparationObjectsOnServer()
    {
        if (!CanRunOnServer())
            return;

        foreach (NetworkObject crate
                 in spawnedCrates)
        {
            if (crate != null &&
                crate.IsSpawned)
            {
                crate.Despawn(true);
            }
        }

        spawnedCrates.Clear();

        ItemPickup[] remainingPickups =
            FindObjectsByType<ItemPickup>(
                FindObjectsSortMode.None
            );

        foreach (ItemPickup pickup
                 in remainingPickups)
        {
            if (pickup == null ||
                !pickup.IsSpawned)
            {
                continue;
            }

            pickup.NetworkObject.Despawn(true);
        }
    }

    private List<ItemId> CreateFairLootPlan(
        int crateCount)
    {
        List<ItemId> result = new();

        if (crateCount <= 0)
            return result;

        // Her odada en az bir silah garanti.
        ItemId selectedWeapon =
            GetWeightedRandomItem(weaponLoot);

        if (selectedWeapon != ItemId.None)
        {
            result.Add(selectedWeapon);
        }

        List<ItemId> availablePassives =
            new(passiveLoot);

        while (result.Count < crateCount)
        {
            IReadOnlyList<ItemId> source =
                availablePassives.Count > 0
                    ? availablePassives
                    : passiveLoot;

            ItemId selectedPassive =
                GetWeightedRandomItem(source);

            if (selectedPassive == ItemId.None)
            {
                Debug.LogError(
                    "Geçerli pasif loot seçilemedi."
                );

                break;
            }

            result.Add(selectedPassive);

            // Aynı odada aynı pasif tekrar çıkmasın.
            availablePassives.Remove(
                selectedPassive
            );
        }

        return result;
    }

    private void SpawnRoomCrates(
        Transform[] spawnPoints,
        IReadOnlyList<ItemId> lootPlan)
    {
        for (int index = 0;
             index < spawnPoints.Length;
             index++)
        {
            Transform spawnPoint =
                spawnPoints[index];

            if (spawnPoint == null)
            {
                Debug.LogError(
                    $"Sandık noktası {index} atanmamış."
                );

                continue;
            }

            PreparationLootCrate crate =
                Instantiate(
                    preparationCratePrefab,
                    spawnPoint.position,
                    spawnPoint.rotation
                );

            NetworkObject networkObject =
                crate.GetComponent<NetworkObject>();

            networkObject.Spawn();

            ItemId lootItemId =
                lootPlan[index];

            if (!itemCatalog.TryGetItem(
                    lootItemId,
                    out ItemDefinition itemDefinition))
            {
                Debug.LogError(
                    $"ItemDefinition bulunamadı: {lootItemId}"
                );

                networkObject.Despawn(true);
                continue;
            }

            crate.ServerSetLoot(
                lootItemId,
                itemDefinition.Rarity
            );

            spawnedCrates.Add(
                networkObject
            );
        }
    }

    private ItemId GetWeightedRandomItem(
    IReadOnlyList<ItemId> possibleItems)
{
    if (possibleItems == null ||
        possibleItems.Count == 0)
    {
        Debug.LogError(
            "Loot listesi boş."
        );

        return ItemId.None;
    }

    List<ItemDefinition> validItems =
        new();

    foreach (ItemId itemId in possibleItems)
    {
        if (itemCatalog.TryGetItem(
                itemId,
                out ItemDefinition item))
        {
            validItems.Add(item);
        }
    }

    if (validItems.Count == 0)
    {
        Debug.LogError(
            "Loot listesinde geçerli item yok."
        );

        return ItemId.None;
    }

    float totalWeight = 0f;

    foreach (ItemDefinition item in validItems)
    {
        int rarityItemCount =
            CountItemsWithRarity(
                validItems,
                item.Rarity
            );

        totalWeight +=
            GetRarityWeight(item.Rarity) /
            rarityItemCount;
    }

    // Bütün ağırlıklar yanlışlıkla sıfırsa
    // normal rastgele seçime geri dön.
    if (totalWeight <= 0f)
    {
        return validItems[
            Random.Range(
                0,
                validItems.Count
            )
        ].Id;
    }

    float randomValue =
        Random.Range(0f, totalWeight);

    foreach (ItemDefinition item in validItems)
    {
        int rarityItemCount =
            CountItemsWithRarity(
                validItems,
                item.Rarity
            );

        float itemWeight =
            GetRarityWeight(item.Rarity) /
            rarityItemCount;

        if (randomValue < itemWeight)
            return item.Id;

        randomValue -= itemWeight;
    }

    return validItems[
        validItems.Count - 1
    ].Id;
}

private int CountItemsWithRarity(
    IReadOnlyList<ItemDefinition> items,
    ItemRarity rarity)
{
    int count = 0;

    foreach (ItemDefinition item in items)
    {
        if (item != null &&
            item.Rarity == rarity)
        {
            count++;
        }
    }

    return Mathf.Max(1, count);
}

private float GetRarityWeight(
    ItemRarity rarity)
{
    return rarity switch
    {
        ItemRarity.Common =>
            commonWeight,

        ItemRarity.Uncommon =>
            uncommonWeight,

        ItemRarity.Rare =>
            rareWeight,

        ItemRarity.Epic =>
            epicWeight,

        ItemRarity.Legendary =>
            legendaryWeight,

        _ => 0f
    };
}

    private bool HaveSameItems(
        IReadOnlyList<ItemId> firstPlan,
        IReadOnlyList<ItemId> secondPlan)
    {
        if (firstPlan == null ||
            secondPlan == null ||
            firstPlan.Count != secondPlan.Count)
        {
            return false;
        }

        HashSet<ItemId> firstItems =
            new(firstPlan);

        return firstItems.SetEquals(
            secondPlan
        );
    }
    
    private bool CanRunOnServer()
    {
        return
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsServer;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}