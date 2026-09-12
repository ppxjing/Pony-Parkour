using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    private GameObject[] runners;

    public bool isGameOver = false;

    private bool isGameStarted = false;

    private void Awake()
    {
        instance = this;
        runners = GameObject.FindGameObjectsWithTag("Runner");
    }
    void Start()
    {
        
    }


    

    [System.Obsolete]
    void Update()
    {

        if (!isGameStarted && (Input.GetMouseButtonDown(0) || Input.touchCount > 0))
        {
            StartGame();
        }
    }

    [System.Obsolete]
    public void StartGame()
    {
        if (!isGameStarted)
        {
            isGameStarted = true;

            foreach (GameObject runner in runners)
            {
                PlayerController playerController = runner.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.StartRunning();
                }
                
            }
        }
    }
}
