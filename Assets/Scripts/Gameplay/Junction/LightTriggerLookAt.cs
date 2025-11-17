using UnityEngine;

public class LightTriggerLookAt : MonoBehaviour
{
    private GameObject mainCam;

    void Start()
    {
        mainCam = FindFirstObjectByType<Camera>().gameObject;
    }

    void Update()
    {
        transform.rotation = Quaternion.LookRotation(transform.position - mainCam.transform.position);//lookAt breaks it - use this instead
    }
}
