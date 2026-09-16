using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "UpgradeCardCatalog",
    menuName = "Game/Cards/Upgrade Card Catalog"
)]
public class UpgradeCardCatalog :
    ScriptableObject
{
    [SerializeField]
    private List<UpgradeCardDefinition>
        cards = new();

    public IReadOnlyList<
        UpgradeCardDefinition> Cards =>
        cards;

    public bool TryGetCard(
        UpgradeCardId cardId,
        out UpgradeCardDefinition card)
    {
        foreach (UpgradeCardDefinition
                     candidate in cards)
        {
            if (candidate == null)
                continue;

            if (candidate.Id != cardId)
                continue;

            card = candidate;
            return true;
        }

        card = null;
        return false;
    }

    public UpgradeCardDefinition GetCard(
        UpgradeCardId cardId)
    {
        TryGetCard(
            cardId,
            out UpgradeCardDefinition card
        );

        return card;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        HashSet<UpgradeCardId>
            usedIds = new();

        foreach (UpgradeCardDefinition
                     card in cards)
        {
            if (card == null)
                continue;

            if (card.Id ==
                UpgradeCardId.None)
            {
                Debug.LogWarning(
                    $"[UpgradeCardCatalog] " +
                    $"{card.name} kartının ID değeri None.",
                    card
                );

                continue;
            }

            if (!usedIds.Add(card.Id))
            {
                Debug.LogError(
                    "[UpgradeCardCatalog] Tekrar eden " +
                    $"kart ID bulundu: {card.Id}",
                    card
                );
            }
        }
    }
#endif
}