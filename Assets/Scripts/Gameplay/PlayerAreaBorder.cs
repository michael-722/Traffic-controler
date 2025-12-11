using System.ComponentModel;
using Unity.Netcode;
using UnityEngine;

public class PlayerAreaBorder : NetworkBehaviour
{
    [Range(0, 1)][SerializeField] private float minHeight = 0f;
    [Range(0, 1)][SerializeField] private float maxHeight = 0f;

    [SerializeField] private float minHexSize = 0.9f;
    [SerializeField] private float maxHexSize = 1f;

    [SerializeField] private float maxDist = 90f;
    [SerializeField] private float minDist = 0f;

    private Material mat;
    private CameraManager cm;

    private void Start()
    {
        mat = GetComponent<MeshRenderer>().material;
        mat.SetFloat("_height", 0f);

        cm = CameraManager.Instance;
    }

    void Update()
    {
        if (!IsOwner) return;
        float dist = cm.GetDistanceFromCenter();

        float h = Mathf.InverseLerp(minDist, maxDist, dist);
        h = Mathf.Clamp(h, minHeight, maxHeight);
        mat.SetFloat("_height", h);

        float hexSin = Mathf.Lerp(minHexSize, maxHexSize, (Mathf.Sin(Time.time) + 1f) / 2f);
        mat.SetFloat("_hexSize", hexSin);
    }
}