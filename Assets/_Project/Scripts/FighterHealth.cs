using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FighterHealth : NetworkBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;

    [Header("UI")]
    [SerializeField] private HealthBarUI healthBar;

    [Header("Defeated Visual")]
    [SerializeField] private Color defeatedColor = Color.gray;

    private NetworkVariable<int> currentHealth =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private NetworkVariable<bool> isAlive =
        new NetworkVariable<bool>(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private PlayerLoadout playerLoadout;
    private int lastKnownMaxHealth;
    private PlayerController playerController;
    private SpriteRenderer spriteRenderer;
    private Collider2D[] fighterColliders;
    private Color originalColor;
    private HitFeedback hitFeedback;
    
    public int MaxHealth => GetEffectiveMaxHealth();
    public int CurrentHealth => currentHealth.Value;
    public bool IsAlive => isAlive.Value;
    public int TeamId
    {
        get
        {
            if (LobbyManager.Instance != null &&
                LobbyManager.Instance.IsSpawned)
            {
                int playerSlot =
                    LobbyManager.Instance
                        .GetPlayerSlotIndex(
                            OwnerClientId
                        );

                if (playerSlot >= 0)
                    return playerSlot % 2;
            }

            // Lobby listesi henüz hazır değilse
            // 1v1 için geçici güvenli eşleme.
            if (NetworkManager != null &&
                OwnerClientId ==
                NetworkManager.ServerClientId)
            {
                return 0;
            }

            return 1;
        }
    }
    
    private void Awake()
    {
        hitFeedback = GetComponent<HitFeedback>();
        playerController = GetComponent<PlayerController>();
        playerLoadout = GetComponent<PlayerLoadout>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        fighterColliders = GetComponentsInChildren<Collider2D>(true);

        originalColor = spriteRenderer.color;
    }

    public override void OnNetworkSpawn()
    {
        lastKnownMaxHealth =
            GetEffectiveMaxHealth();

        if (playerLoadout != null)
        {
            playerLoadout.LoadoutChanged += OnLoadoutChanged;
        }
        currentHealth.OnValueChanged += OnHealthChanged;
        isAlive.OnValueChanged += OnAliveChanged;

        if (IsServer)
        {
            currentHealth.Value = GetEffectiveMaxHealth();
            isAlive.Value = true;
        }
        
        if (healthBar != null)
        {
            healthBar.SetPlayerRelationship(
                IsOwner
            );
        }

        UpdateHealthBar(currentHealth.Value);
        ApplyAliveState(isAlive.Value);

        if (IsServer)
        {
            if (MatchManager.Instance == null)
            {
                Debug.LogError(
                    $"{name}: MatchManager bulunamadı, fighter kaydedilemedi."
                );
            }
            else
            {
                MatchManager.Instance.RegisterFighter(this);

                Debug.Log(
                    $"{name} kaydedildi. " +
                    $"ClientId: {OwnerClientId} | Takım: {TeamId}"
                );
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
        isAlive.OnValueChanged -= OnAliveChanged;

        if (IsServer && MatchManager.Instance != null)
        {
            MatchManager.Instance.UnregisterFighter(this);
        }
        if (playerLoadout != null)
        {
            playerLoadout.LoadoutChanged -= OnLoadoutChanged;
        }
    }
    
    public void RestoreCurrentVisualColor()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.color =
            isAlive.Value
                ? originalColor
                : defeatedColor;
    }
    
    public void TakeDamage(int damageAmount)
    {
        TakeDamage(
            damageAmount,
            transform.position,
            false
        );
    }

    public void TakeDamage(
        int damageAmount,
        Vector2 hitPosition)
    {
        TakeDamage(
            damageAmount,
            hitPosition,
            false
        );
    }

    public void TakeDamage(
        int damageAmount,
        Vector2 hitPosition,
        bool isCritical)
    {
        if (!IsServer)
            return;

        if (!isAlive.Value)
            return;

        if (damageAmount <= 0)
            return;

        float damageReduction =
            playerLoadout != null
                ? playerLoadout
                    .TotalDamageReduction
                : 0f;

        int resolvedDamage =
            Mathf.Max(
                1,
                Mathf.RoundToInt(
                    damageAmount *
                    (1f - damageReduction)
                )
            );
        
        currentHealth.Value = Mathf.Max(
            currentHealth.Value - resolvedDamage,
            0
        );

        Debug.Log(
            $"Gelen hasar: {damageAmount} | " +
            $"Uygulanan hasar: {resolvedDamage} | "
        );

        if (currentHealth.Value <= 0 &&
            isAlive.Value)
        {
            isAlive.Value = false;

            MatchManager.Instance?
                .NotifyFighterDefeated(this);
        }

        hitFeedback?.PlayOnServer(
            hitPosition,
            isCritical
        );
    }

    public void ResetFighter()
    {
        if (!IsServer)
            return;

        currentHealth.Value = GetEffectiveMaxHealth();
        isAlive.Value = true;

        // Host tarafındaki görsel ve kontrol durumunu
        // aynı karede doğrudan yeniler.
        UpdateHealthBar(currentHealth.Value);
        ApplyAliveState(isAlive.Value);

        Debug.Log(
            $"{name} resetlendi. " +
            $"ClientId: {OwnerClientId} | " +
            $"Can: {currentHealth.Value} | " +
            $"Alive: {isAlive.Value}"
        );
    }

    private void OnHealthChanged(
        int previousHealth,
        int newHealth)
    {
        UpdateHealthBar(newHealth);
    }

    private void OnAliveChanged(
        bool previousValue,
        bool newValue)
    {
        ApplyAliveState(newValue);
    }

    private void UpdateHealthBar(int health)
    {
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(
                health,
                GetEffectiveMaxHealth()
            );
        }
    }

    private void ApplyAliveState(bool alive)
    {
        if (playerController != null)
        {
            playerController.SetControlEnabled(alive);
        }

        if (fighterColliders != null)
        {
            foreach (Collider2D fighterCollider in fighterColliders)
            {
                if (fighterCollider != null)
                {
                    fighterCollider.enabled = alive;
                }
            }
        }

        if (spriteRenderer != null)
        {
            RestoreCurrentVisualColor();
        }
    }
    
    private int GetEffectiveMaxHealth()
    {
        int bonusHealth =
            playerLoadout != null
                ? playerLoadout
                    .TotalMaxHealthBonus
                : 0;

        return Mathf.Max(
            1,
            maxHealth + bonusHealth
        );
    }

    private void OnLoadoutChanged()
    {
        int newMaxHealth =
            GetEffectiveMaxHealth();

        int maxHealthDifference =
            newMaxHealth -
            lastKnownMaxHealth;

        lastKnownMaxHealth =
            newMaxHealth;

        if (IsServer)
        {
            if (maxHealthDifference > 0 &&
                isAlive.Value)
            {
                currentHealth.Value =
                    Mathf.Min(
                        currentHealth.Value +
                        maxHealthDifference,
                        newMaxHealth
                    );
            }
            else if (currentHealth.Value >
                     newMaxHealth)
            {
                currentHealth.Value =
                    newMaxHealth;
            }
        }

        UpdateHealthBar(
            currentHealth.Value
        );
    }
}