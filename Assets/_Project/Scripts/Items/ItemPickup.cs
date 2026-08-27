using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Collider2D))]
public class ItemPickup : NetworkBehaviour
{
    [Header("Item Database")] [SerializeField]
    private ItemCatalog itemCatalog;

    [Header("Visual References")] [SerializeField]
    private Transform visualRoot;

    [SerializeField] private SpriteRenderer itemSpriteRenderer;

    [SerializeField] private SpriteRenderer rarityGlowRenderer;

    [Header("Rarity Reveal Effect")] [SerializeField]
    private ParticleSystem rarityBurst;

    [Header("World Visual")] [SerializeField, Min(0.1f)]
    private float targetWorldSize = 1f;

    [Header("Pickup Timing")] [SerializeField, Min(0f)]
    private float pickupDelay = 0.8f;

    [Header("Reveal Animation")] [SerializeField, Min(0.01f)]
    private float revealDuration = 0.35f;

    [SerializeField, Min(0f)] private float revealRiseDistance = 0.25f;

    [SerializeField, Range(0.05f, 1f)] private float revealStartScale = 0.35f;

    [Header("Idle Animation")] [SerializeField, Min(0f)]
    private float bobAmplitude = 0.04f;

    [SerializeField, Min(0f)] private float bobSpeed = 2.5f;

    [SerializeField, Range(0f, 0.5f)] private float glowPulseAmount = 0.15f;

    private readonly NetworkVariable<ItemId>
        itemId = new(
            ItemId.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private Collider2D pickupCollider;

    private Coroutine enablePickupRoutine;
    private Coroutine visualRoutine;

    private Vector3 visualBaseLocalPosition;
    private Color rarityGlowTargetColor;

    private bool collected;

    public ItemId ItemId =>
        itemId.Value;

    private void Awake()
    {
        pickupCollider =
            GetComponent<Collider2D>();

        if (pickupCollider != null)
        {
            // Yalnızca server, gecikme bittiğinde açar.
            pickupCollider.enabled = false;
        }

        if (visualRoot != null)
        {
            visualBaseLocalPosition =
                visualRoot.localPosition;
        }
    }

    public override void OnNetworkSpawn()
    {
        itemId.OnValueChanged +=
            OnItemIdChanged;

        ApplyItemVisual(
            itemId.Value
        );
    }

    public override void OnNetworkDespawn()
    {
        itemId.OnValueChanged -=
            OnItemIdChanged;

        if (enablePickupRoutine != null)
        {
            StopCoroutine(
                enablePickupRoutine
            );

            enablePickupRoutine = null;
        }

        if (visualRoutine != null)
        {
            StopCoroutine(
                visualRoutine
            );

            visualRoutine = null;
        }
    }

    public void ServerSetItem(
        ItemId newItemId)
    {
        if (!IsServer)
            return;

        if (newItemId == ItemId.None)
        {
            Debug.LogError(
                $"{name}: Pickup için " +
                "ItemId None verildi."
            );

            return;
        }

        collected = false;

        if (enablePickupRoutine != null)
        {
            StopCoroutine(
                enablePickupRoutine
            );
        }

        if (pickupCollider != null)
        {
            pickupCollider.enabled = false;
        }

        itemId.Value = newItemId;

        enablePickupRoutine =
            StartCoroutine(
                EnablePickupAfterDelay()
            );
    }

    private IEnumerator
        EnablePickupAfterDelay()
    {
        yield return new WaitForSeconds(
            pickupDelay
        );

        if (!IsServer ||
            !IsSpawned ||
            collected)
        {
            enablePickupRoutine = null;
            yield break;
        }

        if (pickupCollider != null)
        {
            pickupCollider.enabled = true;
        }

        enablePickupRoutine = null;
    }

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        if (!IsServer ||
            collected ||
            itemId.Value == ItemId.None)
        {
            return;
        }

        if (MatchManager.Instance == null ||
            MatchManager.Instance.CurrentPhase !=
            MatchPhase.Preparation)
        {
            return;
        }

        PlayerLoadout loadout =
            other.GetComponentInParent<
                PlayerLoadout>();

        if (loadout == null)
            return;

        bool itemAdded =
            loadout.ServerTryAddItem(
                itemId.Value
            );

        if (!itemAdded)
            return;

        collected = true;

        if (pickupCollider != null)
        {
            pickupCollider.enabled = false;
        }

        Debug.Log(
            $"{loadout.name}, " +
            $"{itemId.Value} pickup'ını topladı."
        );

        NetworkObject.Despawn(true);
    }

