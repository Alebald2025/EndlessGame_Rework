using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private const string HighScoreFileName = "highscore.json";
    private int savedHighScore = 0;

    public static GameManager Instance { get; private set; }

    [Header("Configuración del Sistema")]
    [Tooltip("¿Iniciar jugando automáticamente o esperar en pantalla de título?")]
    [SerializeField] private bool autoStart = false;

    [Header("Transición de Cámara")]
    [Tooltip("Si es TRUE, el jugador espera quieto (Idle) hasta que la cámara se coloque detrás.\n" +
             "Si es FALSE, el jugador empieza a correr inmediatamente y la cámara le alcanza.")]
    [SerializeField] private bool startRunAfterTransition = true;

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
    private CameraFollow cameraFollow;

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
        // Cargar el High Score guardado desde JSON
        savedHighScore = LoadHighScore();
        HighScore = savedHighScore;

        // Encontrar al jugador
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.GetComponent<PlayerController>();
            playerStartPos = player.transform.position;
        }

        // Encontrar la cámara con el script CameraFollow
        cameraFollow = FindFirstObjectByType<CameraFollow>();

        if (autoStart)
        {
            StartGame();
        }
        else
        {
            // Esperando en el menú de inicio: personaje quieto, cámara en posición frontal
            IsPlaying = false;
            IsGameOver = false;
            if (player != null) player.DisableControls();
            // La cámara ya se posiciona en modo Menu en su propio Start()
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

    /// <summary>
    /// Llamado por el UIManager al pulsar el botón "Play".
    /// Gestiona la transición de cámara y el arranque del juego según la configuración.
    /// </summary>
    public void StartGame()
    {
        CurrentScore = 0;
        CoinsCollected = 0;
        IsGameOver = false;

        if (player != null)
        {
            player.transform.position = playerStartPos;
        }

        // Si tenemos un TrackManager en escena, reiniciarlo
        TrackManager trackM = FindFirstObjectByType<TrackManager>();
        if (trackM != null) trackM.ResetTrack();

        if (cameraFollow != null && !autoStart)
        {
            if (startRunAfterTransition)
            {
                // Modo Cinematográfico: el jugador espera quieto hasta que la cámara llega a su espalda
                if (player != null) player.DisableControls();

                cameraFollow.StartTransitionToGameplay(onComplete: () =>
                {
                    // La cámara llegó: ¡ahora sí a correr!
                    IsPlaying = true;
                    if (player != null) player.EnableControls();
                    if (UIManager.Instance != null) UIManager.Instance.ShowHUD();
                    MusicManager.Instance?.PlayGameplayMusic();
                    Debug.Log("[GameManager] Transición completada. ¡Juego Iniciado!");
                });
            }
            else
            {
                // Modo Acción Inmediata: el jugador sale corriendo y la cámara le sigue mientras transiciona
                IsPlaying = true;
                if (player != null) player.EnableControls();
                if (UIManager.Instance != null) UIManager.Instance.ShowHUD();
                cameraFollow.StartTransitionToGameplay(onComplete: () =>
                {
                    MusicManager.Instance?.PlayGameplayMusic();
                    Debug.Log("[GameManager] Transición de cámara completada (modo acción inmediata).");
                });
            }
        }
        else
        {
            // Sin cámara o autoStart: arrancar directamente sin transición
            IsPlaying = true;
            if (player != null) player.EnableControls();
            if (UIManager.Instance != null) UIManager.Instance.ShowHUD();
            MusicManager.Instance?.PlayGameplayMusic();
            Debug.Log("[GameManager] ¡Juego Iniciado! (sin transición de cámara)");
        }
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
        if (HighScore > savedHighScore)
        {
            savedHighScore = HighScore;
            SaveHighScore(savedHighScore);
        }

        Debug.Log($"[GameManager] Fin de Juego. Puntuación Final: {CurrentScore}");
    }

    public void RestartGame()
    {
        // Recargar la escena activa para resetear todo limpiamente
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private int LoadHighScore()
    {
        string path = GetHighScoreFilePath();
        if (!File.Exists(path))
        {
            return 0;
        }

        try
        {
            string json = File.ReadAllText(path);
            ScoreData data = JsonUtility.FromJson<ScoreData>(json);
            return data != null ? data.highScore : 0;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[GameManager] No se pudo cargar el High Score desde JSON: {ex.Message}");
            return 0;
        }
    }

    private void SaveHighScore(int highScore)
    {
        ScoreData data = new ScoreData { highScore = highScore };
        string json = JsonUtility.ToJson(data);
        string path = GetHighScoreFilePath();
        File.WriteAllText(path, json);
        Debug.Log($"[GameManager] High Score guardado en JSON: {highScore} ({path})");
    }

    private string GetHighScoreFilePath()
    {
        return Path.Combine(Application.persistentDataPath, HighScoreFileName);
    }

    [System.Serializable]
    private class ScoreData
    {
        public int highScore;
    }
}
