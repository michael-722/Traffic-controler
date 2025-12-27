using Unity.Netcode;
using System;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private int targetFrameRate = 60;

    public static GameManager Instance;
    public event Action<GameState> OnGameStateChanged;

    private NetworkVariable<GameState> state = new(GameState.Lobby);

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        Application.targetFrameRate = targetFrameRate;
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

    public GameState GetState() => state.Value;

    public void SetState(GameState newState)
    {
        if (!IsServer)
        {
            Debug.LogWarning("only server can set game state");
            return;
        }

        state.Value = newState;
    }
}

public enum GameState
{
    Lobby,
    Gameplay,
    PostGame
}