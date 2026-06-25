using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;

    private void Start()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void OnPlayButton()
    {
        // Cambia "GameScene" por el nombre exacto de la escena del juego, el PvP o PvE, de preferencia PvP xd
        SceneManager.LoadScene("MultiplayerCombat");
    }

    public void OnSettingsButton()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void OnExitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}