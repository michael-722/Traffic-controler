using Unity.Netcode;
using UnityEngine;

public class CarDespawnTrigger : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            CarSpawner.Instance.DespawnCar(other.gameObject.GetComponent<NetworkObject>());
            //socre
            //update car spawner
        }
    }
}
