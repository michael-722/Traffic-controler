using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class CarController : NetworkBehaviour
{
    #region VARS
    [SerializeField] private float driveSpeed = 7.5f;
    [SerializeField] private float turnSpeed = 2.0f;

    [SerializeField] private BlinkerController leftBlinkers;
    [SerializeField] private BlinkerController rightBlinkers;

    [SerializeField] private Transform roadTransform;

    private float ROAD_WIDTH;
    private float carWidth;
    private float carLength;

    private Rigidbody rb;
    private bool waitingAtLight = false;
    private bool isMoving = false;

    private TrafficLightController currentLight;
    private JunctionController currentJunction;
    private JunctionDirection entryDirection;

    private NetworkVariable<JunctionDirection> chosenDirection = new NetworkVariable<JunctionDirection>(
    JunctionDirection.Up,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);

    private GameObject rearTriggerInstance = null;
    private bool rearTriggerActive = false;

    private bool directionChosen = false;
    private bool isQueued = false;

    private bool hasCrashed = false;
    #endregion

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        carWidth = GetCarWidth();
        carLength = GetCarLenght();
        ROAD_WIDTH = 14f;

        //=====================================================================
        //only have enabled if roads need re calabrating
        //road in x dir

        //ROAD_WIDTH = GetRoadWidth(roadTransform);
        //Debug.Log($"{name} car width: {carWidth} road width: {roadWidth}");
        //=====================================================================

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

    private float GetCarLenght()
    {
        Collider col = GetComponentInChildren<Collider>();
        if (col != null) return col.bounds.size.z;

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
        if (hasCrashed) return;
        if (!waitingAtLight && !isMoving)
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
         
        Vector3 triggerCenter = trigger.bounds.center;
        Vector3 carPosition = transform.position;

        //entry axis
        switch (entryDirection)
        {
            case JunctionDirection.Up:
            case JunctionDirection.Down:
                carPosition.x = triggerCenter.x;
                break;
            case JunctionDirection.Left:
            case JunctionDirection.Right:
                carPosition.z = triggerCenter.z;
                break;
        }
        rb.MovePosition(carPosition);    

        rearTriggerInstance = trigger.gameObject;
        entryDirection = GetDirectionFromTrigger(trigger.name);
        isQueued = trigger.name.Contains("(Clone)");

        
        Transform lightsParent = trigger.transform.parent.parent;//get junction obj
        TrafficLightController[] lights = lightsParent.GetComponentsInChildren<TrafficLightController>();

        currentLight = null;

        //find entry dir light
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
            
            //Debug.LogWarning($"{name} no traffic light found for junction at {trigger.transform.parent.parent.name} from dir {entryDirection}");
            return;
        }

        DetermineNextDirection();
        StartCoroutine(HandleTrafficLight());
    }

    private IEnumerator HandleTrafficLight()
    {
        waitingAtLight = true;

        //wait at light
        while (waitingAtLight)
        {
            if (hasCrashed) break;
            TrafficLightState lightState = currentLight.GetCurrentState();

            //wait when red/yellow
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
            //go/move up when green
            else if (lightState == TrafficLightState.Green)
            {
                if (rearTriggerActive && rearTriggerInstance != null)
                {
                    Destroy(rearTriggerInstance);
                    rearTriggerActive = false;
                }

                //not queued
                if (!isQueued)
                {
                    waitingAtLight = false;
                    break;
                }
                //queued
                else
                {
                    waitingAtLight = false;
                    yield break;
                }
            }

            yield return null;
        }

        StartCoroutine(MoveThroughJunction());
    }


    private IEnumerator MoveThroughJunction()
    {
        isMoving = true;

        JunctionDirection targetDir = chosenDirection.Value;

        //Debug.Log($"{name} entryDir={entryDirection} chosen={chosenDirection.Value} targetDir={targetDir}");

        //move forward to turn point
        float turnDist = GetTurnDistance(entryDirection, targetDir);
        yield return StartCoroutine(MoveForward(turnDist, driveSpeed));

        //turn setup
        float turnAngle = GetTurnAngle(entryDirection, targetDir);
        Quaternion targetRot = Quaternion.Euler(0f, transform.eulerAngles.y + turnAngle, 0f);

        //update entry dir
        entryDirection = targetDir;

        //turning
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
        
        //reset states
        rightBlinkers.setBlinkers(false);
        leftBlinkers.setBlinkers(false);
        directionChosen = false;
        isMoving = false;
    }

    private IEnumerator MoveForward(float distance, float speed)
    {
        isMoving = true;

        Vector3 startPosition = rb.position;
        Vector3 targetPosition = startPosition + transform.forward * distance;

        //move forward
        while (Vector3.Distance(rb.position, targetPosition) > 0.05f)
        {
            Vector3 newPosition = Vector3.MoveTowards(rb.position, targetPosition, speed * Time.deltaTime);
            rb.MovePosition(newPosition);

            yield return null;
        }

        rb.MovePosition(targetPosition);
        isMoving = false;
    }

    private void DetermineNextDirection()
    {
        if (directionChosen) return;//already chosen dir
        directionChosen = true;

        //vaild dirs
        List<JunctionDirection> validDirs = new List<JunctionDirection>(currentJunction.junctionMap.Keys);
        validDirs.Remove(entryDirection);

        //choose random dir
        if (IsServer)
        {
            JunctionDirection newDir = validDirs[Random.Range(0, validDirs.Count)];
            chosenDirection.Value = newDir;
        }

        JunctionDirection selectedDir = chosenDirection.Value;

        //set blinkers
        float turnAngle = GetTurnAngle(entryDirection, chosenDirection.Value);
        if (turnAngle == -90f)//right
        {
            leftBlinkers.setBlinkers(false);
            rightBlinkers.setBlinkers(true);
        }
        else if (turnAngle == 90f)//left
        {
            leftBlinkers.setBlinkers(true);
            rightBlinkers.setBlinkers(false);
        }
        else//straight
        {
            leftBlinkers.setBlinkers(false);
            rightBlinkers.setBlinkers(false);
        }
    }

    private float GetTurnDistance(JunctionDirection entryDir, JunctionDirection newDir)
    {
        //Debug.Log($"{name} turn from {entryDir} to {newDir}");  
        int turnAngle = (int)GetTurnAngle(entryDir, newDir);

        //left turn
        if (turnAngle == -90) return ROAD_WIDTH / 2f;
        //right turn
        else if (turnAngle == 90) return ROAD_WIDTH;
        //straight
        else return 0f;
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
                if (target == JunctionDirection.Right) return -90f;//left
                if (target == JunctionDirection.Left) return 90f;//right
                break;

            case JunctionDirection.Down:
                if (target == JunctionDirection.Left) return -90f;//left
                if (target == JunctionDirection.Right) return 90f;//right
                break;

            case JunctionDirection.Left:
                if (target == JunctionDirection.Up) return -90f;//left
                if (target == JunctionDirection.Down) return 90f;//right
                break;

            case JunctionDirection.Right://checked
                if (target == JunctionDirection.Down) return -90f;//left
                if (target == JunctionDirection.Up) return 90f;//right
                break;
        }
        return 0f;//straight
    }

    public void setHasCrashed(bool state)
    {
        hasCrashed = state;
        rb.angularVelocity = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
    }
}