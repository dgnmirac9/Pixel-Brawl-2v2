using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [Header("Player List")]
    [SerializeField]
    private TMP_Text[] playerSlotTexts;

    [Header("Lobby Information")]
    [SerializeField]
    private TMP_Text lobbyJoinCodeText;

    [SerializeField]
    private TMP_Text lobbyStatusText;

    [Header("Buttons")]
    [SerializeField]
    private Button readyButton;

    [SerializeField]
    private TMP_Text readyButtonText;

    [SerializeField]
    private Button startMatchButton;

    private LobbyManager lobbyManager;

    private readonly Color readyColor =
        new Color32(143, 209, 106, 255);

    private readonly Color waitingColor =
        new Color32(232, 199, 102, 255);

    private readonly Color emptyColor =
        new Color32(150, 150, 150, 255);

    private void OnEnable()
    {
        if (readyButton != null)
        {
            readyButton.onClick.AddListener(
                HandleReadyClicked
            );
        }

        if (startMatchButton != null)
        {
            startMatchButton.onClick.AddListener(
                HandleStartMatchClicked
            );
        }

        StartCoroutine(InitializeLobbyUI());
    }

    private IEnumerator InitializeLobbyUI()
    {
        while (LobbyManager.Instance == null ||
               !LobbyManager.Instance.IsSpawned)
        {
            yield return null;
        }

        lobbyManager = LobbyManager.Instance;

        lobbyManager.LobbyStateChanged -= RefreshUI;
        lobbyManager.LobbyStateChanged += RefreshUI;

        RefreshUI();
    }

    private void HandleReadyClicked()
    {
        if (lobbyManager == null)
            return;

        bool newReadyState =
            !lobbyManager.IsLocalPlayerReady();

        lobbyManager.SetLocalPlayerReady(
            newReadyState
        );
    }

    private void HandleStartMatchClicked()
    {
        if (lobbyManager == null)
            return;

        lobbyManager.TryStartMatch();
    }

    private void RefreshUI()
    {
        if (lobbyManager == null ||
            NetworkManager.Singleton == null)
        {
            return;
        }

        ulong localClientId =
            NetworkManager.Singleton.LocalClientId;

        for (int index = 0;
             index < playerSlotTexts.Length;
             index++)
        {
            TMP_Text slotText =
                playerSlotTexts[index];

            if (slotText == null)
                continue;

            if (index >=
                lobbyManager.ConnectedPlayerCount)
            {
                slotText.text =
                    $"PLAYER {index + 1}: EMPTY";

                slotText.color = emptyColor;
                continue;
            }

            ulong clientId =
                lobbyManager.GetConnectedClientId(
                    index
                );

            bool isLocal =
                clientId == localClientId;

            bool isHost =
                clientId ==
                NetworkManager.ServerClientId;

            bool isReady =
                lobbyManager.IsPlayerReady(
                    clientId
                );

            string playerLabel =
                isLocal
                    ? "YOU"
                    : $"PLAYER {index + 1}";

            if (isHost)
                playerLabel += " (HOST)";

            slotText.text =
                $"{playerLabel}: " +
                (isReady
                    ? "READY"
                    : "NOT READY");

            slotText.color =
                isReady
                    ? readyColor
                    : waitingColor;
        }

        bool localPlayerReady =
            lobbyManager.IsLocalPlayerReady();

        if (readyButtonText != null)
        {
            readyButtonText.text =
                localPlayerReady
                    ? "CANCEL READY"
                    : "READY";
        }

        if (readyButton != null)
        {
            readyButton.interactable =
                !lobbyManager.HasMatchStarted;
        }

        bool localPlayerIsHost =
            NetworkManager.Singleton.IsHost;

        if (startMatchButton != null)
        {
            startMatchButton.gameObject.SetActive(
                localPlayerIsHost
            );

            startMatchButton.interactable =
                localPlayerIsHost &&
                lobbyManager.CanStartMatch &&
                !lobbyManager.HasMatchStarted;
        }

        if (lobbyStatusText != null)
        {
            if (lobbyManager.ConnectedPlayerCount < 2)
            {
                lobbyStatusText.text =
                    "WAITING FOR PLAYERS";
            }
            else if (!lobbyManager.CanStartMatch)
            {
                lobbyStatusText.text =
                    $"READY: " +
                    $"{lobbyManager.ReadyPlayerCount}/" +
                    $"{lobbyManager.ConnectedPlayerCount}";
            }
            else if (localPlayerIsHost)
            {
                lobbyStatusText.text =
                    "ALL PLAYERS READY";
            }
            else
            {
                lobbyStatusText.text =
                    "WAITING FOR HOST";
            }
        }

        if (lobbyJoinCodeText != null)
        {
            string joinCode =
                RelayManager.Instance != null
                    ? RelayManager.Instance
                        .CurrentJoinCode
                    : null;

            lobbyJoinCodeText.text =
                string.IsNullOrEmpty(joinCode)
                    ? "------"
                    : joinCode;
        }
    }

    private void OnDisable()
    {
        if (readyButton != null)
        {
            readyButton.onClick.RemoveListener(
                HandleReadyClicked
            );
        }

        if (startMatchButton != null)
        {
            startMatchButton.onClick.RemoveListener(
                HandleStartMatchClicked
            );
        }

        if (lobbyManager != null)
        {
            lobbyManager.LobbyStateChanged -=
                RefreshUI;
        }
    }
}