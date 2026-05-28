using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Paneles de la Interfaz")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Textos del HUD")]
    [SerializeField] private TextMeshProUGUI hudScoreText;
    [SerializeField] private TextMeshProUGUI hudCoinsText;

    [Header("Textos del Menú Inicio")]
    [SerializeField] private TextMeshProUGUI menuHighScoreText;

    [Header("Textos de Game Over")]
    [SerializeField] private TextMeshProUGUI gameOverScoreText;
    [SerializeField] private TextMeshProUGUI gameOverHighScoreText;



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

    private IEnumerator Start()
    {
        // Esperar un frame para asegurar que GameManager haya cargado el récord desde JSON.
        yield return null;
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


    }

    public void ShowStartScreen()
    {
        startPanel.SetActive(true);
        hudPanel.SetActive(false);
        gameOverPanel.SetActive(false);

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
                gameOverScoreText.text = "PUNTOS: " + GameManager.Instance.CurrentScore.ToString();

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

    public void OnClickQuitButton()
    {
        Application.Quit();
    }


}
