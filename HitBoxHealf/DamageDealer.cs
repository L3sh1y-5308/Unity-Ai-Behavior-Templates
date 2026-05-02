using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Количество наносимого урона")]
    [SerializeField] private int damage = 10;

    // Срабатывает при попадании в триггер
    private void OnTriggerEnter(Collider other)
    {
        DealDamage(other.gameObject);
    }

    private void DealDamage(GameObject target)
    {
        // Ищем компонент приема урона на объекте, с которым столкнулись
        if (target.TryGetComponent(out DamageReceiver receiver))
        {
            receiver.ReceiveDamage(damage);
        }
    }
}