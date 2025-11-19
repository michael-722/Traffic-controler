using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Unity.Netcode;

public enum TrafficLightState
{
    Red,
    Yellow,
    Green
}

public class TrafficLightController : NetworkBehaviour
{
    [SerializeField] private float lightChangeDelay = 0.75f;
    [SerializeField] private float intensity = 30.0f;

    private NetworkVariable<TrafficLightState> currentState = new NetworkVariable<TrafficLightState>(
        TrafficLightState.Red,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server);

    private bool isChanging = false;

    [SerializeField] private Material greenLightMat;
    [SerializeField] private Material yellowLightMat;
    [SerializeField] private Material redLightMat;

    private GameObject redLight;
    private GameObject yellowLight;
    private GameObject greenLight;

    private Color redEmission;
    private Color yellowEmission;
    private Color greenEmission;

    [SerializeField] private Button changeLightBtn;

    private void Start()
    {
        redLight = transform.Find("RedLight").gameObject;
        yellowLight = transform.Find("YellowLight").gameObject;
        greenLight = transform.Find("GreenLight").gameObject;

        redEmission = redLightMat.GetColor("_EmissionColor");
        yellowEmission = yellowLightMat.GetColor("_EmissionColor");
        greenEmission = greenLightMat.GetColor("_EmissionColor");

        UpdateTrafficLightVisual();

        currentState.OnValueChanged += OnTrafficLightStateChanged;

        changeLightBtn.onClick.AddListener(OnTrafficLightClicked);
    }

    public void OnTrafficLightClicked()
    {
        if (!IsOwner || isChanging) return;//only owner can change

        RequestChangeServerRpc();
    }

    //client calls to change light
    [ServerRpc(RequireOwnership = false)]
    private void RequestChangeServerRpc()
    {

        if (!isChanging) StartCoroutine(ChangeColor());
    }

    private IEnumerator ChangeColor()
    {
        isChanging = true;

        // switch light states
        if (currentState.Value == TrafficLightState.Red)
        {
            yield return ChangeState(TrafficLightState.Yellow);
            yield return ChangeState(TrafficLightState.Green);
        }
        else if (currentState.Value == TrafficLightState.Green)
        {
            yield return ChangeState(TrafficLightState.Yellow);
            yield return ChangeState(TrafficLightState.Red);
        }

        isChanging = false;
    }

    private IEnumerator ChangeState(TrafficLightState nextState)
    {
        if (IsServer) currentState.Value = nextState;

        yield return new WaitForSeconds(lightChangeDelay);
    }

    //update clients
    private void OnTrafficLightStateChanged(TrafficLightState oldState, TrafficLightState newState)
    {
        UpdateTrafficLightVisual();
    }

    private void UpdateTrafficLightVisual()
    {
        //Debug.Log("Traffic light state: " + currentState.Value);

        switch (currentState.Value)
        {
            case TrafficLightState.Red:
                //Debug.Log("red");
                SetEmission(redLight, true);
                SetEmission(yellowLight, false);
                SetEmission(greenLight, false);
                break;
            case TrafficLightState.Yellow:
                //Debug.Log("yellow");
                SetEmission(redLight, false);
                SetEmission(yellowLight, true);
                SetEmission(greenLight, false);
                break;
            case TrafficLightState.Green:
                //Debug.Log("green");
                SetEmission(redLight, false);
                SetEmission(yellowLight, false);
                SetEmission(greenLight, true);
                break;
        }
    }

    private void SetEmission(GameObject lightObject, bool enable)
    {
        var renderer = lightObject.GetComponent<Renderer>();
        Color emissionColor = Color.black;

        //get intensity/color per light
        if (renderer == redLight.GetComponent<Renderer>()) emissionColor = redEmission;
        else if (renderer == yellowLight.GetComponent<Renderer>()) emissionColor = yellowEmission;
        else if (renderer == greenLight.GetComponent<Renderer>()) emissionColor = greenEmission;

        //set emission
        if (enable)
        {
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", emissionColor * intensity);
        }
        else
        {
            renderer.material.DisableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", Color.black);
        }
    }

    public TrafficLightState GetCurrentState()
    {
        return currentState.Value;
    }
}