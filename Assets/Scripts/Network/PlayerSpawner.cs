using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private List<Transform> spawnPoints;
    [SerializeField] private GameObject playerPrefab;

    private int nextSpawnIndex = 0;

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) enabled = false;
    }

    private void OnClientConnected(ulong clientId)
    {
        SpawnPlayer(clientId);
    }

    private void SpawnPlayer(ulong clientId)
    {

        if (spawnPoints.Count == 0)
        {
            Debug.LogError("No spawn points assigned!");
            return;
        }

        Transform spawnPoint = spawnPoints[nextSpawnIndex];
        nextSpawnIndex = (nextSpawnIndex + 1) % spawnPoints.Count;

        GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

        SetCameraClientRpc(spawnPoint.position, clientId);
    }


    [ClientRpc]
    private void SetCameraClientRpc(Vector3 spawnPosition, ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId) return;

        Camera.main.transform.position = new Vector3(spawnPosition.x, 35f, spawnPosition.z);
        CameraManager.Instance.SetAreaCenter(spawnPosition);
    }
}