using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ArenaCameraController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField]
    private Camera targetCamera;

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

        Transform selectedPoint =
            combatCameraPoint;

        if (matchManager.CurrentPhase ==
            MatchPhase.Preparation)
        {
            selectedPoint =
                GetLocalPreparationCameraPoint();
        }

        if (selectedPoint == null)
            return;

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
            (int)(
                localClientId %
                (ulong)preparationCameraPoints.Length
            );

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
}