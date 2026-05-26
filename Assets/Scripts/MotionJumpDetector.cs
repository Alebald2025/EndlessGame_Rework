using UnityEngine;
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
        lastAcceleration = Input.acceleration;
        lastJumpTime = -jumpCooldown;
    }

    private void Update()
    {
        currentRawAcceleration = Input.acceleration;

        // Calcular el cambio de aceleración (Jerk / Tirón)
        // Delta de aceleración entre este frame y el anterior
        Vector3 accelDelta = currentRawAcceleration - lastAcceleration;

        // Medir la fuerza del tirón en las direcciones deseadas
        float forceY = monitorYAxis ? Mathf.Max(0, accelDelta.y) : 0f;
        // El movimiento hacia adelante/arriba suele registrarse en el eje Z positivo o negativo según inclinación.
        // Al sacudir hacia arriba inclinando la pantalla hacia uno, el eje Z suele experimentar un cambio rápido positivo.
        float forceZ = monitorZAxis ? Mathf.Abs(accelDelta.z) : 0f;

        // Tomamos el valor de fuerza más alto detectado en los ejes configurados
        // Lo dividimos por Time.deltaTime para obtener la tasa de cambio real (independiente de FPS)
        // O bien usamos el delta absoluto escalado. Dividir por deltaTime da valores más grandes pero estables ante fluctuaciones de fotogramas.
        float dt = Time.deltaTime > 0 ? Time.deltaTime : 0.02f;
        currentJerkForce = Mathf.Max(forceY, forceZ) / dt;

        // Registrar el pico máximo para calibración
        if (currentJerkForce > maxJerkForceRecorded)
        {
            maxJerkForceRecorded = currentJerkForce;
        }

        // Comprobar si supera el umbral y ha pasado el cooldown
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
