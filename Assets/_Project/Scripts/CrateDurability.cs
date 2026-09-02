using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(BreakableObject))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(AudioSource))]
public class CrateDurability :
    NetworkBehaviour
{
    [Header("Durability")]
    [SerializeField, Min(1)]
    private int maximumDurability = 3;

    [Header("Damage Sprites")]
    [SerializeField]
    private Sprite damagedSprite;

    [SerializeField]
    private Sprite heavilyDamagedSprite;

    [Header("Hit Feedback")]
    [SerializeField]
    private AudioClip hitSound;

    [SerializeField, Range(0f, 1f)]
    private float hitSoundVolume = 0.65f;

    [SerializeField, Min(0.01f)]
    private float hitFlashDuration = 0.09f;

    [SerializeField, Min(0f)]
    private float shakeDistance = 0.045f;

    private readonly NetworkVariable<int>
        currentDurability = new(
            3,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private BreakableObject breakableObject;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private bool hasRestingLocalPosition;
    private Sprite intactSprite;
    private Color normalColor;
    private Vector3 restingLocalPosition;
    private Coroutine feedbackRoutine;
    
    
    public int CurrentDurability =>
        currentDurability.Value;

    public int MaximumDurability =>
        maximumDurability;
    
    public event Action<int, int>
        DurabilityChanged;

    private void Awake()
    {
        breakableObject =
            GetComponent<BreakableObject>();

        spriteRenderer =
            GetComponent<SpriteRenderer>();

        audioSource =
            GetComponent<AudioSource>();

        intactSprite =
            spriteRenderer.sprite;

        normalColor =
            spriteRenderer.color;
    }

    public override void OnNetworkSpawn()
    {
        currentDurability.OnValueChanged +=
            OnDurabilityChanged;

        if (IsServer)
        {
            currentDurability.Value =
                maximumDurability;
        }

        ApplyDurabilityVisual(
            currentDurability.Value
        );
        
        DurabilityChanged?.Invoke(
            currentDurability.Value,
            maximumDurability
        );
    }

    public override void OnNetworkDespawn()
    {
        currentDurability.OnValueChanged -=
            OnDurabilityChanged;

        StopFeedback();
    }

    public void DamageOnServer(
        int damage,
        Vector2 impactPoint)
    {
        if (!IsServer ||
            breakableObject == null ||
            breakableObject.IsBroken)
        {
            return;
        }

        damage = Mathf.Max(1, damage);

        int newDurability =
            Mathf.Max(
                0,
                currentDurability.Value -
                damage
            );

        currentDurability.Value =
            newDurability;

        if (newDurability <= 0)
        {
            breakableObject.BreakOnServer(
                impactPoint
            );

            return;
        }

        PlayDamageFeedbackRpc();
    }

    public void ResetDurabilityOnServer()
    {
        if (!IsServer)
            return;

        currentDurability.Value =
            maximumDurability;
    }

    private void OnDurabilityChanged(
        int previousValue,
        int newValue)
    {
        ApplyDurabilityVisual(newValue);

        DurabilityChanged?.Invoke(
            newValue,
            maximumDurability
        );
    }

    private void ApplyDurabilityVisual(
        int durability)
    {
        if (spriteRenderer == null ||
            breakableObject == null ||
            breakableObject.IsBroken ||
            durability <= 0)
        {
            return;
        }

        float durabilityRatio =
            (float)durability /
            maximumDurability;

        if (durabilityRatio <= 0.34f &&
            heavilyDamagedSprite != null)
        {
            spriteRenderer.sprite =
                heavilyDamagedSprite;
        }
        else if (durabilityRatio <= 0.67f &&
                 damagedSprite != null)
        {
            spriteRenderer.sprite =
                damagedSprite;
        }
        else
        {
            spriteRenderer.sprite =
                intactSprite;
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayDamageFeedbackRpc()
    {
        // İlk hasar geldiğinde network konumu artık
        // uygulanmış durumdadır.
        if (!hasRestingLocalPosition)
        {
            restingLocalPosition =
                transform.localPosition;

            hasRestingLocalPosition = true;
        }

        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
        }

        transform.localPosition =
            restingLocalPosition;

        spriteRenderer.color =
            normalColor;

        if (hitSound != null &&
            audioSource != null)
        {
            audioSource.PlayOneShot(
                hitSound,
                hitSoundVolume
            );
        }

        feedbackRoutine =
            StartCoroutine(
                DamageFeedbackRoutine()
            );
    }

    private IEnumerator
        DamageFeedbackRoutine()
    {
        float elapsed = 0f;

        spriteRenderer.color =
            Color.Lerp(
                normalColor,
                Color.white,
                0.75f
            );

        while (elapsed <
               hitFlashDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    hitFlashDuration
                );

            float remainingShake =
                1f - progress;

            Vector2 randomOffset =
                UnityEngine.Random.insideUnitCircle *
                shakeDistance *
                remainingShake;

            transform.localPosition =
                restingLocalPosition +
                (Vector3)randomOffset;

            spriteRenderer.color =
                Color.Lerp(
                    Color.white,
                    normalColor,
                    progress
                );

            yield return null;
        }

        transform.localPosition =
            restingLocalPosition;

        spriteRenderer.color =
            normalColor;

        feedbackRoutine = null;
    }

    private void StopFeedback()
    {
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color =
                normalColor;
        }

        if (hasRestingLocalPosition)
        {
            transform.localPosition =
                restingLocalPosition;
        }
    }
}