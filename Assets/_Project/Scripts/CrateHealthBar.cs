using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CrateHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private CrateDurability durability;

    [SerializeField]
    private GameObject healthBarRoot;

    [SerializeField]
    private Image[] healthSegments;

    [Header("Colors")]
    [SerializeField]
    private Color fullSegmentColor =
        new Color32(220, 173, 90, 255);

    [SerializeField]
    private Color emptySegmentColor =
        new Color32(45, 42, 38, 210);

    private void Awake()
    {
        if (durability == null)
        {
            durability =
                GetComponent<CrateDurability>();
        }
    }

    private void OnEnable()
    {
        if (durability == null)
            return;

        durability.DurabilityChanged +=
            OnDurabilityChanged;

        RefreshHealthBar(
            durability.CurrentDurability,
            durability.MaximumDurability
        );
    }

    private void OnDisable()
    {
        if (durability != null)
        {
            durability.DurabilityChanged -=
                OnDurabilityChanged;
        }
    }

    private void OnDurabilityChanged(
        int currentDurability,
        int maximumDurability)
    {
        RefreshHealthBar(
            currentDurability,
            maximumDurability
        );
    }

    private void RefreshHealthBar(
        int currentDurability,
        int maximumDurability)
    {
        if (healthBarRoot != null)
        {
            healthBarRoot.SetActive(
                currentDurability > 0
            );
        }

        if (healthSegments == null ||
            healthSegments.Length == 0 ||
            maximumDurability <= 0)
        {
            return;
        }

        float durabilityRatio =
            Mathf.Clamp01(
                (float)currentDurability /
                maximumDurability
            );

        int filledSegmentCount =
            Mathf.CeilToInt(
                durabilityRatio *
                healthSegments.Length
            );

        for (int index = 0;
             index < healthSegments.Length;
             index++)
        {
            Image segment =
                healthSegments[index];

            if (segment == null)
                continue;

            segment.color =
                index < filledSegmentCount
                    ? fullSegmentColor
                    : emptySegmentColor;
        }
    }
}