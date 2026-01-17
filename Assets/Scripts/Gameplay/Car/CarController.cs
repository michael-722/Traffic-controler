using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class CarController : NetworkBehaviour
{
    #region VARS
    [SerializeField] private float driveSpeed = 7.5f;
    [SerializeField] private float turnSpeed = 2.0f;
    [Range(2,15)][SerializeField] private float queDist = 2.0f;

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

    private GameObject originalTrigger;

    private bool directionChosen = false;
    private bool isQueued = false;

    private bool hasCrashed = false;

    private CarWaitUI carWaitUI;
    #endregion

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        carWidth = GetCarWidth();
        carLength = GetCarLenght();
        ROAD_WIDTH = 14f;

        Debug.Log($"car length: {carLength}");

        //=====================================================================
        //only have enabled if roads need re calabrating
        //road in x dir

        //ROAD_WIDTH = GetRoadWidth(roadTransform);
        //Debug.Log($"{name} car width: {carWidth} road width: {roadWidth}");
        //=====================================================================

        carWaitUI = gameObject.GetComponent<CarWaitUI>();

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
        if (col != null)
        {
            //get local size - account for rotation/scaling on spawn
            Vector3 localSize = col.transform.InverseTransformVector(col.bounds.size);
            Vector3 lossyScale = col.transform.lossyScale;
            float scaledLength = Mathf.Abs(localSize.z * lossyScale.z);
            return scaledLength;
        }

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
        if (!IsServer && IsSpawned) return;
        if (hasCrashed) return;

        //cars infront had a crash
        if (waitingAtLight && originalTrigger == null)
        {
            waitingAtLight = false;
            rearTriggerActive = false;
            Destroy(rearTriggerInstance);
            rearTriggerInstance = null;
            carWaitUI.SetState(false);
        }

        //normal driving
        if (!waitingAtLight && !isMoving)
            rb.MovePosition(transform.position + transform.forward * driveSpeed * Time.fixedDeltaTime);
    }

    //hit a junction trigger 
    private void OnTriggerEnter(Collider trigger)
    {
        if (!IsServer && IsSpawned) return;
        if (isMoving || waitingAtLight || hasCrashed) return;
        if (!trigger.transform.name.Contains("Trigger")) return;

        currentJunction = trigger.gameObject.GetComponentInParent<JunctionController>();
        if (currentJunction == null)
        {
            waitingAtLight = false;
            return;
        }

        if (rearTriggerInstance != null && trigger.gameObject == rearTriggerInstance)
        {
            waitingAtLight = false;
            return;
        }
        //inherit light trigger
        else rearTriggerInstance = trigger.gameObject;

        entryDirection = GetDirectionFromTrigger(trigger.name);
        isQueued = trigger.name.Contains("(Clone)");

        //trigger alignment check
        Vector3 triggerForward = -trigger.transform.forward;
        float alignment = Mathf.Abs(Vector3.Dot(triggerForward.normalized, transform.forward.normalized));
        if (alignment < 0.9f)
        {
            waitingAtLight = false;
            return;
        }
        

        //align car to lane
        Vector3 carPosition = transform.position;
        Vector3 triggerCenter = trigger.bounds.center;
        if (entryDirection == JunctionDirection.Up || entryDirection == JunctionDirection.Down)
            carPosition.x = triggerCenter.x;
        else
            carPosition.z = triggerCenter.z;
        rb.MovePosition(carPosition);

        //find entry dir light
        TrafficLightController[] lights = currentJunction.GetComponentsInChildren<TrafficLightController>();
        currentLight = null;
        //flipped bc lights are roted 180
        Vector3 expectedLightDir = -GetWorldDirection(entryDirection);

        bool foundLight = false;

        foreach (TrafficLightController light in lights)
        {
            float dot = Vector3.Dot(light.transform.forward.normalized, expectedLightDir.normalized);

            /* traffic lights are rotated 180 - if fixed must chage dot to -dot
            * * dot product for 180 lights
            * dot 1 = same direction
            * dot -1 = opposite direction
            * dot 0 = perpendicular
            * */
            if (dot > 0.9f)
            {
                foundLight = true;
                currentLight = light;
                break;
            }
        }

        //handle no traffic light
        if (currentLight == null)
        {
            NoTrafficLightTurning[] noTrafficTurnings = currentJunction.GetComponentsInChildren<NoTrafficLightTurning>();
            foreach (NoTrafficLightTurning turn in noTrafficTurnings)
            {
                float dot = Vector3.Dot(turn.transform.forward.normalized, expectedLightDir.normalized);
                if (dot > 0.5f)
                {
                    waitingAtLight = false;
                    DetermineNextDirection();
                    StartCoroutine(MoveThroughJunction());
                    return;
                }
            }

            //no light/turn found
            waitingAtLight = false;
            //Debug.LogWarning($"[CarController] {name} could not find light/turn for direction: {entryDirection}");
            return;
        }

        if (!foundLight) return;

        DetermineNextDirection();

        if (!rearTriggerActive)
        {
            rearTriggerActive = true;
            originalTrigger = rearTriggerInstance;

            float offsetDistance = carLength + queDist;
            Quaternion cloneRotation = originalTrigger.transform.rotation;
            Vector3 clonePosition = originalTrigger.transform.position - transform.forward * offsetDistance;

            GameObject newTrigger = Instantiate(originalTrigger, clonePosition, cloneRotation);
            newTrigger.transform.SetParent(originalTrigger.transform.parent, worldPositionStays: false);

            rearTriggerInstance = newTrigger;
        }

        StartCoroutine(HandleTrafficLight());

        foundLight = false;
    }

    private IEnumerator HandleTrafficLight()
    {
        waitingAtLight = true;
        carWaitUI.SetState(true);

        bool firstCheck = true;

        //wait at light
        while (waitingAtLight)
        {
            if (hasCrashed) break;
            TrafficLightState lightState = currentLight.GetCurrentState();
            //Debug.Log($"{name} light state: {lightState} queued: {isQueued}");

            //wait when red/yellow
            if (lightState == TrafficLightState.Red || lightState == TrafficLightState.Yellow)
            {
                yield return null;
                firstCheck = false;
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
                    if (firstCheck)
                        //register even if no wait
                        StatTracker.Instance.AddWaitTimeServerRpc(OwnerClientId, 0f);
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
            firstCheck = false;
        }

        carWaitUI.SetState(false);
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
            if (hasCrashed) break;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            rb.MoveRotation(transform.rotation);
            yield return null;
        }

        if (!hasCrashed)
        {
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
    }

    private IEnumerator MoveForward(float distance, float speed)
    {
        isMoving = true;

        Vector3 startPosition = rb.position;
        Vector3 targetPosition = startPosition + transform.forward * distance;

        //move forward
        while (Vector3.Distance(rb.position, targetPosition) > 0.05f)
        {
            if (hasCrashed) break;
            Vector3 newPosition = Vector3.MoveTowards(rb.position, targetPosition, speed * Time.deltaTime);
            rb.MovePosition(newPosition);

            yield return null;
        }

        if (!hasCrashed)
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

        //Debug.Log(name + " valid dirs at junction: ");
        //foreach (JunctionDirection dir in validDirs) { Debug.Log(dir); }

        //choose random dir
        if (IsServer || !IsSpawned)
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
        float roadOffset = 0.95f;//avoid clipping into curbs

        //left turn
        if (turnAngle == -90) return ROAD_WIDTH / 2f + roadOffset;
        //right turn
        else if (turnAngle == 90) return ROAD_WIDTH + roadOffset;
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

        if (waitingAtLight) carWaitUI.UpdateWaitTime();
    }

    public void DestroyRearTriggerInstance()
    {
        if (rearTriggerInstance != null)
        {
            if (rearTriggerInstance.name.Contains("(Clone)"))
                Destroy(rearTriggerInstance);
            
            rearTriggerInstance = null;
            rearTriggerActive = false;
        }
    }
}