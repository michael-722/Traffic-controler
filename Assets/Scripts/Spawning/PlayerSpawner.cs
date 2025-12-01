using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private List<Transform> spawnPoints;
    [SerializeField] private GameObject playerPrefab;

    private int nextSpawnIndex = 0;
    private Vector3 centerOfSpawnPoints;

    private void Awake()
    {
        Vector3 sum = Vector3.zero;
        foreach (Transform point in spawnPoints)
        {
            sum += point.position;
        }
        centerOfSpawnPoints = sum / spawnPoints.Count;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) enabled = false;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        SpawnPlayer(clientId);
    }

    private IEnumerator EnableSpawnPointsWhenReady(GameObject obj, Transform spawnPoint)
    {
        while (!obj.GetComponent<NetworkObject>().IsSpawned)
        {
            yield return null;
        }

        obj.GetComponent<PlayerSpawnController>().SetQuadrant(GetQuadrant(spawnPoint.position));
        obj.GetComponent<PlayerSpawnController>().EnableMySpawnPoints();
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (!IsServer) return;
        if (spawnPoints.Count == 0)
        {
            Debug.LogError("No spawn points assigned!");
            return;
        }
        if (spawnPoints[nextSpawnIndex] == null) return;

        Transform spawnPoint = spawnPoints[nextSpawnIndex];
        nextSpawnIndex = (nextSpawnIndex + 1) % spawnPoints.Count;

        GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

        StartCoroutine(EnableSpawnPointsWhenReady(playerInstance, spawnPoint));
        SetCameraClientRpc(new Vector3(playerInstance.transform.position.x, 35f, playerInstance.transform.position.z), clientId);
    }

    [ClientRpc]
    private void SetCameraClientRpc(Vector3 spawnPosition, ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId) return;
        Camera.main.transform.position = new Vector3(spawnPosition.x, 35f, spawnPosition.z);
        CameraManager.Instance.SetAreaCenter(spawnPosition);
    }

    ///returns quadrant 1-4 relative to center
    private int GetQuadrant(Vector3 SpawnPos)
    {
        Vector3 toSpawnPos = SpawnPos - centerOfSpawnPoints;
        Vector3 right = new Vector3(1, 0, 0);
        Vector3 forward = new Vector3(0, 0, 1);

        float dotRight = Vector3.Dot(toSpawnPos, right);
        float dotForward = Vector3.Dot(toSpawnPos, forward);

        ///1 +x +z
        ///2 -x +z
        ///3 -x -z
        ///4 +x -z
        if (dotRight >= 0 && dotForward >= 0)
            return 1;
        else if (dotRight < 0 && dotForward >= 0)
            return 2;
        else if (dotRight < 0 && dotForward < 0)
            return 3;
        else
            return 4;
    }
}