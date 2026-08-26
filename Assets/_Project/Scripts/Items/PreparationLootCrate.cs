using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(BreakableObject))]
public class PreparationLootCrate : NetworkBehaviour
{
    [Header("Pickup")]
    [SerializeField]
    private ItemPickup itemPickupPrefab;

    [SerializeField]
    private Vector2 pickupSpawnOffset =
        new Vector2(0f, 0.35f);

    private BreakableObject breakableObject;
    private ItemId lootItemId =
        ItemId.None;

    private void Awake()
    {
        breakableObject =
            GetComponent<BreakableObject>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer &&
            breakableObject != null)
        {
            breakableObject.BrokenOnServer +=
                OnCrateBrokenOnServer;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (breakableObject != null)
        {
            breakableObject.BrokenOnServer -=
                OnCrateBrokenOnServer;
        }
    }

    public void ServerSetLoot(
        ItemId newLootItemId)
    {
        if (!IsServer)
            return;

        lootItemId =
            newLootItemId;
    }

    private void OnCrateBrokenOnServer(
        Vector2 impactPoint)
    {
        if (!IsServer)
            return;

        if (lootItemId == ItemId.None)
        {
            Debug.LogError(
                $"{name}: Sandığa loot atanmamış."
            );

            return;
        }

        if (itemPickupPrefab == null)
        {
            Debug.LogError(
                $"{name}: ItemPickup prefabı atanmamış."
            );

            return;
        }

        Vector3 spawnPosition =
            transform.position +
            (Vector3)pickupSpawnOffset;

        ItemPickup pickup = Instantiate(
            itemPickupPrefab,
            spawnPosition,
            Quaternion.identity
        );

        NetworkObject pickupNetworkObject =
            pickup.GetComponent<NetworkObject>();

        pickupNetworkObject.Spawn();

        pickup.ServerSetItem(
            lootItemId
        );

        Debug.Log(
            $"{name} kırıldı. " +
            $"Düşen item: {lootItemId}"
        );
    }
}