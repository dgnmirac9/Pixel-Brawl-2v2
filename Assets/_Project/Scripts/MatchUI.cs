using System.Collections;
using TMPro;
using UnityEngine;

public class MatchUI : MonoBehaviour
{
    [Header("Preparation")]
    [SerializeField]
    private GameObject preparationPanel;

    [SerializeField]
    private TMP_Text preparationTitleText;

    [SerializeField]
    private TMP_Text preparationCountdownText;

    [Header("Countdown")]
    [SerializeField]
    private GameObject countdownPanel;

    [SerializeField]
    private TMP_Text countdownText;

    [Header("Score UI")]
    [SerializeField]
    private TMP_Text team0ScoreText;

    [SerializeField]
    private TMP_Text roundText;

    [SerializeField]
    private TMP_Text team1ScoreText;

    [Header("Match Result")]
    [SerializeField]
    private GameObject resultPanel;

    [SerializeField]
    private TMP_Text resultText;

    private MatchManager matchManager;
    private Coroutine initializeRoutine;

    private void OnEnable()
    {
        initializeRoutine =
            StartCoroutine(InitializeMatchUI());
    }

    private IEnumerator InitializeMatchUI()
    {
        while (MatchManager.Instance == null ||
               !MatchManager.Instance.IsSpawned)
        {
            yield return null;
        }

        matchManager = MatchManager.Instance;

        // Tekrarlı aboneliği önler.
        matchManager.MatchStateChanged -= RefreshUI;
        matchManager.MatchStateChanged += RefreshUI;

        RefreshUI();

        initializeRoutine = null;
    }

    private void RefreshUI()
    {
        if (matchManager == null)
            return;

        RefreshScoreUI();
        RefreshResultUI();
        RefreshPreparationUI();
        RefreshCombatCountdownUI();
    }

    private void RefreshScoreUI()
    {
        if (team0ScoreText != null)
        {
            team0ScoreText.text =
                matchManager.Team0Score.ToString();
        }

        if (team1ScoreText != null)
        {
            team1ScoreText.text =
                matchManager.Team1Score.ToString();
        }

        if (roundText != null)
        {
            roundText.text =
                $"ROUND {matchManager.RoundNumber}";
        }
    }

    private void RefreshResultUI()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(
                matchManager.MatchEnded
            );
        }

        if (resultText != null &&
            matchManager.MatchEnded)
        {
            resultText.text =
                $"TEAM {matchManager.WinningTeamId + 1} WINS!";
        }
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

        if (!showPreparation)
            return;

        if (preparationTitleText != null)
        {
            preparationTitleText.text =
                "PREPARATION";
        }

        if (preparationCountdownText != null)
        {
            preparationCountdownText.text =
                matchManager.PreparationTimeRemaining
                    .ToString();
        }
    }

    private void RefreshCombatCountdownUI()
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

    private void OnDisable()
    {
        if (initializeRoutine != null)
        {
            StopCoroutine(initializeRoutine);
            initializeRoutine = null;
        }

        if (matchManager != null)
        {
            matchManager.MatchStateChanged -=
                RefreshUI;
        }
    }
}