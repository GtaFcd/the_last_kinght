using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerHealth player2Health;
    [SerializeField] private EnemyAI      enemyAI;

    [Header("Pausa")]
    [SerializeField] private GameObject pauseMenuUI;

    [Header("Delay antes de reiniciar")]
    [SerializeField] private float restartDelay = 1.5f;

    [Header("Marcador")]
    [SerializeField] private TMP_Text p1WinsText;
    [SerializeField] private TMP_Text p2WinsText;

    private const string P1_WINS_KEY = "P1Wins";
    private const string P2_WINS_KEY = "P2Wins";

    private bool isPaused = false;
    private bool gameOver = false;

    private void Start()
    {
        if (playerHealth == null)
        {
            var p1go = GameObject.Find("Player 1");
            if (p1go != null) playerHealth = p1go.GetComponent<PlayerHealth>();
        }
        if (player2Health == null)
        {
            var p2go = GameObject.Find("Player 2");
            if (p2go != null) player2Health = p2go.GetComponent<PlayerHealth>();
        }

        if (playerHealth  != null) playerHealth.OnDeath  += OnPlayer1Death;
        if (player2Health != null) player2Health.OnDeath += OnPlayer2Death;
        if (enemyAI       != null) enemyAI.OnHealthChanged += CheckEnemyDeath;
        if (pauseMenuUI   != null) pauseMenuUI.SetActive(false);

        UpdateScoreUI();
    }

    private void Update()
    {
        if (gameOver) return;
        if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
    }

    private void OnPlayer1Death()
    {
        if (gameOver) return;
        gameOver = true;
        PlayerPrefs.SetInt(P2_WINS_KEY, PlayerPrefs.GetInt(P2_WINS_KEY, 0) + 1);
        PlayerPrefs.Save();
        StartCoroutine(RestartAfterDelay());
    }

    private void OnPlayer2Death()
    {
        if (gameOver) return;
        gameOver = true;
        PlayerPrefs.SetInt(P1_WINS_KEY, PlayerPrefs.GetInt(P1_WINS_KEY, 0) + 1);
        PlayerPrefs.Save();
        StartCoroutine(RestartAfterDelay());
    }

    private void CheckEnemyDeath(float currentHp, float maxHp)
    {
        if (gameOver) return;
        if (currentHp <= 0f)
        {
            gameOver = true;
            PlayerPrefs.SetInt(P1_WINS_KEY, PlayerPrefs.GetInt(P1_WINS_KEY, 0) + 1);
            PlayerPrefs.Save();
            StartCoroutine(RestartAfterDelay());
        }
    }

    private void UpdateScoreUI()
    {
        int p1Wins = PlayerPrefs.GetInt(P1_WINS_KEY, 0);
        int p2Wins = PlayerPrefs.GetInt(P2_WINS_KEY, 0);
        if (p1WinsText != null) p1WinsText.text = p1Wins.ToString();
        if (p2WinsText != null) p2WinsText.text = p2Wins.ToString();
    }

    public void ResetScore()
    {
        PlayerPrefs.SetInt(P1_WINS_KEY, 0);
        PlayerPrefs.SetInt(P2_WINS_KEY, 0);
        PlayerPrefs.Save();
        UpdateScoreUI();
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(restartDelay);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(isPaused);
    }

    public void ResumeGame()  { if (isPaused) TogglePause(); }
    public void RestartGame()
    {
        ResetScore();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void QuitGame() { Application.Quit(); }

    private void OnDestroy()
    {
        if (playerHealth  != null) playerHealth.OnDeath  -= OnPlayer1Death;
        if (player2Health != null) player2Health.OnDeath -= OnPlayer2Death;
        if (enemyAI       != null) enemyAI.OnHealthChanged -= CheckEnemyDeath;
    }
}