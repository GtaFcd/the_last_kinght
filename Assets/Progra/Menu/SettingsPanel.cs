using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider sliderSounds;

    private void OnEnable()
    {
        if (sliderSounds != null)
            sliderSounds.value = 1f;
    }

    public void OnCloseButton()
    {
        gameObject.SetActive(false);
    }

    public void OnVolumeChanged(float value)
    {
        // AudioListener.volume = value;
        Debug.Log("Volume: " + value);
    }
}