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

    public void ConfigureSpawnPoints()
    {
        if (!IsServer) return;

        //Debug.Log($"setting spawn points for player {OwnerClientId} quad {myQuadrant}");

        foreach (Transform point in allSpawnPoints)
        {
            point.gameObject.SetActive(false);
        }

        //enable spawn points based on quadrant
        switch (myQuadrant)
        {
            case 1://top right
                EnablePoints(UpSpawnPoints);
                EnablePoints(RightSpawnPoints);
                EnablePoints(LeftSpawnPoints);
                break;
            case 2://top left
                EnablePoints(UpSpawnPoints);
                EnablePoints(LeftSpawnPoints);
                EnablePoints(DownSpawnPoints);
                break;
            case 3://bottom left
                EnablePoints(DownSpawnPoints);
                EnablePoints(LeftSpawnPoints);
                break;
            case 4://bottom right - default
                EnablePoints(DownSpawnPoints);
                EnablePoints(RightSpawnPoints);
                EnablePoints(LeftSpawnPoints);
                EnablePoints(UpSpawnPoints);
                break;
        }
    }

    public void DisableAdjacentSpawnPoints(int adjacentQuadrant, List<Transform> pointsToDisable)
    {
        if (!IsServer) return;

        //Debug.Log($"player {OwnerClientId} quad {myQuadrant} tring to disable {pointsToDisable.Count} spawn points from adjacent quad {adjacentQuadrant}");

        if (AreQuadrantsAdjacent(myQuadrant, adjacentQuadrant))//disable if adjacent
        {
            foreach (Transform point in pointsToDisable)
            {
                //Debug.Log($"disabling spawn point: {point.name} on player {OwnerClientId}");
                point.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogWarning($"quadrants {myQuadrant} and {adjacentQuadrant} are not adjacent - ignored disable request");
        }
    }

    private bool AreQuadrantsAdjacent(int quadrant1, int quadrant2)
    {
        switch (quadrant1)
        {
            case 1://top right
                return quadrant2 == 2 || quadrant2 == 4;//top left + bottom right
            case 2://top left
                return quadrant2 == 1 || quadrant2 == 3;//top right + bottom left
            case 3://bottom left
                return quadrant2 == 2 || quadrant2 == 4;//top left + bottom right
            case 4://bottom right
                return quadrant2 == 1 || quadrant2 == 3;//top right + bottom left
            default:
                return false;
        }
    }

    private void EnablePoints(List<Transform> points)
    {
        foreach (Transform point in points)
        {
            point.gameObject.SetActive(true);
        }
    }

    public List<Transform> GetSpawnPointsByDirection(string direction)
    {
        switch (direction.ToLower())
        {
            case "up":
                return UpSpawnPoints;
            case "right":
                return RightSpawnPoints;
            case "down":
                return DownSpawnPoints;
            case "left":
                return LeftSpawnPoints;
            default:
                Debug.LogWarning($"unknown dir: {direction}");
                return new List<Transform>();
        }
    }
}
