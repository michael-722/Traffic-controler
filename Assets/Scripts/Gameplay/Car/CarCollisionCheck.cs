using UnityEngine;
using System.Collections;

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
            //stop car  movement
            carController.setHasCrashed(true);

            //score penalty
            int quad = PlayerSpawner.instance.GetQuadrantFromPosition(transform.position);
            ulong onwerId = PlayerSpawner.instance.GetPlayerInQuadrant(quad);
            PlayerScoreManager.Instance.DecreaseScoreServerRpc(onwerId, 1);

            //despawn
            StartCoroutine(Despawn());
        }
    }

    private IEnumerator Despawn()
    {
        yield return new WaitForSecondsRealtime(despawnTimer);

        ulong id = CarSpawner.Instance.GetCarId(gameObject);
        //Debug.Log("despawning car with ID: " + id);
        
        CarSpawner.Instance.DespawnCarServerRpc(id);
    }
}