using System.Collections;
using UnityEngine;

public class LoadoutUI : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField]
    private ItemSlotUI weaponSlot;

    [SerializeField]
    private ItemSlotUI[] passiveSlots;

    private PlayerLoadout localLoadout;
    private Coroutine bindRoutine;

    private void OnEnable()
    {
        ClearSlots();

        bindRoutine =
            StartCoroutine(
                BindToLocalPlayer()
            );
    }

    private IEnumerator BindToLocalPlayer()
    {
        while (localLoadout == null)
        {
            PlayerLoadout[] loadouts =
                FindObjectsByType<PlayerLoadout>(
                    FindObjectsSortMode.None
                );

            foreach (PlayerLoadout loadout
                     in loadouts)
            {
                if (loadout != null &&
                    loadout.IsSpawned &&
                    loadout.IsOwner)
                {
                    localLoadout = loadout;
                    break;
                }
            }

            if (localLoadout == null)
                yield return null;
        }

        localLoadout.LoadoutChanged -=
            RefreshUI;

        localLoadout.LoadoutChanged +=
            RefreshUI;

        RefreshUI();

        bindRoutine = null;
    }

    private void RefreshUI()
    {
        if (localLoadout == null)
            return;

        if (weaponSlot != null)
        {
            weaponSlot.SetItem(
                localLoadout.EquippedWeapon
            );
        }

        if (passiveSlots == null)
            return;

        for (int index = 0;
             index < passiveSlots.Length;
             index++)
        {
            if (passiveSlots[index] == null)
                continue;

            passiveSlots[index].SetItem(
                localLoadout.GetPassiveItem(
                    index
                )
            );
        }
    }

    private void ClearSlots()
    {
        if (weaponSlot != null)
        {
            weaponSlot.SetItem(null);
        }

        if (passiveSlots == null)
            return;

        foreach (ItemSlotUI slot
                 in passiveSlots)
        {
            if (slot != null)
            {
                slot.SetItem(null);
            }
        }
    }

    private void OnDisable()
    {
        if (bindRoutine != null)
        {
            StopCoroutine(bindRoutine);
            bindRoutine = null;
        }

        if (localLoadout != null)
        {
            localLoadout.LoadoutChanged -=
                RefreshUI;
        }

        localLoadout = null;
    }
}