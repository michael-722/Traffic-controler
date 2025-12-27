using Unity.Netcode;
using UnityEngine;

public class WinLoseConditions : MonoBehaviour
{
    public static WinLoseConditions Instance;

    [SerializeField] private int timeToComplete = 120;//sec
    [SerializeField] private int requiredJourniesToWin = 40;

    private int completedJournies = 0;

    private float tickTimer = 0f;
    private bool gameEnded = false;

    private void Start()
    {
        if (Instance == null)
            Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (!NetworkManager.Singleton.IsServer || gameEnded) return;

        tickTimer += Time.deltaTime;

        while (tickTimer >= 1f && !gameEnded)
        {
            tickTimer -= 1f;
            timeToComplete = Mathf.Max(0, timeToComplete - 1);

            if (timeToComplete <= 0)
                EndGame();
        }
    }

    public void CompleteJourney()
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("only server can complete journies");
            return;
        }

        completedJournies++;
        if (completedJournies >= requiredJourniesToWin)
            EndGame();
    }

    private void EndGame()
    {
        if (gameEnded) return;
        gameEnded = true;

        GameManager.Instance.SetState(GameState.PostGame);//post game and menu states stops car spawning

        //stop car movement - idk if this is wanted behaviour - need to test and see
        CarController[] cars = FindObjectsByType<CarController>(sortMode: FindObjectsSortMode.None);
        foreach (CarController car in cars) car.setHasCrashed(true);
    }

    public void ResetConditions()
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("only server can reset win lose conditions");
            return;
        }

        CarSpawner.Instance.ClearAllCars();
        completedJournies = 0;
        timeToComplete = 120;
        gameEnded = false;
    }
}