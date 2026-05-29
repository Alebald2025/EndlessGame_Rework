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
    [Tooltip("Sonido que se reproduce al recolectar la moneda.")]
    [SerializeField] private AudioClip coinSound;
    [Tooltip("Volumen del sonido de recolección (0 = silencio, 1 = máximo).")]
    [SerializeField] [Range(0f, 1f)] private float coinSoundVolume = 1f;

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

    // OnTriggerStay cubre el caso de aterrizar encima de la moneda desde un salto
    private void OnTriggerStay(Collider other)
    {
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

        // Instanciar partículas en la posición de la moneda y destruirlas tras 2 segundos
        if (collectParticlePrefab != null)
        {
            GameObject particles = Instantiate(collectParticlePrefab, transform.position, Quaternion.identity);
            Destroy(particles, 2f);
        }

        // Reproducir sonido de recolección antes de destruir el objeto.
        // Se usa PlayClipAtPoint para que el sonido no se corte al destruir el GameObject.
        if (coinSound != null)
        {
            AudioSource.PlayClipAtPoint(coinSound, transform.position, coinSoundVolume);
        }

        // Destruir la moneda de la escena
        Destroy(gameObject);
    }
}
