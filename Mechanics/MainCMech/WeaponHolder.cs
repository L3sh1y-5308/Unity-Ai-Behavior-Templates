using UnityEngine;

public class WeaponHolder : MonoBehaviour
{
    [Header("Ссылки")]
    public GameObject weaponInstance;

    [Header("Настройки")]
    public bool isEquipped = false;
    public Rigidbody weaponRigidbody;
    public Collider weaponCollider;

    private Transform handTransform;

    private void Awake()
    {
        handTransform = transform;
    }

    public void Equip(GameObject weapon)
    {
        if (weapon == null) return;

        weaponInstance = weapon;

        weaponRigidbody = weapon.GetComponent<Rigidbody>();
        weaponCollider = weapon.GetComponent<Collider>();

        if (weaponRigidbody != null)
        {
            weaponRigidbody.isKinematic = true;
            weaponRigidbody.Sleep();
        }

        // ИСПРАВЛЕНО: Выключаем коллайдер в руках, чтобы он не конфликтовал с коллайдером игрока
        if (weaponCollider != null)
        {
            weaponCollider.enabled = false;
        }

        weapon.transform.parent = handTransform;
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;

        isEquipped = true;
    }

    public void Unequip()
    {
        if (weaponInstance == null) return;

        isEquipped = false;

        if (weaponRigidbody != null)
        {
            weaponRigidbody.isKinematic = false;
            weaponRigidbody.WakeUp();
        }

        // ИСПРАВЛЕНО: Включаем коллайдер, чтобы выброшенное оружие упало на пол, а не под карту
        if (weaponCollider != null)
        {
            weaponCollider.enabled = true;
        }

        // ИСПРАВЛЕНО: Используем weaponInstance вместо несуществующей переменной weapon
        weaponInstance.transform.parent = null;
        weaponInstance = null; // Очищаем ссылку
    }
}