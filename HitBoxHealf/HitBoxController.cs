using UnityEngine;

public class HitBoxController : MonoBehaviour
{
    [SerializeField] public Collider hitBox;

    // Метод для включения хитбокса (вызывается из анимации)
    public void OpenHitBox()
    {
        if (hitBox != null)
            hitBox.enabled = true;
    }

    // Метод для выключния хитбокса (вызывается из анимации)
    public void CloseHitBox()
    {
        if (hitBox != null)
            hitBox.enabled = false;
    }

    // Логика попадания
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Hit: " + other.name);
            // Здесь добавьте логику нанесения урона
        }
    }
}