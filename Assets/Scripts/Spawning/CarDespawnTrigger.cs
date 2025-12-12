using Unity.Netcode;
using UnityEngine;

public class CarDespawnTrigger : MonoBehaviour
{
    private NetworkObject rootNetObj;

    private void Start()
    {
        PlayerSpawnController playerSpawnController = FindFirstObjectByType<PlayerSpawnController>();
        rootNetObj = playerSpawnController.gameObject.GetComponent<NetworkObject>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            CarSpawner.Instance.DespawnCar(other.gameObject.GetComponent<NetworkObject>());
            PlayerScoreManager.Instance.increaseScoreServerRpc(rootNetObj.OwnerClientId, 1);
        }
    }
}
