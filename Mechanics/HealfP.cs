using UnityEngine;
using UnityEngine.Events;

public class HealfP : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] float maxHealth = 100f;
    float currentHealth;

    [Header("Events")]
    public UnityEvent OnDeath;
    public UnityEvent<float> OnHealthChanged;

    public float CurrentHealth => currentHealth;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Max(0, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth);
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} has died.");
        OnDeath?.Invoke();
        
        // По умолчанию просто отключаем объект
        gameObject.SetActive(false);
    }
}
