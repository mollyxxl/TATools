using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FindAssetByGUID :EditorWindow {

    private static string guid;
    private static int WIDTH = 600;
    private static string result;
    [MenuItem("XXL/Scene/GUID → Asset Path", false, 601)]
    private static void GUIDToAssetPath()
    {
        FindAssetByGUID window = (FindAssetByGUID)GetWindow(typeof(FindAssetByGUID), true, "根据GUID查找资源");
        window.minSize = new Vector2(640, 200);
        window.maxSize = window.minSize;
        window.Show();
    }
    void OnGUI()
    {
        GUILayout.BeginVertical();
        GUILayout.Space(10);
        GUILayout.Label("资源查找:");
        GUILayout.EndVertical();

        GUILayout.Label("============================================================");
        //选择的资源或者目录
        GUILayout.BeginHorizontal();
        guid = EditorGUI.TextField(new Rect(3, 70, WIDTH - 20, 16), "需要查找资源GUID", guid);
        GUILayout.EndHorizontal();

        GUILayout.BeginVertical();

        if (GUI.Button(new Rect(WIDTH - 100, 150, 80, 40), "查找"))
        {
            if (string.IsNullOrEmpty(guid))
            {
                EditorUtility.DisplayDialog("提示", "请输入需要查找资源的guid再进行操作", "ok");
                return;
            }
            result= AssetDatabase.GUIDToAssetPath(guid);
        }
        EditorGUI.TextField(new Rect(3, 100, WIDTH - 20, 50), "资源路径：" + result);
        GUILayout.EndVertical();
    }
}
