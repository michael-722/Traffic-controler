using UnityEngine;
using System.Collections;
using System;
using Unity.Netcode;

public class CarCollisionCheck : MonoBehaviour
{
    public static event Action OnCarCrashed;

    [SerializeField] private float despawnTimer = 3f;
    [SerializeField] private GameObject crashFX;

    private CarController carController;
    private GameObject crashFXInstance;

    private int crashSideIndex = 0;

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

            //spawn crash fx
            if (gameObject.GetInstanceID() > collision.gameObject.GetInstanceID())
                CrashFX(collision.contacts[0].point);

            //score penalty
            UpdateScore();

            OnCarCrashed?.Invoke();

            //despawn
            StartCoroutine(Despawn());
        }
    }

    private void CrashFX(Vector3 collision)
    {
        Vector3 contactPoint = transform.InverseTransformPoint(collision);
        Vector3 dir = (collision - transform.position).normalized;

        float forwardDot = Vector3.Dot(transform.forward, dir);
        float rightDot = Vector3.Dot(transform.right, dir);

        //0 - front 1 - right 2 - back 3 - left
        if (Mathf.Abs(forwardDot) > Mathf.Abs(rightDot))
            crashSideIndex = forwardDot > 0.5f ? 0 : 2;
        else crashSideIndex = rightDot > 0.5f ? 1 : 3;

        Vector3 offsetFX = new Vector3(0, 2f, 0);

        //crash fx
        switch (crashSideIndex)
        {
            case 0:
                crashFXInstance = Instantiate(crashFX, (transform.position + transform.forward * 2f) + offsetFX, Quaternion.Euler(0, transform.eulerAngles.y, 0));
                break;
            case 1:
                crashFXInstance = Instantiate(crashFX, (transform.position + transform.right * 2f) + offsetFX, Quaternion.Euler(0, transform.eulerAngles.y + 90f, 0));
                break;
            case 2:
                crashFXInstance = Instantiate(crashFX, (transform.position - transform.forward * 2f) + offsetFX, Quaternion.Euler(0, transform.eulerAngles.y + 180f, 0));
                break;
            case 3:
                crashFXInstance = Instantiate(crashFX, (transform.position - transform.right * 2f) + offsetFX, Quaternion.Euler(0, transform.eulerAngles.y - 90f, 0));
                break;
        }
    }

    private void UpdateScore()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
            return;

        int quad = PlayerSpawner.instance.GetQuadrantFromPosition(transform.position);
        ulong onwerId = PlayerSpawner.instance.GetPlayerInQuadrant(quad);

        PlayerManager.Instance.DecreaseScoreServerRpc(onwerId, 1);

        if (NetworkManager.Singleton.IsServer)
            StatTracker.Instance.IncreaseTotalCrashesServerRpc(onwerId);
    }

    private IEnumerator Despawn()
    {
        yield return new WaitForSecondsRealtime(despawnTimer);

        Destroy(crashFXInstance);

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
            Destroy(gameObject);
        else
        {
            ulong id = CarSpawner.Instance.GetCarId(gameObject);
            //Debug.Log("despawning car with ID: " + id);

            CarSpawner.Instance.DespawnCarServerRpc(id);
        }
    }
}