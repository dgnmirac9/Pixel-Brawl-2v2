using System;
using UnityEngine;
using Unity.Services.Authentication;

public class RankService : MonoBehaviour
{
    public static RankService Instance
    {
        get;
        private set;
    }

    private const string BaseSaveKey =
        "PixelBrawlRankProfile";

    private string activeSaveKey;

    public RankProfileData Profile
    {
        get;
        private set;
    }
    
    public int LastRatingChange
    {
        get;
        private set;
    }
    
    public bool IsReady
    {
        get;
        private set;
    }

    public string ActivePlayerId
    {
        get;
        private set;
    }

    public event Action<RankProfileData>
        ProfileChanged;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }
    
    private async void Start()
    {
        IsReady = false;

        if (UnityServicesInitializer.Instance == null)
        {
            Debug.LogError(
                "[Rank] UnityServicesInitializer bulunamadı."
            );

            return;
        }

        bool servicesReady =
            await UnityServicesInitializer.Instance
                .WaitUntilReadyAsync();

        if (!servicesReady)
        {
            Debug.LogError(
                "[Rank] Unity Services hazır olmadığı için " +
                "rank profili yüklenemedi."
            );

            return;
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogError(
                "[Rank] Oyuncu Authentication ile " +
                "giriş yapmamış."
            );

            return;
        }

        ActivePlayerId =
            AuthenticationService.Instance.PlayerId;

        activeSaveKey =
            $"{BaseSaveKey}_{ActivePlayerId}";

        LoadProfile();

        IsReady = true;

        ProfileChanged?.Invoke(Profile);

        Debug.Log(
            "[Rank] Oyuncuya özel profil yüklendi. " +
            $"PlayerId: {ActivePlayerId} | " +
            $"SaveKey: {activeSaveKey} | " +
            $"Rating: {Profile.rating}"
        );
    }

    public int ApplyMatchResult(
        int opponentRating,
        bool playerWon)
    {
        if (!IsReady || Profile == null)
        {
            Debug.LogWarning(
                "[Rank] Profil henüz hazır değil. " +
                "Maç sonucu uygulanmadı."
            );

            return 0;
        }
        
        int ratingChange =
            RankCalculator.CalculateRatingChange(
                Profile.rating,
                opponentRating,
                playerWon
            );

        LastRatingChange = ratingChange;
        
        Profile.rating = Mathf.Max(
            0,
            Profile.rating + ratingChange
        );

        Profile.matchesPlayed++;

        if (playerWon)
        {
            Profile.wins++;
            Profile.winStreak++;
        }
        else
        {
            Profile.losses++;
            Profile.winStreak = 0;
        }

        Profile.highestRating = Mathf.Max(
            Profile.highestRating,
            Profile.rating
        );

        SaveProfile();

        ProfileChanged?.Invoke(Profile);

        Debug.Log(
            $"[Rank] Sonuç: " +
            $"{(playerWon ? "Win" : "Loss")} | " +
            $"Değişim: {ratingChange} | " +
            $"Yeni Rating: {Profile.rating}"
        );
        
        return ratingChange;
    }

    private void LoadProfile()
    {
        if (string.IsNullOrWhiteSpace(
                activeSaveKey))
        {
            Debug.LogError(
                "[Rank] Aktif save key oluşturulmamış."
            );

            return;
        }

        if (!PlayerPrefs.HasKey(activeSaveKey))
        {
            Profile = new RankProfileData();

            SaveProfile();

            return;
        }

        string json =
            PlayerPrefs.GetString(
                activeSaveKey
            );

        Profile =
            JsonUtility.FromJson<RankProfileData>(
                json
            );

        if (Profile == null)
            Profile = new RankProfileData();
    }

    private void SaveProfile()
    {
        if (Profile == null ||
            string.IsNullOrWhiteSpace(activeSaveKey))
        {
            return;
        }

        string json =
            JsonUtility.ToJson(Profile);

        PlayerPrefs.SetString(
            activeSaveKey,
            json
        );

        PlayerPrefs.Save();
    }

    [ContextMenu("Debug/Simulate Loss")]
    private void SimulateLoss()
    {
        ApplyMatchResult(
            Profile.rating,
            false
        );
    }

    [ContextMenu("Debug/Reset Rank Profile")]
    private void ResetRankProfile()
    {
        Profile = new RankProfileData();

        SaveProfile();

        ProfileChanged?.Invoke(Profile);

        Debug.Log(
            "[Rank] Profil sıfırlandı."
        );
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}