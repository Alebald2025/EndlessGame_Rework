using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    // Eventos a los que otros scripts se pueden suscribir
    public event Action OnSwipeLeft;
    public event Action OnSwipeRight;
    public event Action OnSwipeUp;
    public event Action OnSwipeDown;

    [Header("Configuración de Deslizamiento (Móvil)")]
    [Tooltip("Distancia mínima en píxeles para detectar un deslizamiento táctil.")]
    [SerializeField] private float minSwipeDistance = 50f;

    private Vector2 touchStartPos;
    private Vector2 touchEndPos;
    private bool isTrackingSwipe = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        HandleKeyboardAndGamepadInput();
        HandleTouchInput();
    }

    private void HandleKeyboardAndGamepadInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
            {
                OnSwipeLeft?.Invoke();
            }

            if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
            {
                OnSwipeRight?.Invoke();
            }

            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
            {
                OnSwipeUp?.Invoke();
            }

            if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
            {
                OnSwipeDown?.Invoke();
            }
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            if (gamepad.dpad.left.wasPressedThisFrame || gamepad.leftShoulder.wasPressedThisFrame)
            {
                OnSwipeLeft?.Invoke();
            }

            if (gamepad.dpad.right.wasPressedThisFrame || gamepad.rightShoulder.wasPressedThisFrame)
            {
                OnSwipeRight?.Invoke();
            }

            if (gamepad.buttonSouth.wasPressedThisFrame)
            {
                OnSwipeUp?.Invoke();
            }

            if (gamepad.buttonEast.wasPressedThisFrame)
            {
                OnSwipeDown?.Invoke();
            }
        }
    }

    private void HandleTouchInput()
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen == null)
            return;

        var touch = touchscreen.primaryTouch;

        if (touch.press.wasPressedThisFrame)
        {
            touchStartPos = touch.position.ReadValue();
            isTrackingSwipe = true;
            return;
        }

        if (touch.press.isPressed && isTrackingSwipe)
        {
            Vector2 currentSwipe = touch.position.ReadValue() - touchStartPos;
            if (currentSwipe.magnitude >= minSwipeDistance && touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                DetectSwipeDirection(currentSwipe);
                isTrackingSwipe = false;
            }
        }

        if (touch.press.wasReleasedThisFrame && isTrackingSwipe)
        {
            touchEndPos = touch.position.ReadValue();
            Vector2 finalSwipe = touchEndPos - touchStartPos;
            if (finalSwipe.magnitude >= minSwipeDistance)
            {
                DetectSwipeDirection(finalSwipe);
            }
            isTrackingSwipe = false;
        }
    }

    private void DetectSwipeDirection(Vector2 swipeVector)
    {
        if (Mathf.Abs(swipeVector.x) > Mathf.Abs(swipeVector.y))
        {
            if (swipeVector.x > 0)
            {
                OnSwipeRight?.Invoke();
            }
            else
            {
                OnSwipeLeft?.Invoke();
            }
        }
        else
        {
            if (swipeVector.y > 0)
            {
                OnSwipeUp?.Invoke();
            }
            else
            {
                OnSwipeDown?.Invoke();
            }
        }
    }

    // ── Métodos públicos para sensores externos ──────────────────────────
    public void InvokeSwipeLeft()  => OnSwipeLeft?.Invoke();
    public void InvokeSwipeRight() => OnSwipeRight?.Invoke();
    public void InvokeSwipeUp()    => OnSwipeUp?.Invoke();
    public void InvokeSwipeDown()  => OnSwipeDown?.Invoke();
}
