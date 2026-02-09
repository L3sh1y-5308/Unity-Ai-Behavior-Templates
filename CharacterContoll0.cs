using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterContoll0 : MonoBehaviour
{

    //parametrs
    PlayerInput playerInput;
    CharacterController characterController;
    Animator animator;

    //variables
    Vector2 currentMovementInput;

    Vector3 currentMovement;

    Vector3 currentRunMovement;



    //states
    bool isMovementPressed;

    // bool isRunPressed;



    float rotationFPS = 25.0f;
    float MultiplierRun = 3.0f;
    int zero = 0;
    float gravity = -9.8f;
    float groundedGravity = .05f;



    bool isJumpPressed = false;
    float initialJumpVelocity;
    float maxJumpHeight = 4.0f;
    float maxJumpTime = 0.5f;
    bool isJumping = false;
    int isJumpingHash;
    bool isJumpAnimating = false;



    int isWalkingHash;
    // int isRunningHash;


    private void Awake()
    {
        playerInput = new PlayerInput();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        isWalkingHash = Animator.StringToHash("isWalking");
        //isRunningHash = Animator.StringToHash("isRunning");
        isJumpingHash = Animator.StringToHash("isJumping");

        playerInput.Player.Move.started += onMovementInput;
        playerInput.Player.Move.canceled += onMovementInput;
        playerInput.Player.Move.performed += onMovementInput;
            

        playerInput.Player.Jump.started += onJump;
        playerInput.Player.Jump.canceled += onJump;

        // playerInput.Player.Run.started += onRun;
        // playerInput.Player.Run.canceled += onRun;


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
    }

    // void onRun(InputAction.CallbackContext context)
    // {
    //     isRunPressed = context.ReadValueAsButton();
    // }

    void handleRotation()
    {
        Vector3 positionToLookAt;

        positionToLookAt.x = currentMovement.x;
        positionToLookAt.y = zero;
        positionToLookAt.z = currentMovement.z;
        Quaternion currentRotation = transform.rotation; ; // на заметку




        if (isMovementPressed)
        {

            Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);

            transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, rotationFPS * Time.deltaTime);
        }



    }






    void onMovementInput(InputAction.CallbackContext context) //function
    {
        currentMovementInput = context.ReadValue<Vector2>();

        currentMovement.x = currentMovementInput.x;
        currentMovement.z = currentMovementInput.y;

        currentRunMovement.x = currentMovementInput.x * MultiplierRun;
        currentRunMovement.z = currentMovementInput.y * MultiplierRun;

        isMovementPressed = currentMovementInput.x != zero || currentMovementInput.y != zero;
    }


    void handleAnimation()
    {
        bool isWalking = animator.GetBool(isWalkingHash);

        // bool isRunning = animator.GetBool(isRunningHash);

        if (isMovementPressed && !isWalking)
        {
            animator.SetBool("isWalking", true);
        }


        else if (!isMovementPressed && isWalking)
        {
            animator.SetBool("isWalking", false);
        }

        // if ((isMovementPressed && isRunPressed) && !isRunning)
        // {
        //     animator.SetBool(isRunningHash, true);
        // }

        // else if ((!isMovementPressed || !isRunPressed) && isRunning)
        // {
        //     animator.SetBool(isRunningHash, false);
        // }



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




    // Update is called once per frame
    void Update()
    {
        handleAnimation();
        handleRotation();

        // if (isRunPressed)
        // {
        //     characterController.Move(currentRunMovement * Time.deltaTime);
        // }
        // else
        // {
            characterController.Move(currentMovement * Time.deltaTime);

        // }

        handleGravity();
        handleJump();

        // if (isRunPressed)
        //     characterController.Move(currentRunMovement * Time.deltaTime);
        // else
            characterController.Move(currentMovement * Time.deltaTime);


    }

    private void OnEnable()
    {
        playerInput.Player.Enable();
    }

    private void OnDisable()
    {
        playerInput.Player.Disable();
    }

}







