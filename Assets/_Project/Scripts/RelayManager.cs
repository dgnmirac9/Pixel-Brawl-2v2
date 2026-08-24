using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    [Header("Relay Settings")]
    [SerializeField] private int maxClientConnections = 3;

    public string CurrentJoinCode { get; private set; }
    public string StatusMessage { get; private set; }
    public bool IsBusy { get; private set; }
    public event Action StateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        StatusMessage = "Relay hazır değil.";
    }

    public async Task<string> StartHostWithRelayAsync()
    {
        if (IsBusy)
            return null;

        if (!await CanStartRelayAsync())
            return null;

        if (!ValidateNetworkManager())
            return null;

        IsBusy = true;
        SetStatus("Relay allocation oluşturuluyor...");

        try
        {
            Allocation allocation =
                await RelayService.Instance.CreateAllocationAsync(
                    maxClientConnections
                );

            CurrentJoinCode =
                await RelayService.Instance.GetJoinCodeAsync(
                    allocation.AllocationId
                );

            UnityTransport transport =
                NetworkManager.Singleton
                    .GetComponent<UnityTransport>();

            if (transport == null)
            {
                SetStatus("UnityTransport bulunamadı.");
                Debug.LogError("[Relay] UnityTransport bulunamadı.");
                return null;
            }

            RelayServerData relayServerData =
                allocation.ToRelayServerData("dtls");

            transport.SetRelayServerData(relayServerData);

            bool hostStarted =
                NetworkManager.Singleton.StartHost();

            if (!hostStarted)
            {
                CurrentJoinCode = null;
                SetStatus("Netcode Host başlatılamadı.");
                return null;
            }

            SetStatus(
                $"Host başladı. Join Code: {CurrentJoinCode}"
            );

            Debug.Log(
                $"[Relay] Host başladı. " +
                $"Join Code: {CurrentJoinCode}"
            );

            return CurrentJoinCode;
        }
        catch (Exception exception)
        {
            CurrentJoinCode = null;
            SetStatus($"Relay Host hatası: {exception.Message}");

            Debug.LogError("[Relay] Host başlatılamadı.");
            Debug.LogException(exception);

            return null;
        }
        finally
        {
            IsBusy = false;
            StateChanged?.Invoke();
        }
    }

    public async Task<bool> StartClientWithRelayAsync(
        string joinCode)
    {
        if (IsBusy)
            return false;

        joinCode = joinCode?.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            SetStatus("Join code boş olamaz.");
            return false;
        }

        if (!await CanStartRelayAsync())
            return false;

        if (!ValidateNetworkManager())
            return false;

        IsBusy = true;
        SetStatus("Relay allocation'a katılınıyor...");

        try
        {
            JoinAllocation joinAllocation =
                await RelayService.Instance.JoinAllocationAsync(
                    joinCode
                );

            UnityTransport transport =
                NetworkManager.Singleton
                    .GetComponent<UnityTransport>();

            if (transport == null)
            {
                SetStatus("UnityTransport bulunamadı.");
                Debug.LogError("[Relay] UnityTransport bulunamadı.");
                return false;
            }

            RelayServerData relayServerData =
                joinAllocation.ToRelayServerData("dtls");

            transport.SetRelayServerData(relayServerData);

            bool clientStarted =
                NetworkManager.Singleton.StartClient();

            if (!clientStarted)
            {
                SetStatus("Netcode Client başlatılamadı.");
                return false;
            }
            
            CurrentJoinCode = joinCode;

            SetStatus("Client bağlantısı başlatıldı.");

            Debug.Log(
                $"[Relay] Client bağlanıyor. Join Code: {joinCode}"
            );

            return true;
        }
        catch (Exception exception)
        {
            SetStatus($"Relay Client hatası: {exception.Message}");

            Debug.LogError("[Relay] Client başlatılamadı.");
            Debug.LogException(exception);

            return false;
        }
        finally
        {
            IsBusy = false;
            StateChanged?.Invoke();
        }
    }

    private async Task<bool> CanStartRelayAsync()
    {
        if (UnityServicesInitializer.Instance == null)
        {
            SetStatus("UnityServicesInitializer bulunamadı.");
            Debug.LogError(
                "[Relay] UnityServicesInitializer bulunamadı."
            );

            return false;
        }

        bool servicesReady =
            await UnityServicesInitializer.Instance
                .WaitUntilReadyAsync();

        if (!servicesReady)
        {
            SetStatus("Unity Services hazır değil.");
            return false;
        }

        return true;
    }

    private bool ValidateNetworkManager()
    {
        if (NetworkManager.Singleton == null)
        {
            SetStatus("NetworkManager bulunamadı.");
            return false;
        }

        if (NetworkManager.Singleton.IsListening)
        {
            SetStatus("Network zaten çalışıyor.");
            return false;
        }

        return true;
    }

    private void SetStatus(string message)
    {
        StatusMessage = message;

        Debug.Log($"[Relay] {message}");

        StateChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}