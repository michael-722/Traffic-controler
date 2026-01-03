using UnityEditor;
using UnityEngine;

//[ExecuteAlways]//needed for editor
public class LookAtCam : MonoBehaviour
{
    private GameObject mainCam;

    void Start()
    {
        mainCam = FindFirstObjectByType<Camera>().gameObject;
    }

    void Update()
    {
        transform.rotation = Quaternion.LookRotation(transform.position - mainCam.transform.position);
#if UNITY_EDITOR
        if (!Application.isPlaying)
            transform.rotation = Quaternion.LookRotation(transform.position - SceneView.GetAllSceneCameras()[0].transform.position);
#endif
    }
}