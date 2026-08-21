using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class BreakableObject : NetworkBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip breakSound;

    [SerializeField, Range(0f, 1f)]
    private float breakSoundVolume = 0.8f;

    private AudioSource audioSource;
    
    [Header("Visuals")]
    [SerializeField] private Sprite intactSprite;
    [SerializeField] private Sprite brokenSprite;

    [SerializeField]
    private ParticleSystem breakEffectPrefab;

    private readonly NetworkVariable<bool> isBroken =
        new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private SpriteRenderer spriteRenderer;
    private Collider2D objectCollider;

    public bool IsBroken => isBroken.Value;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        
        spriteRenderer =
            GetComponent<SpriteRenderer>();

        objectCollider =
            GetComponent<Collider2D>();

        if (intactSprite == null)
        {
            intactSprite =
                spriteRenderer.sprite;
        }
    }

    public override void OnNetworkSpawn()
    {
        isBroken.OnValueChanged +=
            OnBrokenStateChanged;

        ApplyBrokenState(isBroken.Value);
    }

    public override void OnNetworkDespawn()
    {
        isBroken.OnValueChanged -=
            OnBrokenStateChanged;
    }

    public void BreakOnServer(Vector2 impactPoint)
    {
        if (!IsServer || isBroken.Value)
            return;

        isBroken.Value = true;

        PlayBreakEffectRpc(impactPoint);
    }

    public void ResetOnServer()
    {
        if (!IsServer)
            return;

        isBroken.Value = false;
    }

    private void OnBrokenStateChanged(
        bool previousValue,
        bool newValue)
    {
        ApplyBrokenState(newValue);
    }

    private void ApplyBrokenState(bool broken)
    {
        if (objectCollider != null)
        {
            objectCollider.enabled = !broken;
        }

        if (spriteRenderer == null)
            return;

        if (broken)
        {
            if (brokenSprite != null)
            {
                spriteRenderer.sprite =
                    brokenSprite;

                spriteRenderer.enabled = true;
            }
            else
            {
                spriteRenderer.enabled = false;
            }
        }
        else
        {
            spriteRenderer.sprite =
                intactSprite;

            spriteRenderer.enabled = true;
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayBreakEffectRpc(
        Vector2 impactPoint)
    {
        if (breakEffectPrefab != null)
        {
            ParticleSystem effect = Instantiate(
                breakEffectPrefab,
                impactPoint,
                Quaternion.identity
            );

            effect.Play();

            ParticleSystem.MainModule main =
                effect.main;

            Destroy(
                effect.gameObject,
                main.duration +
                main.startLifetime.constantMax
            );
        }

        if (breakSound != null &&
            audioSource != null)
        {
            audioSource.PlayOneShot(
                breakSound,
                breakSoundVolume
            );
        }
    }
}