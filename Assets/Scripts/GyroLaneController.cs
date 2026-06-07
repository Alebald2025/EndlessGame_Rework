using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Sensor 2: Giroscopio — cambia de carril inclinando el dispositivo.
/// Izquierda  → carril izquierdo.
/// Derecha → carril derecho.
///
/// Requiere "Active Input Handling" = "Input System Package (New)" en Player Settings.
/// </summary>
public class GyroLaneController : MonoBehaviour
{
    public static GyroLaneController Instance { get; private set; }

    [Header("Configuración del sensor")]
    [Tooltip("Ángulo mínimo de inclinación (en grados) para cambiar de carril.")]
    [Range(5f, 45f)]
    [SerializeField] private float tiltThreshold = 18f;

    [Tooltip("Segundos entre cambios de carril por inclinación (evita cambios involuntarios).")]
    [SerializeField] private float switchCooldown = 0.5f;

    [Header("Calibración")]
    [Tooltip("Si es TRUE, al iniciar se guarda la orientación actual como posición neutra.")]
    [SerializeField] private bool calibrateOnStart = true;

    [Header("Debug")]
    [SerializeField] private bool verboseLogging = false;

    // Telemetría pública (útil para una UI de calibración)
    [HideInInspector] public float currentTiltAngle;
    [HideInInspector] public bool sensorReady = false;

    private AttitudeSensor attitudeSensor;
    private Quaternion neutralAttitude = Quaternion.identity;
    private float lastSwitchTime = -999f;

    // ── Inicialización ────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        StartCoroutine(InitAttitudeSensor());
    }

    private IEnumerator InitAttitudeSensor()
    {
        int attempts = 0;
        const int maxAttempts = 10;

        while (attempts < maxAttempts)
        {
            attitudeSensor = AttitudeSensor.current;

            if (attitudeSensor != null)
            {
                InputSystem.EnableDevice(attitudeSensor);
                yield return null; // esperar un frame para que lleguen datos

                if (calibrateOnStart)
                    Calibrate();

                sensorReady = true;
                Debug.Log($"[GyroLane] AttitudeSensor listo en intento {attempts + 1}. " +
                          $"Neutro: {neutralAttitude.eulerAngles}");
                yield break;
            }

            attempts++;
            Debug.LogWarning($"[GyroLane] AttitudeSensor null (intento {attempts}/{maxAttempts}). Reintentando...");
            yield return new WaitForSeconds(0.2f);
        }

        Debug.LogError("[GyroLane] No se pudo inicializar el AttitudeSensor. " +
                       "El control por inclinación no estará disponible.");
    }

    private void OnDisable()
    {
        if (attitudeSensor != null)
            InputSystem.DisableDevice(attitudeSensor);
    }

    // ── Update ────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!sensorReady || attitudeSensor == null || !attitudeSensor.enabled) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)        return;

        // Orientación actual relativa a la posición neutra calibrada
        Quaternion currentAttitude = attitudeSensor.attitude.ReadValue();
        Quaternion relativeRotation = Quaternion.Inverse(neutralAttitude) * currentAttitude;

        // Extraer el ángulo de inclinación lateral (eje Z en modo portrait)
        currentTiltAngle = relativeRotation.eulerAngles.z;

        // Convertir de [0, 360] a [-180, 180] para tener signos claros
        if (currentTiltAngle > 180f) currentTiltAngle -= 360f;

        if (verboseLogging)
            Debug.Log($"[GyroLane] Tilt: {currentTiltAngle:F1}°  Umbral: ±{tiltThreshold}°");

        // Cooldown activo → no procesar
        if (Time.time - lastSwitchTime < switchCooldown) return;

        // Inclinación suficiente → cambiar carril
        if (currentTiltAngle < -tiltThreshold)
        {
            TriggerLaneChange(right: true);
        }
        else if (currentTiltAngle > tiltThreshold)
        {
            TriggerLaneChange(right: false);
        }
    }

    // ── Lógica de cambio de carril ────────────────────────────────────────

    private void TriggerLaneChange(bool right)
    {
        lastSwitchTime = Time.time;

        if (right)
        {
            InputManager.Instance?.InvokeSwipeRight();
            Debug.Log($"[GyroLane] → Carril DERECHO (tilt: {currentTiltAngle:F1}°)");
        }
        else
        {
            InputManager.Instance?.InvokeSwipeLeft();
            Debug.Log($"[GyroLane] ← Carril IZQUIERDO (tilt: {currentTiltAngle:F1}°)");
        }
    }

    // ── API pública ───────────────────────────────────────────────────────

    /// <summary>
    /// Guarda la orientación actual como posición neutra (posición de reposo del móvil).
    /// Se puede llamar desde un botón "Recalibrar" en la UI.
    /// </summary>
    public void Calibrate()
    {
        if (attitudeSensor == null) return;
        neutralAttitude = attitudeSensor.attitude.ReadValue();
        Debug.Log($"[GyroLane] Calibrado. Neutro: {neutralAttitude.eulerAngles}");
    }
}
