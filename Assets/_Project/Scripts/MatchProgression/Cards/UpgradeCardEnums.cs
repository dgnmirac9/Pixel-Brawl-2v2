public enum UpgradeCardId : ushort
{
    None = 0,

    Vitality = 1,
    QuickRecovery = 2,
    RapidStrikes = 3,
    SharpenedInstinct = 4,
    CriticalMastery = 5,

    Endurance = 10,
    SecondWind = 11,
    ReinforcedArmor = 12,

    HeavySwordCard = 100,
    LuckyRapierCard = 101,
    SwiftBootsCard = 102,
    VitalityRubyCard = 103,

    SwordOfGodCard = 200
}

public enum UpgradeEffectType : byte
{
    None = 0,

    MaxHealthBonus = 1,
    AttackCooldownMultiplier = 2,
    DashCooldownMultiplier = 3,
    CriticalChanceBonus = 4,
    CriticalDamageBonus = 5,
    MaxStaminaBonus = 6,
    StaminaRegenerationMultiplier = 7,
    DamageReductionBonus = 8,

    GrantItem = 100
}