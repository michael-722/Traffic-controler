using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;

public class PlayerSpawner : NetworkBehaviour
{
    public static PlayerSpawner instance;

    [SerializeField] private List<Transform> spawnPoints;
    [SerializeField] private GameObject playerPrefab;

    private Vector3 centerPoint;
    private int nextSpawnIndex = 0;
    private Dictionary<ulong, int> playerQuadrantMap = new Dictionary<ulong, int>();

    private void Awake()
    {
        if (spawnPoints.Count > 0)
        {
            Vector3 sum = Vector3.zero;
            foreach (var sp in spawnPoints)
            {
                sum += sp.position;
            }
            Vector3 avg = sum / spawnPoints.Count;
        }
        else
        {
            Debug.LogWarning("no spawn points assigned to PlayerSpawner");
        }
    }

    private void Start()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        //if (!IsServer) enabled = false;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        SpawnPlayer(clientId);
    }

    private IEnumerator EnableSpawnPointsWhenReady(GameObject obj, Transform spawnPoint)
    {
        while (!obj.GetComponent<NetworkObject>().IsSpawned)
        {
            yield return null;
        }

        int quadrant = GetQuadrantFromPosition(spawnPoint.position);
        playerQuadrantMap[obj.GetComponent<NetworkObject>().OwnerClientId] = quadrant;

        var playerSpawnController = obj.GetComponent<PlayerSpawnController>();
        if (playerSpawnController != null)
        {
            playerSpawnController.SetPlayerQuadrant(quadrant);
        }

        playerSpawnController.EnableMySpawnPointsServerRpc();
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

    private int GetQuadrantFromPosition(Vector3 position)
    {
        Vector3 relativePosition = position - centerPoint;

        if (relativePosition.x >= 0 && relativePosition.z >= 0) return 1;//top right
        if (relativePosition.x < 0 && relativePosition.z >= 0) return 2;//top left
        if (relativePosition.x < 0 && relativePosition.z < 0) return 3;//bottom left
        return 4;//bottom right
    }

    [ClientRpc]
    private void SetCameraClientRpc(Vector3 spawnPosition, ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId) return;
        Camera.main.transform.position = new Vector3(spawnPosition.x, 35f, spawnPosition.z);
        CameraManager.Instance.SetAreaCenter(spawnPosition);
    }

    public int GetPlayerQuadrant(ulong clientId)
    {
        if (playerQuadrantMap.ContainsKey(clientId))
        {
            return playerQuadrantMap[clientId];
        }
        return -1;
    }

    public GameObject GetPlayerInQuadrant(int quadrant)
    {
        foreach (var player in playerQuadrantMap)
        {
            if (player.Value == quadrant)
            {
                NetworkClient client = NetworkManager.Singleton.ConnectedClients[player.Key];
                if (client != null && client.PlayerObject != null)
                {
                    return client.PlayerObject.gameObject;
                }
            }
        }
        return null;
    }
}
