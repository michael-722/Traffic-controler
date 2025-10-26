using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CarController : MonoBehaviour
{
    [SerializeField] private float driveSpeed = 7.5f;
    [SerializeField] private float turnSpeed = 2.0f;

    [SerializeField] private float roadWidth = 7.5f;
    [SerializeField] private float carWidth = 4.0f;

    private Rigidbody rb;
    private bool waitingAtLight = false;

    private TrafficLightController currentLight;
    private JunctionController currentJunction;
    private JunctionDirection entryDirection;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
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
        if (trigger.transform.name.EndsWith("Trigger"))
        {
            currentJunction = trigger.gameObject.GetComponentInParent<JunctionController>();
            if (currentJunction == null)
            {   
                Debug.LogError("junction controller not found on obj");
                return;
            }

            entryDirection = GetDirectionFromTrigger(trigger.name);
            Debug.Log($"{name} entered junction from {entryDirection} via trigger {trigger.name}");

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
                    //Debug.Log($"{name} found traffic light {light.name} for trigger {trigger.name}");
                    break;
                }
            }

            StartCoroutine(HandleTrafficLight());
        }
    }

    private IEnumerator HandleTrafficLight()
    {
        waitingAtLight = true;

        yield return new WaitForSeconds(0.1f);

        //keep checking traffic light state
        while (waitingAtLight)
        {
            TrafficLightState lightState = currentLight.GetCurrentState();
            //Debug.Log($"{name} sees {currentLight.name} as {lightState}");

            //wait
            if (lightState == TrafficLightState.Red || lightState == TrafficLightState.Yellow)
            {
                yield return null;
                continue;
            }
            else if (lightState == TrafficLightState.Green)
            {
                waitingAtLight = false;
                break;//go
            }

            yield return null;
        }

        yield return MoveThroughJunction();
    }


    private IEnumerator MoveThroughJunction()
    {
        //get valid dirs
        List<JunctionDirection> validDirs = new List<JunctionDirection>(currentJunction.junctionMap.Keys);

        foreach (JunctionDirection dir in System.Enum.GetValues(typeof(JunctionDirection)))
        {
            if (dir == entryDirection)
            {
                validDirs.Remove(dir);
            }
        }

        JunctionDirection newDir = validDirs[Random.Range(0, validDirs.Count)];

        //Debug.Log("entry dir " + entryDirection + " new dir " + newDir);

        //forward for turns ================need to check if dists are good over time and dont drift
        if (newDir != JunctionDirection.Up)
        {
            float turnDist = GetTurnDistance(newDir);//offset dist for turn
            yield return StartCoroutine(MoveForward(turnDist, driveSpeed));
        }

        //rotate toward target dir 
        //=================add acutal end points to fix turning
        Vector3 targetDir = GetLocalDirection(newDir);//local

        Quaternion targetRot = Quaternion.LookRotation(targetDir);

        //Debug.Log($"{name} turning from {entryDirection} to {newDir}");
        entryDirection = newDir;

        while (Quaternion.Angle(transform.rotation, targetRot) > 0.5f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            yield return null;
        }
    }

    private IEnumerator MoveForward(float distance, float speed)
    {
        Vector3 startPosition = rb.position;
        Vector3 targetPosition = startPosition + transform.forward * distance;

        while (Vector3.Distance(rb.position, targetPosition) > 0.01f)
        {
            Vector3 newPosition = Vector3.MoveTowards(rb.position, targetPosition, speed * Time.deltaTime);
            rb.MovePosition(newPosition);
            yield return null;
        }
    }


    //Need to take into account entry dir as well as new dir to give turn dist needed========================
    //cba to fix it now this fking thing kinda works needs fixing as next thing to do tho
    private float GetTurnDistance(JunctionDirection newDir)
    {
        //Debug.Log("calculating turn distance for " + newDir);

        float turnRadius = (roadWidth / 2f) + (carWidth / 2f);

        if (newDir == JunctionDirection.Up) return 0f;
        if (newDir == JunctionDirection.Left) return turnRadius;
        else return turnRadius + roadWidth;
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

    private Vector3 GetLocalDirection(JunctionDirection dir)
    {
        Vector3 localRot = transform.rotation.eulerAngles;

        float angle = Mathf.Round(localRot.y / 90) * 90;//snap to 90s

        switch (angle)
        {
            case 0f://up
                switch (dir)
                {
                    case JunctionDirection.Up: return transform.forward;
                    case JunctionDirection.Down: return -transform.forward;
                    case JunctionDirection.Left: return -transform.right;
                    case JunctionDirection.Right: return transform.right;
                    default: return transform.forward;
                }

            case 90f://right
                switch (dir)
                {
                    case JunctionDirection.Up: return transform.right;
                    case JunctionDirection.Down: return -transform.right;
                    case JunctionDirection.Left: return -transform.forward;
                    case JunctionDirection.Right: return transform.forward;
                    default: return transform.right;
                }

            case 180f://down
                switch (dir)
                {
                    case JunctionDirection.Up: return -transform.forward;
                    case JunctionDirection.Down: return transform.forward;
                    case JunctionDirection.Left: return transform.right;
                    case JunctionDirection.Right: return -transform.right;
                    default: return -transform.forward;
                }

            case 270f://left
                switch (dir)
                {
                    case JunctionDirection.Up: return transform.right;
                    case JunctionDirection.Down: return -transform.right;
                    case JunctionDirection.Left: return -transform.forward;
                    case JunctionDirection.Right: return transform.forward;
                    default: return -transform.right;
                }

            default:
                return transform.forward;
        }
    }

}