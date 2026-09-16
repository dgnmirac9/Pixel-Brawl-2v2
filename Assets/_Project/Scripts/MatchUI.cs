using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class MatchUI : MonoBehaviour
{
    [Header("Preparation")] [SerializeField]
    private GameObject preparationPanel;

    [SerializeField] private TMP_Text preparationTimerText;

    [Header("Countdown")] [SerializeField] private GameObject countdownPanel;

    [SerializeField] private TMP_Text countdownText;

    [Header("Score UI")] [SerializeField] private TMP_Text team0ScoreText;

    [SerializeField] private TMP_Text roundText;

    [SerializeField] private TMP_Text team1ScoreText;

    [Header("Match End Transition")] [SerializeField]
    private CanvasGroup matchEndFadeCanvasGroup;

    [SerializeField, Min(0f)] private float resultReadDuration = 3f;

    [SerializeField, Min(0f)] private float returningTextDuration = 0.8f;

    [SerializeField, Min(0.01f)] private float matchEndFadeDuration = 0.6f;

    [Header("Match Result")] [SerializeField]
    private GameObject resultPanel;

    [SerializeField] private TMP_Text resultText;

    [SerializeField] private TMP_Text finalScoreText;

    [SerializeField] private TMP_Text ratingChangeText;

    [SerializeField] private TMP_Text ratingProgressText;

    [SerializeField] private TMP_Text rankStatusText;

    [SerializeField] private TMP_Text returnStatusText;

    private MatchManager matchManager;
    private bool rankResultReady;
    private Coroutine matchEndVisualRoutine;

    private void OnEnable()
    {
        StartCoroutine(
            InitializeMatchUI()
        );
    }

    private IEnumerator InitializeMatchUI()
    {
        while (MatchManager.Instance == null ||
               !MatchManager.Instance.IsSpawned)
        {
            yield return null;
        }

        matchManager =
            MatchManager.Instance;

        matchManager.MatchStateChanged -=
            RefreshUI;

        matchManager.MatchStateChanged +=
            RefreshUI;

        matchManager.LocalRankResultProcessed -=
            HandleLocalRankResultProcessed;

        matchManager.LocalRankResultProcessed +=
            HandleLocalRankResultProcessed;

        rankResultReady = false;

        RefreshUI();

        if (!rankResultReady &&
            matchManager.TryGetCachedLocalRankResult(
                out bool localPlayerWon,
                out int previousRating,
                out int newRating,
                out int ratingChange
            ))
        {
            HandleLocalRankResultProcessed(
                localPlayerWon,
                previousRating,
                newRating,
                ratingChange
            );
        }
    }

    private void RefreshUI()
    {
        if (matchManager == null)
            return;

        RefreshScoreUI();
        RefreshPreparationUI();
        RefreshCountdownUI();
        RefreshResultUI();
    }

    private void RefreshPreparationUI()
    {
        bool showPreparation =
            matchManager.CurrentPhase ==
            MatchPhase.Preparation;

        if (preparationPanel != null)
        {
            preparationPanel.SetActive(
                showPreparation
            );
        }

        if (preparationTimerText != null &&
            showPreparation)
        {
            preparationTimerText.text =
                matchManager.PreparationTimeRemaining
                    .ToString();
        }
    }

    private void RefreshScoreUI()
    {
        if (team0ScoreText != null)
        {
            team0ScoreText.text =
                matchManager.Team0Score
                    .ToString();
        }

        if (team1ScoreText != null)
        {
            team1ScoreText.text =
                matchManager.Team1Score
                    .ToString();
        }

        if (roundText != null)
        {
            roundText.text =
                $"ROUND " +
                $"{matchManager.RoundNumber}";
        }
    }

    private void RefreshCountdownUI()
    {
        bool showCountdown =
            matchManager.CurrentPhase ==
            MatchPhase.Countdown;

        if (countdownPanel != null)
        {
            countdownPanel.SetActive(
                showCountdown
            );
        }

        if (countdownText != null &&
            showCountdown)
        {
            countdownText.text =
                matchManager.CountdownValue > 0
                    ? matchManager.CountdownValue
                        .ToString()
                    : "FIGHT!";
        }
    }

    private void RefreshResultUI()
    {
        bool isNewSession =
            matchManager.CurrentPhase ==
            MatchPhase.Lobby ||
            matchManager.CurrentPhase ==
            MatchPhase.Preparation;

        if (isNewSession)
        {
            rankResultReady = false;

            ClearRankResultUI();
            ResetMatchEndVisuals();
        }

        bool showResult =
            matchManager.MatchEnded;

        if (resultPanel != null)
        {
            resultPanel.SetActive(
                showResult
            );
        }

        if (!showResult)
            return;

        RefreshMatchResultHeadline();

        if (finalScoreText != null)
        {
            finalScoreText.text =
                $"{matchManager.Team0Score}" +
                " - " +
                $"{matchManager.Team1Score}";
        }

        if (!rankResultReady)
        {
            ShowWaitingForRankResult();
        }
    }

    private void HandleLocalRankResultProcessed(
        bool localPlayerWon,
        int previousRating,
        int newRating,
        int ratingChange)
    {
        rankResultReady = true;

        if (resultPanel != null)
            resultPanel.SetActive(true);

        ApplyResultHeadline(
            localPlayerWon
        );

        RefreshRankResult(
            previousRating,
            newRating,
            ratingChange
        );

        if (finalScoreText != null &&
            matchManager != null)
        {
            finalScoreText.text =
                $"{matchManager.Team0Score}" +
                " - " +
                $"{matchManager.Team1Score}";
        }

        if (matchEndVisualRoutine == null)
        {
            matchEndVisualRoutine =
                StartCoroutine(
                    RunMatchEndVisualSequence()
                );
        }
    }

    private IEnumerator RunMatchEndVisualSequence()
    {
        if (returnStatusText != null)
        {
            returnStatusText.gameObject
                .SetActive(false);
        }

        if (matchEndFadeCanvasGroup != null)
        {
            matchEndFadeCanvasGroup.alpha = 0f;

            matchEndFadeCanvasGroup
                .blocksRaycasts = false;

            matchEndFadeCanvasGroup
                .interactable = false;
        }

        // Oyuncunun sonucu ve RP değişimini
        // okuyabilmesi için bekler.
        yield return new WaitForSecondsRealtime(
            resultReadDuration
        );

        if (returnStatusText != null)
        {
            returnStatusText.text =
                "RETURNING TO MENU...";

            returnStatusText.gameObject
                .SetActive(true);
        }

        yield return new WaitForSecondsRealtime(
            returningTextDuration
        );

        if (matchEndFadeCanvasGroup != null)
        {
            matchEndFadeCanvasGroup
                .blocksRaycasts = true;

            float elapsed = 0f;

            while (elapsed < matchEndFadeDuration)
            {
                elapsed +=
                    Time.unscaledDeltaTime;

                float progress =
                    Mathf.Clamp01(
                        elapsed /
                        matchEndFadeDuration
                    );

                matchEndFadeCanvasGroup.alpha =
                    Mathf.Lerp(
                        0f,
                        1f,
                        progress
                    );

                yield return null;
            }

            matchEndFadeCanvasGroup.alpha = 1f;
        }

        // Client'ların da fade işlemini bitirmesi için
        // kısa bir güvenlik aralığı.
        yield return new WaitForSecondsRealtime(
            0.2f
        );

        bool localInstanceIsHost =
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsHost;

        if (localInstanceIsHost)
        {
            if (ConnectionUI.Instance != null)
            {
                ConnectionUI.Instance
                    .ReturnToMainMenuAfterMatch();
            }
            else
            {
                Debug.LogError(
                    "[MatchUI] ConnectionUI bulunamadı. " +
                    "Ana menüye dönülemedi."
                );
            }
        }

        matchEndVisualRoutine = null;
    }

    private void ResetMatchEndVisuals()
    {
        if (matchEndVisualRoutine != null)
        {
            StopCoroutine(
                matchEndVisualRoutine
            );

            matchEndVisualRoutine = null;
        }

        if (returnStatusText != null)
        {
            returnStatusText.gameObject
                .SetActive(false);
        }

        if (matchEndFadeCanvasGroup != null)
        {
            matchEndFadeCanvasGroup.alpha = 0f;

            matchEndFadeCanvasGroup
                .blocksRaycasts = false;

            matchEndFadeCanvasGroup
                .interactable = false;
        }
    }

    private void RefreshMatchResultHeadline()
    {
        if (!TryGetLocalPlayerWon(
                out bool localPlayerWon))
        {
            if (resultText != null)
            {
                resultText.text =
                    $"TEAM " +
                    $"{matchManager.WinningTeamId + 1} " +
                    "WINS!";

                resultText.color =
                    Color.white;
            }

            return;
        }

        ApplyResultHeadline(
            localPlayerWon
        );
    }

    private void ApplyResultHeadline(
        bool localPlayerWon)
    {
        if (resultText == null)
            return;

        bool wasForfeit =
            matchManager != null &&
            matchManager.MatchEndedByForfeit;

        if (localPlayerWon)
        {
            resultText.text =
                wasForfeit
                    ? "VICTORY\nOPPONENT LEFT"
                    : "VICTORY";

            resultText.color =
                new Color32(
                    232,
                    199,
                    102,
                    255
                );
        }
        else
        {
            resultText.text =
                wasForfeit
                    ? "DEFEAT\nFORFEIT"
                    : "DEFEAT";

            resultText.color =
                new Color32(
                    229,
                    107,
                    93,
                    255
                );
        }
    }

    private void RefreshRankResult(
        int previousRating,
        int newRating,
        int ratingChange)
    {
        RankTier previousTier =
            RankCalculator.GetTier(
                previousRating
            );

        RankTier currentTier =
            RankCalculator.GetTier(
                newRating
            );

        if (ratingChangeText != null)
        {
            string prefix =
                ratingChange > 0
                    ? "+"
                    : string.Empty;

            ratingChangeText.text =
                $"{prefix}{ratingChange} RP";

            ratingChangeText.color =
                ratingChange >= 0
                    ? new Color32(
                        143,
                        209,
                        106,
                        255
                    )
                    : new Color32(
                        229,
                        107,
                        93,
                        255
                    );
        }

        if (ratingProgressText != null)
        {
            ratingProgressText.text =
                $"{previousRating} RP  >  " +
                $"{newRating} RP";
        }

        if (rankStatusText == null)
            return;

        if (currentTier > previousTier)
        {
            rankStatusText.text =
                "PROMOTED TO " +
                currentTier
                    .ToString()
                    .ToUpperInvariant();

            rankStatusText.color =
                new Color32(
                    232,
                    199,
                    102,
                    255
                );
        }
        else if (currentTier < previousTier)
        {
            rankStatusText.text =
                "DEMOTED TO " +
                currentTier
                    .ToString()
                    .ToUpperInvariant();

            rankStatusText.color =
                new Color32(
                    229,
                    107,
                    93,
                    255
                );
        }
        else
        {
            rankStatusText.text =
                "RANK: " +
                currentTier
                    .ToString()
                    .ToUpperInvariant();

            rankStatusText.color =
                new Color32(
                    255,
                    244,
                    214,
                    255
                );
        }
    }

    private bool TryGetLocalPlayerWon(
        out bool localPlayerWon)
    {
        localPlayerWon = false;

        if (NetworkManager.Singleton == null ||
            NetworkManager.Singleton.LocalClient ==
            null)
        {
            return false;
        }

        NetworkObject localPlayerObject =
            NetworkManager.Singleton
                .LocalClient
                .PlayerObject;

        if (localPlayerObject == null)
            return false;

        FighterHealth localFighter =
            localPlayerObject
                .GetComponent<FighterHealth>();

        if (localFighter == null)
        {
            localFighter =
                localPlayerObject
                    .GetComponentInChildren<
                        FighterHealth>();
        }

        if (localFighter == null)
            return false;

        localPlayerWon =
            localFighter.TeamId ==
            matchManager.WinningTeamId;

        return true;
    }

    private void ShowWaitingForRankResult()
    {
        if (ratingChangeText != null)
        {
            ratingChangeText.text =
                "CALCULATING RP...";

            ratingChangeText.color =
                new Color32(
                    255,
                    244,
                    214,
                    255
                );
        }

        if (ratingProgressText != null)
        {
            ratingProgressText.text =
                string.Empty;
        }

        if (rankStatusText != null)
        {
            rankStatusText.text =
                string.Empty;
        }

        if (returnStatusText != null)
        {
            returnStatusText.gameObject
                .SetActive(false);
        }
    }

    private void ClearRankResultUI()
    {
        if (ratingChangeText != null)
            ratingChangeText.text = string.Empty;

        if (ratingProgressText != null)
            ratingProgressText.text = string.Empty;

        if (rankStatusText != null)
            rankStatusText.text = string.Empty;

        if (returnStatusText != null)
        {
            returnStatusText.gameObject
                .SetActive(false);
        }
    }

    private void OnDisable()
    {
        ResetMatchEndVisuals();
        StopAllCoroutines();

        if (matchManager == null)
            return;

        matchManager.MatchStateChanged -=
            RefreshUI;

        matchManager.LocalRankResultProcessed -=
            HandleLocalRankResultProcessed;
    }
}