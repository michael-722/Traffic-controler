using Unity.Netcode;
using UnityEngine;
using System;

public class CarDespawnTrigger : MonoBehaviour
{
    public static event Action OnCarPassed;

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        if (other.CompareTag("Car"))
        {
            NetworkObject carNetworkObject = other.gameObject.GetComponent<NetworkObject>();

            if (carNetworkObject == null)
                return;
            else 
                Destroy(other.gameObject);

            //despawn via spawner
            if (CarSpawner.Instance != null)
                CarSpawner.Instance.DespawnCar(carNetworkObject);
            else Debug.LogError("car spawner is null");

            if (GameManager.Instance != null)
                WinLoseConditions.Instance.CompleteJourney();
            else  Debug.LogError("game manager is null");

            //scoring
            if (PlayerManager.Instance != null && NetworkManager.Singleton != null)
            {
                //ulong localPlayerId = NetworkManager.Singleton.LocalClientId;//triggers on server need to get player area collision happened in for id 
                ulong localPlayerId = GetComponentInParent<NetworkObject>().OwnerClientId;
                PlayerManager.Instance.IncreaseScoreServerRpc(localPlayerId, 5);
            }
            else Debug.LogError("player score manager or net manager is null");

            OnCarPassed?.Invoke();
        }
    }
}