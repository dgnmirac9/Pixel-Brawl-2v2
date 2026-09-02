using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PlayerColorIdentity :
    NetworkBehaviour
{
    [Header("Character Visual")] 
    [SerializeField] private ColorSwap_HeroKnight colorSwap;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        ApplyPlayerColor();
    }

    private void ApplyPlayerColor()
    {
        if (colorSwap == null)
        {
            Debug.LogError(
                "PlayerColorIdentity: " +
                "ColorSwap_HeroKnight atanmamış.",
                this
            );

            return;
        }

        bool usesAlternativeColor =
            OwnerClientId % 2 == 1;

        if (usesAlternativeColor)
        {
            // Client: Yellow renkleri uygula.
            colorSwap.SwapDemoColors();
        }
        else
        {
            // Host: Orijinal renkleri koru.
            colorSwap.ClearAllSpritesColors();
        }
    }
}