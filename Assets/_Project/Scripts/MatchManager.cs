using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance { get; private set; }
    
    [Header("Preparation Settings")]
    [SerializeField, Min(1)]
    private int preparationDurationSeconds = 10;
    
    [Header("Countdown Settings")]
    [SerializeField, Min(1)]
    private int initialCountdownSeconds = 3;

    [SerializeField, Min(1)]
    private int roundCountdownSeconds = 2;

    [SerializeField, Min(0f)]
    private float roundEndPauseDuration = 1f;

    [Header("Match Settings")]
    [SerializeField, Min(1)] private int roundsToWin = 3;
    
    [Header("Round Settings")]
    [SerializeField] private float roundRestartDelay = 2f;

    private readonly List<FighterHealth> fighters = new();
    private bool roundEnding;

    private readonly NetworkVariable<int>
        countdownValue = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
    private readonly NetworkVariable<int>
        preparationTimeRemaining = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
    
    private readonly NetworkVariable<MatchPhase>
        currentPhase = new(
            MatchPhase.Lobby,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
    
    private readonly NetworkVariable<bool> matchEnded = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<int> winningTeamId = new(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    private readonly NetworkVariable<int> team0Score = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<int> team1Score = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<int> roundNumber = new(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    

    public event Action MatchStateChanged;
    public MatchPhase CurrentPhase =>
        currentPhase.Value;
    public int CountdownValue =>
        countdownValue.Value;
    public int PreparationTimeRemaining =>
        preparationTimeRemaining.Value;
    public bool IsCombatActive =>
        currentPhase.Value ==
        MatchPhase.Combat;
    public int Team0Score => team0Score.Value;
    public int Team1Score => team1Score.Value;
    public int RoundNumber => roundNumber.Value;
    public bool IsRoundEnding => roundEnding;
    public int RoundsToWin => roundsToWin;
    public bool MatchEnded => matchEnded.Value;
    public int WinningTeamId => winningTeamId.Value;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Sahnede birden fazla MatchManager bulundu.");
            enabled = false;
            return;
        }

        Instance = this;
    }
    
    private void OnIntegerMatchStateChanged(
        int previousValue,
        int newValue)
    {
        NotifyMatchStateChanged();
    }

    private void OnBooleanMatchStateChanged(
        bool previousValue,
        bool newValue)
    {
        NotifyMatchStateChanged();
    }

    private void NotifyMatchStateChanged()
    {
        MatchStateChanged?.Invoke();
    }
    
    public override void OnNetworkSpawn()
    {
        countdownValue.OnValueChanged +=
            OnIntegerMatchStateChanged;
        preparationTimeRemaining.OnValueChanged +=
            OnIntegerMatchStateChanged;
        currentPhase.OnValueChanged +=
            OnMatchPhaseChanged;
        team0Score.OnValueChanged += OnIntegerMatchStateChanged;
        team1Score.OnValueChanged += OnIntegerMatchStateChanged;
        roundNumber.OnValueChanged += OnIntegerMatchStateChanged;
        winningTeamId.OnValueChanged += OnIntegerMatchStateChanged;
        matchEnded.OnValueChanged += OnBooleanMatchStateChanged;

        // Client, OnNetworkSpawn çalıştığında güncel NetworkVariable
        // değerlerini zaten almış olur.
        NotifyMatchStateChanged();
        
        if (!IsServer)
            return;

        countdownValue.Value = 0;
        preparationTimeRemaining.Value = 0;
        currentPhase.Value =
            MatchPhase.Lobby;
        
        team0Score.Value = 0;
        team1Score.Value = 0;
        roundNumber.Value = 1;

        matchEnded.Value = false;
        winningTeamId.Value = -1;
        roundEnding = false;

        FighterHealth[] existingFighters =
            FindObjectsByType<FighterHealth>(
                FindObjectsSortMode.None
            );

        foreach (FighterHealth fighter in existingFighters)
        {
            if (fighter.IsSpawned)
                RegisterFighter(fighter);
        }

        Debug.Log(
            $"Maç başladı. İlk {roundsToWin} round'u alan kazanır."
        );
    }
    
    public override void OnNetworkDespawn()
    {
        countdownValue.OnValueChanged -=
            OnIntegerMatchStateChanged;
        preparationTimeRemaining.OnValueChanged -=
            OnIntegerMatchStateChanged;
        currentPhase.OnValueChanged -=
            OnMatchPhaseChanged;
        team0Score.OnValueChanged -= OnIntegerMatchStateChanged;
        team1Score.OnValueChanged -= OnIntegerMatchStateChanged;
        roundNumber.OnValueChanged -= OnIntegerMatchStateChanged;
        winningTeamId.OnValueChanged -= OnIntegerMatchStateChanged;
        matchEnded.OnValueChanged -= OnBooleanMatchStateChanged;
    }
    
    public bool ServerBeginMatch()
    {
        if (!IsServer)
            return false;

        if (currentPhase.Value !=
            MatchPhase.Lobby)
        {
            return false;
        }

        // Preparation ve countdown sırasında
        // ölüm bildirimi round bitirmesin.
        roundEnding = true;

        StartCoroutine(
            RunPreparationSequence()
        );

        Debug.Log(
            "Waiting Room tamamlandı. " +
            "Hazırlık aşaması başladı."
        );

        return true;
    }
    private IEnumerator RunPreparationSequence()
    {
        if (!IsServer)
            yield break;

        currentPhase.Value =
            MatchPhase.Preparation;

        countdownValue.Value = 0;

        PrepareFightersForPreparation();

        SetAllFighterControls(true);

        for (int seconds =
                 preparationDurationSeconds;
             seconds > 0;
             seconds--)
        {
            preparationTimeRemaining.Value =
                seconds;

            yield return new WaitForSeconds(1f);
        }

        preparationTimeRemaining.Value = 0;

        // Hazırlık süresi bittiğinde hareketi
        // combat alanına geçmeden önce kapat.
        SetAllFighterControls(false);

        PrepareFightersForRound();

        yield return RunCountdown(
            initialCountdownSeconds
        );
    }
    private IEnumerator RunCountdown(
        int countdownSeconds)
    {
        currentPhase.Value =
            MatchPhase.Countdown;

        SetAllFighterControls(false);

        for (int seconds = countdownSeconds;
             seconds > 0;
             seconds--)
        {
            countdownValue.Value = seconds;

            yield return new WaitForSeconds(1f);
        }

        // UI bu değerde FIGHT! gösterecek.
        countdownValue.Value = 0;

        yield return new WaitForSeconds(0.4f);

        currentPhase.Value =
            MatchPhase.Combat;

        roundEnding = false;

        SetAllFighterControls(true);

        Debug.Log(
            $"Round {roundNumber.Value} başladı."
        );
    }
    private void PrepareFightersForPreparation()
    {
        if (!IsServer)
            return;

        fighters.RemoveAll(
            fighter => fighter == null
        );

        foreach (FighterHealth fighter
                 in fighters)
        {
            if (fighter == null)
                continue;

            fighter.ResetFighter();

            PlayerController controller =
                fighter.GetComponent<PlayerController>();

            if (controller == null)
            {
                Debug.LogError(
                    $"ClientId {fighter.OwnerClientId}: " +
                    "PlayerController bulunamadı."
                );

                continue;
            }

            if (PlayerSpawnManager.Instance == null)
            {
                Debug.LogError(
                    "PlayerSpawnManager bulunamadı."
                );

                continue;
            }

            Vector3 spawnPosition =
                PlayerSpawnManager.Instance
                    .GetPreparationSpawnPosition(
                        fighter.OwnerClientId
                    );

            controller.ServerMoveToSpawn(
                spawnPosition
            );

            controller.ServerSetControlEnabled(
                true
            );
        }
    }
    private void PrepareFightersForRound()
    {
        if (!IsServer)
            return;

        fighters.RemoveAll(
            fighter => fighter == null
        );

        foreach (FighterHealth fighter
                 in fighters)
        {
            if (fighter == null)
                continue;

            fighter.ResetFighter();

            PlayerController controller =
                fighter.GetComponent<
                    PlayerController>();

            if (controller == null)
                continue;

            if (PlayerSpawnManager.Instance !=
                null)
            {
                Vector3 spawnPosition =
                    PlayerSpawnManager.Instance
                        .GetCombatSpawnPosition(
                            fighter.OwnerClientId
                        );

                controller.ServerMoveToSpawn(
                    spawnPosition
                );
            }

            // ResetFighter kontrolü kısa süreliğine
            // açabileceği için yeniden kapatıyoruz.
            controller.ServerSetControlEnabled(
                false
            );
        }
    }
    
    public void RegisterFighter(FighterHealth fighter)
    {
        if (!IsServer || fighter == null)
            return;

        if (!fighters.Contains(fighter))
            fighters.Add(fighter);
        
        PlayerController controller =
            fighter.GetComponent<PlayerController>();

        if (controller != null)
        {
            bool canControl =
                currentPhase.Value ==
                MatchPhase.Combat &&
                fighter.IsAlive;

            controller.ServerSetControlEnabled(
                canControl
            );
        }
    }

    public void UnregisterFighter(FighterHealth fighter)
    {
        if (!IsServer)
            return;

        fighters.Remove(fighter);
    }

    public void NotifyFighterDefeated(
        FighterHealth defeatedFighter)
    {
        if (!IsServer)
            return;

        if (roundEnding || matchEnded.Value)
            return;

        if (defeatedFighter == null)
            return;

        RegisterFighter(defeatedFighter);

        int defeatedTeam = defeatedFighter.TeamId;

        Debug.Log(
            $"Ölüm bildirimi alındı. " +
            $"ClientId: {defeatedFighter.OwnerClientId} | " +
            $"Kaybeden takım adayı: {defeatedTeam} | " +
            $"Kayıtlı oyuncu: {fighters.Count}"
        );

        if (!IsTeamEliminated(defeatedTeam))
        {
            Debug.Log(
                $"Takım {defeatedTeam} henüz tamamen elenmedi."
            );

            return;
        }

        roundEnding = true;
        
        currentPhase.Value =
            MatchPhase.RoundEnding;

        int winnerId = defeatedTeam == 0 ? 1 : 0;

        if (winnerId == 0)
            team0Score.Value++;
        else
            team1Score.Value++;

        int winnerScore = winnerId == 0
            ? team0Score.Value
            : team1Score.Value;

        SetAllFighterControls(false);

        // Kazanan takım gerekli round sayısına ulaştı mı?
        if (winnerScore >= roundsToWin)
        {
            winningTeamId.Value = winnerId;
            matchEnded.Value = true;
            currentPhase.Value =
                MatchPhase.MatchEnded;
            
            NotifyMatchStateChanged();
            
            Debug.Log(
                $"MAÇ BİTTİ! Kazanan Takım: {winnerId} | " +
                $"Final Skoru: " +
                $"{team0Score.Value} - {team1Score.Value}"
            );

            // Maç bittiği için RestartRound başlatmıyoruz.
            return;
        }

        Debug.Log(
            $"Round {roundNumber.Value} bitti. " +
            $"Kazanan Takım: {winnerId} | " +
            $"Skor: {team0Score.Value} - {team1Score.Value}"
        );

        StartCoroutine(RestartRound());
    }
    
    private void OnMatchPhaseChanged(
        MatchPhase previousPhase,
        MatchPhase newPhase)
    {
        NotifyMatchStateChanged();
    }
    
    private bool IsTeamEliminated(int teamId)
    {
        bool teamMemberFound = false;

        foreach (FighterHealth fighter in fighters)
        {
            if (fighter == null || fighter.TeamId != teamId)
                continue;

            teamMemberFound = true;

            if (fighter.IsAlive)
                return false;
        }

        return teamMemberFound;
    }

    private IEnumerator RestartRound()
    {
        yield return new WaitForSeconds(
            roundEndPauseDuration
        );

        ResetBreakableObjects();

        roundNumber.Value++;

        PrepareFightersForRound();

        yield return RunCountdown(
            roundCountdownSeconds
        );
    }

    private void SetAllFighterControls(bool controlEnabled)
    {
        foreach (FighterHealth fighter in fighters)
        {
            if (fighter == null)
                continue;

            PlayerController controller =
                fighter.GetComponent<PlayerController>();

            if (controller != null)
                controller.ServerSetControlEnabled(controlEnabled);
        }
    }
    
    private void ResetBreakableObjects()
    {
        if (!IsServer)
            return;

        BreakableObject[] breakableObjects =
            FindObjectsByType<BreakableObject>(
                FindObjectsSortMode.None
            );

        foreach (BreakableObject breakable
                 in breakableObjects)
        {
            if (breakable == null ||
                !breakable.IsSpawned)
            {
                continue;
            }

            breakable.ResetOnServer();
        }
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}