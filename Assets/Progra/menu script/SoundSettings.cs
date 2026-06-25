using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
public class SoundManager : MonoBehaviour
{
    [SerializeField] Slider VolumenSlider;

    void Start()
    {
        if (PlayerPrefs.HasKey("musicVolumen"))
        {
            PlayerPrefs.SetFloat("musicVolumen", 1);
            Load();
        }
        else
        {
            Load();
        }
    }
    public void ChangeVolumen()
    {
        AudioListener.volume = VolumenSlider.value;
        save();
    }
    private void Load()
    {

    }
    private void save()
    {
        PlayerPrefs.SetFloat("musicVolumen", VolumenSlider.value);
    }
}


