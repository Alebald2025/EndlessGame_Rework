using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Movimiento Lateral")]
    [SerializeField] private float laneDistance = 2.5f;
    [SerializeField] private float laneSwitchSpeed = 15f;

    [Header("Movimiento Adelante")]
    public float forwardSpeed = 5f;
    [SerializeField] private float speedIncreaseRate = 0.05f;
    [SerializeField] private float maxSpeed = 20f;

    [Header("Salto")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float gravity = -25f;

    [Header("Deslizamiento (Slide)")]
    [SerializeField] private float slideDuration = 1.0f;
    [SerializeField] private float airSlideDuration = 0.6f; // Más corto en el aire
    [SerializeField] private float slideHeightMultiplier = 0.5f;

    [Header("Animación")]
    [SerializeField] private Animator animator;
    [SerializeField] private string hitObstacleTrigger = "HitObstacle";

    private CharacterController controller;
    private int desiredLane = 1;
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

        originalHeight = controller.height;
        originalCenter = controller.center;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnSwipeLeft += MoveLeft;
            InputManager.Instance.OnSwipeRight += MoveRight;
            InputManager.Instance.OnSwipeUp += Jump;
            InputManager.Instance.OnSwipeDown += Slide;
        }

        if (MotionJumpDetector.Instance != null)
            MotionJumpDetector.Instance.OnMotionJump += Jump;
    }

    private void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnSwipeLeft -= MoveLeft;
            InputManager.Instance.OnSwipeRight -= MoveRight;
            InputManager.Instance.OnSwipeUp -= Jump;
            InputManager.Instance.OnSwipeDown -= Slide;
        }

        if (MotionJumpDetector.Instance != null)
            MotionJumpDetector.Instance.OnMotionJump -= Jump;
    }

    private void Update()
    {
        if (!isControlEnabled) return;

        if (forwardSpeed < maxSpeed && GameManager.Instance?.IsPlaying == true)
            forwardSpeed += speedIncreaseRate * Time.deltaTime;

        // Ground check
        Vector3 raycastStart = transform.TransformPoint(controller.center);
        float raycastDistance = (controller.height / 2f) + 0.1f;
        bool groundCheck = controller.isGrounded || Physics.Raycast(raycastStart, Vector3.down, raycastDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        isGrounded = groundCheck && yVelocity <= 0;

        if (isGrounded && yVelocity <= 0)
            yVelocity = -0.1f;
        else
            yVelocity += gravity * Time.deltaTime;

        // Movimiento lateral
        float targetX = (desiredLane - 1) * laneDistance;
        float nextX = Mathf.MoveTowards(transform.position.x, targetX, laneSwitchSpeed * Time.deltaTime);
        float deltaX = nextX - transform.position.x;

        Vector3 motion = new Vector3(deltaX, yVelocity * Time.deltaTime, forwardSpeed * Time.deltaTime);
        controller.Move(motion);

        if (animator != null)
        {
            animator.SetBool("isGrounded", isGrounded);
            animator.SetFloat("speed", forwardSpeed);
        }
    }

    public void Jump()
    {
        if (isGrounded && isControlEnabled && GameManager.Instance?.IsPlaying == true)
        {
            if (isSliding) StopSlide();

            yVelocity = jumpForce;

            if (animator != null)
                animator.SetTrigger("triggerJump");

            Debug.Log("[Player] Saltando!");
        }
    }

    public void Slide()
    {
        if (!isControlEnabled || GameManager.Instance?.IsPlaying != true) return;

        // === SLIDE EN EL SUELO ===
        if (isGrounded && !isSliding)
        {
            slideCoroutine = StartCoroutine(SlideCoroutine(false));
        }
        // === CANCELAR SALTO Y HACER DIVE ===
        else if (!isGrounded && !isSliding)
        {
            // Cancelar impulso hacia arriba
            if (yVelocity > 0)
                yVelocity = -jumpForce * 0.6f; // Baja más rápido

            slideCoroutine = StartCoroutine(SlideCoroutine(true));
            Debug.Log("[Player] Salto cancelado → Dive/Slide en el aire");
        }
    }

    private IEnumerator SlideCoroutine(bool isAirSlide)
    {
        isSliding = true;

        if (animator != null)
            animator.SetBool("isSliding", true);

        float duration = isAirSlide ? airSlideDuration : slideDuration;

        if (!isAirSlide)
        {
            // Slide normal en suelo
            float targetHeight = originalHeight * slideHeightMultiplier;
            controller.height = targetHeight;
            controller.center = new Vector3(originalCenter.x, (originalCenter.y - originalHeight / 2f) + targetHeight / 2f, originalCenter.z);
        }

        yield return new WaitForSeconds(duration);

        RestoreFromSlide();
    }

    private void RestoreFromSlide()
    {
        if (!isSliding) return;

        controller.height = originalHeight;
        controller.center = originalCenter;
        isSliding = false;

        if (animator != null)
            animator.SetBool("isSliding", false);
    }

    private void StopSlide()
    {
        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);

        RestoreFromSlide();
    }

    // Resto de métodos sin cambios (MoveLeft, MoveRight, DisableControls, etc.)
    private void MoveLeft()
    {
        if (!isControlEnabled) return;
        if (desiredLane > 0) desiredLane--;
    }

    private void MoveRight()
    {
        if (!isControlEnabled) return;
        if (desiredLane < 2) desiredLane++;
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle")) HitObstacle();
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("Obstacle")) HitObstacle();
    }

    private void HitObstacle()
    {
        if (!isControlEnabled) return;

        if (animator != null)
            animator.SetTrigger(hitObstacleTrigger);

        DisableControls();
        GameManager.Instance?.GameOver();
    }
}