using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ItemCatalog",
    menuName = "Game/Items/Item Catalog"
)]
public class ItemCatalog : ScriptableObject
{
    [SerializeField]
    private ItemDefinition[] items;

    private Dictionary<ItemId, ItemDefinition>
        itemLookup;

    public IReadOnlyList<ItemDefinition> Items =>
        items;

    private void OnEnable()
    {
        RebuildLookup();
    }

    private void OnValidate()
    {
        RebuildLookup();
    }

    public ItemDefinition GetItem(ItemId itemId)
    {
        if (itemId == ItemId.None)
            return null;

        EnsureLookup();

        if (itemLookup.TryGetValue(
                itemId,
                out ItemDefinition definition))
        {
            return definition;
        }

        Debug.LogError(
            $"{name}: {itemId} ItemCatalog içinde bulunamadı.",
            this
        );

        return null;
    }

    public bool TryGetItem(
        ItemId itemId,
        out ItemDefinition definition)
    {
        definition = null;

        if (itemId == ItemId.None)
            return false;

        EnsureLookup();

        return itemLookup.TryGetValue(
            itemId,
            out definition
        );
    }

    private void EnsureLookup()
    {
        if (itemLookup == null)
        {
            RebuildLookup();
        }
    }

    private void RebuildLookup()
    {
        itemLookup =
            new Dictionary<ItemId, ItemDefinition>();

        if (items == null)
            return;

        foreach (ItemDefinition item in items)
        {
            if (item == null)
                continue;

            if (item.Id == ItemId.None)
            {
                Debug.LogError(
                    $"{item.name}: ItemId None olamaz.",
                    item
                );

                continue;
            }

            if (!itemLookup.TryAdd(
                    item.Id,
                    item))
            {
                Debug.LogError(
                    $"{name}: Tekrarlı ItemId bulundu: " +
                    $"{item.Id}",
                    this
                );
            }
        }
    }
}