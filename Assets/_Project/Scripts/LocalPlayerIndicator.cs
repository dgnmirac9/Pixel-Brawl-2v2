using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class LocalPlayerIndicator : NetworkBehaviour
{
    [SerializeField]
    private GameObject indicatorRoot;

    private MatchManager observedMatchManager;
    private Coroutine connectionRoutine;

    public override void OnNetworkSpawn()
    {
        SetIndicatorVisible(false);

        // Bu oyuncu bu bilgisayarın oyuncusu değilse
        // gösterge hiçbir zaman açılmaz.
        if (!IsOwner)
            return;

        connectionRoutine =
            StartCoroutine(
                ConnectToMatchManager()
            );
    }

    public override void OnNetworkDespawn()
    {
        if (connectionRoutine != null)
        {
            StopCoroutine(connectionRoutine);
            connectionRoutine = null;
        }

        DisconnectFromMatchManager();
        SetIndicatorVisible(false);
    }

    private IEnumerator ConnectToMatchManager()
    {
        while (IsSpawned &&
               MatchManager.Instance == null)
        {
            yield return null;
        }

        if (!IsSpawned)
            yield break;

        observedMatchManager =
            MatchManager.Instance;

        observedMatchManager.MatchStateChanged +=
            RefreshVisibility;

        RefreshVisibility();

        connectionRoutine = null;
    }

    private void DisconnectFromMatchManager()
    {
        if (observedMatchManager == null)
            return;

        observedMatchManager.MatchStateChanged -=
            RefreshVisibility;

        observedMatchManager = null;
    }

    private void RefreshVisibility()
    {
        if (!IsOwner ||
            observedMatchManager == null)
        {
            SetIndicatorVisible(false);
            return;
        }

        MatchPhase phase =
            observedMatchManager.CurrentPhase;

        bool shouldBeVisible =
            phase == MatchPhase.Preparation ||
            phase == MatchPhase.Countdown;

        SetIndicatorVisible(
            shouldBeVisible
        );
    }

    private void SetIndicatorVisible(bool visible)
    {
        if (indicatorRoot != null)
        {
            indicatorRoot.SetActive(visible);
        }
    }
}