using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class CarSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject carPrefab;
    [SerializeField] private List<Transform> spawnPoints;

    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int maxSpawnedCars = 10;

    private List<NetworkObject> spawnedCars = new List<NetworkObject>();
    private float timer;

    private void Update()
    {
        if (!IsServer) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval && spawnedCars.Count < maxSpawnedCars)
        {
            timer = 0f;
            SpawnCar();
        }
    }

    private void SpawnCar()
    {
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
        GameObject carInstance = Instantiate(carPrefab, spawnPoint.position, spawnPoint.rotation);
        NetworkObject netObj = carInstance.GetComponent<NetworkObject>();

        netObj.Spawn(true);
        spawnedCars.Add(netObj);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        foreach (NetworkObject car in spawnedCars)
        {
            if (car != null && car.IsSpawned)
            {
                car.NetworkShow(clientId);//synced for new clients
            }
        }
    }
}