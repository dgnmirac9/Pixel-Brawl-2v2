using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance { get; private set; }

    private NetworkList<ulong> connectedClientIds;
    private NetworkList<ulong> readyClientIds;

    private readonly NetworkVariable<bool> matchStarted = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public event Action LobbyStateChanged;
    public event Action MatchStarted;

    public int ConnectedPlayerCount =>
        connectedClientIds?.Count ?? 0;

    public int ReadyPlayerCount =>
        readyClientIds?.Count ?? 0;

    public bool HasMatchStarted =>
        matchStarted.Value;

    public bool CanStartMatch =>
        ConnectedPlayerCount >= 2 &&
        ReadyPlayerCount == ConnectedPlayerCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        connectedClientIds = new NetworkList<ulong>();
        readyClientIds = new NetworkList<ulong>();
    }

    public override void OnNetworkSpawn()
    {
        connectedClientIds.OnListChanged +=
            HandleLobbyListChanged;

        readyClientIds.OnListChanged +=
            HandleLobbyListChanged;

        matchStarted.OnValueChanged +=
            HandleMatchStartedChanged;

        if (IsServer)
        {
            connectedClientIds.Clear();
            readyClientIds.Clear();
            matchStarted.Value = false;

            NetworkManager.OnClientConnectedCallback +=
                HandleClientConnected;

            NetworkManager.OnClientDisconnectCallback +=
                HandleClientDisconnected;

            foreach (ulong clientId
                     in NetworkManager.ConnectedClientsIds)
            {
                AddConnectedClient(clientId);
            }
        }

        LobbyStateChanged?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        connectedClientIds.OnListChanged -=
            HandleLobbyListChanged;

        readyClientIds.OnListChanged -=
            HandleLobbyListChanged;

        matchStarted.OnValueChanged -=
            HandleMatchStartedChanged;

        if (NetworkManager != null && IsServer)
        {
            NetworkManager.OnClientConnectedCallback -=
                HandleClientConnected;

            NetworkManager.OnClientDisconnectCallback -=
                HandleClientDisconnected;
        }
    }

    public ulong GetConnectedClientId(int index)
    {
        if (index < 0 ||
            index >= ConnectedPlayerCount)
        {
            return ulong.MaxValue;
        }

        return connectedClientIds[index];
    }

    public bool IsPlayerReady(ulong clientId)
    {
        return readyClientIds != null &&
               readyClientIds.Contains(clientId);
    }

    public bool IsLocalPlayerReady()
    {
        if (NetworkManager == null)
            return false;

        return IsPlayerReady(
            NetworkManager.LocalClientId
        );
    }

    public void SetLocalPlayerReady(bool ready)
    {
        if (!IsSpawned)
            return;

        SetPlayerReadyRpc(ready);
    }

    public void TryStartMatch()
    {
        if (!IsSpawned)
            return;

        TryStartMatchRpc();
    }

    [Rpc(
        SendTo.Server,
        RequireOwnership = false
    )]
    private void SetPlayerReadyRpc(
        bool ready,
        RpcParams rpcParams = default)
    {
        ulong senderClientId =
            rpcParams.Receive.SenderClientId;

        if (!connectedClientIds.Contains(
                senderClientId))
        {
            return;
        }

        if (ready)
        {
            if (!readyClientIds.Contains(
                    senderClientId))
            {
                readyClientIds.Add(
                    senderClientId
                );
            }
        }
        else
        {
            readyClientIds.Remove(
                senderClientId
            );
        }
    }

    [Rpc(
        SendTo.Server,
        RequireOwnership = false
    )]
    private void TryStartMatchRpc(
        RpcParams rpcParams = default)
    {
        ulong senderClientId =
            rpcParams.Receive.SenderClientId;

        Debug.Log(
            "[LobbyManager] Start Match isteği. " +
            $"Sender: {senderClientId} | " +
            $"Connected: {ConnectedPlayerCount} | " +
            $"Ready: {ReadyPlayerCount}"
        );

        if (senderClientId !=
            NetworkManager.ServerClientId)
        {
            Debug.LogWarning(
                "[LobbyManager] Start Match reddedildi: " +
                "İsteği Host göndermedi."
            );

            return;
        }

        if (!CanStartMatch)
        {
            Debug.LogWarning(
                "[LobbyManager] Start Match reddedildi: " +
                $"Connected={ConnectedPlayerCount}, " +
                $"Ready={ReadyPlayerCount}"
            );

            return;
        }

        MatchManager matchManager =
            MatchManager.Instance;

        if (matchManager == null ||
            !matchManager.IsSpawned)
        {
            Debug.LogError(
                "[LobbyManager] MatchManager bulunamadı " +
                "veya NetworkObject spawn olmadı."
            );

            return;
        }

        // Önceki network oturumundan phase kaldıysa
        // yeni lobby durumuna geri getir.
        if (matchManager.CurrentPhase !=
            MatchPhase.Lobby)
        {
            Debug.LogWarning(
                "[LobbyManager] Eski match phase bulundu: " +
                $"{matchManager.CurrentPhase}. Resetleniyor."
            );

            matchManager.ServerResetForNewSession();
        }

        bool matchSuccessfullyStarted =
            matchManager.ServerBeginMatch();

        if (!matchSuccessfullyStarted)
        {
            Debug.LogError(
                "[LobbyManager] ServerBeginMatch false döndürdü."
            );

            return;
        }

        matchStarted.Value = true;

        Debug.Log(
            "[LobbyManager] Maç başarıyla başlatıldı."
        );
    }

    private void HandleClientConnected(
        ulong clientId)
    {
        AddConnectedClient(clientId);
    }

    private void AddConnectedClient(
        ulong clientId)
    {
        if (!connectedClientIds.Contains(
                clientId))
        {
            connectedClientIds.Add(clientId);
        }
    }

    private void HandleClientDisconnected(
        ulong clientId)
    {
        readyClientIds.Remove(clientId);
        connectedClientIds.Remove(clientId);
    }

    private void HandleLobbyListChanged(
        NetworkListEvent<ulong> changeEvent)
    {
        LobbyStateChanged?.Invoke();
    }

    private void HandleMatchStartedChanged(
        bool previousValue,
        bool newValue)
    {
        LobbyStateChanged?.Invoke();

        if (newValue)
            MatchStarted?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}