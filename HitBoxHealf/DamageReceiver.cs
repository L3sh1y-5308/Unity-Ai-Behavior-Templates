using UnityEngine;

[RequireComponent(typeof(HealfP))]
public class DamageReceiver : MonoBehaviour
{
    private HealfP healthComponent;

    private void Awake()
    {
        // Автоматически находим прикрепленный скрипт HealfP на этом же объекте
        healthComponent = GetComponent<HealfP>();
    }

    /// <summary>
    /// Принимает урон в int и передает его компоненту HealfP.
    /// </summary>
    /// <param name="damageAmount">Количество урона (int)</param>
    public void ReceiveDamage(int damageAmount)
    {
        if (healthComponent != null)
        {
            // Неявное преобразование int в float при передаче аргумента
            healthComponent.TakeDamage(damageAmount);
        }
    }
}