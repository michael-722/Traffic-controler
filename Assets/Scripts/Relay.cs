using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

//to cal ping save time msg sent and then time recieve response then t1 - t0 gives ping 

public class Relay : MonoBehaviour
{
    public event Action<string> joinCodeCreated;
    public event Action<int> connectedPlayerCountChange;
    public event Action<string> dissconnectMsg;

    private List<ulong> connectedPlayerIds = new List<ulong>();

    private ulong serverID;

    private async void Awake()
    {
        DontDestroyOnLoad(gameObject);

        //srelay init
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            //AuthenticationService.Instance.SignedIn += () => { Debug.Log("signed in " + AuthenticationService.Instance.PlayerId); };
        }
        catch (System.Exception e)
        {
            Debug.LogError($"failed to initialize unity services: {e.Message}");
        }
    }

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += Connection;
        NetworkManager.Singleton.OnClientDisconnectCallback += Dissconnection;

        NetworkManager.Singleton.OnTransportFailure += ConnectionLost;
    }

    public async void CreateHost()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);//max 4 players

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            //Debug.Log($"join code: {joinCode}");

            joinCodeCreated?.Invoke(joinCode);

            //set relay data for netcode transport
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData);

            NetworkManager.Singleton.StartHost();
            serverID = NetworkManager.ServerClientId;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"failed to create relay: {e.Message}");
        }
    }

    public async void JoinGame(string joinCode)
    {
        //Debug.Log("created client");
        try
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            Debug.Log($"joined relay with allocation id: {allocation.AllocationId}");

            //set relay data for netcode transport
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.HostConnectionData);

            NetworkManager.Singleton.StartClient();
        }
        catch (System.Exception e)
        {
            if (e.Message.Contains("invalid request")) dissconnectMsg.Invoke("Invalid join code Please enter vaild code");

            Debug.LogError($"failed to join relay: {e.Message}");
        }
    }

    public void Dissconect()
    {
        Dissconnection(NetworkManager.Singleton.LocalClientId);
    }

    public void Connection(ulong id)
    {
        if (!connectedPlayerIds.Contains(id))
        {
            //Debug.Log("connected with id: " +  id);
            connectedPlayerIds.Add(id);
            connectedPlayerCountChange?.Invoke(connectedPlayerIds.Count);
        }
        else Debug.LogWarning($"player {id} is already connected");
    }

    public void Dissconnection(ulong id)
    {
        //server dc
        if (id == serverID)
        {
            //Debug.Log("Host disconnected");

            ConnectionLost();
            DisconnectAllPlayersServerRpc();

            connectedPlayerIds.Remove(id);

            NetworkManager.Singleton.Shutdown();
            return;
        }

        //client dc
        if (connectedPlayerIds.Contains(id))
        {
            //Debug.Log("dissconeted player id: " + id);
            connectedPlayerIds.Remove(id);
            connectedPlayerCountChange?.Invoke(connectedPlayerIds.Count);
            ConnectionLost();
        }
        else Debug.LogWarning($"player {id} has already disconnected");
    }

    private void ConnectionLost()
    {
        dissconnectMsg.Invoke("Host has disconnected Please leave game");
    }

    [ServerRpc]
    public void DisconnectAllPlayersServerRpc()
    {
        for (int i = connectedPlayerIds.Count - 1; i >= 0; i--)
        {
            ulong playerId = connectedPlayerIds[i];
            NetworkManager.Singleton.DisconnectClient(playerId);
        }
    }
}