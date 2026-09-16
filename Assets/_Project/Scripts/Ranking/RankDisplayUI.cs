using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RankDisplayUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private RankService rankService;

    [SerializeField]
    private TMP_Text playerNameText;

    [SerializeField]
    private TMP_Text rankNameText;

    [SerializeField]
    private TMP_Text ratingText;

    [SerializeField]
    private Image rankIcon;
    
    [Header("Rank Sprites")]
    [SerializeField] private Sprite bronzeRankSprite;
    [SerializeField] private Sprite silverRankSprite;
    [SerializeField] private Sprite goldRankSprite;
    [SerializeField] private Sprite diamondRankSprite;

    private void Start()
    {
        if (rankService == null)
            rankService = RankService.Instance;

        if (rankService == null)
        {
            Debug.LogError(
                "RankDisplayUI: RankService bulunamadı."
            );

            return;
        }

        rankService.ProfileChanged +=
            HandleProfileChanged;

        HandleProfileChanged(
            rankService.Profile
        );
    }

    private void HandleProfileChanged(
        RankProfileData profile)
    {
        if (profile == null)
            return;

        RankTier tier =
            RankCalculator.GetTier(
                profile.rating
            );

        Color tierColor = GetTierColor(tier);

        if (playerNameText != null)
            playerNameText.text = "PLAYER";

        if (rankNameText != null)
        {
            rankNameText.text =
                tier.ToString().ToUpperInvariant();

            rankNameText.color = tierColor;
        }

        if (ratingText != null)
        {
            ratingText.text =
                $"{profile.rating} RP";

            ratingText.color = tierColor;
        }

        if (rankIcon != null)
        {
            Sprite tierSprite =
                GetTierSprite(tier);

            rankIcon.sprite = tierSprite;
            rankIcon.color = Color.white;
            rankIcon.enabled = tierSprite != null;
        }
    }

    private Sprite GetTierSprite(RankTier tier)
    {
        switch (tier)
        {
            case RankTier.Bronze:
                return bronzeRankSprite;

            case RankTier.Silver:
                return silverRankSprite;

            case RankTier.Gold:
                return goldRankSprite;

            case RankTier.Diamond:
                return diamondRankSprite;

            default:
                return bronzeRankSprite;
        }
    }
    
    private Color GetTierColor(RankTier tier)
    {
        switch (tier)
        {
            case RankTier.Bronze:
                return new Color32(176, 106, 67, 255);

            case RankTier.Silver:
                return new Color32(185, 198, 210, 255);

            case RankTier.Gold:
                return new Color32(232, 190, 70, 255);

            case RankTier.Diamond:
                return new Color32(130, 105, 235, 255);

            default:
                return Color.white;
        }
    }

    private void OnDestroy()
    {
        if (rankService != null)
        {
            rankService.ProfileChanged -=
                HandleProfileChanged;
        }
    }
}