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

    private CharacterController controller;
    private int desiredLane = 1; // 0 = Izquierda, 1 = Centro, 2 = Derecha
    private float yVelocity = 0f;
    private bool isGrounded;
    private bool isControlEnabled = true;
    private float initialForwardSpeed;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        controller = GetComponent<CharacterController>();
        initialForwardSpeed = forwardSpeed;
    }

    private void Start()
    {
        // Suscribirse a los eventos de entrada de deslizamiento normal
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnSwipeLeft += MoveLeft;
            InputManager.Instance.OnSwipeRight += MoveRight;
            InputManager.Instance.OnSwipeUp += Jump;
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

        // Determinar si tocamos el suelo (usando Raycast como respaldo para mayor confiabilidad)
        isGrounded = controller.isGrounded || Physics.Raycast(transform.position, Vector3.down, (controller.height / 2f) + 0.1f);

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
    }

    public void Jump()
    {
        if (isGrounded && isControlEnabled && GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            yVelocity = jumpForce;
            Debug.Log("[Player] Saltando!");
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
        DisableControls();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }
}
