using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;

public class CarSpawner : NetworkBehaviour
{
    public static CarSpawner Instance;

    [SerializeField] private GameObject carPrefab;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int carsPerPlayer = 10;
    [SerializeField] private string spawnTag = "CanSpawn";

    private bool canSpawn = false;
    private float spawnTimer;

    Dictionary<ulong, List<Transform>> groupedSpawnPoints = new Dictionary<ulong, List<Transform>>();

    private List<Transform> spawnPoints = new();
    private NetworkVariable<List<ulong>> spawnedCars = new NetworkVariable<List<ulong>>(
        new List<ulong>(),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        GameManager.Instance.OnGameStateChanged += HandleState;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
    }

    private void HandleState(GameState state)
    {

        if (state == GameState.Gameplay)
        {
            RescanSpawnPoints();
            TurnOnCarSpawning();
        }
        else TrunOffCarSpawning();
    }

    private void HandleClientDisconnect(ulong clientId)
    {
        if (groupedSpawnPoints.ContainsKey(clientId))
            groupedSpawnPoints.Remove(clientId);
    }

    private void Update()
    {
        if (!NetworkManager.Singleton.IsListening) return;
        if (!canSpawn || !IsServer) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval && spawnedCars.Value.Count < GetMaxCars())
        {
            spawnTimer = 0f;
            SpawnCar();
        }
    }

    private void SpawnCar()
    {
        List<ulong> deadKeys = new List<ulong>();

        foreach (var playerArea in groupedSpawnPoints)
        {
            List<Transform> spawnList = playerArea.Value;
            spawnList.RemoveAll(item => item == null);

            if (spawnList.Count == 0)
            {
                deadKeys.Add(playerArea.Key);
                continue;
            }

            Transform sp = spawnList[UnityEngine.Random.Range(0, spawnList.Count)];

            // Final safety check
            if (sp != null)
            {
                GameObject go = Instantiate(carPrefab, sp.position, sp.rotation);

                NetworkObject netObj = go.GetComponent<NetworkObject>();
                netObj.Spawn(true);

                spawnedCars.Value.Add(netObj.NetworkObjectId);

                if (spawnedCars.Value.Count >= GetMaxCars())
                    break;
            }
        }

        foreach (ulong key in deadKeys)
            groupedSpawnPoints.Remove(key);
        
    }

    public void DespawnCar(NetworkObject car)
    {
        if (car == null) return;
        if (!IsServer) return;

        if (spawnedCars.Value.Contains(car.NetworkObjectId))
        {
            car.Despawn(true);
            spawnedCars.Value.Remove(car.NetworkObjectId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DespawnCarServerRpc(ulong netObjId)
    {
        if (!spawnedCars.Value.Contains(netObjId))
        {
            Debug.Log("failed");
            return;
        }

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netObjId, out NetworkObject car))
        {
            if (car == null) return;
            car.Despawn(true);
            spawnedCars.Value.Remove(netObjId);
        }
        else Debug.Log("car with ID " + netObjId + " not found");

    }

    #region HELPERS
    public void TurnOnCarSpawning()
    {
        if (!IsServer) return;
        canSpawn = true;
    }
    //dont combine need to be called via events from buttons
    public void TrunOffCarSpawning()
    {
        if (!IsServer) return;
        canSpawn = false;
    }

    private void RescanSpawnPoints()
    {
        spawnPoints.Clear();

        GameObject[] taggedObjs = GameObject.FindGameObjectsWithTag(spawnTag);
        foreach (GameObject obj in taggedObjs)
        {
            //find root objs netObj 
            Transform current = obj.transform;
            NetworkObject rootNetObj = null;
            while (current != null)
            {
                rootNetObj = current.GetComponent<NetworkObject>();
                if (rootNetObj != null)
                    break;
                current = current.parent;
            }

            //group spawn points by parent netObj id
            if (rootNetObj != null)
            {
                ulong parentId = rootNetObj.NetworkObjectId;

                if (!groupedSpawnPoints.ContainsKey(parentId))
                    groupedSpawnPoints[parentId] = new List<Transform>();

                groupedSpawnPoints[parentId].Add(obj.transform);
            }
            else Debug.LogWarning("spawn point parent has no netObj");
        }
    }

    public void ClearAllCars()
    {
        if (!IsServer || !NetworkManager.Singleton.IsListening) return;

        List<ulong> carsToDespawn = new List<ulong>(spawnedCars.Value);

        foreach (ulong objId in carsToDespawn)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objId, out NetworkObject car))
            {
                car.Despawn(true);
                spawnedCars.Value.Remove(objId);
            }
        }
    }

    public ulong GetCarId(GameObject obj)
    {
        NetworkObject netObj = obj.GetComponent<NetworkObject>();

        if (spawnedCars.Value.Contains(netObj.NetworkObjectId))
            return netObj.NetworkObjectId;
        else return 0;
    }

    private int GetMaxCars()
    {
        int playerCount = Mathf.Max(1, NetworkManager.Singleton.ConnectedClients.Count);
        return playerCount * carsPerPlayer;
    }
    #endregion
}