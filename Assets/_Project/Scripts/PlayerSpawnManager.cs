using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance
    {
        get;
        private set;
    }

    [Header("Combat Spawn Points")]
    [FormerlySerializedAs("spawnPoints")]
    [SerializeField]
    private Transform[] combatSpawnPoints;

    [Header("Preparation Spawn Points")]
    [SerializeField]
    private Transform[] preparationSpawnPoints;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // Eski çağrılar bozulmasın diye korunuyor.
    public Vector3 GetSpawnPosition(ulong clientId)
    {
        return GetCombatSpawnPosition(clientId);
    }

    public Vector3 GetCombatSpawnPosition(
        ulong clientId)
    {
        return GetSpawnPositionFromArray(
            combatSpawnPoints,
            clientId,
            "Combat"
        );
    }

    public Vector3 GetPreparationSpawnPosition(
        ulong clientId)
    {
        return GetSpawnPositionFromArray(
            preparationSpawnPoints,
            clientId,
            "Preparation"
        );
    }

    private Vector3 GetSpawnPositionFromArray(
        Transform[] points,
        ulong clientId,
        string groupName)
    {
        if (points == null || points.Length == 0)
        {
            Debug.LogError(
                $"{groupName} spawn point bulunamadı!"
            );

            return Vector3.zero;
        }

        int spawnIndex =
            ResolvePlayerSlotIndex(
                clientId,
                points.Length
            );

        if (spawnIndex < 0 ||
            spawnIndex >= points.Length)
        {
            Debug.LogError(
                $"{groupName}: ClientId {clientId} " +
                "için geçerli oyuncu slotu bulunamadı."
            );

            return Vector3.zero;
        }

        Transform spawnPoint = points[spawnIndex];

        if (spawnPoint == null)
        {
            Debug.LogError(
                $"{groupName} spawn point " +
                $"{spawnIndex} atanmamış!"
            );

            return Vector3.zero;
        }

        return spawnPoint.position;
    }
    
    private int ResolvePlayerSlotIndex(
        ulong clientId,
        int pointCount)
    {
        if (LobbyManager.Instance != null &&
            LobbyManager.Instance.IsSpawned)
        {
            int lobbySlot =
                LobbyManager.Instance
                    .GetPlayerSlotIndex(clientId);

            if (lobbySlot >= 0 &&
                lobbySlot < pointCount)
            {
                return lobbySlot;
            }
        }

        // Player ilk spawn olurken NetworkList henüz
        // senkronize olmamışsa 1v1 için güvenli fallback.
        if (NetworkManager.Singleton != null)
        {
            if (clientId ==
                NetworkManager.ServerClientId)
            {
                return 0;
            }

            if (pointCount > 1)
                return 1;
        }

        return pointCount > 0 ? 0 : -1;
    }

    private void OnDrawGizmos()
    {
        DrawSpawnPoints(
            combatSpawnPoints,
            Color.cyan
        );

        DrawSpawnPoints(
            preparationSpawnPoints,
            Color.yellow
        );
    }

    private void DrawSpawnPoints(
        Transform[] points,
        Color color)
    {
        if (points == null)
            return;

        Gizmos.color = color;

        foreach (Transform point in points)
        {
            if (point == null)
                continue;

            Gizmos.DrawWireSphere(
                point.position,
                0.3f
            );

            Gizmos.DrawLine(
                point.position,
                point.position + Vector3.up * 0.6f
            );
        }
    }
}