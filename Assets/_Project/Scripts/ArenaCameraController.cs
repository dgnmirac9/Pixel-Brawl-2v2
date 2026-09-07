using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ArenaCameraController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField]
    private Camera targetCamera;

    [Header("Camera Sizes")]
    [SerializeField, Min(0.1f)]
    private float combatOrthographicSize = 7.3f;

    [SerializeField, Min(0.1f)]
    private float preparationOrthographicSize = 5.5f;
    
    [Header("Camera Points")]
    [SerializeField]
    private Transform combatCameraPoint;

    [SerializeField]
    private Transform[] preparationCameraPoints;

    private MatchManager matchManager;
    private Coroutine bindRoutine;

    private void OnEnable()
    {
        bindRoutine = StartCoroutine(
            BindToMatchManager()
        );
    }

    private void OnDisable()
    {
        if (bindRoutine != null)
        {
            StopCoroutine(bindRoutine);
            bindRoutine = null;
        }

        if (matchManager != null)
        {
            matchManager.MatchStateChanged -=
                RefreshCameraPosition;
        }
    }

    private IEnumerator BindToMatchManager()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        while (MatchManager.Instance == null ||
               !MatchManager.Instance.IsSpawned)
        {
            yield return null;
        }

        matchManager = MatchManager.Instance;

        matchManager.MatchStateChanged +=
            RefreshCameraPosition;

        RefreshCameraPosition();

        bindRoutine = null;
    }

    private void RefreshCameraPosition()
    {
        if (targetCamera == null ||
            matchManager == null)
        {
            return;
        }

        bool isPreparation =
            matchManager.CurrentPhase ==
            MatchPhase.Preparation;

        Transform selectedPoint =
            isPreparation
                ? GetLocalPreparationCameraPoint()
                : combatCameraPoint;

        if (selectedPoint == null)
            return;

        targetCamera.orthographicSize =
            isPreparation
                ? preparationOrthographicSize
                : combatOrthographicSize;
        
        Vector3 currentCameraPosition =
            targetCamera.transform.position;

        targetCamera.transform.position =
            new Vector3(
                selectedPoint.position.x,
                selectedPoint.position.y,
                currentCameraPosition.z
            );
    }

    private Transform
        GetLocalPreparationCameraPoint()
    {
        if (preparationCameraPoints == null ||
            preparationCameraPoints.Length == 0)
        {
            Debug.LogError(
                "Preparation camera point bulunamadı!"
            );

            return combatCameraPoint;
        }

        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsListening)
        {
            return preparationCameraPoints[0];
        }

        ulong localClientId =
            NetworkManager.Singleton.LocalClientId;

        int cameraIndex =
            ResolveLocalPlayerSlotIndex(
                localClientId
            );

        if (cameraIndex < 0 ||
            cameraIndex >=
            preparationCameraPoints.Length)
        {
            Debug.LogError(
                $"ClientId {localClientId} için " +
                "kamera slotu bulunamadı."
            );

            return combatCameraPoint;
        }

        Transform cameraPoint =
            preparationCameraPoints[cameraIndex];

        if (cameraPoint == null)
        {
            Debug.LogError(
                $"Preparation camera point " +
                $"{cameraIndex} atanmamış!"
            );

            return combatCameraPoint;
        }

        return cameraPoint;
    }
    private int ResolveLocalPlayerSlotIndex(
        ulong localClientId)
    {
        if (LobbyManager.Instance != null &&
            LobbyManager.Instance.IsSpawned)
        {
            int lobbySlot =
                LobbyManager.Instance
                    .GetPlayerSlotIndex(
                        localClientId
                    );

            if (lobbySlot >= 0)
                return lobbySlot;
        }

        // NetworkList henüz client'a ulaşmadıysa
        // Host oda 0, uzak Client oda 1.
        if (NetworkManager.Singleton != null)
        {
            return NetworkManager.Singleton.IsHost
                ? 0
                : Mathf.Min(
                    1,
                    preparationCameraPoints.Length - 1
                );
        }

        return 0;
    }
}