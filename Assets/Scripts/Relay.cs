using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

//to cal ping save time msg sent and then time recieve response then t1 - t0 gives ping 

public class Relay : MonoBehaviour
{
    public event Action<string> joinCodeCreated;
    public event Action<int> connectedPlayerCountChange;
    public event Action<string> hostDissconnected;

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
            Debug.LogError($"failed to join relay: {e.Message}");
        }
    }

    public void Dissconect()
    {
        NetworkManager.Singleton.Shutdown();
    }

    public async void HostDissconect()
    {
        for (int i = 1; i < connectedPlayerIds.Count; i--)
        {
            NetworkManager.Singleton.DisconnectClient(connectedPlayerIds[i]);
        }
        await Task.Delay(1000);
        NetworkManager.Singleton.Shutdown();
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
        
        if (connectedPlayerIds.Contains(id))
        {
            //Debug.Log("dissconeted player id: " + id);
            connectedPlayerIds.Remove(id);
            connectedPlayerCountChange?.Invoke(connectedPlayerIds.Count);
        }
        else Debug.LogWarning($"player {id} has already disconnected");
    }
}