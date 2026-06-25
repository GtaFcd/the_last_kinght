using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class buttomplay : MonoBehaviour

{
   
    public void play()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
    public void OpenSettings(string scena)
    {
        SceneManager.LoadScene("main_setting");
    }
    public void exit()
    {
        Debug.Log("saliendo del juego...");
        Application.Quit();
    }
}
