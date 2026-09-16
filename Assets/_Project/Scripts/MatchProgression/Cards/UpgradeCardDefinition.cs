using UnityEngine;

[CreateAssetMenu(
    fileName = "UpgradeCard",
    menuName = "Game/Cards/Upgrade Card Definition"
)]
public class UpgradeCardDefinition :
    ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private UpgradeCardId cardId;

    [SerializeField]
    private string displayName;

    [SerializeField, TextArea(2, 5)]
    private string description;

    [SerializeField]
    private Sprite icon;

    [Header("Shop")]
    [SerializeField]
    private ItemRarity rarity;

    [SerializeField, Min(0)]
    private int goldCost = 75;

    [SerializeField, Min(1)]
    private int minimumShopRound = 1;

    [SerializeField, Min(1)]
    private int maximumPurchasesPerMatch = 1;

    [Header("Effect")]
    [SerializeField]
    private UpgradeEffectType effectType;

    [SerializeField]
    private float effectAmount;

    [SerializeField]
    private ItemId grantedItemId =
        ItemId.None;

    public UpgradeCardId Id =>
        cardId;

    public string DisplayName =>
        displayName;

    public string Description =>
        description;

    public Sprite Icon =>
        icon;

    public ItemRarity Rarity =>
        rarity;

    public int GoldCost =>
        goldCost;

    public int MinimumShopRound =>
        minimumShopRound;

    public int MaximumPurchasesPerMatch =>
        maximumPurchasesPerMatch;

    public UpgradeEffectType EffectType =>
        effectType;

    public float EffectAmount =>
        effectAmount;

    public ItemId GrantedItemId =>
        grantedItemId;

    public bool IsItemCard =>
        effectType ==
        UpgradeEffectType.GrantItem;

    private void OnValidate()
    {
        goldCost =
            Mathf.Max(
                0,
                goldCost
            );

        minimumShopRound =
            Mathf.Max(
                1,
                minimumShopRound
            );

        maximumPurchasesPerMatch =
            Mathf.Max(
                1,
                maximumPurchasesPerMatch
            );

        if (effectType ==
            UpgradeEffectType.GrantItem)
        {
            effectAmount = 0f;
        }
        else
        {
            grantedItemId =
                ItemId.None;
        }
    }
}