using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace XXL
{
    [InitializeOnLoad]
    public static class SceneViewSelection
    {
        static SceneViewSelection()
        {
            List<KeyCode> hotkeys = new List<KeyCode>() {
                KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.UpArrow, KeyCode.DownArrow
            };
            SceneView.onSceneGUIDelegate += delegate (SceneView sceneView)
            {
                if (UnityEditor.Tools.current == Tool.Rect)
                {
                    Event evt = Event.current;
                    if (evt != null && evt.type == EventType.KeyDown)
                    {
                        if (!hotkeys.Contains(evt.keyCode))
                            return;

                        GameObject[] list = Selection.gameObjects;
                        if (list == null || list.Length == 0)
                            return;

                        foreach (GameObject go in list)
                        {
                            bool valid = true;
                            Transform parent = go.transform;
                            while (valid && (parent = parent.parent) != null)
                            {
                                GameObject obj = parent.gameObject;
                                for (int i = 0; i < list.Length; i++)
                                {
                                    if (list[i] == obj)
                                    {
                                        valid = false;
                                        break;
                                    }
                                }
                            }
                            if (valid)
                            {
                                Vector3 position = go.transform.localPosition;
                                Vector3 offset = Vector3.zero;
                                if (evt.keyCode == KeyCode.LeftArrow)
                                {
                                    offset.x = -1;
                                }
                                else if (evt.keyCode == KeyCode.RightArrow)
                                {
                                    offset.x = 1;
                                }
                                else if (evt.keyCode == KeyCode.UpArrow)
                                {
                                    offset.y = 1;
                                }
                                else if (evt.keyCode == KeyCode.DownArrow)
                                {
                                    offset.y = -1;
                                }
                                offset = offset * (evt.shift ? 10 : (evt.control ? 5 : 1));
                                go.transform.localPosition = position + offset;
                            }
                        }
                        evt.Use();
                    }
                }
            };
        }
    }
}