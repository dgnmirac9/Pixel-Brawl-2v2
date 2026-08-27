using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PlayerLoadout : NetworkBehaviour
{
    public const int PassiveSlotCount = 3;

    [Header("Item Database")] [SerializeField]
    private ItemCatalog itemCatalog;

    private readonly NetworkVariable<ItemId>
        equippedWeaponId = new(
            ItemId.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private readonly NetworkVariable<ItemId>
        passiveSlot0 = new(
            ItemId.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private readonly NetworkVariable<ItemId>
        passiveSlot1 = new(
            ItemId.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private readonly NetworkVariable<ItemId>
        passiveSlot2 = new(
            ItemId.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public event Action LoadoutChanged;

    public event Action<ItemDefinition>
        ItemCollected;

    public ItemCatalog Catalog =>
        itemCatalog;

    public ItemId EquippedWeaponId =>
        equippedWeaponId.Value;

    public ItemDefinition EquippedWeapon =>
        GetDefinition(equippedWeaponId.Value);

    public float TotalDamageReduction
    {
        get
        {
            float totalReduction = 0f;

            for (int index = 0;
                 index < PassiveSlotCount;
                 index++)
            {
                ItemDefinition item =
                    GetPassiveItem(index);

                if (item != null)
                {
                    totalReduction +=
                        item.DamageReduction;
                }
            }

            return Mathf.Clamp(
                totalReduction,
                0f,
                0.75f
            );
        }
    }

    public float TotalMoveSpeedMultiplier
    {
        get
        {
            float multiplier = 1f;

            for (int index = 0;
                 index < PassiveSlotCount;
                 index++)
            {
                ItemDefinition item =
                    GetPassiveItem(index);

                if (item != null)
                {
                    multiplier *=
                        item.MoveSpeedMultiplier;
                }
            }

            return Mathf.Max(
                0.01f,
                multiplier
            );
        }
    }

    public int TotalMaxHealthBonus
    {
        get
        {
            int totalBonus = 0;

            for (int index = 0;
                 index < PassiveSlotCount;
                 index++)
            {
                ItemDefinition item =
                    GetPassiveItem(index);

                if (item != null)
                {
                    totalBonus +=
                        item.MaxHealthBonus;
                }
            }

            return Mathf.Max(0, totalBonus);
        }
    }
    
    public float TotalMaxStaminaBonus
    {
        get
        {
            float totalBonus = 0f;

            for (int index = 0;
                 index < PassiveSlotCount;
                 index++)
            {
                ItemDefinition item =
                    GetPassiveItem(index);

                if (item != null)
                {
                    totalBonus +=
                        item.MaxStaminaBonus;
                }
            }

            return Mathf.Max(
                0f,
                totalBonus
            );
        }
    }
    public float TotalDashStaminaCostMultiplier
    {
        get
        {
            float multiplier = 1f;

            for (int index = 0;
                 index < PassiveSlotCount;
                 index++)
            {
                ItemDefinition item =
                    GetPassiveItem(index);

                if (item != null)
                {
                    multiplier *=
                        item.DashStaminaCostMultiplier;
                }
            }

            return Mathf.Clamp(
                multiplier,
                0.25f,
                2f
            );
        }
    }
    
    public float TotalDashCooldownMultiplier
    {
        get
        {
            float multiplier = 1f;

            for (int index = 0;
                 index < PassiveSlotCount;
                 index++)
            {
                ItemDefinition item =
                    GetPassiveItem(index);

                if (item != null)
                {
                    multiplier *=
                        item.DashCooldownMultiplier;
                }
            }

            return Mathf.Clamp(
                multiplier,
                0.25f,
                2f
            );
        }
    }
    
    public float TotalStaminaRegenerationMultiplier
    {
        get
        {
            float multiplier = 1f;

            for (int index = 0;
                 index < PassiveSlotCount;
                 index++)
            {
                ItemDefinition item =
                    GetPassiveItem(index);

                if (item != null)
                {
                    multiplier *=
                        item.StaminaRegenerationMultiplier;
                }
            }

            return Mathf.Max(
                0.01f,
                multiplier
            );
        }
    }

    public override void OnNetworkSpawn()
    {
        equippedWeaponId.OnValueChanged +=
            OnItemChanged;

        passiveSlot0.OnValueChanged +=
            OnItemChanged;

        passiveSlot1.OnValueChanged +=
            OnItemChanged;

        passiveSlot2.OnValueChanged +=
            OnItemChanged;

        LoadoutChanged?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        equippedWeaponId.OnValueChanged -=
            OnItemChanged;

        passiveSlot0.OnValueChanged -=
            OnItemChanged;

        passiveSlot1.OnValueChanged -=
            OnItemChanged;

        passiveSlot2.OnValueChanged -=
            OnItemChanged;
    }

    public ItemId GetPassiveItemId(int slotIndex)
    {
        return slotIndex switch
        {
            0 => passiveSlot0.Value,
            1 => passiveSlot1.Value,
            2 => passiveSlot2.Value,

            _ => ItemId.None
        };
    }

    public ItemDefinition GetPassiveItem(
        int slotIndex)
    {
        return GetDefinition(
            GetPassiveItemId(slotIndex)
        );
    }

    public bool ServerTryAddItem(
        ItemId itemId)
    {
        if (!IsServer)
            return false;

        if (itemCatalog == null)
        {
            Debug.LogError(
                $"{name}: ItemCatalog atanmamış."
            );

            return false;
        }

        if (!itemCatalog.TryGetItem(
                itemId,
                out ItemDefinition item))
        {
            return false;
        }

        bool itemAdded = false;

        if (item.Type == ItemType.Weapon)
        {
            equippedWeaponId.Value =
                item.Id;

            itemAdded = true;

            Debug.Log(
                $"{name}, " +
                $"{item.DisplayName} kuşandı."
            );
        }
        else if (item.Type ==
                 ItemType.Passive)
        {
            itemAdded =
                ServerTryAddPassive(item);
        }

        if (!itemAdded)
            return false;

        NotifyItemCollectedRpc(
            item.Id
        );

        return true;
    }

    [Rpc(SendTo.Owner)]
    private void NotifyItemCollectedRpc(
        ItemId collectedItemId)
    {
        ItemDefinition item =
            GetDefinition(
                collectedItemId
            );

        if (item == null)
            return;

        ItemCollected?.Invoke(item);
    }

    public void ServerClearLoadout()
    {
        if (!IsServer)
            return;

        equippedWeaponId.Value =
            ItemId.None;

        passiveSlot0.Value =
            ItemId.None;

        passiveSlot1.Value =
            ItemId.None;

        passiveSlot2.Value =
            ItemId.None;
    }

    private bool ServerTryAddPassive(
        ItemDefinition item)
    {
        if (ContainsPassive(item.Id))
        {
            Debug.Log(
                $"{name}, {item.DisplayName} " +
                "itemine zaten sahip."
            );

            return false;
        }

        if (passiveSlot0.Value == ItemId.None)
        {
            passiveSlot0.Value = item.Id;
            return true;
        }

        if (passiveSlot1.Value == ItemId.None)
        {
            passiveSlot1.Value = item.Id;
            return true;
        }

        if (passiveSlot2.Value == ItemId.None)
        {
            passiveSlot2.Value = item.Id;
            return true;
        }

        Debug.Log(
            $"{name}: Pasif item slotları dolu."
        );

        return false;
    }

    private bool ContainsPassive(ItemId itemId)
    {
        return
            passiveSlot0.Value == itemId ||
            passiveSlot1.Value == itemId ||
            passiveSlot2.Value == itemId;
    }

    private ItemDefinition GetDefinition(
        ItemId itemId)
    {
        if (itemCatalog == null)
            return null;

        return itemCatalog.GetItem(itemId);
    }

    private void OnItemChanged(
        ItemId previousValue,
        ItemId newValue)
    {
        LoadoutChanged?.Invoke();
    }
}