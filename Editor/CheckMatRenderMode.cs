using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CheckMatRenderMode 
{
    [MenuItem("XXL/Scene/CheckMatRenderMode")]
    public static void MatPropertiesChanges()
    {
        try
        {
            string Path = "Assets/Res";
            var guids = AssetDatabase.FindAssets("t:Material", new string[] { Path });
            for (int i = 0; i < guids.Length; i++)
            {
                var guid = guids[i];
                var path = AssetDatabase.GUIDToAssetPath(guid);
                EditorUtility.DisplayProgressBar("Hold On...", path, i * 1f / guids.Length);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat.shader.name.Equals("Lab/Scene/PBR") ||
                    mat.shader.name.Equals("Lab/Scene/PBR-TeamColor"))
                {
                    SerializedObject so = new SerializedObject(mat);
                    var prosObj = so.FindProperty("m_SavedProperties");
                    var flaots = prosObj.FindPropertyRelative("m_Floats");

                    if (flaots.isArray)
                    {
                        SerializedProperty renderProperty = flaots;
                        for (int m = 0; m < flaots.arraySize; m++)
                        {
                            var s = flaots.GetArrayElementAtIndex(m);
                            if (s.displayName.Equals("_Mode"))
                            {
                                renderProperty = s.FindPropertyRelative("second");
                                break;
                            }
                        }

                        if (renderProperty.floatValue == 0)
                        {
                            for (int m = 0; m < flaots.arraySize; m++)
                            {
                                var s = flaots.GetArrayElementAtIndex(m);

                                if (s.displayName.Equals("_AlphaTest"))
                                {
                                    var alphaTest = s;
                                    if (alphaTest.hasChildren)
                                    {
                                        var second = alphaTest.FindPropertyRelative("second");
                                        if (second.floatValue == 1f)
                                        {
                                            renderProperty.floatValue = 1f;
                                            flaots.DeleteArrayElementAtIndex(m);
                                            Debug.Log(path);
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                        else if (renderProperty.floatValue == 1f)
                        {
                            Debug.Log("Tranparent: " + path);
                        }
                        so.ApplyModifiedProperties();
                    }
                    else
                    {
                        Debug.Log("m_Floats 获取异常");
                    }
                }
            }
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }
        catch
        {
            EditorUtility.ClearProgressBar();
        }
    }
}
