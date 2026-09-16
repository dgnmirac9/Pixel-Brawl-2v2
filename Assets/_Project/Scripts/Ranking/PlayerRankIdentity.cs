using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerRankIdentity : NetworkBehaviour
{
    private const int MinimumRating = 0;
    private const int MaximumPrototypeRating = 5000;

    private readonly NetworkVariable<int>
        networkRating = new(
            1000,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private readonly NetworkVariable<bool>
        ratingSubmitted = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public int Rating =>
        networkRating.Value;

    public bool HasSubmittedRating =>
        ratingSubmitted.Value;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;

        StartCoroutine(
            SubmitLocalRatingWhenReady()
        );
    }

    private IEnumerator SubmitLocalRatingWhenReady()
    {
        float startedAt =
            Time.realtimeSinceStartup;

        const float timeoutDuration = 10f;

        while (RankService.Instance == null ||
               !RankService.Instance.IsReady ||
               RankService.Instance.Profile == null)
        {
            if (Time.realtimeSinceStartup -
                startedAt >= timeoutDuration)
            {
                Debug.LogError(
                    "[RankIdentity] RankService " +
                    "zamanında hazır olmadı."
                );

                yield break;
            }

            yield return null;
        }

        int localRating =
            RankService.Instance.Profile.rating;

        SubmitRatingServerRpc(
            localRating
        );
    }

    [ServerRpc]
    private void SubmitRatingServerRpc(
        int submittedRating)
    {
        int validatedRating =
            Mathf.Clamp(
                submittedRating,
                MinimumRating,
                MaximumPrototypeRating
            );

        networkRating.Value =
            validatedRating;

        ratingSubmitted.Value = true;

        Debug.Log(
            "[RankIdentity] Rating alındı. " +
            $"ClientId: {OwnerClientId} | " +
            $"Rating: {validatedRating}"
        );
    }

    public override void OnNetworkDespawn()
    {
        StopAllCoroutines();
    }
}