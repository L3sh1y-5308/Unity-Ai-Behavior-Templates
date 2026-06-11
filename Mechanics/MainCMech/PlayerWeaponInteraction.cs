using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerWeaponInteraction : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Animator _animator;
    [SerializeField] private WeaponHolder _weaponHolder;

    [Header("Параметры Анимации (Animator Parameters)")]
    [SerializeField] private string _pickUpAnimParameter = "IsPickingUp";
    [SerializeField] private string _dropAnimParameter = "IsDropping";

    private GameObject _weaponInZone; // Сюда автоматически запишется факел, когда мы к нему подойдем
    private bool _isHoldingWeapon = false;
    private InputAction _interactAction;

    private void Awake()
    {
        // Настройка кнопки взаимодействия (кнопка "Юг" на геймпаде или можно поменять на клавиатуру)
        _interactAction = new InputAction("Interact", binding: "<Gamepad>/buttonSouth");
        _interactAction.Enable();
    }

    private void OnDisable()
    {
        _interactAction.Disable();
    }

    private void Update()
    {
        // Если нажали кнопку взаимодействия
        if (_interactAction.triggered)
        {
            // Если в руках уже есть оружие — выбрасываем его
            if (_isHoldingWeapon)
            {
                DropWeapon();
            }
            // Если рукu пусты, но мы стоим внутри зоны оружия — подбираем его
            else if (_weaponInZone != null)
            {
                PickUpWeapon(_weaponInZone.transform);
            }
        }
    }

    // Этот метод срабатывает автоматически, когда объект с тегом Weapon входит в наш зеленый куб
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Weapon"))
        {
            _weaponInZone = other.gameObject;
        }
    }

    // Этот метод срабатывает, когда мы отходим от факела и он выходит из зеленого куба
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Weapon") && _weaponInZone == other.gameObject)
        {
            _weaponInZone = null;
        }
    }

    private void PickUpWeapon(Transform weaponTransform)
    {
        if (_animator != null) _animator.SetBool(_pickUpAnimParameter, true);

        if (_weaponHolder != null)
        {
            _weaponHolder.Equip(weaponTransform.gameObject);
        }

        _isHoldingWeapon = true;
        _weaponInZone = null; // Очищаем зону, так как предмет уже в руках
    }

    private void DropWeapon()
    {
        if (_animator != null) _animator.SetBool(_dropAnimParameter, true);

        if (_weaponHolder != null)
        {
            _weaponHolder.Unequip();
        }

        _isHoldingWeapon = false;
    }

    public void OnDropFinished()
    {
        if (_animator != null) _animator.SetBool(_dropAnimParameter, false);
    }
}