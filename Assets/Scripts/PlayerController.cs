using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Movimiento Lateral")]
    [Tooltip("Distancia entre los carriles (Izquierda - Centro - Derecha).")]
    [SerializeField] private float laneDistance = 2.5f;
    [Tooltip("Velocidad de transición al cambiar de carril.")]
    [SerializeField] private float laneSwitchSpeed = 15f;

    [Header("Movimiento Adelante")]
    [Tooltip("Velocidad inicial de carrera.")]
    public float forwardSpeed = 5f;
    [Tooltip("Incremento de velocidad por segundo para aumentar dificultad.")]
    [SerializeField] private float speedIncreaseRate = 0.05f;
    [Tooltip("Velocidad máxima permitida.")]
    [SerializeField] private float maxSpeed = 20f;

    [Header("Salto")]
    [Tooltip("Fuerza inicial del salto vertical.")]
    [SerializeField] private float jumpForce = 10f;
    [Tooltip("Gravedad personalizada aplicada al salto.")]
    [SerializeField] private float gravity = -25f;

    [Header("Deslizamiento (Slide)")]
    [Tooltip("Duración física del deslizamiento en segundos.")]
    [SerializeField] private float slideDuration = 1.0f;
    [Tooltip("Multiplicador de altura del CharacterController durante el deslizamiento.")]
    [SerializeField] private float slideHeightMultiplier = 0.5f;

    [Header("Animación")]
    [Tooltip("Referencia al componente Animator del personaje.")]
    [SerializeField] private Animator animator;

    [Tooltip("Trigger que activa la animación de choque contra obstáculo.")]
    [SerializeField] private string hitObstacleTrigger = "HitObstacle";

    private CharacterController controller;
    private int desiredLane = 1; // 0 = Izquierda, 1 = Centro, 2 = Derecha
    private float yVelocity = 0f;
    private bool isGrounded;
    private bool isControlEnabled = true;
    private float initialForwardSpeed;

    private float originalHeight;
    private Vector3 originalCenter;
    private bool isSliding = false;
    private Coroutine slideCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        controller = GetComponent<CharacterController>();
        initialForwardSpeed = forwardSpeed;

        // Guardar valores de colisión originales
        originalHeight = controller.height;
        originalCenter = controller.center;

        // Buscar el Animator en los hijos si no está explícitamente asignado
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Start()
    {
        // Suscribirse a los eventos de entrada de deslizamiento normal
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnSwipeLeft += MoveLeft;
            InputManager.Instance.OnSwipeRight += MoveRight;
            InputManager.Instance.OnSwipeUp += Jump;
            InputManager.Instance.OnSwipeDown += Slide;
        }

        // Suscribirse al evento de salto por sensor de movimiento
        if (MotionJumpDetector.Instance != null)
        {
            MotionJumpDetector.Instance.OnMotionJump += Jump;
        }
    }

    private void OnDestroy()
    {
        // Desuscribirse para evitar fugas de memoria
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnSwipeLeft -= MoveLeft;
            InputManager.Instance.OnSwipeRight -= MoveRight;
            InputManager.Instance.OnSwipeUp -= Jump;
            InputManager.Instance.OnSwipeDown -= Slide;
        }

        if (MotionJumpDetector.Instance != null)
        {
            MotionJumpDetector.Instance.OnMotionJump -= Jump;
        }
    }

    private void Update()
    {
        if (!isControlEnabled) return;

        // Incrementar la velocidad del juego progresivamente
        if (forwardSpeed < maxSpeed && GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            forwardSpeed += speedIncreaseRate * Time.deltaTime;
        }

        // Determinar si tocamos el suelo (usando Raycast seguro desde el centro del CharacterController)
        Vector3 raycastStart = transform.TransformPoint(controller.center);
        float raycastDistance = (controller.height / 2f) + 0.1f;
        bool groundCheck = controller.isGrounded || Physics.Raycast(raycastStart, Vector3.down, raycastDistance);
        
        // Si nos estamos moviendo hacia arriba (salto activo), forzamos isGrounded a falso para evitar que el Animator cancele el salto inmediatamente
        isGrounded = groundCheck && yVelocity <= 0;

        // Aplicar gravedad (sólo reseteamos a -0.1f si tocamos el suelo y no estamos saltando hacia arriba)
        if (isGrounded && yVelocity <= 0)
        {
            yVelocity = -0.1f; // Pequeña fuerza hacia abajo para mantener el contacto con el suelo
        }
        else
        {
            yVelocity += gravity * Time.deltaTime;
        }

        // Calcular posición objetivo en X basado en el carril deseado
        float targetX = (desiredLane - 1) * laneDistance;

        // Interpolar en el eje X para suavizar el cambio de carril
        float nextX = Mathf.MoveTowards(transform.position.x, targetX, laneSwitchSpeed * Time.deltaTime);
        float deltaX = nextX - transform.position.x;

        // Construir vector de movimiento
        // El movimiento en Z es continuo
        // El movimiento en Y es afectado por la velocidad vertical (salto/gravedad)
        Vector3 motion = new Vector3(deltaX, yVelocity * Time.deltaTime, forwardSpeed * Time.deltaTime);

        // Mover personaje con el CharacterController
        controller.Move(motion);

        // Actualizar parámetros del Animator
        if (animator != null)
        {
            animator.SetBool("isGrounded", isGrounded);
            animator.SetFloat("speed", forwardSpeed);
        }
    }

    public void Jump()
    {
        if (isGrounded && isControlEnabled && GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            // Cancelar el deslizamiento si estamos saltando
            if (isSliding)
            {
                StopSlide();
            }

            yVelocity = jumpForce;
            
            if (animator != null)
            {
                animator.SetTrigger("triggerJump");
            }
            
            Debug.Log("[Player] Saltando!");
        }
    }

    public void Slide()
    {
        if (isGrounded && isControlEnabled && !isSliding && GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            slideCoroutine = StartCoroutine(SlideCoroutine());
        }
    }

    private IEnumerator SlideCoroutine()
    {
        isSliding = true;

        if (animator != null)
        {
            animator.SetBool("isSliding", true);
        }

        // Reducir la altura y ajustar el centro del CharacterController para pasar por debajo de obstáculos
        float targetHeight = originalHeight * slideHeightMultiplier;
        controller.height = targetHeight;
        controller.center = new Vector3(originalCenter.x, (originalCenter.y - originalHeight / 2f) + targetHeight / 2f, originalCenter.z);

        yield return new WaitForSeconds(slideDuration);

        // Restaurar altura y centro originales
        controller.height = originalHeight;
        controller.center = originalCenter;
        
        isSliding = false;

        if (animator != null)
        {
            animator.SetBool("isSliding", false);
        }
    }

    private void StopSlide()
    {
        if (!isSliding) return;

        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
        }

        controller.height = originalHeight;
        controller.center = originalCenter;
        isSliding = false;

        if (animator != null)
        {
            animator.SetBool("isSliding", false);
        }
    }

    private void MoveLeft()
    {
        if (!isControlEnabled) return;
        if (desiredLane > 0)
        {
            desiredLane--;
        }
    }

    private void MoveRight()
    {
        if (!isControlEnabled) return;
        if (desiredLane < 2)
        {
            desiredLane++;
        }
    }

    public void DisableControls()
    {
        isControlEnabled = false;
        forwardSpeed = 0f;
        StopSlide();
    }

    public void EnableControls()
    {
        isControlEnabled = true;
        desiredLane = 1;
        yVelocity = 0f;
        forwardSpeed = initialForwardSpeed;
    }

    // Detección de colisiones para monedas y obstáculos que no utilicen CharacterController.Move() directamente
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            HitObstacle();
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Alternativa para detectar colisiones físicas con obstáculos etiquetados
        if (hit.gameObject.CompareTag("Obstacle"))
        {
            HitObstacle();
        }
    }

    private void HitObstacle()
    {
        if (!isControlEnabled) return;
        
        Debug.Log("[Player] Chocó con un obstáculo!");

        if (animator != null)
        {
            animator.SetTrigger(hitObstacleTrigger);
        }

        DisableControls();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }
}
