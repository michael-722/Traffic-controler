using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public enum TrafficLightState
{
    Red,
    Yellow,
    Green
}

public class TrafficLightController : MonoBehaviour
{
    [SerializeField] private float lightChangeDelay = 0.75f;
    [SerializeField] private float intensity = 30.0f;

    private TrafficLightState currentState = TrafficLightState.Red;
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

        UpdateTrafficLight();

        changeLightBtn.onClick.AddListener(OnTrafficLightClicked);
    }

    public void OnTrafficLightClicked()
    {
        if (!isChanging)
        {
            StartCoroutine(ChangeColor());
        }
    }

    private IEnumerator ChangeColor()
    {
        isChanging = true;
        
        //switch light states
        if (currentState == TrafficLightState.Red)
        {
            yield return ChangeState(TrafficLightState.Yellow);
            yield return ChangeState(TrafficLightState.Green);
        }
        else if (currentState == TrafficLightState.Green)
        {
            yield return ChangeState(TrafficLightState.Yellow);
            yield return ChangeState(TrafficLightState.Red);
        }

        isChanging = false;
    }

    private IEnumerator ChangeState(TrafficLightState nextState)
    {
        currentState = nextState;
        yield return new WaitForSeconds(lightChangeDelay);

        UpdateTrafficLight();
    }

    private void UpdateTrafficLight()
    {
        //Debug.Log("Traffic light state: " + currentState);

        switch (currentState)
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
}