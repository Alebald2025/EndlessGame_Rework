using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    private static bool hasShownLoadingScreen;

    [Header("Paneles de la Interfaz")]
    [SerializeField] private GameObject loadingScreen;
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

    [Header("Animación de moneda (DOTween)")]
    [Tooltip("Escala extra que añade el punch (0.3 = crece un 30%)")]
    [SerializeField] private float punchScale = 0.3f;
    [Tooltip("Duración total del punch en segundos")]
    [SerializeField] private float punchDuration = 0.35f;



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
        // Mostrar la pantalla de carga solo en el primer inicio del juego.
        if (!hasShownLoadingScreen && loadingScreen != null)
        {
            loadingScreen.SetActive(true);
            startPanel.SetActive(false);
            hudPanel.SetActive(false);
            gameOverPanel.SetActive(false);

            yield return new WaitForSeconds(3.5f);

            loadingScreen.SetActive(false);
            hasShownLoadingScreen = true;
        }
        else
        {
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(false);
            }
        }

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

        MusicManager.Instance?.PlayMenuMusic();
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

    /// <summary>
    /// Anima el texto de monedas y puntuación con un punch scale de DOTween.
    /// Llamar desde GameManager al recoger una moneda.
    /// </summary>
    public void PlayCoinCollectAnimation()
    {
        PunchText(hudCoinsText);
        PunchText(hudScoreText);
    }

    private void PunchText(TextMeshProUGUI text)
    {
        if (text == null) return;

        // Cancelar tween anterior para que no se solapen
        text.transform.DOKill();
        text.transform.localScale = Vector3.one;

        text.transform
            .DOPunchScale(Vector3.one * punchScale, punchDuration, vibrato: 1, elasticity: 0.5f)
            .SetUpdate(true); // ignora Time.timeScale por si el juego está pausado
    }
}
