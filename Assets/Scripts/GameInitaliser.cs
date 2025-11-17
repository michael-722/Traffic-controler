using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameInitaliser : MonoBehaviour
{
    private Relay relay = null;
    private UIManager ui = null;

    private void Start()
    {
        Application.targetFrameRate = 60;
    }

    private void OnEnable()
    {
        StartCoroutine(SetUpListener());
    }

    private IEnumerator SetUpListener()
    {
        while (relay == null || ui == null)
        {
            relay = FindFirstObjectByType<Relay>();
            ui = FindFirstObjectByType<UIManager>();

            yield return null;
        }

        ui.OnStartHostPressed += relay.CreateHost;
        ui.OnStartClientPressed += relay.JoinGame;
        ui.OnDissconnectPressed += relay.Dissconect;
        ui.OnStartLANPressed += relay.CreateLAN;
        ui.OnJoinLANPressed += relay.JoinLAN;

        relay.joinCodeCreated += ui.DisplayJoinCode;
        relay.connectedPlayerCountChange += ui.UpdateConnectedPlayerCount;
        relay.dissconnectMsg += ui.UpdateJoinedWaitingTextClientRpc;
        relay.ipAddressFetched += ui.DisplayIpAdress;
    }
}