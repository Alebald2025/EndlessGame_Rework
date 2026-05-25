using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Efectos de la Moneda")]
    [Tooltip("Velocidad de rotación visual de la moneda en grados por segundo.")]
    [SerializeField] private float rotationSpeed = 100f;
    [Tooltip("Puntos que otorga esta moneda al ser recogida.")]
    [SerializeField] private int scoreValue = 10;
    [Tooltip("Efecto de partículas opcional que se genera al recolectar.")]
    [SerializeField] private GameObject collectParticlePrefab;

    private void Update()
    {
        // Hacer rotar la moneda para que tenga dinamismo visual
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Comprobar si quien entra es el jugador
        if (other.CompareTag("Player"))
        {
            CollectCoin();
        }
    }

    private void CollectCoin()
    {
        // Sumar moneda y puntaje en el GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CollectCoin(scoreValue);
        }

        // Instanciar partículas en la posición de la moneda
        if (collectParticlePrefab != null)
        {
            Instantiate(collectParticlePrefab, transform.position, Quaternion.identity);
        }

        // Destruir la moneda de la escena
        Destroy(gameObject);
    }
}
