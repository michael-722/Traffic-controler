using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class CarController : NetworkBehaviour
{
    [SerializeField] private float driveSpeed = 7.5f;
    [SerializeField] private float turnSpeed = 2.0f;

    private float roadWidth;
    private float carWidth;

    private Rigidbody rb;
    private bool waitingAtLight = false;
    private bool isMovingInQueue = false;
    private bool isAdvancingToTrigger = false;

    private TrafficLightController currentLight;
    private JunctionController currentJunction;
    private JunctionDirection entryDirection;

    [SerializeField] private Transform roadTransform;

    private NetworkVariable<JunctionDirection> chosenDirection = new NetworkVariable<JunctionDirection>(
    JunctionDirection.Up,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);

    [SerializeField] private BlinkerController leftBlinkers;
    [SerializeField] private BlinkerController rightBlinkers;

    private GameObject rearTriggerInstance = null;
    private bool rearTriggerActive = false;

    private bool directionChosen = false;
    private bool isQueued = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        carWidth = GetCarWidth();
        roadWidth = 14f;

        //only have enabled if roads need re calarating
        //road in x dir
        //roadWidth = GetRoadWidth(roadTransform);
        //Debug.Log($"{name} car width: {carWidth} road width: {roadWidth}");

        leftBlinkers.setBlinkers(false);
        rightBlinkers.setBlinkers(false);
    }

    private float GetCarWidth()
    {
        Collider col = GetComponentInChildren<Collider>();
        if (col != null) return col.bounds.size.x;

        Debug.LogWarning(name + " car has no collider");
        return 0f;
    }

    private float GetRoadWidth(Transform roadTransform)
    {
        Collider col = roadTransform.GetComponent<Collider>();
        if (col != null) return col.bounds.size.x;

        Debug.LogWarning("get road width failed");
        return 0f;
    }

    private void FixedUpdate()
    {
        if (!waitingAtLight && !isMovingInQueue)
        {
            rb.MovePosition(transform.position + transform.forward * driveSpeed * Time.fixedDeltaTime);
        }
    }

    private void OnTriggerEnter(Collider trigger)
    {

        if (!IsServer) return;

        //check if is junction trigger
        if (!trigger.transform.name.Contains("Trigger")) return;

        currentJunction = trigger.gameObject.GetComponentInParent<JunctionController>();
        if (currentJunction == null)
        {
            Debug.LogError("junction controller not found on obj");
            return;
        }

        if (rearTriggerInstance != null && trigger.gameObject == rearTriggerInstance) return;

        //need to add check if colliding with vaild trigger re adjust pos to correct pos bsed of trigger - since then itll always reset the cars pos so it doesnt drift 
        //doesnt drift much rn but it does a bit so need to look into==================================================================================================
        /*if (gameObject.transform.rotation.eulerAngles.y == 0 || gameObject.transform.rotation.eulerAngles.y == 180)//180 + 0 same case -90 + 90 same case for alligining
        {
            
            //x
        }*/
        //z
        

        rearTriggerInstance = trigger.gameObject;
        entryDirection = GetDirectionFromTrigger(trigger.name);
        isQueued = trigger.name.Contains("(Clone)");

        //find traffic light
        Transform lightsParent = trigger.transform.parent.parent;//get junction obj
        TrafficLightController[] lights = lightsParent.GetComponentsInChildren<TrafficLightController>();

        currentLight = null;

        foreach (TrafficLightController light in lights)
        {
            Vector3 lightForward = light.transform.forward;

            //flipped bc lights are roted 180
            Vector3 expectedLightDir = -GetWorldDirection(entryDirection);

            float dot = Vector3.Dot(lightForward.normalized, expectedLightDir.normalized);

            /* traffic lights are rotated 180 - if fixed must chage dot to -dot
             * * dot product for 180 lights
             * dot 1 = same direction
             * dot -1 = opposite direction
             * dot 0 = perpendicular
             * */
            if (dot > 0.8f)
            {
                currentLight = light;
                break;
            }
        }

        if (currentLight == null)
        {
            waitingAtLight = false;
            //null bc triggers on roads where cars spawn need to do more work S
            //dynamically remove triggers when no lights? -> handel in junction controller=============================================== 

            //Debug.LogWarning($"{name} no traffic light found for junction at {trigger.transform.parent.parent.name} from dir {entryDirection}");
            return;
        }

        if (isAdvancingToTrigger) return;

        if (!directionChosen) DetermineNextDirection();

        StartCoroutine(HandleTrafficLight());
    }

    private void DetermineNextDirection()
    {
        if (directionChosen) return;//already chosen dir
        directionChosen = true;

        List<JunctionDirection> validDirs = new List<JunctionDirection>(currentJunction.junctionMap.Keys);
        validDirs.Remove(entryDirection);

        if (IsServer)
        {
            JunctionDirection newDir = validDirs[Random.Range(0, validDirs.Count)];
            chosenDirection.Value = newDir;
        }

        JunctionDirection selectedDir = chosenDirection.Value;

        int turnAngle = (int)GetTurnAngle(entryDirection, selectedDir);
        switch (turnAngle)
        {
            case 90:
                //right indicator
                rightBlinkers.setBlinkers(true);
                break;
            case -90:
                //left indicator
                leftBlinkers.setBlinkers(true);
                break;
            case 0:
                //no indicator
                leftBlinkers.setBlinkers(false);
                rightBlinkers.setBlinkers(false);
                break;
            default:
                //no indicator
                leftBlinkers.setBlinkers(false);
                rightBlinkers.setBlinkers(false);
                break;
        }
    }

    private IEnumerator HandleTrafficLight()
    {
        waitingAtLight = true;

        //keep checking traffic light state
        while (waitingAtLight)
        {
            TrafficLightState lightState = currentLight.GetCurrentState();
            //Debug.Log($"{name} sees {currentLight.name} as {lightState}");

            //wait
            if (lightState == TrafficLightState.Red || lightState == TrafficLightState.Yellow)
            {
                if (!rearTriggerActive)
                {
                    rearTriggerActive = true;

                    GameObject originalTrigger = rearTriggerInstance;
                    Transform originalParent = rearTriggerInstance.transform.parent;
                    Vector3 clonePosition = rearTriggerInstance.transform.position - transform.forward * 8f;

                    rearTriggerInstance = Instantiate(originalTrigger, clonePosition, originalTrigger.transform.rotation);
                    rearTriggerInstance.transform.parent = originalParent;
                }
                yield return null;
                continue;
            }
            else if (lightState == TrafficLightState.Green)
            {
                if (rearTriggerActive && rearTriggerInstance != null)
                {
                    Destroy(rearTriggerInstance);
                    rearTriggerActive = false;
                }

                if (isQueued)
                {
                    //in que - move forward to main trigger

                    isAdvancingToTrigger = true;//prevent trigger logic from firing
                    yield return StartCoroutine(MoveForward(8f, driveSpeed));
                    isAdvancingToTrigger = false;

                    continue;
                }

                //start of que
                waitingAtLight = false;
                break;
            }

            yield return null;
        }

        StartCoroutine(MoveThroughJunction());
    }

    private IEnumerator MoveThroughJunction()
    {
        isMovingInQueue = true;

        JunctionDirection targetDir = chosenDirection.Value;

        //Debug.Log($"{name} entryDir={entryDirection} chosen={chosenDirection.Value} targetDir={targetDir}");
        float turnDist = GetTurnDistance(entryDirection, targetDir);
        yield return StartCoroutine(MoveForward(turnDist, driveSpeed));

        float turnAngle = GetTurnAngle(entryDirection, targetDir);
        Quaternion targetRot = Quaternion.Euler(0f, transform.eulerAngles.y + turnAngle, 0f);

        entryDirection = targetDir;

        while (Quaternion.Angle(transform.rotation, targetRot) > 1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            rb.MoveRotation(transform.rotation);
            yield return null;
        }

        //sync fianl
        Vector3 euler = transform.eulerAngles;
        euler.y = Mathf.Round(euler.y / 90f) * 90f;
        transform.eulerAngles = euler;
        rb.MoveRotation(Quaternion.Euler(euler));

        directionChosen = false;
        isMovingInQueue = false;
    }

    private IEnumerator MoveForward(float distance, float speed)
    {
        isMovingInQueue = true;

        Vector3 startPosition = rb.position;
        Vector3 targetPosition = startPosition + transform.forward * distance;

        while (Vector3.Distance(rb.position, targetPosition) > 0.05f)
        {
            Vector3 newPosition = Vector3.MoveTowards(rb.position, targetPosition, speed * Time.deltaTime);
            rb.MovePosition(newPosition);

            yield return null;
        }

        rb.MovePosition(targetPosition);
        isMovingInQueue = false;
    }

    private float GetTurnDistance(JunctionDirection entryDir, JunctionDirection newDir)
    {
        float laneOffset = carWidth / 2f;//lane center 
        float turnRadius = roadWidth / 2f;//turning rad

        //Debug.Log("lane offset: " + laneOffset + " turn radius: " + turnRadius);
        //Debug.Log($"{name} turn from {entryDir} to {newDir}"); 

        if (newDir == entryDir) return 0f;//straight

        int turnAngle = (int)GetTurnAngle(entryDir, newDir);

        //left turn
        if (turnAngle == -90) return turnRadius;//narrow
        //right turn
        else if (turnAngle == 90)return roadWidth;//wide

        return 0f;
    }

    private JunctionDirection GetDirectionFromTrigger(string triggerName)
    {
        if (triggerName.Contains("Up")) return JunctionDirection.Up;
        if (triggerName.Contains("Down")) return JunctionDirection.Down;
        if (triggerName.Contains("Left")) return JunctionDirection.Left;
        return JunctionDirection.Right;
    }

    private Vector3 GetWorldDirection(JunctionDirection dir)
    {
        switch (dir)
        {
            case JunctionDirection.Up: return Vector3.forward;
            case JunctionDirection.Down: return Vector3.back;
            case JunctionDirection.Left: return Vector3.left;
            case JunctionDirection.Right: return Vector3.right;
            default: return Vector3.forward;
        }
    }

    private float GetTurnAngle(JunctionDirection entry, JunctionDirection target)
    {
        switch (entry)
        {
            case JunctionDirection.Up:
                switch (target)
                {
                    case JunctionDirection.Left: return 90f;
                    case JunctionDirection.Right: return -90f;
                    case JunctionDirection.Down: return 0f;
                }
                break;

            case JunctionDirection.Down:
                switch (target)
                {
                    case JunctionDirection.Left: return -90f;
                    case JunctionDirection.Right: return 90f;
                    case JunctionDirection.Up: return 0f;
                }
                break;

            case JunctionDirection.Left:
                switch (target)
                {
                    case JunctionDirection.Up: return -90f;
                    case JunctionDirection.Down: return 90f;
                    case JunctionDirection.Right: return 0f;
                }
                break;

            case JunctionDirection.Right:
                switch (target)
                {
                    case JunctionDirection.Up: return 90f;
                    case JunctionDirection.Down: return -90f;
                    case JunctionDirection.Left: return 0f;
                }
                break;
        }
        return 0f;
    }
}