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

        // İlk sandık mutlaka bir kılıç verir.
        result.Add(
            GetRandomItem(weaponLoot)
        );

        List<ItemId> availablePassives =
            new(passiveLoot);

        while (result.Count < crateCount)
        {
            if (availablePassives.Count == 0)
            {
                result.Add(
                    GetRandomItem(passiveLoot)
                );

                continue;
            }

            int randomIndex =
                Random.Range(
                    0,
                    availablePassives.Count
                );

            result.Add(
                availablePassives[randomIndex]
            );

            availablePassives.RemoveAt(
                randomIndex
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

            crate.ServerSetLoot(
                lootPlan[index]
            );

            spawnedCrates.Add(
                networkObject
            );
        }
    }

    private ItemId GetRandomItem(
        ItemId[] possibleItems)
    {
        if (possibleItems == null ||
            possibleItems.Length == 0)
        {
            Debug.LogError(
                "Loot listesi boş."
            );

            return ItemId.None;
        }

        return possibleItems[
            Random.Range(
                0,
                possibleItems.Length
            )
        ];
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