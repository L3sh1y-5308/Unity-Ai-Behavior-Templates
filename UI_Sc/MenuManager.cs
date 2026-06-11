using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Ссылки")]
    public PlayerInput playerInput;
    public GameObject menuBoard;

    [Header("Настройки анимации и времени")]
    [SerializeField] private float _openAnimDuration = 0.3f;
    [SerializeField] private Ease _openEase = Ease.OutBack;
    [SerializeField] private Ease _closeEase = Ease.InBack;

    [Header("Схемы управления")]
    public string mouseControlScheme = "KeyboardMouse";
    public string gameplayControlScheme = "Gamepad";

    [Header("Input System")]
    public InputActionReference menuToggleActionRef;

    private bool _isOpen = false;
    private RectTransform _menuRectTransform;

    private void Awake()
    {
        // Кэшируем RectTransform заранее для оптимизации
        if (menuBoard != null)
        {
            _menuRectTransform = menuBoard.GetComponent<RectTransform>();
            if (_menuRectTransform == null)
            {
                Debug.LogError("MenuBoard должен иметь компонент RectTransform!", this);
            }
        }
    }

    private void OnEnable()
    {
        if (menuToggleActionRef != null)
        {
            menuToggleActionRef.action.Enable();
            menuToggleActionRef.action.performed += OnToggleMenu;
        }
    }

    private void OnDisable()
    {
        if (menuToggleActionRef != null)
        {
            menuToggleActionRef.action.performed -= OnToggleMenu;
            menuToggleActionRef.action.Disable();
        }
    }

    private void OnToggleMenu(InputAction.CallbackContext context)
    {
        ToggleMenu();
    }

    public void ToggleMenu()
    {
        if (_menuRectTransform == null) return;

        _isOpen = !_isOpen;

        // Убиваем прошлые анимации, чтобы избежать конфликтов при быстром прощёлкивании
        _menuRectTransform.DOKill();

        if (_isOpen)
        {
            menuBoard.SetActive(true);

            // 1. Устанавливаем НАЧАЛЬНУЮ позицию (за экраном, снизу)
            _menuRectTransform.anchoredPosition = new Vector2(0, -Screen.height);

            // 2. Анимируем в КОНЕЧНУЮ позицию (центр экрана)
            _menuRectTransform.DOAnchorPos(Vector2.zero, _openAnimDuration)
                .SetEase(_openEase)
                .SetUpdate(true);

            Time.timeScale = 0f;

            // Безопасное переключение схемы управления
            if (playerInput != null && Keyboard.current != null)
            {
                playerInput.SwitchCurrentControlScheme(mouseControlScheme, Keyboard.current);
            }
        }
        else
        {
            // Анимация выплывания вниз
            _menuRectTransform.DOAnchorPos(new Vector2(0, -Screen.height), _openAnimDuration)
                .SetEase(_closeEase)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    menuBoard.SetActive(false);
                    Time.timeScale = 1f; // Возвращаем время только после исчезновения меню
                });

            // Возвращаем управление обратно
            if (playerInput != null && Gamepad.current != null)
            {
                playerInput.SwitchCurrentControlScheme(gameplayControlScheme, Gamepad.current);
            }
            else if (playerInput != null && Keyboard.current != null)
            {
                playerInput.SwitchCurrentControlScheme(mouseControlScheme, Keyboard.current);
            }
        }
    }
}