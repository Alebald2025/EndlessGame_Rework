using UnityEngine;

public class Obstacle : MonoBehaviour
{

    private void Start()
    {
        // Asegurarse de que el obstáculo tenga la etiqueta correcta asignada automáticamente
        if (!gameObject.CompareTag("Obstacle"))
        {
            gameObject.tag = "Obstacle";
        }
    }
}
