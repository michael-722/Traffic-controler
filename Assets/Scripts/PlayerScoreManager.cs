using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerScoreManager : NetworkBehaviour
{
    public static PlayerScoreManager Instance;

    private Dictionary<ulong, NetworkVariable<int>> playerScores = new Dictionary<ulong, NetworkVariable<int>>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += handlePlayerConnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= handlePlayerConnected;
        }
    }

    private void handlePlayerConnected(ulong clientId)
    {
        if (IsServer)
        {
            initializePlayerScore(clientId);
        }
    }

    private void initializePlayerScore(ulong clientId)
    {
        var scoreVar = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        playerScores[clientId] = scoreVar;
    }

    [ServerRpc]
    public void increaseScoreServerRpc(ulong id, int val)
    {
        if (playerScores.ContainsKey(id))
        {
            Debug.Log("increasing score for player id: " + id + " by " + val);
            playerScores[id].Value += val;
        }
        else
        {
             Debug.Log("player id not found: " + id);
        }
    }

    [ServerRpc]
    public void decreaseScoreServerRpc(ulong id, int val)
    {
        if (playerScores.ContainsKey(id))
        {
            playerScores[id].Value -= val;
        }
        else
        {
            Debug.Log("player id not found: " + id);
        }
    }

    [ServerRpc]
    public void setScoreServerRpc(ulong id, int val)
    {
        if (playerScores.ContainsKey(id))
        {
            playerScores[id].Value = val;
        }
        else
        {
            Debug.Log("player id not found: " + id);
        }
    }

    public int getPlayerScore(ulong id)
    {
        if (playerScores.ContainsKey(id))
        {
            return playerScores[id].Value;
        }
        else
        {
            Debug.Log("player id not found: " + id);
            return 0;
        }
    }
}
