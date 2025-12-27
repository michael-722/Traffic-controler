using UnityEngine;

public class GameInitaliser : MonoBehaviour
{
    [SerializeField] private Relay relay = null;
    [SerializeField] private UIManager ui = null;

    private void Start()
    {
        Application.targetFrameRate = 60;
        SetUpListener();
    }

    private void SetUpListener()
    {
        ui.OnStartHostPressed += relay.CreateHost;
        ui.OnStartClientPressed += relay.JoinGame;
        ui.OnDissconnectPressed += relay.Dissconect;
        ui.OnStartLANPressed += relay.CreateLAN;
        ui.OnJoinLANPressed += relay.JoinLAN;
        
        ui.OnStartGamePressed += () => GameManager.Instance.RequestStartGame();
        GameManager.Instance.OnGameStateChanged += ui.ToggleUIBasedOnGameState;

        relay.joinCodeCreated += ui.DisplayJoinCode;
        relay.connectedPlayerCountChange += ui.UpdateConnectedPlayerCount;
        relay.dissconnectMsg += ui.UpdateJoinedWaitingText;
        relay.ipAddressFetched += ui.DisplayIpAdress;
    }

    //NOTE================
    /*
     need to look into making universal broadcast system 
    so stuff just listens rather then maually liking 

    is possible?
    pros vs cons?
     */
}