using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Panel de Settings")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Slider de volumen")]
    [SerializeField] private Slider volumeSlider;

    private const string VOLUME_KEY = "musicVolumen";

    private void Start()
    {
        // Cerrar panel al inicio
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // Cargar volumen guardado
        float saved = PlayerPrefs.GetFloat(VOLUME_KEY, 1f);
        AudioListener.volume = saved;

        if (volumeSlider != null)
        {
            volumeSlider.value = saved;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
    }

    // Botón Play
    public void Play()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // Botón Settings — abre/cierra el panel
    public void ToggleSettings()
    {
        if (settingsPanel == null) return;
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    // Botón X o Back dentro del panel
    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    // Botón Quit
    public void Quit()
    {
        Debug.Log("Saliendo...");
        Application.Quit();
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VOLUME_KEY, value);
    }

    private void OnDestroy()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
    }
}