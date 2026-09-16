using TMPro;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(AudioSource))]
public class BlockFeedback : NetworkBehaviour
{
    [Header("Audio")]
    [SerializeField]
    private AudioClip successfulBlockClip;

    [SerializeField, Range(0f, 1f)]
    private float successfulBlockVolume = 0.8f;

    [Header("Blocked Text")]
    [SerializeField]
    private GameObject blockedTextPrefab;

    [SerializeField]
    private Vector2 blockedTextOffset =
        new Vector2(0f, 0.85f);

    [SerializeField, Min(0.05f)]
    private float blockedTextLifetime = 0.8f;

    [SerializeField]
    private Color blockedTextColor =
        new Color32(164, 221, 255, 255);

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource =
            GetComponent<AudioSource>();
    }

    public void PlayOnServer(
        Vector2 hitPosition)
    {
        if (!IsServer || !IsSpawned)
            return;

        PlayBlockFeedbackRpc(
            hitPosition
        );
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayBlockFeedbackRpc(
        Vector2 hitPosition)
    {
        if (successfulBlockClip != null &&
            audioSource != null)
        {
            audioSource.PlayOneShot(
                successfulBlockClip,
                successfulBlockVolume
            );
        }

        if (blockedTextPrefab == null)
            return;

        Vector3 spawnPosition =
            (Vector3)hitPosition +
            (Vector3)blockedTextOffset;

        GameObject blockedText =
            Instantiate(
                blockedTextPrefab,
                spawnPosition,
                Quaternion.identity
            );

        blockedText.SetActive(true);

        TMP_Text textComponent =
            blockedText.GetComponentInChildren<
                TMP_Text>();

        if (textComponent != null)
        {
            textComponent.text = "BLOCKED";
            textComponent.color =
                blockedTextColor;
        }

        Destroy(
            blockedText,
            blockedTextLifetime
        );
    }
}
