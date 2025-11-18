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

    private TrafficLightController currentLight;
    private JunctionController currentJunction;
    private JunctionDirection entryDirection;

    [SerializeField] private Transform roadTransform;

    private JunctionDirection newDir;

    private NetworkVariable<JunctionDirection> chosenDirection = new NetworkVariable<JunctionDirection>(
    JunctionDirection.Up,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);

    [SerializeField] private BlinkerController leftBlinkers;
    [SerializeField] private BlinkerController rightBlinkers;

    private GameObject rearTriggerInstance;
    private bool rearTriggerActive = false;
    private bool checkTriggerAgain = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        carWidth = GetCarWidth();
        roadWidth = 14f;

        //only have enabled if roads need re calarating
        //road in x dir
        //roadWidth = GetRoadWidth(roadTransform);
        //Debug.Log($"{name} car width: {carWidth} road width: {roadWidth}");
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
        //move car forward if not waiting
        if (!waitingAtLight)
        {
            rb.MovePosition(transform.position + transform.forward * driveSpeed * Time.fixedDeltaTime);
        }
    }

    private void OnTriggerEnter(Collider trigger)
    {
        //check if is junction trigger
        if (!trigger.transform.name.Contains("Trigger")) return;

        currentJunction = trigger.gameObject.GetComponentInParent<JunctionController>();
        if (currentJunction == null)
        {
            Debug.LogError("junction controller not found on obj");
            return;
        }

        if (trigger.transform.name.Contains("Clone")) checkTriggerAgain = true;
        else checkTriggerAgain = false;

        rearTriggerInstance = trigger.gameObject;
        entryDirection = GetDirectionFromTrigger(trigger.name);

        //Debug.Log($"{name} entered junction from {entryDirection} via trigger {trigger.name}");

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
             * 
             * dot product for 180 lights
             * dot 1 = same direction
             * dot -1 = opposite direction
             * dot 0 = perpendicular
             */
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
            //dynamically remove triggers when no lights - handel in junction controller=============================================== 

            //Debug.LogWarning($"{name} no traffic light found for junction at {trigger.transform.parent.parent.name} from dir {entryDirection}");
            return;
        }

        StartCoroutine(HandleTrafficLight());
    }

    private IEnumerator HandleTrafficLight()
    {
        waitingAtLight = true;

        yield return DetermineNextDirection();

        //yield return new WaitForSeconds(0.1f);

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
                    Transform originalParent = rearTriggerInstance.transform.parent;
                    Transform originalPos = rearTriggerInstance.transform;

                    rearTriggerInstance = Instantiate(rearTriggerInstance);

                    rearTriggerInstance.transform.parent = originalParent;
                    rearTriggerInstance.transform.position = originalPos.transform.position - transform.forward * 8f;
                }
                yield return null;
                continue;
            }
            else if (lightState == TrafficLightState.Green)
            {
                Destroy(rearTriggerInstance);
                rearTriggerActive = false;

                waitingAtLight = false;
                break;//go
            }

            yield return null;
        }

        if (checkTriggerAgain) yield return null;
        else yield return MoveThroughJunction();
    }

    private IEnumerator DetermineNextDirection()
    {
        List<JunctionDirection> validDirs = new List<JunctionDirection>(currentJunction.junctionMap.Keys);
        validDirs.Remove(entryDirection);

        if (IsServer)
        {
            newDir = validDirs[Random.Range(0, validDirs.Count)];
            chosenDirection.Value = newDir;
        }
        else
        {
            yield return new WaitUntil(() => chosenDirection.Value != entryDirection);
            newDir = chosenDirection.Value;
        }
        //-90 left | 0 straight | 90 right 
        int turnAngle = (int)GetTurnAngle(entryDirection, newDir);
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

    private IEnumerator MoveThroughJunction()
    {
        float turnDist = GetTurnDistance(entryDirection, newDir);
        yield return StartCoroutine(MoveForward(turnDist, driveSpeed));

        float turnAngle = GetTurnAngle(entryDirection, newDir);
        Quaternion targetRot = Quaternion.Euler(0f, transform.eulerAngles.y + turnAngle, 0f);

        entryDirection = newDir;

        while (Quaternion.Angle(transform.rotation, targetRot) > 0.2f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            yield return null;
        }

        Vector3 euler = transform.eulerAngles;
        euler.y = Mathf.Round(euler.y / 90f) * 90f;
        transform.eulerAngles = euler;
    }

    private IEnumerator MoveForward(float distance, float speed)
    {
        bool turnOffBlinkers = false;

        Vector3 startPosition = rb.position;
        Vector3 targetPosition = startPosition + transform.forward * distance;

        while (Vector3.Distance(rb.position, targetPosition) > 0.01f)
        {
            Vector3 newPosition = Vector3.MoveTowards(rb.position, targetPosition, speed * Time.deltaTime);
            rb.MovePosition(newPosition);

            if (!turnOffBlinkers && Vector3.Distance(rb.position, startPosition) >= distance / 2)//blinkers off
            {
                turnOffBlinkers = true;
                leftBlinkers.setBlinkers(false);
                rightBlinkers.setBlinkers(false);
            }

            yield return null;
        }
    }

    private float GetTurnDistance(JunctionDirection entryDir, JunctionDirection newDir)
    {
        float laneOffset = carWidth / 2f;//lane center 
        float turnRadius = roadWidth / 2f;//turning rad

        //Debug.Log("lane offset: " + laneOffset + " turn radius: " + turnRadius);
        //Debug.Log($"{name} turn from {entryDir} to {newDir}"); 

        if (newDir == entryDir) return 0f;//straight

        switch (entryDir)
        {
            case JunctionDirection.Up:
                if (newDir == JunctionDirection.Left) return roadWidth;//tight right turn
                if (newDir == JunctionDirection.Right) return turnRadius;//wide left
                break;
            case JunctionDirection.Down://correct
                if (newDir == JunctionDirection.Left) return turnRadius;//tight left turn
                if (newDir == JunctionDirection.Right) return roadWidth;//wide right
                break;

            case JunctionDirection.Left:
                if (newDir == JunctionDirection.Up) return turnRadius;//tight left turn
                if (newDir == JunctionDirection.Down) return roadWidth;//wide right
                break;
            case JunctionDirection.Right:
                if (newDir == JunctionDirection.Down) return turnRadius;//tight left turn
                if (newDir == JunctionDirection.Up) return roadWidth;//wide right
                break;
        }

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
                    case JunctionDirection.Down: return 180f;
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