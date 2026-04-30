using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterContoll0 : MonoBehaviour
{
    //parametrs
    PlayerInputM playerInput;
    CharacterController characterController;
    Animator animator;

    //variables
    Vector2 currentMovementInput;
    Vector3 currentMovement;
    Vector3 currentRunMovement;

    // Управление факелом
    bool hasTorch = false;
    int torchLayerIndex = -1;

    //states animator cntrol
    bool isMovementPressed;
    bool isRunPressed;
    bool isJumpPressed = false;
    bool isJumping = false;
    bool isJumpAnimating = false;
    bool isCrouching = false;
    bool isAttacking = false;
    bool isPickingUp = false;

    //states base
    float rotationFPS = 25.0f;
    float MultiplierRun = 3.0f;
    int zero = 0;
    float gravity = -9.8f;
    float groundedGravity = .05f;

    float initialJumpVelocity;
    float maxJumpHeight = 4.0f;
    float maxJumpTime = 0.5f;

    float pickupRange = 2.0f;

    //new Hashes
    int isJumpingHash;
    int isWalkingHash;
    int isRunningHash;
    int isCrouchingHash;
    int isAttackingHash;
    int isPickingUpHash;

    private void Awake()
    {
        playerInput = new PlayerInputM();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        // Проверка компонентов
        if (animator == null)
        {
            Debug.LogError("Animator НЕ НАЙДЕН на объекте!");
            return;
        }

        // Инициализация слоя факела
        torchLayerIndex = animator.GetLayerIndex("Torch Layer");
        if (torchLayerIndex != -1)
        {
            animator.SetLayerWeight(torchLayerIndex, 0f);
        }

        // Хеши параметров
        isWalkingHash = Animator.StringToHash("isWalking");
        isRunningHash = Animator.StringToHash("isRunning");
        isJumpingHash = Animator.StringToHash("isJumping");

        // ОТЛАДКА: Проверяем параметры
        Debug.Log("=== ПАРАМЕТРЫ ANIMATOR ===");
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            Debug.Log($"Параметр: '{param.name}', Тип: {param.type}");
        }
        Debug.Log($"isWalkingHash: {isWalkingHash}");
        Debug.Log($"isRunningHash: {isRunningHash}");

        playerInput.CharacterControls.Move.started += onMovementInput;
        playerInput.CharacterControls.Move.canceled += onMovementInput;
        playerInput.CharacterControls.Move.performed += onMovementInput;


        playerInput.CharacterControls.Jump.started += onJump;
        playerInput.CharacterControls.Jump.canceled += onJump;

        playerInput.CharacterControls.Sprint.started += onRun;
        playerInput.CharacterControls.Sprint.canceled += onRun;

        setupJumpVariables();
    }

    void setupJumpVariables()
    {
        float timeToApex = maxJumpTime / 2;
        gravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToApex, 2);
        initialJumpVelocity = (2 * maxJumpHeight) / timeToApex;
    }

    void handleJump()
    {
        if (!isJumping && characterController.isGrounded && isJumpPressed)
        {
            animator.SetBool(isJumpingHash, true);
            isJumpAnimating = true;
            isJumping = true;
            currentMovement.y = initialJumpVelocity * .5f;
            currentRunMovement.y = initialJumpVelocity * .5f;
        }
        else if (!isJumpPressed && isJumping && characterController.isGrounded)
        {
            isJumping = false;
        }
    }

    void onJump(InputAction.CallbackContext context)
    {
        isJumpPressed = context.ReadValueAsButton();
        Debug.Log($"Jump pressed: {isJumpPressed}");
    }

    void onRun(InputAction.CallbackContext context)
    {
        isRunPressed = context.ReadValueAsButton();
        Debug.Log($"Run pressed: {isRunPressed}");
    }

    void handleRotation()
    {
        Vector3 positionToLookAt;
        positionToLookAt.x = currentMovement.x;
        positionToLookAt.y = zero;
        positionToLookAt.z = currentMovement.z;
        Quaternion currentRotation = transform.rotation;

        if (isMovementPressed)
        {
            Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
            transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, rotationFPS * Time.deltaTime);
        }
    }

    void onMovementInput(InputAction.CallbackContext context)
    {
        currentMovementInput = context.ReadValue<Vector2>();
        currentMovement.x = currentMovementInput.x;
        currentMovement.z = currentMovementInput.y;
        currentRunMovement.x = currentMovementInput.x * MultiplierRun;
        currentRunMovement.z = currentMovementInput.y * MultiplierRun;
        isMovementPressed = currentMovementInput.x != zero || currentMovementInput.y != zero;

        Debug.Log($"Movement Input: {currentMovementInput}, isMovementPressed: {isMovementPressed}");
    }

    void handleAnimation()
    {
        if (animator == null) return;

        bool isWalking = animator.GetBool(isWalkingHash);
        bool isRunning = animator.GetBool(isRunningHash);

        // ОТЛАДКА: выводим каждый кадр
        if (Time.frameCount % 30 == 0) // каждые 30 кадров
        {
            Debug.Log($"[ANIMATOR] isMovementPressed: {isMovementPressed}, isRunPressed: {isRunPressed}");
            Debug.Log($"[ANIMATOR] isWalking (current): {isWalking}, isRunning (current): {isRunning}");
        }

        // Логика ходьбы
        if (isMovementPressed && !isWalking)
        {
            animator.SetBool(isWalkingHash, true);
            Debug.Log(">>> SetBool isWalking = TRUE");
        }
        else if (!isMovementPressed && isWalking)
        {
            animator.SetBool(isWalkingHash, false);
            Debug.Log(">>> SetBool isWalking = FALSE");
        }

        // Логика бега
        if ((isMovementPressed && isRunPressed) && !isRunning)
        {
            animator.SetBool(isRunningHash, true);
            Debug.Log(">>> SetBool isRunning = TRUE");
        }
        else if ((!isMovementPressed || !isRunPressed) && isRunning)
        {
            animator.SetBool(isRunningHash, false);
            Debug.Log(">>> SetBool isRunning = FALSE");
        }

        // Управление весом слоя факела
        if (torchLayerIndex != -1)
        {
            float targetWeight = hasTorch ? 1f : 0f;
            float currentWeight = animator.GetLayerWeight(torchLayerIndex);
            float newWeight = Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * 5.0f);
            animator.SetLayerWeight(torchLayerIndex, newWeight);
        }
    }

    void handleGravity()
    {
        bool isFalling = currentMovement.y <= 0.0f || !isJumpPressed;
        float fallMultiplier = 2.0f;

        if (characterController.isGrounded)
        {
            animator.SetBool(isJumpingHash, false);
            isJumpAnimating = false;
            currentMovement.y = groundedGravity;
            currentRunMovement.y = groundedGravity;
        }
        else if (isFalling)
        {
            float previousYVelocity = currentMovement.y;
            float newYVelocity = currentMovement.y + (gravity * fallMultiplier * Time.deltaTime);
            float nextYVelocity = (previousYVelocity + newYVelocity) * .5f;
            currentMovement.y = nextYVelocity;
            currentRunMovement.y = nextYVelocity;
        }
        else
        {
            float previousYVelocity = currentMovement.y;
            float newYVelocity = currentMovement.y + (gravity * Time.deltaTime);
            float nextYVelocity = (previousYVelocity + newYVelocity) * .5f;
            currentMovement.y = nextYVelocity;
            currentRunMovement.y = nextYVelocity;
        }
    }

    void Update()
    {
        handleAnimation();
        handleRotation();

        if (isRunPressed)
        {
            characterController.Move(currentRunMovement * Time.deltaTime);
        }
        else
        {
            characterController.Move(currentMovement * Time.deltaTime);
        }

        handleGravity();
        handleJump();

        if (isRunPressed)
            characterController.Move(currentRunMovement * Time.deltaTime);
        else
            characterController.Move(currentMovement * Time.deltaTime);
    }

    // Публичные методы для управления факелом
    public void PickupTorch()
    {
        hasTorch = true;
    }

    public void DropTorch()
    {
        hasTorch = false;
    }

    public bool HasTorch()
    {
        return hasTorch;
    }

    private void OnEnable()
    {
        playerInput.CharacterControls.Enable();
    }

    private void OnDisable()
    {
        playerInput.CharacterControls.Disable();
    }
}