using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PlayerMatchProgression :
    NetworkBehaviour
{
    private const int MaximumMatchGold =
        9999;

    private const int DamageRequiredPerGold =
        5;

    [Header("Upgrade Limits")]
    [SerializeField, Min(0)]
    private int maximumBonusHealth = 200;

    [SerializeField, Range(0.1f, 1f)]
    private float minimumAttackCooldownMultiplier =
        0.45f;

    [SerializeField, Range(0.1f, 1f)]
    private float minimumDashCooldownMultiplier =
        0.45f;

    [SerializeField, Range(0f, 1f)]
    private float maximumCriticalChanceBonus =
        0.50f;

    [SerializeField, Min(0f)]
    private float maximumCriticalDamageBonus =
        2f;

    private readonly NetworkVariable<int>
        matchGold = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private readonly NetworkVariable<int>
        bonusMaxHealth = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private readonly NetworkVariable<float>
        attackCooldownMultiplier = new(
            1f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private readonly NetworkVariable<float>
        dashCooldownMultiplier = new(
            1f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private readonly NetworkVariable<float>
        criticalChanceBonus = new(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private readonly NetworkVariable<float>
        criticalDamageBonus = new(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private readonly Dictionary<
        UpgradeCardId,
        int> purchaseCounts = new();

    private int unconvertedDamage;

    public int Gold =>
        matchGold.Value;

    public int BonusMaxHealth =>
        bonusMaxHealth.Value;

    public float AttackCooldownMultiplier =>
        attackCooldownMultiplier.Value;

    public float DashCooldownMultiplier =>
        dashCooldownMultiplier.Value;

    public float CriticalChanceBonus =>
        criticalChanceBonus.Value;

    public float CriticalDamageBonus =>
        criticalDamageBonus.Value;

    public event Action<int, int>
        GoldChanged;

    public event Action
        ProgressionChanged;

    public override void OnNetworkSpawn()
    {
        matchGold.OnValueChanged +=
            HandleGoldChanged;

        bonusMaxHealth.OnValueChanged +=
            HandleIntegerUpgradeChanged;

        attackCooldownMultiplier.OnValueChanged +=
            HandleFloatUpgradeChanged;

        dashCooldownMultiplier.OnValueChanged +=
            HandleFloatUpgradeChanged;

        criticalChanceBonus.OnValueChanged +=
            HandleFloatUpgradeChanged;

        criticalDamageBonus.OnValueChanged +=
            HandleFloatUpgradeChanged;

        if (IsServer)
        {
            ServerResetForMatch();
        }

        GoldChanged?.Invoke(
            matchGold.Value,
            matchGold.Value
        );

        ProgressionChanged?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        matchGold.OnValueChanged -=
            HandleGoldChanged;

        bonusMaxHealth.OnValueChanged -=
            HandleIntegerUpgradeChanged;

        attackCooldownMultiplier.OnValueChanged -=
            HandleFloatUpgradeChanged;

        dashCooldownMultiplier.OnValueChanged -=
            HandleFloatUpgradeChanged;

        criticalChanceBonus.OnValueChanged -=
            HandleFloatUpgradeChanged;

        criticalDamageBonus.OnValueChanged -=
            HandleFloatUpgradeChanged;

        purchaseCounts.Clear();
    }

    public void ServerResetForMatch()
    {
        if (!IsServer)
            return;

        matchGold.Value = 0;
        unconvertedDamage = 0;

        bonusMaxHealth.Value = 0;
        attackCooldownMultiplier.Value = 1f;
        dashCooldownMultiplier.Value = 1f;
        criticalChanceBonus.Value = 0f;
        criticalDamageBonus.Value = 0f;

        purchaseCounts.Clear();

        ProgressionChanged?.Invoke();

        Debug.Log(
            "[MatchProgression] Oyuncunun maçlık " +
            "Gold ve buff değerleri sıfırlandı. " +
            $"ClientId: {OwnerClientId}"
        );
    }

    public void ServerAddGold(
        int amount)
    {
        if (!IsServer ||
            amount <= 0)
        {
            return;
        }

        int previousGold =
            matchGold.Value;

        matchGold.Value =
            Mathf.Clamp(
                previousGold + amount,
                0,
                MaximumMatchGold
            );

        Debug.Log(
            "[MatchGold] Gold kazanıldı. " +
            $"ClientId: {OwnerClientId} | " +
            $"Miktar: +{amount} | " +
            $"Toplam: {matchGold.Value}"
        );
    }

    public void ServerRegisterDamageDealt(
        int actualDamage)
    {
        if (!IsServer ||
            actualDamage <= 0)
        {
            return;
        }

        unconvertedDamage +=
            actualDamage;

        int earnedGold =
            unconvertedDamage /
            DamageRequiredPerGold;

        unconvertedDamage %=
            DamageRequiredPerGold;

        if (earnedGold <= 0)
            return;

        ServerAddGold(
            earnedGold
        );
    }

    public bool ServerTrySpendGold(
        int cost)
    {
        if (!IsServer ||
            cost < 0)
        {
            return false;
        }

        if (matchGold.Value < cost)
        {
            return false;
        }

        matchGold.Value -= cost;

        return true;
    }

    public int ServerGetPurchaseCount(
        UpgradeCardId cardId)
    {
        if (!IsServer)
            return 0;

        return purchaseCounts.TryGetValue(
            cardId,
            out int count
        )
            ? count
            : 0;
    }

    public bool ServerCanPurchaseCard(
        UpgradeCardDefinition card)
    {
        if (!IsServer ||
            card == null ||
            card.Id == UpgradeCardId.None)
        {
            return false;
        }

        if (matchGold.Value <
            card.GoldCost)
        {
            return false;
        }

        int currentPurchaseCount =
            ServerGetPurchaseCount(
                card.Id
            );

        if (currentPurchaseCount >=
            card.MaximumPurchasesPerMatch)
        {
            return false;
        }

        return ServerCanApplyCardEffect(
            card
        );
    }

    public bool ServerTryPurchaseCard(
        UpgradeCardDefinition card)
    {
        if (!ServerCanPurchaseCard(card))
            return false;

        if (!ServerTrySpendGold(
                card.GoldCost))
        {
            return false;
        }

        ApplyCardEffectOnServer(
            card
        );

        int previousPurchaseCount =
            ServerGetPurchaseCount(
                card.Id
            );

        purchaseCounts[card.Id] =
            previousPurchaseCount + 1;

        Debug.Log(
            "[MatchProgression] Kart satın alındı. " +
            $"ClientId: {OwnerClientId} | " +
            $"Kart: {card.DisplayName} | " +
            $"Fiyat: {card.GoldCost} | " +
            $"Satın alma sayısı: " +
            $"{purchaseCounts[card.Id]}"
        );

        return true;
    }

    private bool ServerCanApplyCardEffect(
        UpgradeCardDefinition card)
    {
        float amount =
            card.EffectAmount;

        switch (card.EffectType)
        {
            case UpgradeEffectType.MaxHealthBonus:
                return
                    amount > 0f &&
                    bonusMaxHealth.Value <
                    maximumBonusHealth;

            case UpgradeEffectType.AttackCooldownMultiplier:
                return
                    amount > 0f &&
                    amount < 1f &&
                    attackCooldownMultiplier.Value >
                    minimumAttackCooldownMultiplier;

            case UpgradeEffectType.DashCooldownMultiplier:
                return
                    amount > 0f &&
                    amount < 1f &&
                    dashCooldownMultiplier.Value >
                    minimumDashCooldownMultiplier;

            case UpgradeEffectType.CriticalChanceBonus:
                return
                    amount > 0f &&
                    criticalChanceBonus.Value <
                    maximumCriticalChanceBonus;

            case UpgradeEffectType.CriticalDamageBonus:
                return
                    amount > 0f &&
                    criticalDamageBonus.Value <
                    maximumCriticalDamageBonus;

            // Item kartlarını PlayerLoadout ile
            // bağladığımız aşamada açacağız.
            case UpgradeEffectType.GrantItem:
                return false;

            default:
                return false;
        }
    }

    private void ApplyCardEffectOnServer(
        UpgradeCardDefinition card)
    {
        float amount =
            card.EffectAmount;

        switch (card.EffectType)
        {
            case UpgradeEffectType.MaxHealthBonus:
                bonusMaxHealth.Value =
                    Mathf.Min(
                        maximumBonusHealth,
                        bonusMaxHealth.Value +
                        Mathf.RoundToInt(amount)
                    );
                break;

            case UpgradeEffectType.AttackCooldownMultiplier:
                attackCooldownMultiplier.Value =
                    Mathf.Max(
                        minimumAttackCooldownMultiplier,
                        attackCooldownMultiplier.Value *
                        amount
                    );
                break;

            case UpgradeEffectType.DashCooldownMultiplier:
                dashCooldownMultiplier.Value =
                    Mathf.Max(
                        minimumDashCooldownMultiplier,
                        dashCooldownMultiplier.Value *
                        amount
                    );
                break;

            case UpgradeEffectType.CriticalChanceBonus:
                criticalChanceBonus.Value =
                    Mathf.Min(
                        maximumCriticalChanceBonus,
                        criticalChanceBonus.Value +
                        amount
                    );
                break;

            case UpgradeEffectType.CriticalDamageBonus:
                criticalDamageBonus.Value =
                    Mathf.Min(
                        maximumCriticalDamageBonus,
                        criticalDamageBonus.Value +
                        amount
                    );
                break;
        }
    }

    private void HandleGoldChanged(
        int previousGold,
        int newGold)
    {
        GoldChanged?.Invoke(
            previousGold,
            newGold
        );
    }

    private void HandleIntegerUpgradeChanged(
        int previousValue,
        int newValue)
    {
        ProgressionChanged?.Invoke();
    }

    private void HandleFloatUpgradeChanged(
        float previousValue,
        float newValue)
    {
        ProgressionChanged?.Invoke();
    }
}