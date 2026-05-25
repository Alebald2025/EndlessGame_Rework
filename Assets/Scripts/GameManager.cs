using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Configuración del Sistema")]
    [Tooltip("¿Iniciar jugando automáticamente o esperar en pantalla de título?")]
    [SerializeField] private bool autoStart = false;

    // Estado del Juego
    public bool IsPlaying { get; private set; }
    public bool IsGameOver { get; private set; }

    // Datos de Partida
    public int CurrentScore { get; private set; }
    public int HighScore { get; private set; }
    public int CoinsCollected { get; private set; }

    private float scoreAccumulator = 0f;
    private Vector3 playerStartPos;
    private PlayerController player;

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
        // Cargar el High Score guardado
        HighScore = PlayerPrefs.GetInt("HighScore", 0);
        
        // Encontrar al jugador
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.GetComponent<PlayerController>();
            playerStartPos = player.transform.position;
        }

        if (autoStart)
        {
            StartGame();
        }
        else
        {
            // Esperando en el menú de inicio
            IsPlaying = false;
            IsGameOver = false;
            if (player != null) player.DisableControls();
        }
    }

    private void Update()
    {
        if (IsPlaying && !IsGameOver && player != null)
        {
            // Calcular puntuación basada en la distancia recorrida en el eje Z
            float distanceRun = player.transform.position.z - playerStartPos.z;
            
            // Sumar puntos por distancia recorrida
            CurrentScore = Mathf.Max(0, (int)distanceRun) + (CoinsCollected * 10);

            // Actualizar High Score en tiempo real
            if (CurrentScore > HighScore)
            {
                HighScore = CurrentScore;
            }
        }
    }

    public void StartGame()
    {
        IsPlaying = true;
        IsGameOver = false;
        CurrentScore = 0;
        CoinsCollected = 0;
        
        if (player != null)
        {
            player.transform.position = playerStartPos;
            player.EnableControls();
        }

        // Si tenemos un TrackManager en escena, reiniciarlo
        TrackManager trackM = FindFirstObjectByType<TrackManager>();
        if (trackM != null)
        {
            trackM.ResetTrack();
        }

        Debug.Log("[GameManager] ¡Juego Iniciado!");
    }

    public void CollectCoin(int value)
    {
        if (IsGameOver) return;
        CoinsCollected++;
        CurrentScore += value;
        
        if (CurrentScore > HighScore)
        {
            HighScore = CurrentScore;
        }

        Debug.Log($"[GameManager] Moneda recogida. Monedas: {CoinsCollected}, Puntuación: {CurrentScore}");
    }

    public void GameOver()
    {
        if (IsGameOver) return;
        
        IsPlaying = false;
        IsGameOver = true;

        // Guardar High Score si se superó
        if (HighScore > PlayerPrefs.GetInt("HighScore", 0))
        {
            PlayerPrefs.SetInt("HighScore", HighScore);
            PlayerPrefs.Save();
        }

        Debug.Log($"[GameManager] Fin de Juego. Puntuación Final: {CurrentScore}");
    }

    public void RestartGame()
    {
        // Recargar la escena activa para resetear todo limpiamente
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
