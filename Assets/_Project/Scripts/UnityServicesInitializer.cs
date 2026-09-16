using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

#if UNITY_EDITOR
using Unity.Multiplayer.PlayMode;
#endif

public class UnityServicesInitializer : MonoBehaviour
{
    public static UnityServicesInitializer Instance { get; private set; }

    public Task InitializationTask { get; private set; }

    public bool IsReady { get; private set; }

    public string ActiveProfileName { get; private set; }

    [Header("Authentication Profile")] [SerializeField]
    private string defaultProfileName =
        "UGS_Default";

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

        InitializationTask =
            InitializeServicesAsync();
    }

    public async Task<bool> WaitUntilReadyAsync()
    {
        if (InitializationTask != null)
            await InitializationTask;

        return IsReady;
    }

    private async Task InitializeServicesAsync()
    {
        try
        {
            IsReady = false;

            ActiveProfileName =
                ResolveAuthenticationProfile();

            if (UnityServices.State !=
                ServicesInitializationState.Initialized)
            {
                InitializationOptions options =
                    new InitializationOptions();

                options.SetProfile(
                    ActiveProfileName
                );

                await UnityServices.InitializeAsync(
                    options
                );
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance
                    .SignInAnonymouslyAsync();
            }

            IsReady = true;

            Debug.Log(
                "[UGS] Servisler hazır. " +
                $"Profile: {ActiveProfileName} | " +
                $"Authentication Profile: " +
                $"{AuthenticationService.Instance.Profile} | " +
                $"PlayerId: " +
                $"{AuthenticationService.Instance.PlayerId}"
            );
        }
        catch (Exception exception)
        {
            IsReady = false;

            Debug.LogError(
                "[UGS] Servis başlatma veya giriş başarısız."
            );

            Debug.LogException(exception);
        }
    }

    private string ResolveAuthenticationProfile()
    {
#if UNITY_EDITOR
        foreach (string playerTag in
                 CurrentPlayer.ReadOnlyTags())
        {
            if (string.IsNullOrWhiteSpace(
                    playerTag))
            {
                continue;
            }

            if (playerTag.StartsWith(
                    "UGS_",
                    StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log(
                    "[UGS] Multiplayer Play Mode " +
                    $"profili seçildi: {playerTag}"
                );

                return playerTag;
            }
        }
#endif

        string commandLineProfile =
            GetCommandLineProfile();

        if (!string.IsNullOrWhiteSpace(
                commandLineProfile))
        {
            Debug.Log(
                "[UGS] Komut satırı profili seçildi: " +
                commandLineProfile
            );

            return commandLineProfile;
        }

        Debug.Log(
            "[UGS] Varsayılan profil seçildi: " +
            defaultProfileName
        );

        return defaultProfileName;
    }

    private string GetCommandLineProfile()
    {
        string[] arguments =
            Environment.GetCommandLineArgs();

        for (int index = 0;
             index < arguments.Length - 1;
             index++)
        {
            if (!string.Equals(
                    arguments[index],
                    "-ugsProfile",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return arguments[index + 1];
        }

        return null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}