using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class StatTracker : NetworkBehaviour
{
    public static StatTracker Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else Destroy(gameObject);
    }


    [ServerRpc(RequireOwnership = false)]
    public void IncreaseTotalCrashesServerRpc(ulong clientId)
    {
        PlayerStats playerStats = FindStatsForPlayer(clientId);

        if (playerStats != null) playerStats.increaseTotalCrashes();
        else Debug.LogError($"playerscore for client {clientId} not found");
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddWaitTimeServerRpc(ulong clientId, float waitTime)
    {
        PlayerStats playerStats = FindStatsForPlayer(clientId);
        if (playerStats != null) playerStats.AddWaitTime(waitTime);
        else Debug.LogError($"playerStats for client {clientId} not found");
    }

    private PlayerStats FindStatsForPlayer(ulong clientId)
    {
        PlayerStats[] playerStats = FindObjectsByType<PlayerStats>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);

        foreach (PlayerStats stats in playerStats)
        {
            if (stats != null)
            {
                NetworkObject networkObject = stats.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned && networkObject.OwnerClientId == clientId)
                    return stats;
            }
        }
        return null;
    }

    public Dictionary<ulong, int> GetAllPlayerCrashes()
    {
        Dictionary<ulong, int> PlayerCrashes = new Dictionary<ulong, int>();
        PlayerStats[] playersStats = FindObjectsByType<PlayerStats>(FindObjectsSortMode.InstanceID);

        foreach (PlayerStats player in playersStats)
        {
            if (player != null)
            {
                NetworkObject networkObj = player.GetComponent<NetworkObject>();
                if (networkObj != null)
                    PlayerCrashes[networkObj.OwnerClientId] = player.getCrashes();
            }
        }
        return PlayerCrashes;
    }

    public Dictionary<ulong, float> GetAllPlayerTotalWaitTime()
    {
        Dictionary<ulong, float> PlayerTotalWaitTime = new Dictionary<ulong, float>();
        PlayerStats[] playersStats = FindObjectsByType<PlayerStats>(FindObjectsSortMode.InstanceID);

        foreach (PlayerStats player in playersStats)
        {
            if (player != null)
            {
                NetworkObject networkObj = player.GetComponent<NetworkObject>();
                if (networkObj != null)
                    PlayerTotalWaitTime[networkObj.OwnerClientId] = player.getTotalWaitTime();
            }
        }
        return PlayerTotalWaitTime;
    }

    public Dictionary<ulong, int> GetAllPlayerWaitTimeEnties()
    {
        Dictionary<ulong, int> PlayerWaitTimes = new Dictionary<ulong, int>();
        PlayerStats[] playersStats = FindObjectsByType<PlayerStats>(FindObjectsSortMode.InstanceID);

        foreach (PlayerStats player in playersStats)
        {
            if (player != null)
            {
                NetworkObject networkObj = player.GetComponent<NetworkObject>();
                if (networkObj != null)
                    PlayerWaitTimes[networkObj.OwnerClientId] = player.getWaitTimeEntries();
            }
        }
        return PlayerWaitTimes;
    }
}