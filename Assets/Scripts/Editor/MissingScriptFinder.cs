using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class MissingScriptFinder : EditorWindow
{
    [MenuItem("Tools/Find Missing Scripts")]
    public static void ShowWindow() => GetWindow<MissingScriptFinder>("Script Finder");

    void OnGUI()
    {
        if (GUILayout.Button("Find Missing Scripts in Hierarchy"))
        {
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
            foreach (GameObject go in allObjects)
            {
                Component[] components = go.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                        Debug.LogError($"Missing Script found on: {go.name}", go);
                }
            }
        }

        if (GUILayout.Button("Find Missing Scripts in Prefabs"))
        {
            // Finds all prefab assets in the project
            string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
            foreach (string path in allAssetPaths)
            {
                if (path.EndsWith(".prefab"))
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    Component[] components = prefab.GetComponentsInChildren<Component>(true);
                    foreach (Component c in components)
                    {
                        if (c == null)
                            Debug.LogError($"Missing Script found in Prefab: {path}", prefab);
                    }
                }
            }
        }
    }
}