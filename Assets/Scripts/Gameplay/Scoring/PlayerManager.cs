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
        PlayerScore playerScore = FindPlayerScoreForClient(clientId);

        if (playerScore != null) playerScore.addScore(amount);
        else Debug.LogError($"playerscore for client {clientId} not found");
    }

    [ServerRpc(RequireOwnership = false)]
    public void DecreaseScoreServerRpc(ulong clientId, int amount)
    {
        PlayerScore playerScore = FindPlayerScoreForClient(clientId);

        if (playerScore != null) playerScore.subtractScore(amount);
        else Debug.LogError($"playerscore for client {clientId} not found");
    }

    private PlayerScore FindPlayerScoreForClient(ulong clientId)
    {
        PlayerScore[] allPlayerScores = FindObjectsByType<PlayerScore>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);

        foreach (PlayerScore playerScore in allPlayerScores)
        {
            if (playerScore != null)
            {
                NetworkObject networkObject = playerScore.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned && networkObject.OwnerClientId == clientId)
                    return playerScore;
            }
        }
        return null;
    }

    public int getPlayerScore(ulong clientId)
    {
        PlayerScore playerScore = FindPlayerScoreForClient(clientId);
        if (playerScore != null) return playerScore.GetScore();
        return 0;
    }

    public Dictionary<ulong, int> GetAllPlayerScores()
    {
        Dictionary<ulong, int> scores = new Dictionary<ulong, int>();
        PlayerScore[] allPlayerScores = FindObjectsByType<PlayerScore>(FindObjectsSortMode.InstanceID);

        foreach (PlayerScore playerScore in allPlayerScores)
        {
            if (playerScore != null)
            {
                NetworkObject networkObject = playerScore.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    scores[networkObject.OwnerClientId] = playerScore.GetScore();
                }
            }
        }
        return scores;
    }

    public Dictionary<ulong, string> GetAllPlayerUsernames()
    {
        Dictionary<ulong, string> usernames = new Dictionary<ulong, string>();
        PlayerScore[] allPlayerScores = FindObjectsByType<PlayerScore>(FindObjectsSortMode.InstanceID);

        foreach (PlayerScore playerScore in allPlayerScores)
        {
            if (playerScore != null)
            {
                NetworkObject netObj = playerScore.GetComponent<NetworkObject>();
                if (netObj != null)
                    usernames[netObj.OwnerClientId] = playerScore.GetUsername();
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
}