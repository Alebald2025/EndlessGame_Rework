using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Paneles de la Interfaz")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject calibrationPanel;

    [Header("Textos del HUD")]
    [SerializeField] private TextMeshProUGUI hudScoreText;
    [SerializeField] private TextMeshProUGUI hudCoinsText;

    [Header("Textos del Menú Inicio")]
    [SerializeField] private TextMeshProUGUI menuHighScoreText;

    [Header("Textos de Game Over")]
    [SerializeField] private TextMeshProUGUI gameOverScoreText;
    [SerializeField] private TextMeshProUGUI gameOverHighScoreText;

    [Header("Calibración del Sensor (Premium)")]
    [SerializeField] private TextMeshProUGUI calibRawAccText;
    [SerializeField] private TextMeshProUGUI calibJerkText;
    [SerializeField] private TextMeshProUGUI calibMaxJerkText;
    [SerializeField] private Slider calibThresholdSlider;
    [SerializeField] private TextMeshProUGUI calibThresholdValueText;

    public static UIManager Instance { get; private set; }

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
        // Configurar el slider de calibración si está asignado
        if (calibThresholdSlider != null && MotionJumpDetector.Instance != null)
        {
            calibThresholdSlider.minValue = 0.5f;
            calibThresholdSlider.maxValue = 5.0f;
            calibThresholdSlider.value = MotionJumpDetector.Instance.jumpThreshold;
            calibThresholdSlider.onValueChanged.AddListener(OnThresholdSliderChanged);
            UpdateThresholdText(calibThresholdSlider.value);
        }

        ShowStartScreen();
    }

    private void Update()
    {
        // Actualizar datos del HUD si el juego está en curso
        if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            if (hudScoreText != null)
                hudScoreText.text = "PUNTOS: " + GameManager.Instance.CurrentScore.ToString();

            if (hudCoinsText != null)
                hudCoinsText.text = "MONEDAS: " + GameManager.Instance.CoinsCollected.ToString();
        }

        // Actualizar datos en pantalla si Game Over acaba de activarse
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver && !gameOverPanel.activeSelf)
        {
            ShowGameOverScreen();
        }

        // Actualizar telemetría en el panel de calibración si está visible
        if (calibrationPanel != null && calibrationPanel.activeSelf && MotionJumpDetector.Instance != null)
        {
            Vector3 rawAcc = MotionJumpDetector.Instance.currentRawAcceleration;
            if (calibRawAccText != null)
                calibRawAccText.text = $"Acc. Cruda (X,Y,Z): \n{rawAcc.x:F2}, {rawAcc.y:F2}, {rawAcc.z:F2}";

            if (calibJerkText != null)
                calibJerkText.text = $"Fuerza de sacudida actual: {MotionJumpDetector.Instance.currentJerkForce:F2}";

            if (calibMaxJerkText != null)
                calibMaxJerkText.text = $"Pico máximo registrado: {MotionJumpDetector.Instance.maxJerkForceRecorded:F2}";
        }
    }

    public void ShowStartScreen()
    {
        startPanel.SetActive(true);
        hudPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        if (calibrationPanel != null) calibrationPanel.SetActive(false);

        if (GameManager.Instance != null && menuHighScoreText != null)
        {
            menuHighScoreText.text = "RECORD: " + GameManager.Instance.HighScore.ToString();
        }
    }

    public void OnClickPlayButton()
    {
        startPanel.SetActive(false);
        gameOverPanel.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
    }

    public void ShowHUD()
    {
        if (hudPanel != null)
        {
            hudPanel.SetActive(true);
        }
    }

    private void ShowGameOverScreen()
    {
        hudPanel.SetActive(false);
        gameOverPanel.SetActive(true);

        if (GameManager.Instance != null)
        {
            if (gameOverScoreText != null)
                gameOverScoreText.text = "PUNTUACIÓN: " + GameManager.Instance.CurrentScore.ToString();

            if (gameOverHighScoreText != null)
                gameOverHighScoreText.text = "RECORD ACTUAL: " + GameManager.Instance.HighScore.ToString();
        }
    }

    public void OnClickRestartButton()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
    }

    // Métodos para el panel de calibración
    public void ToggleCalibrationPanel()
    {
        if (calibrationPanel != null)
        {
            bool isActive = !calibrationPanel.activeSelf;
            calibrationPanel.SetActive(isActive);
            
            // Si lo abrimos, resetear el pico máximo anterior para calibración limpia
            if (isActive && MotionJumpDetector.Instance != null)
            {
                MotionJumpDetector.Instance.ResetMaxJerkRecord();
            }
        }
    }

    public void OnClickResetMaxJerk()
    {
        if (MotionJumpDetector.Instance != null)
        {
            MotionJumpDetector.Instance.ResetMaxJerkRecord();
        }
    }

    private void OnThresholdSliderChanged(float value)
    {
        if (MotionJumpDetector.Instance != null)
        {
            MotionJumpDetector.Instance.jumpThreshold = value;
            UpdateThresholdText(value);
        }
    }

    private void UpdateThresholdText(float value)
    {
        if (calibThresholdValueText != null)
        {
            calibThresholdValueText.text = "Umbral: " + value.ToString("F2");
        }
    }
}
