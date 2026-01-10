using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;
    /*
 tutorial needs to do the following:

- show cera movement and player area 
- show that cars spawn and will approach traffic lights then wait 
- show that player needs to interact with traffic lights to let cars through
- show that cars will get impatient and leave if not let through in time
- show that crashes deduct points
- show that letting cars through gives points
 */

    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private GameObject tutorialPanel;

    [SerializeField] private GameObject carPrefab;
    [SerializeField] private GameObject interactPrefab;
    [SerializeField] private TrafficLightController targetLight;

    [SerializeField] private Transform initalCarSpawnPoint;
    [SerializeField] private List<Transform> carSpawnPoints;

    private GameObject activeRing;

    private void Start()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void StartTutorial()
    {
        tutorialPanel.SetActive(true);
        StartCoroutine(TutorialRoutine());
    }

    public void StopTutorial()
    {
        disableCarSpawning();
        HideRing();
    }

    private IEnumerator TutorialRoutine()
    {
        yield return null;
        
        //<color=red></color>
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
        yield return RunStep("Two cars colliding will cause a major score penalty! Press SPACE to finish.", () => Input.GetKeyDown(KeyCode.Space));

        instructionText.text = "Tutorial Complete! You're ready to manage the city.";
        yield return new WaitForSeconds(3f);
        tutorialPanel.SetActive(false);
    }

    private Coroutine carSpawnRoutine;

    private void enableCarSpawning()
    {
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
    }

    private IEnumerator CarSpawnLoop()
    {
        float spawnInterval = 7f;
        while (tutorialPanel.activeSelf)
        {
            int index = UnityEngine.Random.Range(0, carSpawnPoints.Count);
            Instantiate(carPrefab, carSpawnPoints[index].position, carSpawnPoints[index].rotation);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    //display text and wait
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