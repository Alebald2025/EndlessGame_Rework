using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class MotionJumpDetector : MonoBehaviour
{
    public static MotionJumpDetector Instance { get; private set; }

    public event Action OnMotionJump;

    [Header("Configuración del Sensor")]
    [Tooltip("Umbral de fuerza del tirón (jerk) necesario para saltar. Valores típicos entre 1.5 y 3.5.")]
    [Range(0.5f, 5.0f)]
    public float jumpThreshold = 2.0f;

    [Tooltip("Tiempo de espera mínimo (cooldown) entre saltos detectados por movimiento en segundos.")]
    [SerializeField] private float jumpCooldown = 0.8f;

    [Header("Ejes a Monitorear")]
    [Tooltip("Monitorear movimiento arriba-abajo (eje Y local del teléfono). Es el estándar al levantar el móvil.")]
    [SerializeField] private bool monitorYAxis = true;
    
    [Tooltip("Monitorear movimiento adelante-atrás (eje Z local del teléfono). Útil si el tirón es hacia adelante.")]
    [SerializeField] private bool monitorZAxis = true;

    // Variables de telemetría para calibración
    [HideInInspector] public float currentJerkForce;
    [HideInInspector] public float maxJerkForceRecorded;
    [HideInInspector] public Vector3 currentRawAcceleration;

    private Vector3 lastAcceleration;
    private float lastJumpTime;
    private bool hasAccelerometer;

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

    private void Start()
    {
        var accelerometer = Accelerometer.current;
        hasAccelerometer = accelerometer != null;

        if (hasAccelerometer)
        {
            lastAcceleration = accelerometer.acceleration.ReadValue();
        }
        else
        {
            lastAcceleration = Vector3.zero;
            Debug.LogWarning("[MotionJumpDetector] Acelerómetro no disponible en este dispositivo.");
        }

        lastJumpTime = -jumpCooldown;
    }

    private void Update()
    {
        if (!hasAccelerometer)
            return;

        var accelerometer = Accelerometer.current;
        if (accelerometer == null)
            return;

        currentRawAcceleration = accelerometer.acceleration.ReadValue();

        Vector3 accelDelta = currentRawAcceleration - lastAcceleration;
        float forceY = monitorYAxis ? Mathf.Max(0, accelDelta.y) : 0f;
        float forceZ = monitorZAxis ? Mathf.Abs(accelDelta.z) : 0f;

        float dt = Time.deltaTime > 0 ? Time.deltaTime : 0.02f;
        currentJerkForce = Mathf.Max(forceY, forceZ) / dt;

        if (currentJerkForce > maxJerkForceRecorded)
        {
            maxJerkForceRecorded = currentJerkForce;
        }

        if (currentJerkForce >= jumpThreshold && (Time.time - lastJumpTime) >= jumpCooldown)
        {
            TriggerJump();
        }

        lastAcceleration = currentRawAcceleration;
    }

    private void TriggerJump()
    {
        lastJumpTime = Time.time;
        OnMotionJump?.Invoke();
        Debug.Log($"[MotionJump] ¡Salto detectado! Fuerza: {currentJerkForce:F2} (Umbral: {jumpThreshold})");
    }

    // Método público para reiniciar el pico máximo en la UI de calibración
    public void ResetMaxJerkRecord()
    {
        maxJerkForceRecorded = 0f;
    }
}