    private void OnItemIdChanged(
        ItemId previousValue,
        ItemId newValue)
    {
        ApplyItemVisual(
            newValue
        );
    }

    private void ApplyItemVisual(
        ItemId newItemId)
    {
        if (itemSpriteRenderer == null)
        {
            Debug.LogError(
                $"{name}: Item Sprite Renderer " +
                "atanmamış."
            );

            return;
        }

        if (itemCatalog == null)
        {
            HideVisuals();

            Debug.LogError(
                $"{name}: ItemCatalog atanmamış."
            );

            return;
        }

        ItemDefinition item =
            itemCatalog.GetItem(
                newItemId
            );

        if (item == null ||
            item.Icon == null)
        {
            HideVisuals();
            return;
        }

        itemSpriteRenderer.sprite =
            item.Icon;

        itemSpriteRenderer.enabled =
            true;

        NormalizeVisualSize(item.WorldVisualScale);

        RestartVisualAnimation();

        PlayRarityRevealEffect(
            item.Rarity
        );
    }

    private void PlayRarityRevealEffect(
        ItemRarity rarity)
    {
        if (rarityBurst == null)
            return;

        if (rarity != ItemRarity.Epic &&
            rarity != ItemRarity.Legendary)
        {
            return;
        }

        ParticleSystem.MainModule main =
            rarityBurst.main;

        ParticleSystem.EmissionModule emission =
            rarityBurst.emission;

        main.simulationSpace =
            ParticleSystemSimulationSpace.World;

        emission.rateOverTime = 0f;

        if (rarity == ItemRarity.Epic)
        {
            Color epicColor =
                new Color32(
                    215,
                    135,
                    255,
                    255
                );

            main.startColor =
                new ParticleSystem.MinMaxGradient(
                    epicColor
                );

            main.startLifetime =
                new ParticleSystem.MinMaxCurve(
                    0.55f,
                    0.85f
                );

            main.startSpeed =
                new ParticleSystem.MinMaxCurve(
                    0.9f,
                    1.6f
                );

            main.startSize =
                new ParticleSystem.MinMaxCurve(
                    0.10f,
                    0.18f
                );

            main.gravityModifier =
                new ParticleSystem.MinMaxCurve(
                    0.10f
                );

            ParticleSystem.Burst[] bursts =
            {
                new ParticleSystem.Burst(
                    0f,
                    (short)22
                ),

                new ParticleSystem.Burst(
                    0.10f,
                    (short)8
                )
            };

            emission.SetBursts(bursts);
        }
        else
        {
            Color legendaryColor =
                new Color32(
                    255,
                    211,
                    92,
                    255
                );

            main.startColor =
                new ParticleSystem.MinMaxGradient(
                    legendaryColor
                );

            main.startLifetime =
                new ParticleSystem.MinMaxCurve(
                    0.7f,
                    1.1f
                );

            main.startSpeed =
                new ParticleSystem.MinMaxCurve(
                    1.1f,
                    2f
                );

            main.startSize =
                new ParticleSystem.MinMaxCurve(
                    0.12f,
                    0.22f
                );

            main.gravityModifier =
                new ParticleSystem.MinMaxCurve(
                    0.08f
                );

            ParticleSystem.Burst[] bursts =
            {
                new ParticleSystem.Burst(
                    0f,
                    (short)30
                ),

                new ParticleSystem.Burst(
                    0.14f,
                    (short)14
                )
            };

            emission.SetBursts(bursts);
        }

        rarityBurst.Stop(
            true,
            ParticleSystemStopBehavior
                .StopEmittingAndClear
        );

        rarityBurst.Play(true);
    }

