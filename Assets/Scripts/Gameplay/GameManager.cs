using Unity.Netcode;
using System;
using UnityEngine;
using System.Threading.Tasks;

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
        //only server and offline can set state
        if (!IsServer && IsSpawned)
        {
            OnGameStateChanged?.Invoke(GetState());
            Debug.LogWarning("only server can set game state");
            if (NetworkManager != null) return;
        }

        state.Value = newState;
        OnGameStateChanged?.Invoke(newState);
    }

    public async void RequestReturnToLobby()
    {
        await Task.Delay(TimeSpan.FromSeconds(0.5));

        SetState(GameState.Lobby);
        OnGameStateChanged?.Invoke(GameState.Lobby);

        MainMenuUI ui = UIManager.Instance.GetMenuUI();
        ui.ShowMenuUI();

        CameraManager.Instance.ResetCameraPosition();
        TutorialManager.Instance.ShowTutorial();
    }

    public async void RequestClientReturnToLobby()
    {
        await Task.Delay(TimeSpan.FromSeconds(0.5));

        SetState(GameState.Lobby);
        OnGameStateChanged?.Invoke(GameState.Lobby);

        CameraManager.Instance.ResetCameraPosition();
        TutorialManager.Instance.ShowTutorial();
    }

    public void RequestRestGame()
    {
        MainMenuUI ui = UIManager.Instance.GetMenuUI();
        if (ui == null)
        {
            Debug.LogError("ui is null cannot reset game");
            return;
        }

        if (IsServer)
        {
            PlayerManager.Instance.ResetAllPlayersScore();
            WinLoseConditions.Instance.ResetConditions();

            //need to differentiate between online and LAN
            ui.showOnlineHostingUI();

            SetState(GameState.Lobby);
            OnGameStateChanged?.Invoke(GameState.Lobby);
        }
        else
        {
            //need to differentiate between online and LAN
            ui.showOnlineJoinedUI();
        }
    }
}

public enum GameState
{
    Lobby,
    Gameplay,
    PostGame,
    Tutorial
}