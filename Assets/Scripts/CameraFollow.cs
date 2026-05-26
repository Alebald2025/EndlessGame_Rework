using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo")]
    [Tooltip("El transform del jugador a seguir.")]
    [SerializeField] private Transform target;

    [Header("Desplazamiento (Offset)")]
    [Tooltip("Distancia relativa entre la cámara y el jugador.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 3.35f, -7.84f);

    [Header("Suavizado")]
    [Tooltip("Velocidad de seguimiento de la cámara.")]
    [SerializeField] private float smoothSpeed = 10f;
    [Tooltip("¿Seguir al jugador instantáneamente en el eje de avance (Z)?")]
    [SerializeField] private bool instantZFollow = true;

    private void Start()
    {
        // Intentar encontrar al jugador si no está asignado
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                target = playerObj.transform;
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Calcular la posición deseada
        Vector3 desiredPosition = target.position + offset;

        // Suavizar la posición en X e Y
        float targetX = Mathf.Lerp(transform.position.x, desiredPosition.x, smoothSpeed * Time.deltaTime);
        float targetY = Mathf.Lerp(transform.position.y, desiredPosition.y, smoothSpeed * Time.deltaTime);
        
        // En Z, solemos querer seguimiento instantáneo para que la cámara no se quede atrás a gran velocidad
        float targetZ = instantZFollow ? desiredPosition.z : Mathf.Lerp(transform.position.z, desiredPosition.z, smoothSpeed * Time.deltaTime);

        // Aplicar la nueva posición
        transform.position = new Vector3(targetX, targetY, targetZ);
    }
}
