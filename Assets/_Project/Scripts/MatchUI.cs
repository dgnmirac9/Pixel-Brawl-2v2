using System.Collections;
using TMPro;
using UnityEngine;

public class MatchUI : MonoBehaviour
{
    [Header("Score UI")]
    [SerializeField] private TMP_Text team0ScoreText;
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text team1ScoreText;

    [Header("Match Result")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;

    private MatchManager matchManager;

    private void OnEnable()
    {
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
    }

    private void RefreshUI()
    {
        if (matchManager == null)
            return;
        
        if (team0ScoreText != null)
            team0ScoreText.text =
                matchManager.Team0Score.ToString();

        if (team1ScoreText != null)
            team1ScoreText.text =
                matchManager.Team1Score.ToString();

        if (roundText != null)
        {
            roundText.text =
                $"ROUND {matchManager.RoundNumber}";
        }

        if (resultPanel != null)
            resultPanel.SetActive(matchManager.MatchEnded);

        if (resultText != null && matchManager.MatchEnded)
        {
            resultText.text =
                $"TEAM {matchManager.WinningTeamId + 1} WINS!";
        }
    }

    private void OnDisable()
    {
        if (matchManager != null)
        {
            matchManager.MatchStateChanged -= RefreshUI;
        }
    }
}