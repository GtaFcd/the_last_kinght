using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class menu : MonoBehaviour
{ 
    
public void OpenSettings(string scena)
{
        SceneManager.LoadScene(scena);
}

}
