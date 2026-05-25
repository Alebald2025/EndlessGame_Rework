using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("Configuración del Obstáculo")]
    [Tooltip("¿El obstáculo es destruible o inmóvil?")]
    [SerializeField] private bool isDestructible = false;

    private void Start()
    {
        // Asegurarse de que el obstáculo tenga la etiqueta correcta asignada automáticamente
        if (!gameObject.CompareTag("Obstacle"))
        {
            gameObject.tag = "Obstacle";
        }
    }
}
