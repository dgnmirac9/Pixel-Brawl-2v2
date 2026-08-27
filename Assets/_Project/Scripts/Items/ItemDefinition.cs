using UnityEngine;

public enum ItemId : ushort
{
    None = 0,

    BalancedSword = 1,
    DuelistSword = 2,
    HeavySword = 3,
    QuickfangSword = 4,
    ExecutionerSword = 5,
    LuckyRapier = 6,

    IronShield = 100,
    SwiftBoots = 101,
    VitalityRuby = 102,
    StaminaCrystal = 103,
    DashFeather = 104
}

public enum ItemType : byte
{
    Weapon = 0,
    Passive = 1
}

public enum ItemRarity : byte
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4
}

[CreateAssetMenu(
    fileName = "ItemDefinition",
    menuName = "Game/Items/Item Definition"
)]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity")] [SerializeField] private ItemId itemId;

    [SerializeField] private string displayName;

    [SerializeField, TextArea(2, 5)] private string description;

    [SerializeField] private Sprite icon;

    [SerializeField] private ItemType itemType;

    [SerializeField] private ItemRarity rarity;

    [Header("World Pickup Visual")] [SerializeField, Range(0.5f, 2f)]
    private float worldVisualScale = 1f;

    [Header("Weapon Stats")] [SerializeField, Min(0)]
    private int attackDamage;

    [SerializeField, Min(0.05f)] private float attackCooldown = 0.4f;

    [SerializeField, Range(0f, 1f)] private float criticalChance = 0.15f;

    [SerializeField, Min(1f)] private float criticalDamageMultiplier = 1.5f;

    [Header("Passive Stats")] [SerializeField, Range(0f, 0.9f)]
    private float damageReduction;

    [SerializeField, Min(0.01f)] private float moveSpeedMultiplier = 1f;

    [SerializeField, Min(0)] private int maxHealthBonus;

    [SerializeField, Min(0)] private float maxStaminaBonus;

    [SerializeField, Min(0.01f)] private float dashStaminaCostMultiplier = 1f;

    [SerializeField, Min(0.01f)] private float dashCooldownMultiplier = 1f;

    [SerializeField, Min(0.01f)] private float staminaRegenerationMultiplier = 1f;

    public ItemId Id => itemId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public ItemType Type => itemType;
    public ItemRarity Rarity => rarity;

    public float WorldVisualScale =>
        worldVisualScale;

    public int AttackDamage => attackDamage;
    public float AttackCooldown => attackCooldown;
    public float CriticalChance => criticalChance;

    public float StaminaRegenerationMultiplier =>
        staminaRegenerationMultiplier;

    public float CriticalDamageMultiplier =>
        criticalDamageMultiplier;

    public float DamageReduction =>
        damageReduction;

    public float MoveSpeedMultiplier =>
        moveSpeedMultiplier;

    public int MaxHealthBonus =>
        maxHealthBonus;

    public float MaxStaminaBonus =>
        maxStaminaBonus;

    public float DashStaminaCostMultiplier =>
        dashStaminaCostMultiplier;

    public float DashCooldownMultiplier =>
        dashCooldownMultiplier;
}