using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System;

public class PlayerSpawnController : NetworkBehaviour
{
    private int myQuadrant = -1;

    [SerializeField] private List<Transform> UpSpawnPoints;
    [SerializeField] private List<Transform> RightSpawnPoints;
    [SerializeField] private List<Transform> DownSpawnPoints;
    [SerializeField] private List<Transform> LeftSpawnPoints;

    private List<Transform> allSpawnPoints;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            allSpawnPoints = new List<Transform>();
            allSpawnPoints.AddRange(UpSpawnPoints);
            allSpawnPoints.AddRange(RightSpawnPoints);
            allSpawnPoints.AddRange(DownSpawnPoints);
            allSpawnPoints.AddRange(LeftSpawnPoints);
        }
    }

    public void SetPlayerQuadrant(int quadrant)
    {
        myQuadrant = quadrant;

        if (IsServer)
        {
            Debug.Log($"Player {OwnerClientId} assigned to quadrant {quadrant}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void EnableMySpawnPointsServerRpc()
    {
        if (!IsServer) return;

        EnableSpawnPointsForQuadrant(myQuadrant);
    }

    private void EnableSpawnPointsForQuadrant(int quadrant)
    {
        foreach (Transform point in allSpawnPoints)
        {
            point.gameObject.SetActive(false);
        }

        switch (quadrant)
        {
            case 1: //top right
                EnablePoints(myQuadrant, UpSpawnPoints);
                EnablePoints(myQuadrant, RightSpawnPoints);
                EnablePoints(myQuadrant, LeftSpawnPoints);

                DisablePoints(4, DownSpawnPoints);
                break;
            case 2: //top left
                EnablePoints(myQuadrant, UpSpawnPoints);
                EnablePoints(myQuadrant, LeftSpawnPoints);
                break;
            case 3: //bottom left
                EnablePoints(myQuadrant, DownSpawnPoints);
                EnablePoints(myQuadrant, LeftSpawnPoints);
                break;
            case 4: //bottom right
                EnablePoints(myQuadrant, DownSpawnPoints);
                EnablePoints(myQuadrant, RightSpawnPoints);
                break;
        }
    }

    //doesnt find the other player - also dont want it to be recursive =================================
    private void EnablePoints(int quad, List<Transform> points)
    {
        GameObject player = PlayerSpawner.instance.GetPlayerInQuadrant(quad);

        if (myQuadrant != quad && player != null)
        {
            player.GetComponent<PlayerSpawnController>().EnablePoints(quad, points);
        }
        else
        {
            Debug.LogWarning($"no player found in quadrant {quad} to enable points");
        }

        foreach (Transform point in points)
        {
            point.gameObject.SetActive(true);
        }
    }

    private void DisablePoints(int quad, List<Transform> points)
    {
        GameObject player = PlayerSpawner.instance.GetPlayerInQuadrant(quad);

        if (myQuadrant != quad && player != null)
        {
            player.GetComponent<PlayerSpawnController>().DisablePoints(quad, points);
        }
        else
        {
            Debug.LogWarning($"no player found in quadrant {quad} to disable points");
        }

        foreach (Transform point in points)
        {
            point.gameObject.SetActive(false);
        }
    }

}
