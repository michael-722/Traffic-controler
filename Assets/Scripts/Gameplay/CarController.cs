using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CarController : MonoBehaviour
{
    [SerializeField] private float driveSpeed = 5f;

    [SerializeField] private float leftTurnDist = 5.0f;
    [SerializeField] private float rightTurnDist = 5.0f;

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
            currentJunction = trigger.transform.root.GetComponent<JunctionController>();
            if (currentJunction == null) return;

            entryDirection = GetDirectionFromTrigger(trigger.name);

            //find traffic light
            Transform lightsParent = trigger.transform.parent.parent;//get junction obj
            TrafficLightController[] lights = lightsParent.GetComponentsInChildren<TrafficLightController>();

            currentLight = null;

            foreach (TrafficLightController light in lights)
            {
                Vector3 dirTrigger = trigger.transform.forward;
                Vector3 lightForward = light.transform.forward;

                float dot = Vector3.Dot(lightForward, dirTrigger);

                //Debug.Log($"{trigger.name} checking {light.name} dot={dot}");

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

            StartCoroutine(HandleTrafficLight());
        }
    }


    private IEnumerator HandleTrafficLight()
    {
        waitingAtLight = true;

        yield return new WaitForSeconds(0.1f);

        //keep checking traffic light state
        while (true)
        {

            TrafficLightState lightState = currentLight.GetCurrentState();
            //Debug.Log($"{name} sees {currentLight.name} as {lightState}");

            //wait
            if (lightState == TrafficLightState.Red || lightState == TrafficLightState.Yellow)
            {
                yield return null;
                continue;
            }
            else if (lightState == TrafficLightState.Green) break;//go

            yield return null;
        }

        waitingAtLight = false;
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

        if (newDir != JunctionDirection.Up)
        {
            if (newDir == JunctionDirection.Left)
            {
                yield return StartCoroutine(MoveForward(leftTurnDist, driveSpeed));
            }
            else
            {   
                yield return StartCoroutine(MoveForward(rightTurnDist, driveSpeed));
            }
        }

        //rotate toward target dir 
        //===============add acutal end points to fix turning
        Vector3 targetDir = GetWorldDirection(newDir);

        Quaternion targetRot = Quaternion.LookRotation(targetDir);
        float turnSpeed = 2f;

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
}