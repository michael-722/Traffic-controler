using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class CarCollisionCheck : MonoBehaviour
{
    [SerializeField] private float despawnTimer = 3f;

    private CarController carController;

    void Start()
    {
        carController = gameObject.GetComponent<CarController>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Car")
        {
            carController.setHasCrashed(true);
            //subtract score -- need to identify in which player area it happened - player bounds check
            Despawn();
        }
    }

    private IEnumerator Despawn()
    {
        yield return new WaitForSecondsRealtime(despawnTimer);

        CarSpawner.Instance.DespawnCar(gameObject.GetComponent<NetworkObject>());
    }
}