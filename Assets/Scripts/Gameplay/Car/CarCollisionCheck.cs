using UnityEngine;
using System.Collections;
using UnityEngine.VFX;

public class CarCollisionCheck : MonoBehaviour
{
    [SerializeField] private float despawnTimer = 3f;
    [SerializeField] private GameObject crashFX;

    private CarController carController;
    private GameObject crashFXInstance;

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

            //crash fx
            Vector3 crashPosition = collision.contacts[0].point;
            crashPosition.y += 1f;
            

            crashFXInstance = Instantiate(this.crashFX);
            crashFXInstance.transform.position = crashPosition;

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

        Destroy(crashFXInstance);

        ulong id = CarSpawner.Instance.GetCarId(gameObject);
        //Debug.Log("despawning car with ID: " + id);
        
        CarSpawner.Instance.DespawnCarServerRpc(id);
    }
}