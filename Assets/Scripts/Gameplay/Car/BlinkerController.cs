using UnityEngine;
using Unity.Netcode;

public class BlinkerController : NetworkBehaviour
{
    [SerializeField] private float intensity = 7.0f;
    [SerializeField] private Color blinkerColor = new Color(1.0f, 0.529f, 0.0f);
    [SerializeField] private float flashInterval = 0.5f;
    private float timer = 0.0f;
    private bool isLightOn = false;

    private NetworkVariable<bool> blinkersOn = new NetworkVariable<bool>(false, 
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private void Awake()
    {
        blinkersOn.OnValueChanged += OnBlinkersChanged;
    }

    public override void OnDestroy()
    {
        blinkersOn.OnValueChanged -= OnBlinkersChanged;
        base.OnDestroy();
    }

    private void OnBlinkersChanged(bool oldValue, bool newValue)
    {
        if (!newValue)
        {
            flashBlinkers(false);
            isLightOn = false;
            timer = 0f;
        }
    }

    public void setBlinkers(bool state)
    {
        if (IsServer)
        {
            blinkersOn.Value = state;
        }
    }

    private void Update()
    {
        if (!blinkersOn.Value) return;

        timer += Time.deltaTime;
        if (isLightOn)
        {
            if (timer >= flashInterval)
            {
                flashBlinkers(false);
                isLightOn = false;
                timer = 0f;
            }
        }
        else
        {
            if (timer >= flashInterval * 0.5f)
            {
                flashBlinkers(true);
                isLightOn = true;
                timer = 0f;
            }
        }
    }

    private void flashBlinkers(bool state)
    {
        if (state)
        {
            foreach (Transform child in this.gameObject.transform)
            {
                Renderer renderer = child.GetComponent<Renderer>();
                renderer.material.SetColor("_EmissionColor", blinkerColor * intensity);
            }
        }
        else
        {
            foreach (Transform child in this.gameObject.transform)
            {
                Renderer renderer = child.GetComponent<Renderer>();
                renderer.material.SetColor("_EmissionColor", Color.black);
            }
        }
    }
}