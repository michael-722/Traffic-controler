using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerScoreManager : NetworkBehaviour
{
    public static PlayerScoreManager Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }

    [ServerRpc(RequireOwnership = false)]
    public void increaseScoreServerRpc(ulong clientId, int amount)
    {
        //Debug.Log($"increase score for client {clientId} by {amount}");

        //find player score for client id
        PlayerScore playerScore = FindPlayerScoreForClient(clientId);
        if (playerScore != null)
            playerScore.addScore(amount);
        else
            Debug.LogError($"playerscore for client {clientId} not found");
    }

    private PlayerScore FindPlayerScoreForClient(ulong clientId)
    {
        PlayerScore[] allPlayerScores = FindObjectsByType<PlayerScore>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);

        //itterate through all scores to find client id match
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
        if (playerScore != null)
        {
            return playerScore.GetScore();
        }
        return 0;
    }

    public Dictionary<ulong, int> GetAllPlayerScores()
    {
        Dictionary<ulong, int> scores = new Dictionary<ulong, int>();
        PlayerScore[] allPlayerScores = FindObjectsByType<PlayerScore>(FindObjectsSortMode.InstanceID);

        //rerun all players and scores in dict
        foreach (PlayerScore playerScore in allPlayerScores)
        {
            if (playerScore != null)
            {
                NetworkObject networkObject = playerScore.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    ulong ownerId = networkObject.OwnerClientId;
                    scores[ownerId] = playerScore.GetScore();
                }
            }
        }
        return scores;
    }

    public int GetLocalPlayerScore()
    {
        if (NetworkManager.Singleton != null)
        {
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            return getPlayerScore(localClientId);
        }
        return 0;
    }
}