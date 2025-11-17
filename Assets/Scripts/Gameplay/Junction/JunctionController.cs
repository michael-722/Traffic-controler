using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum JunctionDirection { Up, Down, Left, Right }

public class JunctionController : MonoBehaviour
{
    public Dictionary<JunctionDirection, GameObject> junctionMap = new Dictionary<JunctionDirection, GameObject>();

    void Start()
    {
        foreach (Transform child in transform)
        {
            //Debug.Log("checking: " + child.name);
            //if there is a traffic light road or a road with no traffic light
            if (child.GetComponent<TrafficLightController>() != null || child.GetComponent<NoTrafficLightTurning>() != null)
            {
                if (child.gameObject.activeInHierarchy == false) return;//testing

                Vector3 localPos = child.localPosition;
                JunctionDirection dir = GetDirectionFromPosition(localPos);
                junctionMap[dir] = child.gameObject;

                //Debug.Log("traffic light found " + child.name + dir);
            }
        }
    }

    private JunctionDirection GetDirectionFromPosition(Vector3 localPos)
    {
        bool isHorizontal = Mathf.Abs(localPos.x) > Mathf.Abs(localPos.z);

        //if ? true : false
        if (isHorizontal) return localPos.x > 0 ? JunctionDirection.Right : JunctionDirection.Left;
        else return localPos.z > 0 ? JunctionDirection.Up : JunctionDirection.Down;
    }

    public bool GetIntersection(JunctionDirection direction)
    {
        junctionMap.TryGetValue(direction, out var result);

        if (result != null) return true;
        else return false;
    }
}