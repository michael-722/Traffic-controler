using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    public static PlayerSpawner instance;

    [SerializeField] private List<Transform> spawnPoints;
    [SerializeField] private GameObject playerPrefab;

    private Vector3 centerPoint;
    private int nextSpawnIndex = 0;
    private Dictionary<ulong, int> playerQuadrantMap = new Dictionary<ulong, int>();
    private List<ulong> spawnedPlayers = new List<ulong>();

    private void Awake()
    {
        if (spawnPoints.Count > 0)
        {
            Vector3 sum = Vector3.zero;
            foreach (var sp in spawnPoints)
            {
                sum += sp.position;
            }
            centerPoint = sum / spawnPoints.Count;
        }
        else
        {
            Debug.LogWarning("no spawn points assigned to PlayerSpawner");
        }
    }

    private void Start()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        SpawnPlayer(clientId);
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (!IsServer) return;
        
        Transform spawnPoint = spawnPoints[nextSpawnIndex];
        nextSpawnIndex = (nextSpawnIndex + 1) % spawnPoints.Count;

        GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

        SetCameraClientRpc(new Vector3(spawnPoint.position.x, 35f, spawnPoint.position.z), clientId);

        StartCoroutine(EnableSpawnPointsWhenReady(playerInstance, spawnPoint));
    }

    private IEnumerator EnableSpawnPointsWhenReady(GameObject obj, Transform spawnPoint)
    {
        NetworkObject networkObject = obj.GetComponent<NetworkObject>();
        while (!networkObject.IsSpawned)//wait for network spawn
        {
            yield return null;
        }

        //seetting spawn quad
        int quadrant = GetQuadrantFromPosition(spawnPoint.position);
        playerQuadrantMap[networkObject.OwnerClientId] = quadrant;
        spawnedPlayers.Add(networkObject.OwnerClientId);

        var playerSpawnController = obj.GetComponent<PlayerSpawnController>();
        if (playerSpawnController != null)
        {
            playerSpawnController.SetPlayerQuadrant(quadrant);
            HandleNewPlayerConnections(networkObject.OwnerClientId);
        }
    }

    private void HandleNewPlayerConnections(ulong newPlayerId)
    {
        int newPlayerQuadrant = GetPlayerQuadrant(newPlayerId);
        PlayerSpawnController newPlayerController = GetPlayerController(newPlayerId);

        if (newPlayerController == null) return;

        //base spawn pos set
        newPlayerController.ConfigureSpawnPoints();

        //chcek adjacent players     -1 no slef check
        for (int i = 0; i < spawnedPlayers.Count - 1; i++)
        {
            ulong existingPlayerId = spawnedPlayers[i];
            int existingPlayerQuadrant = GetPlayerQuadrant(existingPlayerId);
            PlayerSpawnController existingPlayerController = GetPlayerController(existingPlayerId);

            if (existingPlayerController != null && AreQuadrantsAdjacent(newPlayerQuadrant, existingPlayerQuadrant))
            {
                HandlePlayerConnection(newPlayerController, existingPlayerController, newPlayerQuadrant, existingPlayerQuadrant);
            }
        }
    }

    private void HandlePlayerConnection(PlayerSpawnController player1, PlayerSpawnController player2, int quadrant1, int quadrant2)
    {
        //Debug.Log($"sorting connection between player quad {quadrant1} and player quad {quadrant2}");

        switch (quadrant1)
        {
            case 4://player1 bottom right
                if (quadrant2 == 1)//player2 is top right
                {
                    //vert connection
                    player1.DisableAdjacentSpawnPoints(quadrant2, player1.GetSpawnPointsByDirection("up"));
                    player2.DisableAdjacentSpawnPoints(quadrant1, player2.GetSpawnPointsByDirection("down"));
                }
                else if (quadrant2 == 3)//player2 is bottom left
                {
                    //horiz connection
                    player1.DisableAdjacentSpawnPoints(quadrant2, player1.GetSpawnPointsByDirection("left"));
                    player2.DisableAdjacentSpawnPoints(quadrant1, player2.GetSpawnPointsByDirection("right"));
                }
                break;

            case 1: //player1 top right
                if (quadrant2 == 4)//player2 bottom right
                {
                    //vert connection
                    player1.DisableAdjacentSpawnPoints(quadrant2, player1.GetSpawnPointsByDirection("down"));
                    player2.DisableAdjacentSpawnPoints(quadrant1, player2.GetSpawnPointsByDirection("up"));
                }
                else if (quadrant2 == 2)//player2 top left
                {
                    //horiz connection
                    player1.DisableAdjacentSpawnPoints(quadrant2, player1.GetSpawnPointsByDirection("left"));
                    player2.DisableAdjacentSpawnPoints(quadrant1, player2.GetSpawnPointsByDirection("right"));
                }
                break;

            case 2://player1 top left
                if (quadrant2 == 1)//player2 top right
                {
                    //horiz connection
                    player1.DisableAdjacentSpawnPoints(quadrant2, player1.GetSpawnPointsByDirection("right"));
                    player2.DisableAdjacentSpawnPoints(quadrant1, player2.GetSpawnPointsByDirection("left"));
                }
                else if (quadrant2 == 3)//player2 bottom left
                {
                    //vertconnection
                    player1.DisableAdjacentSpawnPoints(quadrant2, player1.GetSpawnPointsByDirection("down"));
                    player2.DisableAdjacentSpawnPoints(quadrant1, player2.GetSpawnPointsByDirection("up"));
                }
                break;

            case 3://player1 bottom left
                if (quadrant2 == 2)//player2 top left
                {
                    //vert connection
                    player1.DisableAdjacentSpawnPoints(quadrant2, player1.GetSpawnPointsByDirection("up"));
                    player2.DisableAdjacentSpawnPoints(quadrant1, player2.GetSpawnPointsByDirection("down"));
                }
                else if (quadrant2 == 4)//player2 bottom right
                {
                    //horiz connection
                    player1.DisableAdjacentSpawnPoints(quadrant2, player1.GetSpawnPointsByDirection("right"));
                    player2.DisableAdjacentSpawnPoints(quadrant1, player2.GetSpawnPointsByDirection("left"));
                }
                break;
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

    private PlayerSpawnController GetPlayerController(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
        {
            if (client.PlayerObject != null)
            {
                return client.PlayerObject.GetComponent<PlayerSpawnController>();
            }
        }
        return null;
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
}