    private void RestartVisualAnimation()
    {
        if (visualRoot == null)
            return;

        if (visualRoutine != null)
        {
            StopCoroutine(
                visualRoutine
            );
        }

        visualRoutine =
            StartCoroutine(
                PlayVisualAnimation()
            );
    }

    private IEnumerator
        PlayVisualAnimation()
    {
        Vector3 revealStartPosition =
            visualBaseLocalPosition +
            Vector3.down *
            revealRiseDistance;

        visualRoot.localPosition =
            revealStartPosition;

        visualRoot.localScale =
            Vector3.one *
            revealStartScale;

        SetItemAlpha(0f);
        SetGlowAlpha(0f);

        float elapsedTime = 0f;

        while (elapsedTime <
               revealDuration)
        {
            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime /
                    revealDuration
                );

            float smoothTime =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    normalizedTime
                );

            visualRoot.localPosition =
                Vector3.Lerp(
                    revealStartPosition,
                    visualBaseLocalPosition,
                    smoothTime
                );

            visualRoot.localScale =
                Vector3.one *
                Mathf.Lerp(
                    revealStartScale,
                    1f,
                    smoothTime
                );

            SetItemAlpha(smoothTime);
            SetGlowAlpha(smoothTime);

            elapsedTime +=
                Time.deltaTime;

            yield return null;
        }

        visualRoot.localScale =
            Vector3.one;

        SetItemAlpha(1f);
        SetGlowAlpha(1f);

        // Item toplanana kadar hafifçe süzülür.
        while (true)
        {
            float wave =
                Mathf.Sin(
                    Time.time *
                    bobSpeed
                );

            visualRoot.localPosition =
                visualBaseLocalPosition +
                Vector3.up *
                (wave * bobAmplitude);

            float glowMultiplier =
                1f +
                wave *
                glowPulseAmount;

            SetGlowAlpha(
                glowMultiplier
            );

            yield return null;
        }
    }

    private void SetItemAlpha(
        float alpha)
    {
        if (itemSpriteRenderer == null)
            return;

        Color color =
            itemSpriteRenderer.color;

        color.a =
            Mathf.Clamp01(alpha);

        itemSpriteRenderer.color =
            color;
    }

    private void SetGlowAlpha(
        float multiplier)
    {
        if (rarityGlowRenderer == null)
            return;

        Color color =
            rarityGlowTargetColor;

        color.a =
            Mathf.Clamp01(
                rarityGlowTargetColor.a *
                multiplier
            );

        rarityGlowRenderer.color =
            color;
    }

    private void HideVisuals()
    {
        if (itemSpriteRenderer != null)
        {
            itemSpriteRenderer.enabled =
                false;
        }

        if (rarityGlowRenderer != null)
        {
            rarityGlowRenderer.enabled =
                false;
        }
    }

    private void NormalizeVisualSize(
        float itemScaleMultiplier)
    {
        if (itemSpriteRenderer == null ||
            itemSpriteRenderer.sprite == null)
        {
            return;
        }

        Bounds spriteBounds =
            itemSpriteRenderer.sprite.bounds;

        float largestDimension =
            Mathf.Max(
                spriteBounds.size.x,
                spriteBounds.size.y
            );

        if (largestDimension <= 0f)
            return;

        float requiredScale =
            targetWorldSize /
            largestDimension;

        requiredScale *=
            Mathf.Max(
                0.1f,
                itemScaleMultiplier
            );

        itemSpriteRenderer.transform.localScale =
            new Vector3(
                requiredScale,
                requiredScale,
                1f
            );
    }
}