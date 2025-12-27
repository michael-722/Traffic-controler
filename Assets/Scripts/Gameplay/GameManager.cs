using Unity.Netcode;
using System;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public event Action<GameState> OnGameStateChanged;

    [SerializeField] private int requiredJourniesToWin = 40;
    private int completedJournies = 0;

    private NetworkVariable<GameState> state = new(GameState.Lobby);

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        state.OnValueChanged += (oldVal, newVal) =>
        {
            OnGameStateChanged?.Invoke(newVal);
        };
    }

    public void RequestStartGame()
    {
        if (!IsServer)
        {
            Debug.LogWarning("only server can start game");
            return;
        }

        state.Value = GameState.Gameplay;
    }

    public void CompleteJourney()
    {
        if (!IsServer)
        {
            Debug.LogWarning("only server can complete journies");
            return;
        }

        completedJournies++;
        if (completedJournies >= requiredJourniesToWin)
        {
            state.Value = GameState.PostGame;

            //stop car movement - idk if this is wanted behaviour - need to test and see
            CarController[] cars = FindObjectsByType<CarController>(sortMode: FindObjectsSortMode.None);
            foreach (CarController car in cars) car.setHasCrashed(true);
        }
    }

    public GameState GetState() => state.Value;
}

public enum GameState
{
    Lobby,
    Gameplay,
    PostGame
}