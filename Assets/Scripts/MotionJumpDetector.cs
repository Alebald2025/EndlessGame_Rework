using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;

/// <summary>
/// Detecta un gesto brusco del acelerómetro para saltar.
/// Requiere "Active Input Handling" = "Input System Package (New)" en Player Settings.
/// </summary>
public class MotionJumpDetector : MonoBehaviour
{
    public static MotionJumpDetector Instance { get; private set; }

    public event Action OnMotionJump;

    [Header("Configuración del Sensor")]
    [Tooltip("Umbral de cambio brusco de aceleración (en g) para saltar. Típico: 0.4–1.0.")]
    [Range(0.1f, 3.0f)]
    public float jumpThreshold = 0.5f;

    [Tooltip("Cooldown en segundos entre saltos detectados por movimiento.")]
    [SerializeField] private float jumpCooldown = 0.8f;

    [Header("Ejes a Monitorear")]
    [Tooltip("Eje Y: levantar el móvil hacia arriba.")]
    [SerializeField] private bool monitorYAxis = true;
    [Tooltip("Eje Z: empujar el móvil hacia adelante.")]
    [SerializeField] private bool monitorZAxis = true;

    [Header("Debug")]
    [Tooltip("Activa logs detallados en la consola para calibrar el umbral.")]
    [SerializeField] private bool verboseLogging = true;

    // Telemetría pública para la UI de calibración
    [HideInInspector] public float currentJerkForce;
    [HideInInspector] public float maxJerkForceRecorded;
    [HideInInspector] public Vector3 currentRawAcceleration;

    private Vector3 lastAcceleration;
    private float lastJumpTime;
    private Accelerometer accelerometer;
    private bool sensorReady = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        lastJumpTime = -jumpCooldown;
        StartCoroutine(InitAccelerometer());
    }

    /// <summary>
    /// Intenta inicializar el acelerómetro con reintentos, ya que Accelerometer.current
    /// puede ser null los primeros frames si el Input System aún no ha registrado el dispositivo.
    /// </summary>
    private IEnumerator InitAccelerometer()
    {
        int attempts = 0;
        const int maxAttempts = 10;

        while (attempts < maxAttempts)
        {
            accelerometer = Accelerometer.current;

            if (accelerometer != null)
            {
                InputSystem.EnableDevice(accelerometer);
                // Esperar un frame para que el sensor empiece a emitir datos
                yield return null;
                lastAcceleration = accelerometer.acceleration.ReadValue();
                sensorReady = true;
                Debug.Log($"[MotionJump] Acelerómetro listo en intento {attempts + 1}. Valor inicial: {lastAcceleration}");
                yield break;
            }

            attempts++;
            Debug.LogWarning($"[MotionJump] Accelerometer.current es null (intento {attempts}/{maxAttempts}). Reintentando...");
            yield return new WaitForSeconds(0.2f);
        }

        Debug.LogError("[MotionJump] No se pudo inicializar el acelerómetro. El salto por movimiento no estará disponible.");
    }

    private void OnDisable()
    {
        if (accelerometer != null)
        {
            InputSystem.DisableDevice(accelerometer);
        }
    }

    private void Update()
    {
        if (!sensorReady) return;

        // Re-verificar por si el dispositivo se desconectó y reconectó (ej. al minimizar la app)
        if (accelerometer == null || !accelerometer.enabled)
        {
            accelerometer = Accelerometer.current;
            if (accelerometer != null)
            {
                InputSystem.EnableDevice(accelerometer);
                lastAcceleration = accelerometer.acceleration.ReadValue();
                Debug.Log("[MotionJump] Acelerómetro re-habilitado.");
            }
            return;
        }

        currentRawAcceleration = accelerometer.acceleration.ReadValue();

        Vector3 delta = currentRawAcceleration - lastAcceleration;
        float forceY = monitorYAxis ? Mathf.Max(delta.y, 0f) : 0f; // Solo hacia arriba (delta positivo)
        float forceZ = monitorZAxis ? Mathf.Abs(delta.z) : 0f;
        currentJerkForce = Mathf.Max(forceY, forceZ);

        if (currentJerkForce > maxJerkForceRecorded)
            maxJerkForceRecorded = currentJerkForce;

        if (verboseLogging && currentJerkForce > 0.05f)
        {
            Debug.Log($"[MotionJump] Accel: {currentRawAcceleration:F3}  Delta-g: {currentJerkForce:F3}  (Y:{delta.y:F3} Z:{delta.z:F3})  Umbral: {jumpThreshold}");
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
        Debug.Log($"[MotionJump] *** SALTO DETECTADO *** Delta-g: {currentJerkForce:F3}");
    }

    /// <summary>
    /// Llama este método desde un botón de UI para verificar que el salto
    /// funciona correctamente, independientemente del acelerómetro.
    /// </summary>
    public void TestJumpFromButton()
    {
        Debug.Log("[MotionJump] TEST MANUAL desde botón.");
        TriggerJump();
    }

    public void ResetMaxJerkRecord()
    {
        maxJerkForceRecorded = 0f;
    }
}
