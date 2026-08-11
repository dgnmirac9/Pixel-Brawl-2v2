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

    private PlayerController playerController;
    private SpriteRenderer spriteRenderer;
    private Collider2D[] fighterColliders;
    private Color originalColor;

    public int CurrentHealth => currentHealth.Value;
    public bool IsAlive => isAlive.Value;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        fighterColliders = GetComponentsInChildren<Collider2D>(true);

        originalColor = spriteRenderer.color;
    }

    public override void OnNetworkSpawn()
    {
        currentHealth.OnValueChanged += OnHealthChanged;
        isAlive.OnValueChanged += OnAliveChanged;

        if (IsServer)
        {
            currentHealth.Value = maxHealth;
            isAlive.Value = true;
        }

        UpdateHealthBar(currentHealth.Value);
        ApplyAliveState(isAlive.Value);
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
        isAlive.OnValueChanged -= OnAliveChanged;
    }

    public void TakeDamage(int damageAmount)
    {
        // Hasar kararını yalnızca host/server verir.
        if (!IsServer)
            return;

        if (!isAlive.Value)
            return;

        if (damageAmount <= 0)
            return;

        currentHealth.Value = Mathf.Max(
            currentHealth.Value - damageAmount,
            0
        );

        Debug.Log(
            $"{gameObject.name} hasar aldı. " +
            $"Kalan can: {currentHealth.Value}"
        );

        if (currentHealth.Value <= 0)
        {
            isAlive.Value = false;
        }
    }

    public void ResetFighter()
    {
        // Daha sonra round başlangıcında MatchManager çağıracak.
        if (!IsServer)
            return;

        currentHealth.Value = maxHealth;
        isAlive.Value = true;
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
            healthBar.UpdateHealthBar(health, maxHealth);
        }
    }

    private void ApplyAliveState(bool alive)
    {
        if (playerController != null)
        {
            playerController.enabled = alive;
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
            spriteRenderer.color =
                alive ? originalColor : defeatedColor;
        }
    }
}