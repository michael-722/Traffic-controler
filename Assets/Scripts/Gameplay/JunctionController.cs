using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class JunctionController : MonoBehaviour
{
    private List<GameObject> intersections = new List<GameObject>();

    void Start()
    {
        foreach (Transform child in transform)
        {
            if (child.gameObject.GetComponent<TrafficLightController>() != null) 
            {
                intersections.Add(child.gameObject);
            }
        }

        Debug.Log(intersections.Count + " intersections found in " + gameObject.name);
    }

    void Update()
    {
        
    }
}
