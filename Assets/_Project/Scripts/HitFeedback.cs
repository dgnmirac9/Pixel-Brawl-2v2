using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(FighterHealth))]
public class HitFeedback : NetworkBehaviour
{
    [Header("Flash")]
    [SerializeField]
    private Color normalFlashColor =
        new Color(1f, 0.4f, 0.25f, 1f);

    [SerializeField]
    private Color criticalFlashColor =
        new Color(1f, 0.85f, 0.2f, 1f);

    [SerializeField, Min(0.01f)]
    private float flashDuration = 0.08f;

    [Header("Audio")]
    [SerializeField] private AudioClip normalHitClip;
    [SerializeField] private AudioClip criticalHitClip;

    [SerializeField, Range(0f, 1f)]
    private float normalHitVolume = 0.6f;

    [SerializeField, Range(0f, 1f)]
    private float criticalHitVolume = 0.9f;

    private AudioSource audioSource;
    
    [Header("Particles")]
    [SerializeField]
    private ParticleSystem normalHitEffectPrefab;

    [SerializeField]
    private ParticleSystem criticalHitEffectPrefab;
    
    [Header("Critical Text")]
    [SerializeField]
    private GameObject criticalTextEffectPrefab;

    [SerializeField]
    private Vector2 criticalTextOffset =
        new Vector2(0f, 0.65f);
    
    private SpriteRenderer spriteRenderer;
    private FighterHealth fighterHealth;
    private Coroutine flashRoutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        fighterHealth = GetComponent<FighterHealth>();
    }

    public void PlayOnServer(
        Vector2 hitPosition,
        bool isCritical)
    {
        if (!IsServer)
            return;

        PlayHitFeedbackRpc(
            hitPosition,
            isCritical
        );
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayHitFeedbackRpc(
        Vector2 hitPosition,
        bool isCritical)
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(
            FlashRoutine(isCritical)
        );

        ParticleSystem selectedEffect =
            isCritical
                ? criticalHitEffectPrefab
                : normalHitEffectPrefab;

        if (selectedEffect != null)
        {
            ParticleSystem effect = Instantiate(
                selectedEffect,
                hitPosition,
                Quaternion.identity
            );

            effect.Play();

            ParticleSystem.MainModule main =
                effect.main;

            float destroyDelay =
                main.duration +
                main.startLifetime.constantMax;

            Destroy(
                effect.gameObject,
                destroyDelay
            );
        }
        
        if (isCritical &&
            criticalTextEffectPrefab != null)
        {
            Instantiate(
                criticalTextEffectPrefab,
                hitPosition + criticalTextOffset,
                Quaternion.identity
            );
        }

        AudioClip selectedClip =
            isCritical
                ? criticalHitClip
                : normalHitClip;

        float selectedVolume =
            isCritical
                ? criticalHitVolume
                : normalHitVolume;

        if (selectedClip != null &&
            audioSource != null)
        {
            audioSource.PlayOneShot(
                selectedClip,
                selectedVolume
            );
        }
    }

    private IEnumerator FlashRoutine(bool isCritical)
    {
        spriteRenderer.color =
            isCritical
                ? criticalFlashColor
                : normalFlashColor;

        yield return new WaitForSeconds(
            flashDuration
        );

        fighterHealth.RestoreCurrentVisualColor();
        flashRoutine = null;
    }
}