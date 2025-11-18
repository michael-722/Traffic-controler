using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;

public class CarSpawner : NetworkBehaviour
{
    public event Action EnableCarSpawning;

    [SerializeField] private GameObject carPrefab;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int carsPerPlayer = 10;

    [SerializeField] private string spawnTag = "CanSpawn";

    private bool canSpawn = false;

    private List<NetworkObject> spawnedCars = new();
    private List<Transform> spawnPoints = new();

    private float spawnTimer;

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
        if (spawnTimer >= spawnInterval && spawnedCars.Count < GetMaxCars())
        {
            spawnTimer = 0f;
            SpawnCarServer();
        }
    }

    
    public void TurnOnCarSpawning()
    {
        if (!IsServer) return;
        canSpawn = true;
    }
    //dont combine need to be called via events from btns
    public void TrunOffCarSpawning()
    {
        if (!IsServer) return;
        canSpawn = false;
    }

    private void SpawnCarServer()
    {
        Transform sp = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];

        GameObject go = Instantiate(carPrefab, sp.position, sp.rotation);
        NetworkObject netObj = go.GetComponent<NetworkObject>();

        netObj.Spawn(true);

        spawnedCars.Add(netObj);
    }

    private int GetMaxCars()
    {
        int playerCount = Mathf.Max(1, NetworkManager.Singleton.ConnectedClients.Count);
        return playerCount * carsPerPlayer;
    }

    private void SyncCarsToClient(ulong clientId)
    {
        foreach (var car in spawnedCars)
        {
            if (car != null && car.IsSpawned)
            {
                car.NetworkShow(clientId);
            }
        }
    }
}