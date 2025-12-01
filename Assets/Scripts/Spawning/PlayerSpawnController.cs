using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerSpawnController : NetworkBehaviour
{
    private int myQuadrant = -1;
    private CarSpawner carSpawner;

    [SerializeField] private List<Transform> UpSpawnPos;
    [SerializeField] private List<Transform> RightSpawnPos;
    [SerializeField] private List<Transform> DownSpawnPos;
    [SerializeField] private List<Transform> LeftSpawnPos;

    public List<List<Transform>> spawnPointGroups;

    private void Awake()
    {
        spawnPointGroups = new List<List<Transform>>
        {
            UpSpawnPos,
            RightSpawnPos,
            DownSpawnPos,
            LeftSpawnPos
        };
    }

    private void Start()
    {
        if (IsServer)
        {
            carSpawner = FindFirstObjectByType<CarSpawner>();
            if (carSpawner == null) Debug.LogError("CarSpawner not found");
        }
    }

    public void SetQuadrant(int quadrant)
    {
        myQuadrant = quadrant;
    }

    public void EnableMySpawnPoints()
    {
        if (!IsServer) return;

        switch (myQuadrant)
        {
            case 1://top right
                EnableSpawnPointsInQuadrant(1, JunctionSpawnDirection.Up);
                EnableSpawnPointsInQuadrant(1, JunctionSpawnDirection.Right);
                DisableSpawnPointsInQuadrant(4, JunctionSpawnDirection.Up);
                break;

            case 2://top left
                EnableSpawnPointsInQuadrant(2, JunctionSpawnDirection.Up);
                EnableSpawnPointsInQuadrant(2, JunctionSpawnDirection.Left);
                DisableSpawnPointsInQuadrant(1, JunctionSpawnDirection.Left);
                break;

            case 3://bottom left
                EnableSpawnPointsInQuadrant(3, JunctionSpawnDirection.Down);
                EnableSpawnPointsInQuadrant(3, JunctionSpawnDirection.Left);
                DisableSpawnPointsInQuadrant(2, JunctionSpawnDirection.Down);
                DisableSpawnPointsInQuadrant(4, JunctionSpawnDirection.Left);
                break;

            case 4://bottom right - host
                EnableSpawnPointsInQuadrant(4, JunctionSpawnDirection.Up);
                EnableSpawnPointsInQuadrant(4, JunctionSpawnDirection.Right);
                EnableSpawnPointsInQuadrant(4, JunctionSpawnDirection.Down);
                EnableSpawnPointsInQuadrant(4, JunctionSpawnDirection.Left);
                break;
        }
    }

    public void DisableSpawnPointsInQuadrant(int quadrant, JunctionSpawnDirection direction)
    {
        //need to use quadrant to apply changes to correct players area================================
        if (!IsServer) return;

        int dir = (int)direction;
        Debug.Log($"SERVER disabling quadrant {quadrant} dir {direction} ({dir})");

        BroadcastSpawnPointState(dir, false);
    }

    public void EnableSpawnPointsInQuadrant(int quadrant, JunctionSpawnDirection direction)
    {
        if (!IsServer) return;

        int dir = (int)direction;
        Debug.Log($"SERVER enabling quadrant {quadrant} dir {direction} ({dir})");

        BroadcastSpawnPointState(dir, true);
    }

    private void BroadcastSpawnPointState(int dir, bool state)
    {
        SetSpawnPointStateClientRpc(dir, state);
        SetSpawnPointStateLocal(dir, state);
    }

    [ClientRpc]
    private void SetSpawnPointStateClientRpc(int dir, bool state)
    {
        SetSpawnPointStateLocal(dir, state);
    }

    private void SetSpawnPointStateLocal(int dir, bool state)
    {
        try
        {
            var spawnPoint = spawnPointGroups[dir][0].gameObject;
            spawnPoint.GetComponentInParent<Transform>().gameObject.SetActive(state);
        }
        catch (Exception e)
        {
            Debug.LogError($"spawn point error: {e.Message}");
        }
    }

    public enum JunctionSpawnDirection
    {
        Up,
        Right,
        Down,
        Left
    }
}
