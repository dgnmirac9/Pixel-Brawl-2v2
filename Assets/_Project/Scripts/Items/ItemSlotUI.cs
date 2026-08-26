using UnityEngine;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Image rarityRing;

    [SerializeField]
    private Image itemIcon;

    [Header("Empty Slot")]
    [SerializeField]
    private Color emptyRingColor =
        new Color32(85, 88, 94, 255);

    private ItemDefinition currentItem;

    public ItemDefinition CurrentItem =>
        currentItem;

    public void SetItem(
        ItemDefinition item)
    {
        currentItem = item;

        if (item == null ||
            item.Icon == null)
        {
            if (itemIcon != null)
            {
                itemIcon.sprite = null;
                itemIcon.enabled = false;
            }

            if (rarityRing != null)
            {
                rarityRing.color =
                    emptyRingColor;
            }

            return;
        }

        if (itemIcon != null)
        {
            itemIcon.sprite =
                item.Icon;

            itemIcon.enabled = true;
            itemIcon.preserveAspect = true;
        }

        if (rarityRing != null)
        {
            rarityRing.color =
                GetRarityColor(
                    item.Rarity
                );
        }
    }

    private Color GetRarityColor(
        ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common =>
                new Color32(
                    150, 154, 160, 255
                ),

            ItemRarity.Uncommon =>
                new Color32(
                    80, 180, 105, 255
                ),

            ItemRarity.Rare =>
                new Color32(
                    70, 135, 220, 255
                ),

            ItemRarity.Epic =>
                new Color32(
                    165, 90, 220, 255
                ),

            ItemRarity.Legendary =>
                new Color32(
                    235, 145, 45, 255
                ),

            _ => emptyRingColor
        };
    }
}