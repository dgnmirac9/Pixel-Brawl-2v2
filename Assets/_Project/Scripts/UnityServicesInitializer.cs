using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class UnityServicesInitializer : MonoBehaviour
{
    public static UnityServicesInitializer Instance
    {
        get;
        private set;
    }
    public Task InitializationTask { get; private set; }

    public bool IsReady { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializationTask = InitializeServicesAsync();
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

            if (UnityServices.State !=
                ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance
                    .SignInAnonymouslyAsync();
            }

            IsReady = true;

            Debug.Log(
                "[UGS] Servisler hazır. " +
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

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}