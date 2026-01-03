using System;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private GameObject menuUIObj;
    [SerializeField] private GameObject inGameUIObj;
    [SerializeField] private GameObject postGameUIObj;

    private MainMenuUI menuUI;
    private InGameUI inGameUI;
    private PostGameUI postGameUI;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        try
        {
            menuUI = menuUIObj.GetComponent<MainMenuUI>();
            inGameUI = inGameUIObj.GetComponent<InGameUI>();
            postGameUI = postGameUIObj.GetComponent<PostGameUI>();
        }
        catch (Exception e)
        {
            Debug.LogError($"UIManager failed init - {e.Message}");
        }
    }

    private void Start()
    {
        GameManager.Instance.OnGameStateChanged += ToggleUIBasedOnGameState;
        ToggleUIBasedOnGameState(GameManager.Instance.GetState());
    }

    public void ToggleUIBasedOnGameState(GameState state)
    {
        menuUIObj.SetActive(false);
        inGameUIObj.SetActive(false);
        postGameUIObj.SetActive(false);

        switch (state)
        {
            case GameState.Lobby:
                menuUIObj.SetActive(true);
                break;
            case GameState.Gameplay:
                inGameUIObj.SetActive(true);
                break;
            case GameState.PostGame:
                postGameUIObj.SetActive(true);
                break;
        }
    }

    public MainMenuUI GetMenuUI()
    {
        if (menuUI == null) 
            Debug.LogError("menu ui is null");

        return menuUI;
    }

    public InGameUI GetInGameUI()
    {
        if (inGameUI == null)
            Debug.LogError("inGame ui is null");

        return inGameUI;
    }

    public PostGameUI GetPostGameUI()
    {
        if (postGameUI == null)
            Debug.LogError("postGame ui is null");

        return postGameUI;
    }
}