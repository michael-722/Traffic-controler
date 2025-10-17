using System.Collections;
using UnityEngine;

public class GameInitaliser : MonoBehaviour
{
    private void OnEnable()
    {
        StartCoroutine(SetUpListener());
    }

    private IEnumerator SetUpListener()
    {
        Relay relay = null;
        UIManager ui = null;

        while (relay == null || ui == null)
        {
            relay = FindFirstObjectByType<Relay>();
            ui = FindFirstObjectByType<UIManager>();

            yield return null;
        }

        ui.OnStartHostPressed += relay.CreateHost;
        ui.OnStartClientPressed += relay.JoinGame;
        ui.OnDissconnectPressed += relay.Dissconnect;

        relay.joinCodeCreated += ui.DisplayJoinCode;
    }
}
