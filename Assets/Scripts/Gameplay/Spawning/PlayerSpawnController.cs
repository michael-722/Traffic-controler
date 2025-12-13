using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerSpawnController : NetworkBehaviour
{
    private int myQuadrant = -1;

    [SerializeField] private List<Transform> UpSpawnPoints;
    [SerializeField] private List<Transform> RightSpawnPoints;
    [SerializeField] private List<Transform> DownSpawnPoints;
    [SerializeField] private List<Transform> LeftSpawnPoints;

    private List<Transform> allSpawnPoints;

    private void Awake()
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

        //if (IsServer) Debug.Log($"player {OwnerClientId} assigned to quadrant {quadrant}");
    }

    private void EnablePoints(List<Transform> points)
    {
        foreach (Transform point in points)
        {
            point.gameObject.SetActive(true);
        }
    }

    public void ConfigureSpawnPoints()
    {
        if (!IsServer) return;

        //Debug.Log($"setting spawn points for player {OwnerClientId} quad {myQuadrant}");

        if (allSpawnPoints == null) return;
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

        //Debug.Log($"player {OwnerClientId} quad {myQuadrant} trying to disable {pointsToDisable.Count} spawn points from adjacent quad {adjacentQuadrant}");

        if (AreQuadrantsAdjacent(myQuadrant, adjacentQuadrant))//disable if adjacent
        {
            foreach (Transform point in pointsToDisable)
            {
                //Debug.Log($"disabling spawn point {point.name} on player {OwnerClientId}");
                point.gameObject.SetActive(false);
            }

            //update client side
            string direction = GetDirectionFromQuadrants(myQuadrant, adjacentQuadrant);
            if (!string.IsNullOrEmpty(direction))
            {
                DisableSpawnPointsClientRpc(direction, OwnerClientId);
            }
        }
        else
        {
            Debug.LogWarning($"quadrants {myQuadrant} and {adjacentQuadrant} are not adjacent - ignored disable request");
        }
    }

    [ClientRpc]
    public void DisableSpawnPointsClientRpc(string direction, ulong targetPlayerId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetPlayerId) return;

        List<Transform> pointsToDisable = GetSpawnPointsByDirection(direction);
        foreach (Transform point in pointsToDisable)
        {
            point.gameObject.SetActive(false);
        }
    }

    public void ReenableSpawnPoints(List<Transform> pointsToReenable)
    {
        if (!IsServer) return;

        foreach (Transform point in pointsToReenable)
        {
            //Debug.Log($"enabling spawn point {point.name} on player {OwnerClientId}");
            point.gameObject.SetActive(true);
        }
    }

    [ClientRpc]
    public void ReenableSpawnPointsClientRpc(string direction, ulong targetPlayerId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetPlayerId) return;

        List<Transform> pointsToReenable = GetSpawnPointsByDirection(direction);
        foreach (Transform point in pointsToReenable)
        {
            point.gameObject.SetActive(true);
        }
    }

    private string GetDirectionFromQuadrants(int playerQuadrant, int adjacentQuadrant)
    {
        switch (playerQuadrant)
        {
            case 4://bottom right
                if (adjacentQuadrant == 1) return "up";
                if (adjacentQuadrant == 3) return "left";
                break;
            case 1: //top right
                if (adjacentQuadrant == 4) return "down";
                if (adjacentQuadrant == 2) return "left";
                break;
            case 2://top left
                if (adjacentQuadrant == 1) return "right";
                if (adjacentQuadrant == 3) return "down";
                break;
            case 3://bottom left
                if (adjacentQuadrant == 2) return "up";
                if (adjacentQuadrant == 4) return "right";
                break;
        }
        return "";
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