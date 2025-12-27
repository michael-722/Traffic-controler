using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;

public class CarSpawner : NetworkBehaviour
{
    public static CarSpawner Instance;

    public event Action EnableCarSpawning;

    [SerializeField] private GameObject carPrefab;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int carsPerPlayer = 10;
    [SerializeField] private string spawnTag = "CanSpawn";

    private bool canSpawn = false;
    private float spawnTimer;

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

    public override void OnNetworkSpawn()
    {
        if (IsServer) GameManager.Instance.OnGameStateChanged += HandleState;
        if (!IsServer) enabled = false;
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

    private void RescanSpawnPoints()
    {
        spawnPoints.Clear();
        GameObject[] taggedObjs = GameObject.FindGameObjectsWithTag(spawnTag);
        foreach (GameObject obj in taggedObjs) { spawnPoints.Add(obj.transform); }
    }

    private void Update()
    {
        if (!canSpawn || !IsServer) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval && spawnedCars.Value.Count < GetMaxCars())
        {
            spawnTimer = 0f;
            SpawnCar();
        }
    }

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

    private void SpawnCar()
    {
        Transform sp = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
        GameObject go = Instantiate(carPrefab, sp.position, sp.rotation);
        NetworkObject netObj = go.GetComponent<NetworkObject>();
        netObj.Spawn(true);

        spawnedCars.Value.Add(netObj.NetworkObjectId);
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
            car.Despawn(true);
            spawnedCars.Value.Remove(netObjId);
        }
        else Debug.Log("car with ID " + netObjId + " not found");

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
}