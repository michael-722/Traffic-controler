using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else Destroy(gameObject);
    }

    [ServerRpc(RequireOwnership = false)]
    public void IncreaseScoreServerRpc(ulong clientId, int amount)
    {
        PlayerInfo playerInfo = FindPlayerScoreForClient(clientId);

        if (playerInfo != null) playerInfo.addScore(amount);
        else Debug.LogError($"playerscore for client {clientId} not found");
    }

    [ServerRpc(RequireOwnership = false)]
    public void DecreaseScoreServerRpc(ulong clientId, int amount)
    {
        PlayerInfo playerInfo = FindPlayerScoreForClient(clientId);

        if (playerInfo != null) playerInfo.subtractScore(amount);
        else Debug.LogError($"playerscore for client {clientId} not found");
    }

    public void ResetAllPlayersScore()
    {
        if (!IsServer) return;

        PlayerInfo[] playerInfo = FindObjectsByType<PlayerInfo>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
        foreach (PlayerInfo info in playerInfo)
        {
            if (info != null)
                info.setScore(0);
        }
    }

    private PlayerInfo FindPlayerScoreForClient(ulong clientId)
    {
        PlayerInfo[] playerInfo = FindObjectsByType<PlayerInfo>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);

        foreach (PlayerInfo info in playerInfo)
        {
            if (info != null)
            {
                NetworkObject networkObject = info.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned && networkObject.OwnerClientId == clientId)
                    return info;
            }
        }
        return null;
    }

    public int getPlayerScore(ulong clientId)
    {
        PlayerInfo playerInfo = FindPlayerScoreForClient(clientId);
        if (playerInfo != null) return playerInfo.GetScore();
        return 0;
    }

    public Dictionary<ulong, int> GetAllPlayerScores()
    {
        Dictionary<ulong, int> scores = new Dictionary<ulong, int>();
        PlayerInfo[] playersInfo = FindObjectsByType<PlayerInfo>(FindObjectsSortMode.InstanceID);

        foreach (PlayerInfo info in playersInfo)
        {
            if (info != null)
            {
                NetworkObject networkObject = info.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    scores[networkObject.OwnerClientId] = info.GetScore();
                }
            }
        }
        return scores;
    }

    public Dictionary<ulong, string> GetAllPlayerUsernames()
    {
        Dictionary<ulong, string> usernames = new Dictionary<ulong, string>();
        PlayerInfo[] playersInfo = FindObjectsByType<PlayerInfo>(FindObjectsSortMode.InstanceID);

        foreach (PlayerInfo info in playersInfo)
        {
            if (info != null)
            {
                NetworkObject netObj = info.GetComponent<NetworkObject>();
                if (netObj != null)
                    usernames[netObj.OwnerClientId] = info.GetUsername();
            }
        }
        return usernames;
    }

    public Dictionary<ulong, bool> GetAllPlayerReadyStates()
    {
        Dictionary<ulong, bool> readyStates = new Dictionary<ulong, bool>();
        PlayerReadyState[] allPlayerReadyStates = FindObjectsByType<PlayerReadyState>(FindObjectsSortMode.InstanceID);

        foreach (PlayerReadyState playerReadyState in allPlayerReadyStates)
        {
            if (playerReadyState != null)
            {
                NetworkObject netObj = playerReadyState.GetComponent<NetworkObject>();
                if (netObj != null)
                    readyStates[netObj.OwnerClientId] = playerReadyState.IsReady();
            }
        }
        return readyStates;
    }

    public int GetLocalPlayerScore()
    {
        if (NetworkManager.Singleton != null)
        {
            return getPlayerScore(NetworkManager.Singleton.LocalClientId);
        }
        return 0;
    }

    [ClientRpc]
    public void NotifyLeaderboardUpdateClientRpc(string playerId, int score)
    {
        LeaderboardManager.Instance.UpdateLocalHighlight(playerId, score);
    }
}