using UnityEngine;

public class Pulse : MonoBehaviour
{
    [SerializeField] private float pulseInterval = 5f;
    [SerializeField] private float pulseStrength = 0.2f;
    [SerializeField] private float pulseRot = 2.5f;

    void Update()
    {
        float time = Time.time;
        float sinValue = Mathf.Sin((time / pulseInterval) * Mathf.PI * 2);//rads

        //scale
        float scale = 1 + Mathf.Abs(sinValue) * pulseStrength;
        transform.localScale = new Vector3(scale, scale, scale);

        //rot
        float rotation = sinValue * pulseRot;
        transform.rotation = Quaternion.Euler(0, 0, rotation);
    }
}