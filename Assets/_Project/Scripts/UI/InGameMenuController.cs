using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class InGameMenuController : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject settingsOverlay;
    [SerializeField] private GameObject matchCanvas;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button leaveMatchButton;

    private void Awake()
    {
        IsOpen = false;

        if (settingsOverlay != null)
        {
            settingsOverlay.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(Close);
        }

        if (leaveMatchButton != null)
        {
            leaveMatchButton.onClick.AddListener(
                HandleLeaveMatchClicked
            );
        }
    }

    private void OnDisable()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(Close);
        }

        if (leaveMatchButton != null)
        {
            leaveMatchButton.onClick.RemoveListener(
                HandleLeaveMatchClicked
            );
        }

        IsOpen = false;
        
        if (settingsOverlay != null)
        {
            settingsOverlay.SetActive(false);
        }
    }

    private void Update()
    {
        if (IsOpen && !IsMatchScreenActive())
        {
            Close();
            return;
        }

        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (IsOpen)
        {
            Close();
            return;
        }

        if (CanOpen())
        {
            Open();
        }
    }

    private bool CanOpen()
    {
        NetworkManager networkManager =
            NetworkManager.Singleton;

        if (networkManager == null ||
            !networkManager.IsListening)
        {
            return false;
        }

        return IsMatchScreenActive();
    }

    private bool IsMatchScreenActive()
    {
        return matchCanvas != null &&
               matchCanvas.activeInHierarchy;
    }

    public void Open()
    {
        if (settingsOverlay == null)
            return;

        IsOpen = true;
        settingsOverlay.SetActive(true);
    }

    public void Close()
    {
        IsOpen = false;

        if (settingsOverlay != null)
        {
            settingsOverlay.SetActive(false);
        }
    }

    private void HandleLeaveMatchClicked()
    {
        Close();

        if (MatchManager.Instance != null)
        {
            bool requestSent =
                MatchManager.Instance
                    .RequestLocalForfeit();

            if (requestSent)
            {
                Debug.Log(
                    "[InGameMenu] Forfeit isteği " +
                    "server'a gönderildi."
                );

                return;
            }
        }

        Debug.LogWarning(
            "[InGameMenu] Aktif maç bulunamadı. " +
            "Normal disconnect uygulanıyor."
        );

        if (ConnectionUI.Instance != null)
        {
            ConnectionUI.Instance
                .LeaveCurrentMatch();
        }
        else
        {
            Debug.LogError(
                "[InGameMenu] ConnectionUI bulunamadı."
            );
        }
    }
}