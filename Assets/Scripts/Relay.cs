using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System;

//to cal ping save time msg sent and then time recieve response then t1 - t0 gives ping 

public class Relay : MonoBehaviour
{
    public event Action<string> joinCodeCreated;

    private int connectedPlayerCount = 0;

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
        //Debug.Log("created host");
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"join code: {joinCode}");

            joinCodeCreated?.Invoke(joinCode);

            //set relay data for netcode transport
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData);

            NetworkManager.Singleton.StartHost();
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

    [ServerRpc]
    public int GetConnectedPlayerCountServerRpc()
    {
        return connectedPlayerCount;
    }

    [ServerRpc]
    public void SetConnectedPlayerCountServerRpc(int num)
    {
        connectedPlayerCount += num;
    }

    public void Dissconnect()
    {
        SetConnectedPlayerCountServerRpc(-1);
        NetworkManager.Singleton.Shutdown();
    }
}