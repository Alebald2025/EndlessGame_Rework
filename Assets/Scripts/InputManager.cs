using UnityEngine;
using System;

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
        // Implementación de Singleton simple
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
        HandleKeyboardInput();
        HandleTouchInput();
    }

    private void HandleKeyboardInput()
    {
        // Teclas para cambiar a la izquierda (Flecha izquierda o 'A')
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            OnSwipeLeft?.Invoke();
        }
        // Teclas para cambiar a la derecha (Flecha derecha o 'D')
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            OnSwipeRight?.Invoke();
        }
        // Teclas para saltar (Flecha arriba, 'W' o Espacio) - Respaldo del Editor
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space))
        {
            OnSwipeUp?.Invoke();
        }
        // Teclas para deslizarse/agacharse (Flecha abajo o 'S')
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            OnSwipeDown?.Invoke();
        }
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPos = touch.position;
                    isTrackingSwipe = true;
                    break;

                case TouchPhase.Moved:
                    // Verificación opcional a mitad del gesto
                    if (isTrackingSwipe)
                    {
                        Vector2 currentSwipe = touch.position - touchStartPos;
                        if (currentSwipe.magnitude >= minSwipeDistance)
                        {
                            DetectSwipeDirection(currentSwipe);
                            isTrackingSwipe = false; // Detener rastreo para evitar múltiples detecciones en un solo movimiento
                        }
                    }
                    break;

                case TouchPhase.Ended:
                    if (isTrackingSwipe)
                    {
                        touchEndPos = touch.position;
                        Vector2 finalSwipe = touchEndPos - touchStartPos;
                        if (finalSwipe.magnitude >= minSwipeDistance)
                        {
                            DetectSwipeDirection(finalSwipe);
                        }
                        isTrackingSwipe = false;
                    }
                    break;

                case TouchPhase.Canceled:
                    isTrackingSwipe = false;
                    break;
            }
        }
    }

    private void DetectSwipeDirection(Vector2 swipeVector)
    {
        // Normalizar y ver qué eje tiene mayor magnitud (Horizontal vs Vertical)
        if (Mathf.Abs(swipeVector.x) > Mathf.Abs(swipeVector.y))
        {
            // Deslizamiento Horizontal
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
            // Deslizamiento Vertical
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
}
