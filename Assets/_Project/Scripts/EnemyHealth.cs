using System;
using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    [Header("Knockback Settings")] 
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.15f;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;
    
    [SerializeField] private HealthBarUI healthBar; // Can barı referansı

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isKnockedBack;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
        currentHealth = maxHealth;
    }

    private void Start()
    {
        // Başlangıçta can barını tam dolu göster
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }
    }

    public void TakeDamage(int damageAmount, Vector2 attackerPosition)
    {
        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(currentHealth, 0); // Canın 0'ın altına düşmesini engeller
        
        //vuruş yönünü hesapla (saldırgandan düşmana doğru vektör
        Vector2 knockBackDirection = ((Vector2)transform.position - attackerPosition).normalized;

        //geri tepme ve renk efektini başlat
        StartCoroutine(ApplyKnockback(knockBackDirection));
        StartCoroutine(FlashRed());
        
        // Can barını güncelle
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }
        
        Debug.Log(gameObject.name + " hasar aldı! Kalan Can: " + currentHealth);

        // Can sıfırlandığında objeyi yok et
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator ApplyKnockback(Vector2 direction)
    {
        isKnockedBack = true;
        
        //düşmana anlık kuvvet uygula
        rb.linearVelocity = direction * knockbackForce;
        yield return new WaitForSeconds(knockbackDuration);
        
        //kuvvet süresi bitince hareketi sıfırla
        rb.linearVelocity = Vector2.zero;
        isKnockedBack = false;
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
    private void Die()
    {
        Debug.Log(gameObject.name + " öldü!");
        Destroy(gameObject);
    }
}