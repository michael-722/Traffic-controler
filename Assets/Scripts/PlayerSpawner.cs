using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private List<Vector3> spawnPoints;

    private static int nextSpawnIndex = 0;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (nextSpawnIndex >= spawnPoints.Count)
        {
            Debug.LogWarning("no more spawn points");
            return;
        }

        Vector3 spawnPos = spawnPoints[nextSpawnIndex];
        nextSpawnIndex++;

        transform.position = spawnPos;
        SetPositionClientRpc(spawnPos);
    }

    [ClientRpc]
    private void SetPositionClientRpc(Vector3 pos)
    {
        if (IsOwner)
        {
            CameraManager.Instance.SetAreaCenter(pos);
            Camera.main.transform.position = new Vector3(pos.x, 35f, pos.z);
            transform.position = pos;
        }
    }
}