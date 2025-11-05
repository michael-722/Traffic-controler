using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameInitaliser : MonoBehaviour
{
    private Relay relay = null;
    private UIManager ui = null;

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
        ui.OnHostDissconnectPressed += relay.HostDissconect;

        relay.joinCodeCreated += ui.DisplayJoinCode;
        relay.connectedPlayerCountChange += ui.UpdateConnectedPlayerCount;
        relay.hostDissconnected += ui.UpdateJoinedWaitingTextClientRpc;

        NetworkManager.Singleton.OnClientConnectedCallback += relay.Connection;
        NetworkManager.Singleton.OnClientDisconnectCallback += relay.Dissconnection;
    }
}