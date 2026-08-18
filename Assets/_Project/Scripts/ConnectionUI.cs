using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject connectionPanel;
    [SerializeField] private GameObject matchCanvas;

    [Header("Connection Controls")]
    [SerializeField] private Button hostButton;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private Button copyCodeButton;
    
    [Header("Connection Information")]
    [SerializeField] private TMP_Text generatedCodeText;
    [SerializeField] private TMP_Text statusText;

    private RelayManager relayManager;
    private NetworkManager networkManager;

    private async void Start()
    {
        ShowConnectionMenu();

        relayManager = RelayManager.Instance;
        networkManager = NetworkManager.Singleton;

        if (relayManager == null)
        {
            SetStatus("RELAY MANAGER NOT FOUND");
            Debug.LogError("ConnectionUI: RelayManager bulunamadı.");
            return;
        }

        if (networkManager == null)
        {
            SetStatus("NETWORK MANAGER NOT FOUND");
            Debug.LogError("ConnectionUI: NetworkManager bulunamadı.");
            return;
        }
        
        hostButton.onClick.AddListener(HandleHostClicked);
        joinButton.onClick.AddListener(HandleJoinClicked);
        disconnectButton.onClick.AddListener(
            HandleDisconnectClicked
        );

        joinCodeInput.onValueChanged.AddListener(
            HandleJoinCodeChanged
        );
        copyCodeButton.onClick.AddListener(HandleCopyCodeClicked);

        relayManager.StateChanged += RefreshUI;

        networkManager.OnClientConnectedCallback +=
            HandleClientConnected;

        networkManager.OnClientDisconnectCallback +=
            HandleClientDisconnected;

        SetStatus("SERVICES INITIALIZING...");
        SetButtonsInteractable(false);

        if (UnityServicesInitializer.Instance == null)
        {
            SetStatus("SERVICES INITIALIZER NOT FOUND");
            return;
        }

        bool servicesReady =
            await UnityServicesInitializer.Instance
                .WaitUntilReadyAsync();

        if (!servicesReady)
        {
            SetStatus("SERVICES INITIALIZATION FAILED");
            return;
        }

        SetStatus("SERVICES READY");
        SetButtonsInteractable(true);
        RefreshJoinButton();
    }
    
    private void HandleCopyCodeClicked()
    {
        if (relayManager == null ||
            networkManager == null ||
            !networkManager.IsHost ||
            string.IsNullOrEmpty(relayManager.CurrentJoinCode))
        {
            SetStatus("NO JOIN CODE TO COPY");
            return;
        }

        GUIUtility.systemCopyBuffer =
            relayManager.CurrentJoinCode;

        SetStatus("JOIN CODE COPIED");
    }
    
    private async void HandleHostClicked()
    {
        SetButtonsInteractable(false);
        SetStatus("CREATING RELAY...");

        string joinCode =
            await relayManager.StartHostWithRelayAsync();

        RefreshUI();

        if (string.IsNullOrEmpty(joinCode))
            return;

        generatedCodeText.text =
            $"JOIN CODE: {joinCode}";

        SetStatus("HOST READY - SHARE JOIN CODE");

        if (disconnectButton != null)
            disconnectButton.gameObject.SetActive(true);
    }

    private async void HandleJoinClicked()
    {
        string joinCode =
            joinCodeInput.text.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            SetStatus("ENTER A JOIN CODE");
            return;
        }

        SetButtonsInteractable(false);
        SetStatus("CONNECTING...");

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

        // Host başlatıldığında Host'un kendi yerel Client'ı
        // hemen bağlanır. Menüyü henüz kapatmıyoruz çünkü
        // join code'u diğer oyuncuyla paylaşması gerekiyor.
        bool isHostsLocalClient =
            networkManager.IsHost &&
            clientId == networkManager.LocalClientId;

        if (isHostsLocalClient)
        {
            SetStatus("HOST READY - WAITING FOR PLAYER");

            if (disconnectButton != null)
                disconnectButton.gameObject.SetActive(true);

            return;
        }

        EnterGame();
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (networkManager == null)
            return;

        // Host çalışmaya devam ederken uzak Client ayrıldı.
        if (networkManager.IsHost &&
            networkManager.IsListening &&
            clientId != networkManager.LocalClientId)
        {
            ShowConnectionMenu();
            generatedCodeText.text =
                $"JOIN CODE: {relayManager.CurrentJoinCode}";

            SetStatus(
                "CLIENT DISCONNECTED - WAITING FOR PLAYER"
            );

            disconnectButton.gameObject.SetActive(true);
            return;
        }

        ShowConnectionMenu();
        SetButtonsInteractable(true);
        RefreshJoinButton();
        SetStatus("DISCONNECTED");
    }

    private void HandleDisconnectClicked()
    {
        if (networkManager != null &&
            networkManager.IsListening)
        {
            networkManager.Shutdown();
        }

        ShowConnectionMenu();
        SetButtonsInteractable(true);
        RefreshJoinButton();
        SetStatus("DISCONNECTED");
    }

    private void HandleJoinCodeChanged(string value)
    {
        RefreshJoinButton();
    }

    private void EnterGame()
    {
        if (connectionPanel != null)
            connectionPanel.SetActive(false);

        if (matchCanvas != null)
            matchCanvas.SetActive(true);

        if (disconnectButton != null)
            disconnectButton.gameObject.SetActive(true);

        SetStatus("CONNECTED");

        Debug.Log("[ConnectionUI] Oyun bağlantısı tamamlandı.");
    }

    private void ShowConnectionMenu()
    {
        if (connectionPanel != null)
            connectionPanel.SetActive(true);

        if (matchCanvas != null)
            matchCanvas.SetActive(false);

        if (disconnectButton != null)
            disconnectButton.gameObject.SetActive(false);

        if (generatedCodeText != null)
            generatedCodeText.text = "JOIN CODE: -";
        
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
                    ? "JOIN CODE: -"
                    : $"JOIN CODE: {joinCode}";
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
                networkManager != null &&
                networkManager.IsHost &&
                networkRunning &&
                !string.IsNullOrEmpty(joinCode);
        }

        if (disconnectButton != null)
        {
            disconnectButton.gameObject.SetActive(
                networkRunning
            );
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
            upperMessage.Contains("DISCONNECTED"))
        {
            // Hata veya bağlantı kesilmesi
            statusText.color =
                new Color32(229, 107, 93, 255);
        }
        else if (upperMessage.Contains("WAITING") ||
                 upperMessage.Contains("INITIALIZING") ||
                 upperMessage.Contains("CREATING") ||
                 upperMessage.Contains("CONNECTING"))
        {
            // Devam eden işlem veya bekleme
            statusText.color =
                new Color32(232, 199, 102, 255);
        }
        else if (upperMessage.Contains("READY") ||
                 upperMessage.Contains("CONNECTED") ||
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

        if (disconnectButton != null)
        {
            disconnectButton.onClick.RemoveListener(
                HandleDisconnectClicked
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
    }
}