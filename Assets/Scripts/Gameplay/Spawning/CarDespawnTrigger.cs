using Unity.Netcode;
using UnityEngine;

public class CarDespawnTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            NetworkObject carNetworkObject = other.gameObject.GetComponent<NetworkObject>();

            if (carNetworkObject == null)
                return;
            else 
                Destroy(other.gameObject);

            //despawn via spawner
            if (CarSpawner.Instance != null)
            {
                CarSpawner.Instance.DespawnCar(carNetworkObject);
            }

            //scoring
            if (PlayerScoreManager.Instance != null && NetworkManager.Singleton != null)
            {
                ulong localPlayerId = NetworkManager.Singleton.LocalClientId;
                PlayerScoreManager.Instance.increaseScoreServerRpc(localPlayerId, 1);
            }
            else
            {
                Debug.LogError("player score manager or net manager is null");
            }
        }
    }
}