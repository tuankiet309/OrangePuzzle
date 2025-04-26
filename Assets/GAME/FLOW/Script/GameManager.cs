using NUnit.Framework;
using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

//Mostly do nothing but control the flow
public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance {  get { return instance; } }
    public List<Level> levels ;
    public int indexOfThisLevel = 0;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public void LoadLevel(int index)
    {
        indexOfThisLevel = index;
        SceneManager.LoadScene("CORE");
    }
    public void NextLevel()
    {
        indexOfThisLevel++;
        if(indexOfThisLevel == levels.Count)
        {
            indexOfThisLevel = 0;
        }    
        SceneManager.LoadScene("CORE");
    }
    public void Home()
    {
        indexOfThisLevel = 0;
        SceneManager.LoadScene("Main Menu");
    }


    
}
