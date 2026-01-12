using Unity.Netcode;
using UnityEngine;

public class WinLoseConditions : NetworkBehaviour
{
    public static WinLoseConditions Instance;

    public NetworkVariable<float> timeToComplete = new NetworkVariable<float>(120f);
    public NetworkVariable<int> currentJournies = new NetworkVariable<int>(0);
    
    [HideInInspector] public float maxTime;
    //getters for these instead of public vars? <======

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        maxTime = timeToComplete.Value;
    }

    private void Update()
    {
        if (!IsServer) return;
        if (GameManager.Instance.GetState() != GameState.Gameplay) return;

        timeToComplete.Value -= Time.deltaTime;

        if (timeToComplete.Value <= 0)
        {
            timeToComplete.Value = 0;
            EndGame();
        }
    }

    public void CompleteJourney()
    {
        if (!IsServer) return;

        currentJournies.Value++;
    }

    private void EndGame()
    {
        GameManager.Instance.SetState(GameState.PostGame);//stop cars spawing

        //set cars to crashed to stop movement
        CarController[] cars = FindObjectsByType<CarController>(FindObjectsSortMode.None);
        foreach (CarController car in cars) car.setHasCrashed(true);
    }

    public void ResetConditions()
    {
        if (!IsServer) return;

        CarSpawner.Instance.ClearAllCars();
        currentJournies.Value = 0;
        timeToComplete.Value = 120;
    }
}