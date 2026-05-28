using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Gestiona la cámara del juego en tres estados:
/// Menu     → Frente al jugador (cara a cara), con efecto de flotación suave.
/// Transitioning → Transición animada desde la vista frontal hasta la vista de juego.
/// Gameplay → Seguimiento estándar desde detrás/arriba del jugador.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Estado de la cámara
    // ──────────────────────────────────────────────
    public enum CameraState { Menu, Transitioning, Gameplay }
    public CameraState State { get; private set; } = CameraState.Menu;

    // ──────────────────────────────────────────────
    //  Configuración: Gameplay
    // ──────────────────────────────────────────────
    [Header("Objetivo")]
    [Tooltip("El transform del jugador a seguir.")]
    [SerializeField] private Transform target;

    [Header("Desplazamiento en Juego (Offset)")]
    [Tooltip("Distancia relativa entre la cámara y el jugador durante el gameplay.")]
    [SerializeField] private Vector3 gameplayOffset = new Vector3(0f, 3.35f, -7.84f);

    // (El seguimiento de gameplay es ahora instantáneo y rígido, sin velocidad de suavizado)

    // ──────────────────────────────────────────────
    //  Configuración: Menú (Vista Frontal / Cara a Cara)
    // ──────────────────────────────────────────────
    [Header("Vista de Menú Principal")]
    [Tooltip("Desplazamiento respecto al jugador para la vista cara a cara del menú.\nValor positivo en Z = frente al jugador.")]
    [SerializeField] private Vector3 menuOffset = new Vector3(0f, 1.6f, 3.2f);

    [Tooltip("Punto del cuerpo del jugador al que mira la cámara en el menú (offset desde su pivot).")]
    [SerializeField] private Vector3 menuLookAtOffset = new Vector3(0f, 1.0f, 0f);

    [Tooltip("¿Activar efecto de flotación suave de la cámara durante el menú?")]
    [SerializeField] private bool enableMenuFloating = true;

    [Tooltip("Velocidad del efecto de flotación (bobbing) del menú.")]
    [SerializeField] private float floatingSpeed = 0.8f;

    [Tooltip("Amplitud vertical del efecto de flotación del menú.")]
    [SerializeField] private float floatingAmplitude = 0.06f;

    // ──────────────────────────────────────────────
    //  Configuración: Transición
    // ──────────────────────────────────────────────
    [Header("Transición al Gameplay")]
    [Tooltip("Duración en segundos de la animación de transición.")]
    [SerializeField] private float transitionDuration = 1.5f;

    [Tooltip("Curva de animación para la transición (deja en Default para una curva suave tipo ease-in-out).")]
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // ──────────────────────────────────────────────
    //  Variables internas
    // ──────────────────────────────────────────────
    private float floatingTimeOffset;
    private Coroutine transitionCoroutine;

    // ──────────────────────────────────────────────
    //  Inicialización
    // ──────────────────────────────────────────────
    private void Start()
    {
        // Intentar encontrar al jugador si no está asignado en el Inspector
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
        }

        // Desfase aleatorio para que el efecto de flotación no empiece siempre igual
        floatingTimeOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

        // Posicionar la cámara en el estado inicial correcto desde el primer frame
        if (target != null)
            SnapToMenuPosition();
    }

    // ──────────────────────────────────────────────
    //  Update
    // ──────────────────────────────────────────────
    private void LateUpdate()
    {
        if (target == null) return;

        switch (State)
        {
            case CameraState.Menu:
                UpdateMenuCamera();
                break;

            case CameraState.Gameplay:
                UpdateGameplayCamera();
                break;

            // CameraState.Transitioning se gestiona completamente en la corrutina
        }
    }

    // ──────────────────────────────────────────────
    //  Comportamiento: Menú (Cara a Cara)
    // ──────────────────────────────────────────────
    private void SnapToMenuPosition()
    {
        Vector3 menuPos = GetMenuPosition();
        transform.position = menuPos;
        transform.LookAt(target.position + menuLookAtOffset);
    }

    private void UpdateMenuCamera()
    {
        Vector3 menuPos = GetMenuPosition();

        // Añadir efecto de flotación suave (bobbing) si está activado
        if (enableMenuFloating)
        {
            float floatY = Mathf.Sin((Time.time * floatingSpeed) + floatingTimeOffset) * floatingAmplitude;
            menuPos.y += floatY;
        }

        transform.position = menuPos;
        transform.LookAt(target.position + menuLookAtOffset);
    }

    private Vector3 GetMenuPosition()
    {
        // La posición del menú es el offset relativo a la posición Y/X del jugador,
        // pero en frente de él (positivo en Z del mundo, porque el jugador mirará hacia +Z al correr)
        return new Vector3(
            target.position.x + menuOffset.x,
            target.position.y + menuOffset.y,
            target.position.z + menuOffset.z
        );
    }

    // ──────────────────────────────────────────────
    //  Comportamiento: Gameplay (Seguimiento trasero)
    // ──────────────────────────────────────────────
    private void UpdateGameplayCamera()
    {
        // Posicionar de manera instantánea y rígida detrás del jugador
        transform.position = target.position + gameplayOffset;

        // Rotar instantáneamente para mirar al jugador
        Vector3 lookTarget = target.position + new Vector3(0f, 0.5f, 0f);
        transform.rotation = Quaternion.LookRotation(lookTarget - transform.position);
    }

    // ──────────────────────────────────────────────
    //  API Pública: Iniciar Transición
    // ──────────────────────────────────────────────

    /// <summary>
    /// Inicia la transición animada desde la vista de menú hacia la vista de gameplay.
    /// Llama al callback <paramref name="onComplete"/> al finalizar.
    /// </summary>
    public void StartTransitionToGameplay(Action onComplete = null)
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(TransitionCoroutine(onComplete));
    }

    private IEnumerator TransitionCoroutine(Action onComplete)
    {
        State = CameraState.Transitioning;

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float curvedT = transitionCurve.Evaluate(t);

            // Posición objetivo de gameplay en este frame (el jugador puede estar moviéndose)
            Vector3 targetPosition = target.position + gameplayOffset;
            Quaternion targetRotation = Quaternion.LookRotation((target.position + new Vector3(0f, 0.5f, 0f)) - targetPosition);

            transform.position = Vector3.Lerp(startPosition, targetPosition, curvedT);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, curvedT);

            yield return null;
        }

        // Aseguramos la posición final exacta
        State = CameraState.Gameplay;
        transitionCoroutine = null;

        onComplete?.Invoke();
    }

    /// <summary>
    /// Vuelve la cámara al estado de menú instantáneamente (usado al reiniciar la escena).
    /// </summary>
    public void ResetToMenu()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        State = CameraState.Menu;

        if (target != null)
            SnapToMenuPosition();
    }
}
