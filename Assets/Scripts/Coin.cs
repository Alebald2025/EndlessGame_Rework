using UnityEngine;
using DG.Tweening;

public class Coin : MonoBehaviour
{
    [Header("Efectos de la Moneda")]
    [Tooltip("Puntos que otorga esta moneda al ser recogida.")]
    [SerializeField] private int scoreValue = 10;
    [Tooltip("Efecto de partículas opcional que se genera al recolectar.")]
    [SerializeField] private GameObject collectParticlePrefab;
    [Tooltip("Sonido que se reproduce al recolectar la moneda.")]
    [SerializeField] private AudioClip coinSound;
    [Tooltip("Volumen del sonido de recolección (0 = silencio, 1 = máximo).")]
    [SerializeField] [Range(0f, 1f)] private float coinSoundVolume = 1f;

    [Header("Animación DOTween")]
    [Tooltip("Duración de una vuelta completa de rotación en segundos.")]
    [SerializeField] private float rotateDuration = 1.2f;
    [Tooltip("Escala máxima del pulso (1.2 = crece un 20%).")]
    [SerializeField] private float maxScale = 1.2f;
    [Tooltip("Escala mínima del pulso (0.85 = se encoge un 15%).")]
    [SerializeField] private float minScale = 0.8f;
    [Tooltip("Duración de cada mitad del pulso (crece o encoge).")]
    [SerializeField] private float pulseDuration = 0.6f;

    private void OnEnable()
    {
        // Rotación continua en Y con DOTween (reemplaza el Update manual)
        transform.DORotate(new Vector3(0f, 360f, 0f), rotateDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);

        // Pulso de escala: crece y encoge en bucle infinito
        transform.DOScale(maxScale, pulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .From(minScale);
    }

    private void OnDisable()
    {
        // Matar todos los tweens de esta moneda al desactivarse/destruirse
        transform.DOKill();
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
