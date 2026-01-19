using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using System.Collections;
using WebSocketSharp;

//to cal ping save time msg sent and then time recieve response then t1 - t0 gives ping 

public class Relay : NetworkBehaviour
{
    public static Relay Instance;

    public event Action<string> joinCodeCreated;
    public event Action<string> ipAddressFetched;
    public event Action<int> connectedPlayerCountChange;
    public event Action<string> dissconnectMsg;

    private UnityTransport transport;
    private List<ulong> connectedPlayerIds = new List<ulong>();

    private async void Awake()
    {
        if (Instance == null)
            Instance = this;
        else Destroy(gameObject);

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

    private void OnEnable()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += Connection;
        NetworkManager.Singleton.OnClientDisconnectCallback += Dissconnection;

        var ui = UIManager.Instance.GetMenuUI();

        joinCodeCreated += ui.DisplayJoinCode;
        connectedPlayerCountChange += ui.UpdateConnectedPlayerCount;
        dissconnectMsg += ui.UpdateJoinedWaitingText;
        ipAddressFetched += ui.DisplayIpAdress;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= Connection;
            NetworkManager.Singleton.OnClientDisconnectCallback -= Dissconnection;
        }

        if (UIManager.Instance == null) return;
        var ui = UIManager.Instance.GetMenuUI();

        joinCodeCreated -= ui.DisplayJoinCode;
        connectedPlayerCountChange -= ui.UpdateConnectedPlayerCount;
        dissconnectMsg -= ui.UpdateJoinedWaitingText;
        ipAddressFetched -= ui.DisplayIpAdress;
    }


    private void Start()
    {
        transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
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
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData);

            NetworkManager.Singleton.StartHost();
        }
        catch (Exception e)
        {
            Debug.LogError($"failed to create relay: {e.Message}");
        }
    }

    public async void JoinGame(string joinCode)
    {
        if (joinCode.IsNullOrEmpty())
        {
            dissconnectMsg.Invoke("Try entering a join code before joining");
            return;
        }

        try
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            Debug.Log($"joined relay with allocation id: {allocation.AllocationId}");

            //set relay data for netcode transport
            transport.SetClientRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.HostConnectionData);

            NetworkManager.Singleton.StartClient();
        }
        catch (Exception e)
        {
            if (e.Message.Contains("invalid request")) dissconnectMsg.Invoke("Invalid join code\nPlease enter vaild code");
            else if (e.Message.Contains("Not Found")) dissconnectMsg.Invoke("Game not found\nPlease check join code");

            Debug.LogError($"failed to join relay: {e.Message}");
        }
    }

    public string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new Exception("failed to get ip");
    }

    public void Dissconect()
    {
       // Debug.Log("dissconnecting from game");
        if (NetworkManager.Singleton.IsHost)
        {
            //Debug.Log($"Local id: {NetworkManager.Singleton.LocalClient.ClientId} - disconnecting all players");
            DissconnectAllPlayers();
        }
        else
        {
            //Debug.Log($"Local id: {NetworkManager.Singleton.LocalClient.ClientId} - disconnecting itself");
            DisssconnectPlayerServerRpc(NetworkManager.Singleton.LocalClient.ClientId);
        }

        StartCoroutine(WaitForShutdownAndReset());
    }

    private void DissconnectAllPlayers()
    {
        //Debug.Log("Disconnecting all players from server. Count: " + NetworkManager.Singleton.ConnectedClientsList.Count);
        if (NetworkManager.Singleton.ConnectedClientsList.Count <= 1)
        {
            NetworkManager.Singleton.Shutdown();
            return;
        }

        List<ulong> clientsToDisconnect = new List<ulong>();

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId != NetworkManager.ServerClientId)
                clientsToDisconnect.Add(client.ClientId);
        }

        foreach (ulong id in clientsToDisconnect)
            NetworkManager.Singleton.DisconnectClient(id, "host has ended game");

        NetworkManager.Singleton.Shutdown();
    }

    [ServerRpc(RequireOwnership = false)]
    private void DisssconnectPlayerServerRpc(ulong id)
    {
        NetworkManager.Singleton.DisconnectClient(id, "client left server");
    }

    private IEnumerator WaitForShutdownAndReset()
    {
        while (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient))
            yield return null;

        PlayerManager.Instance.ResetAllPlayersScore();
        WinLoseConditions.Instance.ResetConditions();
    }

    public void Connection(ulong id)
    {
        if (!connectedPlayerIds.Contains(id))
        {
            //Debug.Log($"connected with id: {id}");
            connectedPlayerIds.Add(id);
            connectedPlayerCountChange?.Invoke(connectedPlayerIds.Count);
        }
        else Debug.LogWarning($"player {id} is already connected");
    }

    public void Dissconnection(ulong id)
    {
        if (connectedPlayerIds.Contains(id))
            connectedPlayerIds.Remove(id);

        if (id == NetworkManager.Singleton.LocalClientId)
        {
            string reason = NetworkManager.Singleton.DisconnectReason;
            if (reason == "host has ended game")
            {
                ConnectionLost();
                GameManager.Instance.RequestClientReturnToLobby();
            }
            
            connectedPlayerIds.Clear();
        }

        connectedPlayerCountChange?.Invoke(connectedPlayerIds.Count);
    }

    private void ConnectionLost()
    {
        dissconnectMsg.Invoke("Host has disconnected\nPlease leave game");
    }

    public void CreateLAN()
    {
        string ip = GetLocalIPAddress();
        transport.SetConnectionData(ip, 7777, "0.0.0.0");

        ipAddressFetched?.Invoke(ip);

        try
        {
            NetworkManager.Singleton.StartHost();
        }
        catch (Exception e)
        {
            Debug.LogError($"failed to start LAN host: {e.Message}");
        }
    }

    public void JoinLAN(string ip)
    {
        if (!IsValidIP(ip))
        {
            dissconnectMsg.Invoke("Invalid IP address please enter vaild address");
            return;
        }

        transport.SetConnectionData(ip, 7777, null);

        try
        {
            NetworkManager.Singleton.StartClient();
        }
        catch (Exception e)
        {
            Debug.LogError($"failed to start LAN client: {e.Message}");
        }
    }

    private bool IsValidIP(string ip)
    {
        IPAddress ipAddr;
        return IPAddress.TryParse(ip, out ipAddr);
    }

    public bool IsUsingRelay()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.NetworkConfig.NetworkTransport is UnityTransport transport)
        {
            //Debug.Log($"current transport protocol: {transport.Protocol}");
            return transport.Protocol == UnityTransport.ProtocolType.RelayUnityTransport;
        }
        else
            return false;
    }
}