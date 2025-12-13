using Unity.Netcode;
using System;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    public event Action<GameState> OnGameStateChanged;

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
        if (IsClient) StartGameServerRpc();
        else if (IsServer) state.Value = GameState.Gameplay;
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartGameServerRpc()
    {
        state.Value = GameState.Gameplay;
    }

    public GameState GetState() => state.Value;
}

public enum GameState
{
    Lobby,
    Gameplay,
    PostGame
}