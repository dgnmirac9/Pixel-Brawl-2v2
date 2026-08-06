using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private HealthBarUI healthBar; // Can barı referansı
    private int currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
        
        // Başlangıçta can barını tam dolu göster
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0); // Canın 0'ın altına düşmesini engeller
        
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

    private void Die()
    {
        Debug.Log(gameObject.name + " öldü!");
        Destroy(gameObject);
    }
}