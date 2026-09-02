using System.Collections;
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

    [Header("Rarity Hint")]
    [SerializeField]
    private SpriteRenderer rarityGlowRenderer;

    [SerializeField, Min(0.1f)]
    private float glowPulseSpeed = 3f;

    [SerializeField, Range(0f, 1f)]
    private float minimumGlowAlpha = 0.08f;

    [SerializeField, Range(0f, 1f)]
    private float maximumGlowAlpha = 0.55f;

    private CrateDurability durability;
    private Coroutine glowRoutine;
    
    private BreakableObject breakableObject;
    private ItemId lootItemId =
        ItemId.None;
    
    private readonly NetworkVariable<ItemRarity>
        lootRarity = new(
            ItemRarity.Common,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private void Awake()
    {
        breakableObject =
            GetComponent<BreakableObject>();

        durability =
            GetComponent<CrateDurability>();

        if (rarityGlowRenderer != null)
        {
            rarityGlowRenderer.gameObject.SetActive(
                false
            );
        }
    }

    public override void OnNetworkSpawn()
    {
        lootRarity.OnValueChanged +=
            OnLootRarityChanged;

        if (durability != null)
        {
            durability.DurabilityChanged +=
                OnDurabilityChanged;
        }

        if (IsServer &&
            breakableObject != null)
        {
            breakableObject.BrokenOnServer +=
                OnCrateBrokenOnServer;
        }

        RefreshRarityGlow();
    }

    public override void OnNetworkDespawn()
    {
        lootRarity.OnValueChanged -=
            OnLootRarityChanged;

        if (durability != null)
        {
            durability.DurabilityChanged -=
                OnDurabilityChanged;
        }

        if (breakableObject != null)
        {
            breakableObject.BrokenOnServer -=
                OnCrateBrokenOnServer;
        }

        StopRarityGlow();
    }

    public void ServerSetLoot(
        ItemId newLootItemId,
        ItemRarity newLootRarity)
    {
        if (!IsServer)
            return;

        lootItemId =
            newLootItemId;

        lootRarity.Value =
            newLootRarity;
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
    
    private void OnDurabilityChanged(
    int currentDurability,
    int maximumDurability)
{
    RefreshRarityGlow();
}

private void OnLootRarityChanged(
    ItemRarity previousRarity,
    ItemRarity newRarity)
{
    RefreshRarityGlow();
}

private void RefreshRarityGlow()
{
    bool shouldGlow =
        durability != null &&
        durability.CurrentDurability > 0 &&
        durability.CurrentDurability <
        durability.MaximumDurability &&
        breakableObject != null &&
        !breakableObject.IsBroken;

    if (shouldGlow)
    {
        StartRarityGlow();
    }
    else
    {
        StopRarityGlow();
    }
}

private void StartRarityGlow()
{
    if (rarityGlowRenderer == null ||
        glowRoutine != null)
    {
        return;
    }

    rarityGlowRenderer.gameObject.SetActive(
        true
    );

    glowRoutine =
        StartCoroutine(
            RarityGlowRoutine()
        );
}

private void StopRarityGlow()
{
    if (glowRoutine != null)
    {
        StopCoroutine(glowRoutine);
        glowRoutine = null;
    }

    if (rarityGlowRenderer != null)
    {
        rarityGlowRenderer.gameObject.SetActive(
            false
        );
    }
}

private IEnumerator RarityGlowRoutine()
{
    while (true)
    {
        float pulse =
            (Mathf.Sin(
                 Time.time * glowPulseSpeed
             ) + 1f) * 0.5f;

        float rarityStrength =
            GetRarityStrength(
                lootRarity.Value
            );

        float targetMaximumAlpha =
            Mathf.Lerp(
                minimumGlowAlpha,
                maximumGlowAlpha,
                rarityStrength
            );

        Color glowColor =
            GetRarityColor(
                lootRarity.Value
            );

        glowColor.a =
            Mathf.Lerp(
                minimumGlowAlpha,
                targetMaximumAlpha,
                pulse
            );

        rarityGlowRenderer.color =
            glowColor;

        yield return null;
    }
}

private float GetRarityStrength(
    ItemRarity rarity)
{
    return rarity switch
    {
        ItemRarity.Common => 0.15f,
        ItemRarity.Uncommon => 0.3f,
        ItemRarity.Rare => 0.55f,
        ItemRarity.Epic => 0.78f,
        ItemRarity.Legendary => 1f,
        _ => 0.15f
    };
}

private Color GetRarityColor(
    ItemRarity rarity)
{
    return rarity switch
    {
        ItemRarity.Common =>
            new Color32(180, 180, 180, 255),

        ItemRarity.Uncommon =>
            new Color32(88, 190, 105, 255),

        ItemRarity.Rare =>
            new Color32(76, 150, 235, 255),

        ItemRarity.Epic =>
            new Color32(174, 92, 235, 255),

        ItemRarity.Legendary =>
            new Color32(245, 190, 70, 255),

        _ => Color.white
    };
}
}