using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class ItemPickup : NetworkBehaviour
{
    [Header("Item Database")]
    [SerializeField] private ItemCatalog itemCatalog;
    
    [Header("World Visual")]
    [SerializeField, Min(0.1f)]
    private float targetWorldSize = 1f;
    
    private readonly NetworkVariable<ItemId>
        itemId = new(
            ItemId.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private SpriteRenderer spriteRenderer;
    private Collider2D pickupCollider;
    private bool collected;

    public ItemId ItemId =>
        itemId.Value;

    private void Awake()
    {
        spriteRenderer =
            GetComponent<SpriteRenderer>();

        pickupCollider =
            GetComponent<Collider2D>();
    }

    public override void OnNetworkSpawn()
    {
        itemId.OnValueChanged +=
            OnItemIdChanged;

        ApplyItemVisual(itemId.Value);
    }

    public override void OnNetworkDespawn()
    {
        itemId.OnValueChanged -=
            OnItemIdChanged;
    }

    public void ServerSetItem(ItemId newItemId)
    {
        if (!IsServer)
            return;

        if (newItemId == ItemId.None)
        {
            Debug.LogError(
                $"{name}: Pickup için ItemId None verildi."
            );

            return;
        }

        collected = false;

        if (pickupCollider != null)
        {
            pickupCollider.enabled = true;
        }

        itemId.Value = newItemId;

        ApplyItemVisual(newItemId);
    }

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        if (!IsServer ||
            collected ||
            itemId.Value == ItemId.None)
        {
            return;
        }

        if (MatchManager.Instance == null ||
            MatchManager.Instance.CurrentPhase !=
            MatchPhase.Preparation)
        {
            return;
        }

        PlayerLoadout loadout =
            other.GetComponentInParent<
                PlayerLoadout>();

        if (loadout == null)
            return;

        bool itemAdded =
            loadout.ServerTryAddItem(
                itemId.Value
            );

        if (!itemAdded)
            return;

        collected = true;

        if (pickupCollider != null)
        {
            pickupCollider.enabled = false;
        }

        Debug.Log(
            $"{loadout.name}, " +
            $"{itemId.Value} pickup'ını topladı."
        );

        NetworkObject.Despawn(true);
    }

    private void OnItemIdChanged(
        ItemId previousValue,
        ItemId newValue)
    {
        ApplyItemVisual(newValue);
    }

    private void ApplyItemVisual(
        ItemId newItemId)
    {
        if (spriteRenderer == null)
            return;

        if (itemCatalog == null)
        {
            spriteRenderer.enabled = false;

            Debug.LogError(
                $"{name}: ItemCatalog atanmamış."
            );

            return;
        }

        ItemDefinition item =
            itemCatalog.GetItem(newItemId);

        if (item == null ||
            item.Icon == null)
        {
            spriteRenderer.enabled = false;
            return;
        }

        spriteRenderer.sprite =
            item.Icon;

        spriteRenderer.enabled = true;
        
        NormalizeVisualSize();
    }
    private void NormalizeVisualSize()
    {
        if (spriteRenderer == null ||
            spriteRenderer.sprite == null)
        {
            return;
        }

        Bounds spriteBounds =
            spriteRenderer.sprite.bounds;

        float largestDimension =
            Mathf.Max(
                spriteBounds.size.x,
                spriteBounds.size.y
            );

        if (largestDimension <= 0f)
            return;

        float requiredScale =
            targetWorldSize /
            largestDimension;

        transform.localScale =
            new Vector3(
                requiredScale,
                requiredScale,
                1f
            );
    }
}