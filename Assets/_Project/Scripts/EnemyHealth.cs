using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyHealth : NetworkBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    private NetworkVariable<int> currentHealth =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    [Header("Knockback Settings")] 
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.15f;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;
    
    [SerializeField] private HealthBarUI healthBar; // Can barı referansı

    private Camera mainCamera;
    private Vector2 aimDirection = Vector2.right;
    private float attackPointDistance;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Collider2D[] enemyColliders;
    
    private void Awake()
    {
        enemyColliders = GetComponentsInChildren<Collider2D>(true);
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    public override void OnNetworkSpawn()
    {
        currentHealth.OnValueChanged += OnHealthChanged;

        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        UpdateHealthBar(currentHealth.Value);
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;

        HideEnemy();
    }

    private void OnHealthChanged(int previousHealth, int newHealth)
    {
        UpdateHealthBar(newHealth);
    }

    private void UpdateHealthBar(int health)
    {
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(health, maxHealth);
        }
    }

    public void TakeDamage(int damageAmount, Vector2 attackerPosition)
    {
        // Hasarı yalnızca server/host değiştirebilir.
        if (!IsServer)
        {
            return;
        }

        currentHealth.Value = Mathf.Max(
            currentHealth.Value - damageAmount,
            0
        );

        Vector2 knockbackDirection =
            ((Vector2)transform.position - attackerPosition).normalized;

        StartCoroutine(ApplyKnockback(knockbackDirection));
        StartCoroutine(FlashRed());

        Debug.Log(
            gameObject.name +
            " hasar aldı! Kalan Can: " +
            currentHealth.Value
        );

        if (currentHealth.Value <= 0)
        {
            Die();
        }
    }

    private IEnumerator ApplyKnockback(Vector2 direction)
    {
        //düşmana anlık kuvvet uygula
        rb.linearVelocity = direction * knockbackForce;
        yield return new WaitForSeconds(knockbackDuration);
        
        //kuvvet süresi bitince hareketi sıfırla
        rb.linearVelocity = Vector2.zero;
    }

    private IEnumerator FlashRed()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = damageColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
        }
    }
    private void HideEnemy()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        if (enemyColliders != null)
        {
            foreach (Collider2D enemyCollider in enemyColliders)
            {
                if (enemyCollider != null)
                {
                    enemyCollider.enabled = false;
                }
            }
        }
    }
    private void Die()
    {
        if (!IsServer)
            return;

        Debug.Log(gameObject.name + " öldü!");

        if (NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}