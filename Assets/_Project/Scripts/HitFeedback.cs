using System.Collections;
using Unity.Netcode;
using UnityEngine;

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

    [Header("Particles")]
    [SerializeField]
    private ParticleSystem normalHitEffectPrefab;

    [SerializeField]
    private ParticleSystem criticalHitEffectPrefab;

    private SpriteRenderer spriteRenderer;
    private FighterHealth fighterHealth;
    private Coroutine flashRoutine;

    private void Awake()
    {
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

        if (selectedEffect == null)
            return;

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

        Destroy(effect.gameObject, destroyDelay);
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