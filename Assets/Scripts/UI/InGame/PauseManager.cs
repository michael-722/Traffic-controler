using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuObj;
    
    private bool isPaused = false;

    private void Awake()
    {
        pauseMenuObj.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        pauseMenuObj.SetActive(isPaused);
    }

    public void OnLeaveClicked()
    {
        pauseMenuObj.SetActive(false);
        isPaused = false;
     
        Relay.Instance.Dissconect();

        GameManager.Instance.RequestReturnToLobby();
        StatTracker.Instance.ResetAllPlayerStats();
    }
}