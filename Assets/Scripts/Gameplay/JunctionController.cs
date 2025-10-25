using UnityEngine;
using System.Collections.Generic;

public enum JunctionDirection { Up, Down, Left, Right }

public class JunctionController : MonoBehaviour
{
    private Dictionary<JunctionDirection, GameObject> junctionMap = new Dictionary<JunctionDirection, GameObject>();

    void Start()
    {
        foreach (Transform child in transform)
        {
            //Debug.Log("checking: " + child.name);
            TrafficLightController tl = child.GetComponent<TrafficLightController>();
            if (tl != null)
            {
                Vector3 localPos = child.localPosition;
                JunctionDirection dir = GetDirectionFromPosition(localPos);
                junctionMap[dir] = child.gameObject;
                //Debug.Log(child.name + dir);   
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