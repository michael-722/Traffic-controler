using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;


public class PlayerSpawner : NetworkBehaviour
{
    public static PlayerSpawner instance;

    [SerializeField] private List<Transform> spawnPoints;
    [SerializeField] private GameObject playerPrefab;

    private Vector3 centerPoint;
    private int nextSpawnIndex = 0;

    private Dictionary<ulong, int> playerQuadrantMap = new Dictionary<ulong, int>();
    private List<ulong> spawnedPlayers = new List<ulong>();
    private Dictionary<ulong, PlayerSpawnController> playerControllers = new Dictionary<ulong, PlayerSpawnController>();

    #region SETUP
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
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        SpawnPlayer(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;
        HandlePlayerDisconnected(clientId);
    }
    #endregion

    #region PLAYER SPAWNING
    private void SpawnPlayer(ulong clientId)
    {
        //check quad map for available quads
        Transform spawnPoint = null;
        int spawnIndex = -1;
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            int quadrant = GetQuadrantFromPosition(spawnPoints[i].position);
            bool spotTaken = false;
            foreach (var quad in playerQuadrantMap)
            {
                if (quad.Value == quadrant)
                {
                    spotTaken = true;
                    break;
                }
            }
            if (!spotTaken)
            {
                spawnPoint = spawnPoints[i];
                spawnIndex = i;
                break;
            }
        }
        
        if (spawnPoint == null)
        {
            Debug.LogWarning("All quads are taken - max 4 players");
            return;
        }

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

        //setting spawn quad
        int quadrant = GetQuadrantFromPosition(spawnPoint.position);
        playerQuadrantMap[networkObject.OwnerClientId] = quadrant;
        spawnedPlayers.Add(networkObject.OwnerClientId);

        PlayerSpawnController playerSpawnController = obj.GetComponent<PlayerSpawnController>();

        //store controller ref
        playerControllers[networkObject.OwnerClientId] = playerSpawnController;
        playerSpawnController.SetPlayerQuadrant(quadrant);
        HandleNewPlayerConnections(networkObject.OwnerClientId);
    }

    private void HandleNewPlayerConnections(ulong newPlayerId)
    {
        int newPlayerQuadrant = GetPlayerQuadrant(newPlayerId);
        PlayerSpawnController newPlayerController = GetPlayerController(newPlayerId);

        //base spawn pos set
        newPlayerController.ConfigureSpawnPoints();

        //check adjacent players
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
    #endregion

    #region PLAYER DISCONNECTING
    private void HandlePlayerDisconnected(ulong clientId)
    {
        int disconnectedPlayerQuadrant = GetPlayerQuadrant(clientId);

        if (playerQuadrantMap.ContainsKey(clientId))
        {
            playerQuadrantMap.Remove(clientId);
        }

        if (spawnedPlayers.Contains(clientId))
        {
            spawnedPlayers.Remove(clientId);
        }

        if (playerControllers.ContainsKey(clientId))
        {
            playerControllers.Remove(clientId);
        }

        if (disconnectedPlayerQuadrant != -1)//check if vaild before reenabling spawn points
        {
            ReenableSpawnPointsForDisconnectedPlayer(clientId, disconnectedPlayerQuadrant);
        }
    }

    private void ReenableSpawnPointsForDisconnectedPlayer(ulong disconnectedPlayerId, int disconnectedPlayerQuadrant)
    {
        foreach (ulong remainingPlayerId in spawnedPlayers)
        {
            if (remainingPlayerId == disconnectedPlayerId) continue;

            int remainingPlayerQuadrant = GetPlayerQuadrant(remainingPlayerId);
            PlayerSpawnController remainingPlayerController = GetPlayerController(remainingPlayerId);

            //check adjacency
            if (AreQuadrantsAdjacent(remainingPlayerQuadrant, disconnectedPlayerQuadrant))
            {
                //re enable spawn points for remaining areas
                ReenableAdjacentSpawnPoints(remainingPlayerController, remainingPlayerQuadrant, disconnectedPlayerQuadrant, remainingPlayerId);
            }
        }
    }

    private void ReenableAdjacentSpawnPoints(PlayerSpawnController playerController, int playerQuadrant, int adjacentQuadrant, ulong playerId)
    {
        List<Transform> pointsToReenable = new List<Transform>();
        string direction = "";

        switch (playerQuadrant)
        {
            case 4://bottom right
                if (adjacentQuadrant == 1)//top right
                {
                    pointsToReenable = playerController.GetSpawnPointsByDirection("up");
                    direction = "up";
                }
                else if (adjacentQuadrant == 3)//bottom left
                {
                    pointsToReenable = playerController.GetSpawnPointsByDirection("left");
                    direction = "left";
                }
                break;

            case 1: //top right
                if (adjacentQuadrant == 4)//bottom right
                {
                    pointsToReenable = playerController.GetSpawnPointsByDirection("down");
                    direction = "down";
                }
                else if (adjacentQuadrant == 2)//top left
                {
                    pointsToReenable = playerController.GetSpawnPointsByDirection("left");
                    direction = "left";
                }
                break;

            case 2://top left
                if (adjacentQuadrant == 1)//top right
                {
                    pointsToReenable = playerController.GetSpawnPointsByDirection("right");
                    direction = "right";
                }
                else if (adjacentQuadrant == 3)//bottom left
                {
                    pointsToReenable = playerController.GetSpawnPointsByDirection("down");
                    direction = "down";
                }
                break;

            case 3://bottom left
                if (adjacentQuadrant == 2)//top left
                {
                    pointsToReenable = playerController.GetSpawnPointsByDirection("up");
                    direction = "up";
                }
                else if (adjacentQuadrant == 4)//bottom right
                {
                    pointsToReenable = playerController.GetSpawnPointsByDirection("right");
                    direction = "right";
                }
                break;
        }

        //reenable spawn points - server + client
        if (pointsToReenable.Count > 0 && !string.IsNullOrEmpty(direction))
        {
            playerController.ReenableSpawnPoints(pointsToReenable);
            playerController.ReenableSpawnPointsClientRpc(direction, playerId);
        }
    }
    #endregion

    #region HELPERS
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
        if (playerControllers.ContainsKey(clientId))
        {
            return playerControllers[clientId];
        }

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
        {
            if (client.PlayerObject != null)
            {
                return client.PlayerObject.GetComponent<PlayerSpawnController>();
            }
        }
        return null;
    }

    public int GetQuadrantFromPosition(Vector3 position)
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

    public ulong GetPlayerInQuadrant(int quadrant)
    {
        foreach (var entry in playerQuadrantMap)
        {
            if (entry.Value == quadrant)
            {
                return entry.Key;
            }
        }
        return 0;
    }
    #endregion
}