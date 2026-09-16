using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ConnectionUI : MonoBehaviour
{
    public static ConnectionUI Instance
    {
        get;
        private set;
    }
    
    [Header("Panels")] [SerializeField] private GameObject preGameRoot;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject connectionView;
    [SerializeField] private GameObject waitingRoomPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject matchCanvas;

    [Header("Main Menu Controls")] [SerializeField]
    private Button playButton;

    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button connectionBackButton;
    [SerializeField] private Button settingsBackButton;

    [Header("Waiting Room Controls")]
    [SerializeField] private Button waitingRoomBackButton;

    [Header("Connection Controls")] [SerializeField]
    private Button hostButton;

    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button copyCodeButton;
    [SerializeField] private TMP_Text copyButtonText;

    [SerializeField, Min(0f)] private float copyCooldownDuration = 3f;
    private Coroutine copyCooldownRoutine;
    private bool copyCooldownActive;
    private string defaultCopyButtonText = "COPY";

    [Header("Connection Information")] [SerializeField]
    private TMP_Text generatedCodeText;

    [SerializeField] private TMP_Text statusText;

    private LobbyManager lobbyManager;
    private Coroutine disconnectRoutine;
    private bool disconnecting;
    private Coroutine lobbyInitializationRoutine;
    private RelayManager relayManager;
    private NetworkManager networkManager;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    
    private async void Start()
    {
        if (copyButtonText != null)
        {
            defaultCopyButtonText =
                copyButtonText.text;
        }

        ShowMainMenu();

        relayManager = RelayManager.Instance;
        networkManager = NetworkManager.Singleton;

        if (relayManager == null)
        {
            SetStatus("CONNECTION SERVICE IS UNAVAILABLE");
            Debug.LogError("ConnectionUI: RelayManager bulunamadı.");
            return;
        }

        if (networkManager == null)
        {
            SetStatus("CONNECTION SERVICE IS UNAVAILABLE");
            Debug.LogError("ConnectionUI: NetworkManager bulunamadı.");
            return;
        }

        if (playButton != null)
        {
            playButton.onClick.AddListener(
                ShowConnectionMenu
            );
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(
                ShowSettingsMenu
            );
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(
                HandleQuitClicked
            );
        }

        if (connectionBackButton != null)
        {
            connectionBackButton.onClick.AddListener(
                HandleConnectionBackClicked
            );
        }

        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.AddListener(
                ShowMainMenu
            );
        }

        if (waitingRoomBackButton != null)
        {
            waitingRoomBackButton.onClick.AddListener(
                HandleWaitingRoomBackClicked
            );
        }

        hostButton.onClick.AddListener(HandleHostClicked);
        joinButton.onClick.AddListener(HandleJoinClicked);

        joinCodeInput.onValueChanged.AddListener(
            HandleJoinCodeChanged
        );
        copyCodeButton.onClick.AddListener(HandleCopyCodeClicked);

        relayManager.StateChanged += RefreshUI;

        networkManager.OnClientConnectedCallback +=
            HandleClientConnected;

        networkManager.OnClientDisconnectCallback +=
            HandleClientDisconnected;

        SetStatus("PREPARING ONLINE SERVICES...");
        SetButtonsInteractable(false);

        if (UnityServicesInitializer.Instance == null)
        {
            SetStatus("ONLINE SERVICES ARE UNAVAILABLE");
            return;
        }

        bool servicesReady =
            await UnityServicesInitializer.Instance
                .WaitUntilReadyAsync();

        if (!servicesReady)
        {
            SetStatus("ONLINE SERVICES ARE UNAVAILABLE");
            return;
        }

        SetStatus("ONLINE SERVICES READY");
        SetButtonsInteractable(true);
        RefreshJoinButton();
    }

    private void HandleCopyCodeClicked()
    {
        if (copyCooldownActive)
            return;

        if (relayManager == null ||
            networkManager == null ||
            !networkManager.IsListening ||
            string.IsNullOrEmpty(
                relayManager.CurrentJoinCode))
        {
            SetStatus(
                "NO ROOM CODE TO COPY"
            );

            return;
        }

        GUIUtility.systemCopyBuffer =
            relayManager.CurrentJoinCode;

        SetStatus(
            "ROOM CODE COPIED"
        );

        copyCooldownRoutine =
            StartCoroutine(
                RunCopyButtonCooldown()
            );
    }

    private IEnumerator
        RunCopyButtonCooldown()
    {
        copyCooldownActive = true;

        if (copyCodeButton != null)
        {
            copyCodeButton.interactable =
                false;
        }

        if (copyButtonText != null)
        {
            copyButtonText.text =
                "COPIED";
        }

        yield return new WaitForSecondsRealtime(
            copyCooldownDuration
        );

        copyCooldownActive = false;
        copyCooldownRoutine = null;

        if (copyButtonText != null)
        {
            copyButtonText.text =
                defaultCopyButtonText;
        }

        RefreshUI();
    }

    private async void HandleHostClicked()
    {
        SetButtonsInteractable(false);
        SetStatus("CREATING ROOM...");

        string joinCode =
            await relayManager.StartHostWithRelayAsync();

        RefreshUI();

        if (string.IsNullOrEmpty(joinCode))
            return;

        generatedCodeText.text =
            $"ROOM CODE: {joinCode}";

        SetStatus("ROOM READY - SHARE THE CODE");
    }

    private async void HandleJoinClicked()
    {
        string joinCode =
            joinCodeInput.text.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            SetStatus("ENTER A ROOM CODE");
            return;
        }

        SetButtonsInteractable(false);
        SetStatus("JOINING ROOM...");

        bool clientStarted =
            await relayManager.StartClientWithRelayAsync(
                joinCode
            );

        RefreshUI();

        if (!clientStarted)
        {
            SetButtonsInteractable(true);
            RefreshJoinButton();
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (networkManager == null)
            return;

        if (networkManager.IsHost)
        {
            bool isLocalHost =
                clientId ==
                networkManager.LocalClientId;

            SetStatus(
                isLocalHost
                    ? "ROOM READY - WAITING FOR PLAYER"
                    : "PLAYER JOINED - PREPARING MATCH"
            );
        }
        else
        {
            SetStatus(
                "ROOM JOINED - WAITING FOR HOST"
            );
        }

        BeginWaitingRoomTransition();
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (disconnecting)
            return;

        if (networkManager == null)
            return;

        // Host çalışmaya devam ederken uzak Client ayrıldı.
        if (networkManager.IsHost &&
            networkManager.IsListening &&
            clientId != networkManager.LocalClientId)
        {
            ShowConnectionMenu();
            generatedCodeText.text =
                $"ROOM CODE: {relayManager.CurrentJoinCode}";

            SetStatus(
                "PLAYER LEFT - WAITING FOR PLAYER"
            );
            
            return;
        }

        relayManager?.ResetSessionState();

        ShowMainMenu();
        SetButtonsInteractable(true);
        RefreshJoinButton();
        SetStatus("LEFT ROOM");
    }
    
    public void ReturnToMainMenuAfterMatch()
    {
        if (disconnectRoutine != null)
            return;

        disconnectRoutine =
            StartCoroutine(
                ShutdownNetworkRoutine(false)
            );
    }
    
    public void LeaveCurrentMatch()
    {
        if (disconnectRoutine != null)
            return;

        disconnectRoutine =
            StartCoroutine(
                ShutdownNetworkRoutine(true)
            );
    }

    private void HandleWaitingRoomBackClicked()
    {
        if (disconnectRoutine != null ||
            disconnecting)
        {
            return;
        }

        disconnectRoutine =
            StartCoroutine(
                ShutdownNetworkRoutine(
                    true,
                    true
                )
            );
    }

    private IEnumerator ShutdownNetworkRoutine(
        bool showDisconnectingScreen,
        bool returnToConnectionMenu = false)
    {
        disconnecting = true;

        float disconnectStartedAt =
            Time.unscaledTime;
        
        if (showDisconnectingScreen)
        {
            ShowConnectionMenu();

            SetButtonsInteractable(false);
            SetStatus("LEAVING ROOM...");
        }

        if (lobbyInitializationRoutine != null)
        {
            StopCoroutine(
                lobbyInitializationRoutine
            );

            lobbyInitializationRoutine = null;
        }

        if (lobbyManager != null)
        {
            lobbyManager.MatchStarted -=
                EnterGame;

            lobbyManager = null;
        }

        if (networkManager != null &&
            networkManager.IsListening)
        {
            networkManager.Shutdown();

            while (networkManager != null &&
                   networkManager.IsListening)
            {
                yield return null;
            }

            yield return null;
        }

        // Mesajın fark edilebilmesi için en az
        // 0.4 saniye görünmesini sağlar.
        float visibleTime =
            Time.unscaledTime -
            disconnectStartedAt;

        float remainingTime =
            0.4f - visibleTime;

        if (remainingTime > 0f)
        {
            yield return new WaitForSecondsRealtime(
                remainingTime
            );
        }

        relayManager?.ResetSessionState();

        if (returnToConnectionMenu)
        {
            ShowConnectionMenu();
        }
        else
        {
            ShowMainMenu();
        }

        SetButtonsInteractable(true);
        RefreshJoinButton();
        SetStatus("LEFT ROOM");

        disconnecting = false;
        disconnectRoutine = null;
    }

    private void HandleJoinCodeChanged(string value)
    {
        RefreshJoinButton();
    }

    private void BeginWaitingRoomTransition()
    {
        if (lobbyInitializationRoutine != null)
            return;

        lobbyInitializationRoutine =
            StartCoroutine(
                EnterWaitingRoomWhenReady()
            );
    }

    private IEnumerator EnterWaitingRoomWhenReady()
    {
        while (networkManager != null &&
               networkManager.IsListening &&
               (LobbyManager.Instance == null ||
                !LobbyManager.Instance.IsSpawned))
        {
            yield return null;
        }

        lobbyInitializationRoutine = null;

        if (networkManager == null ||
            !networkManager.IsListening ||
            LobbyManager.Instance == null ||
            !LobbyManager.Instance.IsSpawned)
        {
            yield break;
        }

        if (lobbyManager != null)
        {
            lobbyManager.MatchStarted -=
                EnterGame;
        }

        lobbyManager = LobbyManager.Instance;

        lobbyManager.MatchStarted -=
            EnterGame;

        lobbyManager.MatchStarted +=
            EnterGame;

        ShowWaitingRoom();

        if (lobbyManager.HasMatchStarted)
            EnterGame();
    }

    private void ShowWaitingRoom()
    {
        if (preGameRoot != null)
            preGameRoot.SetActive(true);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (connectionView != null)
            connectionView.SetActive(false);

        if (waitingRoomPanel != null)
            waitingRoomPanel.SetActive(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (matchCanvas != null)
            matchCanvas.SetActive(false);
    }

    private void EnterGame()
    {   
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        
        if (connectionView != null)
            connectionView.SetActive(false);

        if (waitingRoomPanel != null)
            waitingRoomPanel.SetActive(false);

        if (preGameRoot != null)
            preGameRoot.SetActive(false);

        if (matchCanvas != null)
            matchCanvas.SetActive(true);

        SetStatus("MATCH STARTED");

        Debug.Log(
            "[ConnectionUI] Waiting Room tamamlandı."
        );
    }

    private void ShowMainMenu()
    {
        if (preGameRoot != null)
            preGameRoot.SetActive(true);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (connectionView != null)
            connectionView.SetActive(false);

        if (waitingRoomPanel != null)
            waitingRoomPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (matchCanvas != null)
            matchCanvas.SetActive(false);
    }

    private void ShowSettingsMenu()
    {
        if (preGameRoot != null)
            preGameRoot.SetActive(true);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (connectionView != null)
            connectionView.SetActive(false);

        if (waitingRoomPanel != null)
            waitingRoomPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        if (matchCanvas != null)
            matchCanvas.SetActive(false);
    }

    private void HandleConnectionBackClicked()
    {
        bool networkRunning =
            networkManager != null &&
            networkManager.IsListening;

        if (networkRunning)
            return;

        ShowMainMenu();
    }

    private void HandleQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }

    private void ShowConnectionMenu()
    {
        if (preGameRoot != null)
            preGameRoot.SetActive(true);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (connectionView != null)
            connectionView.SetActive(true);

        if (waitingRoomPanel != null)
            waitingRoomPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (matchCanvas != null)
            matchCanvas.SetActive(false);

        if (generatedCodeText != null)
            generatedCodeText.text = "ROOM CODE: -";

        if (copyCodeButton != null)
            copyCodeButton.interactable = false;
    }

    private void RefreshUI()
    {
        if (relayManager == null)
            return;

        string joinCode =
            relayManager.CurrentJoinCode;

        if (!string.IsNullOrEmpty(
                relayManager.StatusMessage))
        {
            ApplyStatus(
                relayManager.StatusMessage
            );
        }

        if (generatedCodeText != null)
        {
            generatedCodeText.text =
                string.IsNullOrEmpty(joinCode)
                    ? "ROOM CODE: -"
                    : $"ROOM CODE: {joinCode}";
        }

        bool networkRunning =
            networkManager != null &&
            networkManager.IsListening;

        bool canStart =
            !relayManager.IsBusy &&
            !networkRunning;

        if (hostButton != null)
            hostButton.interactable = canStart;

        if (joinCodeInput != null)
            joinCodeInput.interactable = canStart;

        RefreshJoinButton();

        if (copyCodeButton != null)
        {
            copyCodeButton.interactable =
                networkRunning &&
                !copyCooldownActive &&
                !string.IsNullOrEmpty(joinCode);
        }
    }

    private void RefreshJoinButton()
    {
        if (joinButton == null ||
            joinCodeInput == null)
        {
            return;
        }

        bool networkRunning =
            networkManager != null &&
            networkManager.IsListening;

        bool relayBusy =
            relayManager != null &&
            relayManager.IsBusy;

        joinButton.interactable =
            !networkRunning &&
            !relayBusy &&
            !string.IsNullOrWhiteSpace(joinCodeInput.text);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (hostButton != null)
            hostButton.interactable = interactable;

        if (joinButton != null)
            joinButton.interactable = interactable;

        if (joinCodeInput != null)
            joinCodeInput.interactable = interactable;
    }

    private void SetStatus(string message)
    {
        ApplyStatus(message);
    }

    private void ApplyStatus(string message)
    {
        if (statusText == null)
            return;

        statusText.text = message;

        string upperMessage =
            message.ToUpperInvariant();

        if (upperMessage.Contains("FAILED") ||
            upperMessage.Contains("ERROR") ||
            upperMessage.Contains("INVALID") ||
            upperMessage.Contains("NOT FOUND") ||
            upperMessage.Contains("COULD NOT") ||
            upperMessage.Contains("UNAVAILABLE"))
        {
            // Hata veya bağlantı kesilmesi
            statusText.color =
                new Color32(229, 107, 93, 255);
        }
        else if (upperMessage.Contains("WAITING") ||
                 upperMessage.Contains("PREPARING") ||
                 upperMessage.Contains("INITIALIZING") ||
                 upperMessage.Contains("CREATING") ||
                 upperMessage.Contains("CONNECTING") ||
                 upperMessage.Contains("JOINING") ||
                 upperMessage.Contains("LEAVING"))
        {
            // Devam eden işlem veya bekleme
            statusText.color =
                new Color32(232, 199, 102, 255);
        }
        else if (upperMessage.Contains("READY") ||
                 upperMessage.Contains("CONNECTED") ||
                 upperMessage.Contains("JOINED") ||
                 upperMessage.Contains("CREATED") ||
                 upperMessage.Contains("COPIED"))
        {
            // Başarılı durum
            statusText.color =
                new Color32(143, 209, 106, 255);
        }
        else
        {
            // Normal bilgi
            statusText.color =
                new Color32(255, 244, 214, 255);
        }
    }

    private void OnDestroy()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(
                ShowConnectionMenu
            );
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(
                ShowSettingsMenu
            );
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(
                HandleQuitClicked
            );
        }

        if (connectionBackButton != null)
        {
            connectionBackButton.onClick.RemoveListener(
                HandleConnectionBackClicked
            );
        }

        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.RemoveListener(
                ShowMainMenu
            );
        }

        
        if (waitingRoomBackButton != null)
        {
            waitingRoomBackButton.onClick.RemoveListener(
                HandleWaitingRoomBackClicked
            );
        }
        
        if (hostButton != null)
        {
            hostButton.onClick.RemoveListener(
                HandleHostClicked
            );
        }

        if (joinButton != null)
        {
            joinButton.onClick.RemoveListener(
                HandleJoinClicked
            );
        }

        if (joinCodeInput != null)
        {
            joinCodeInput.onValueChanged.RemoveListener(
                HandleJoinCodeChanged
            );
        }

        if (relayManager != null)
            relayManager.StateChanged -= RefreshUI;

        if (networkManager != null)
        {
            networkManager.OnClientConnectedCallback -=
                HandleClientConnected;

            networkManager.OnClientDisconnectCallback -=
                HandleClientDisconnected;
        }

        if (copyCodeButton != null)
        {
            copyCodeButton.onClick.RemoveListener(
                HandleCopyCodeClicked
            );
        }

        if (lobbyManager != null)
        {
            lobbyManager.MatchStarted -=
                EnterGame;
        }
        
        if (Instance == this)
            Instance = null;
    }
}
