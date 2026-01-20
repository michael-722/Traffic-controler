using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private GameObject tutorialPanel;

    [SerializeField] private GameObject carPrefab;
    [SerializeField] private GameObject interactPrefab;
    [SerializeField] private TrafficLightController targetLight;

    [SerializeField] private Transform initalCarSpawnPoint;
    [SerializeField] private List<Transform> carSpawnPoints;

    [SerializeField] private GameObject TutorialLayout;
    [SerializeField] private float spawnInterval = 7f;

    private GameObject activeRing;
    private List<GameObject> spawnedCars = new List<GameObject>();
    private bool carSpawnEnabled = false;
    private Coroutine carSpawnRoutine;

    /*
    tutorial needs to do the following:

    - show cera movement and player area 
    - show that cars spawn and will approach traffic lights then wait 
    - show that player needs to interact with traffic lights to let cars through
    - show that cars will get impatient and leave if not let through in time
    - show that crashes deduct points
    - show that letting cars through gives points
    */

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        enableCarSpawning();
    }

    public void StartTutorial()
    {
        tutorialPanel.SetActive(true);
        disableCarSpawning();
        StartCoroutine(TutorialRoutine());
    }

    public void DespawnTutorialCars()
    {
        despawnCars();
    }
    
    public void SetTutorialCarSpawning(bool state) 
    {
        if (state) enableCarSpawning();
        else disableCarSpawning();
    }

    public void SetTutorialVisibilty(bool state)
    {
        TutorialLayout.SetActive(state);
    }

    public void ShowTutorial()
    {
        TutorialLayout.SetActive(true);
        enableCarSpawning();
    }

    private IEnumerator TutorialRoutine()
    {
        yield return null;
        
        //move camera
        yield return RunStep("Drag and click to move your camera around.", () => (Input.GetMouseButton(0) && Input.GetAxis("Mouse X") != 0 && Input.GetAxis("Mouse Y") != 0));

        Instantiate(carPrefab, initalCarSpawnPoint);

        //zoom camera
        yield return RunStep("Use the mouse wheel to zoom in and out.", () => (Input.mouseScrollDelta.y != 0));

        //interact with traffic light
        ShowRing(targetLight.transform.position + Vector3.up * 12f + Vector3.forward * -3.5f);
        bool lightChanged = false;
        Action lightHandler = () => lightChanged = true;
        targetLight.OnLightChanged += lightHandler;

        yield return RunStep("Click the highlighted traffic light to change its state.", () => lightChanged);

        enableCarSpawning();

        targetLight.OnLightChanged -= lightHandler;
        HideRing();

        //wait for car to spawn and pass through junction
        bool carPassed = false;
        Action passHandler = () => carPassed = true;
        CarDespawnTrigger.OnCarPassed += passHandler;

        yield return RunStep("Wait for a car to safely pass through and exit the level to earn points.", () => carPassed);

        CarDespawnTrigger.OnCarPassed -= passHandler;

        //penalty for waiting too long
        yield return RunStep("If cars wait too long, they get impatient and you lose points. Press SPACE to continue.", () => Input.GetKeyDown(KeyCode.Space));

        //crash penalty
        yield return RunStep("Allowing two cars to colliding will result in a score penalty! Press SPACE to continue.", () => Input.GetKeyDown(KeyCode.Space));

        //wait for player to exit
        yield return RunStep("Tutorial Complete! You're ready to manage the city. Press SPACE to leave tutorial", () => Input.GetKeyDown(KeyCode.Space));

        StopTutorial();
    }

    private void StopTutorial()
    {
        disableCarSpawning();
        HideRing();
        tutorialPanel.SetActive(false);
        GameManager.Instance.SetState(GameState.Lobby);
        CameraManager.Instance.ResetCameraPosition();
    }

    private void enableCarSpawning()
    {
        carSpawnEnabled = true;
        if (carSpawnRoutine == null)
        {
            carSpawnRoutine = StartCoroutine(CarSpawnLoop());
        }
    }

    private void disableCarSpawning()
    {
        if (carSpawnRoutine != null)
        {
            StopCoroutine(carSpawnRoutine);
            carSpawnRoutine = null;
        }
        carSpawnEnabled = false;
    }

    private IEnumerator CarSpawnLoop()
    {
        while (carSpawnEnabled)
        {
            int index = UnityEngine.Random.Range(0, carSpawnPoints.Count);
            GameObject car = Instantiate(carPrefab, carSpawnPoints[index].position, carSpawnPoints[index].rotation);
            
            if (car.TryGetComponent<NetworkObject>(out var netObj))
                netObj.CheckObjectVisibility = (id) => false;//disable network fucntionailty for tutorial cars
            spawnedCars.Add(car);

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void despawnCars()
    {
        for (int i = spawnedCars.Count - 1; i >= 0; i--)
        {
            if (spawnedCars[i] != null)
            {
                spawnedCars[i].GetComponent<CarController>().DestroyRearTriggerInstance();
                DestroyImmediate(spawnedCars[i]);
            }
            spawnedCars.RemoveAt(i);
        }
    }

    //display text and wait for condition to be met
    //pass in lambda for condition
    private IEnumerator RunStep(string message, Func<bool> completionCondition)
    {
        instructionText.text = message;

        while (!completionCondition())
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);
    }

    private void ShowRing(Vector3 position)
    {
        if (activeRing != null) Destroy(activeRing);
        activeRing = Instantiate(interactPrefab, position, Quaternion.identity);
    }

    private void HideRing()
    {
        if (activeRing != null) Destroy(activeRing);
    }
}