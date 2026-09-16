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
        StatusMessage = "ROOM SERVICE IS NOT READY";
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
        SetStatus("CREATING ROOM...");

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
                SetStatus("CONNECTION SERVICE IS UNAVAILABLE");
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
                SetStatus("ROOM COULD NOT BE CREATED");
                return null;
            }

            SetStatus(
                $"ROOM CREATED - CODE: {CurrentJoinCode}"
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
            SetStatus("ROOM COULD NOT BE CREATED");

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
            SetStatus("ENTER A ROOM CODE");
            return false;
        }

        if (!await CanStartRelayAsync())
            return false;

        if (!ValidateNetworkManager())
            return false;

        IsBusy = true;
        SetStatus("JOINING ROOM...");

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
                SetStatus("CONNECTION SERVICE IS UNAVAILABLE");
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
                SetStatus("COULD NOT JOIN THE ROOM");
                return false;
            }
            
            CurrentJoinCode = joinCode;

            SetStatus("CONNECTING TO ROOM...");

            Debug.Log(
                $"[Relay] Client bağlanıyor. Join Code: {joinCode}"
            );

            return true;
        }
        catch (Exception exception)
        {
            SetStatus("COULD NOT JOIN THE ROOM");

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
            SetStatus("ONLINE SERVICES ARE UNAVAILABLE");
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
            SetStatus("ONLINE SERVICES ARE UNAVAILABLE");
            return false;
        }

        return true;
    }

    private bool ValidateNetworkManager()
    {
        if (NetworkManager.Singleton == null)
        {
            SetStatus("CONNECTION SERVICE IS UNAVAILABLE");
            return false;
        }

        if (NetworkManager.Singleton.IsListening)
        {
            SetStatus("YOU ARE ALREADY IN A ROOM");
            return false;
        }

        return true;
    }
    
    public void ResetSessionState()
    {
        CurrentJoinCode = null;
        IsBusy = false;

        SetStatus("ROOM SERVICE READY");
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
