using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class cubemapcreate : EditorWindow
{
    private Cubemap cubeMap = null;

    [MenuItem("XXL/Cube Map Generate")]
    public static void GenerateCubeMap()
    {
        GetWindow<cubemapcreate>();
    }

    private void OnGUI()
    {
        cubeMap = EditorGUILayout.ObjectField(cubeMap, typeof(Cubemap), false, GUILayout.Width(400)) as Cubemap;
        if (GUILayout.Button("Render To Cube Map"))
        {
            SceneView.lastActiveSceneView.camera.RenderToCubemap(cubeMap);
        }
    }
}


