using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class UIGammaToLinear
{
    [MenuItem("XXL/Mat/Image组件统一加上Mat")]
    public static void AddMatToImage()
    {
        //string assetPath = "Assets/Res/Gui/GameGUINew/UI-HeadOverlay.prefab";
        string matPath = "Assets/Scripts/UI/GammaToLinear/GammaToLinearSpace_Trans.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            Debug.Log("材质不存在");
            return; 
        }
        var sele = Selection.activeGameObject;
        var assetPath = AssetDatabase.GetAssetPath(sele);
        GameObject instanceObj = PrefabUtility.LoadPrefabContents(assetPath);
        var images = instanceObj.GetComponentsInChildren<UnityEngine.UI.Image>(true);
        foreach (var image in images)
        {
            image.material = mat;
        }
        PrefabUtility.SaveAsPrefabAsset(instanceObj, assetPath);
        PrefabUtility.UnloadPrefabContents(instanceObj);
        Debug.Log("完成");
    }
    [MenuItem("XXL/Prefab/CollectTextMeshProAlignType")]
    public static void CollectTextMeshProAlignType()
    {
        var sele = Selection.objects;
        for (int i = 0; i < sele.Length; i++)
        {
            var assetPath = AssetDatabase.GetAssetPath(sele[i]);
            EditorUtility.DisplayProgressBar("Hold On...", assetPath, (i + 1) * 1f / sele.Length);
            GameObject instanceObj = PrefabUtility.LoadPrefabContents(assetPath);
            var meshTexts = instanceObj.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);

            foreach (var meshText in meshTexts)
            {
                if (meshText.font == null)
                {
                    Debug.LogError("丢字体。。" + assetPath + "  " + meshText.gameObject.name);
                    continue;
                }
                if (meshText.font.name.Equals("zhongyuanjian SDF"))
                {
                    var alignment = meshText.alignment;
                    //Debug.Log(alignment);
                    Debug.Log(meshText.enableWordWrapping + " " + meshText.overflowMode);
                }
            }
            PrefabUtility.UnloadPrefabContents(instanceObj);
        }

        EditorUtility.ClearProgressBar();
        Debug.Log("完成");
    }
    [MenuItem("XXL/Prefab/TextMeshProFontChange")]
    public static void TextMeshProFontChange()
    {
        try
        {
            string fontPath = "Assets/Res/Gui/Font/FZLTCH.TTF";
            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null)
            {
                Debug.Log("字体 FZLTCH.TTF 不存在");
                return;
            }

            var sele = Selection.objects;
            for (int i = 0; i < sele.Length; i++)
            {
                var assetPath = AssetDatabase.GetAssetPath(sele[i]);
                EditorUtility.DisplayProgressBar("Hold On...", assetPath, (i + 1) * 1f / sele.Length);
                GameObject instanceObj = PrefabUtility.LoadPrefabContents(assetPath);
                var meshTexts = instanceObj.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);

                foreach (var meshText in meshTexts)
                {
                    if (meshText.font == null)
                    {
                       // Debug.LogError("丢字体。。" + assetPath + "  " + meshText.gameObject.name);
                        continue;
                    }
                    if (meshText.font.name.Equals("JDJZONGYI SDF"))
                    {
                        if (meshText.gameObject.GetComponentInParent<TMPro.TMP_InputField>() != null)
                        {
                            Debug.LogError("需要手动替换为InputField"+assetPath);
                        }
                        Debug.Log(meshText.gameObject.name);

                        var msg = meshText.text;

                        var fontsize = meshText.fontSize;
                        var sp = meshText.lineSpacing;
                        var fontstyle = meshText.fontStyle;
                        var fontAutoSize = meshText.autoSizeTextContainer;
                        var fontVertexColor = meshText.color;
                        var alignment = meshText.alignment;
                        var raycast = meshText.raycastTarget;
                        var richText = meshText.richText;
                        var obj = meshText.gameObject;
                        GameObject.DestroyImmediate(meshText);
                        var tx = obj.AddComponent<UnityEngine.UI.Text>();
                        tx.text = msg;
                        tx.font = font;
                        tx.fontStyle = (FontStyle)(fontsize > 2 ? 0 : fontsize);
                        tx.fontSize = (int)fontsize;
                        tx.lineSpacing = sp > 1f ? 1f + (float)System.Math.Round(sp / fontsize, 2) : 1f;
                        var unityAligment = TextAnchor.UpperLeft;

                        Debug.Log(alignment);
                        if (alignment == TMPro.TextAlignmentOptions.TopLeft)
                        {
                            unityAligment = TextAnchor.UpperLeft;
                        }
                        else if (alignment == TMPro.TextAlignmentOptions.TopRight)
                        {
                            unityAligment = TextAnchor.UpperRight;
                        }
                        else if (alignment == TMPro.TextAlignmentOptions.MidlineLeft)
                        {
                            unityAligment = TextAnchor.MiddleLeft;
                        }
                        else if (alignment == TMPro.TextAlignmentOptions.MidlineRight)
                        {
                            unityAligment = TextAnchor.MiddleRight;
                        }
                        else if (alignment == TMPro.TextAlignmentOptions.MidlineJustified)  //left middle
                        {
                            unityAligment = TextAnchor.MiddleLeft;
                        }
                        else if (alignment == TMPro.TextAlignmentOptions.Center)  // center middle
                        {
                            unityAligment = TextAnchor.MiddleCenter;
                        }
                        else if (alignment == TMPro.TextAlignmentOptions.Left)  //left middle
                        {
                            unityAligment = TextAnchor.MiddleLeft;
                        }
                        else if (alignment == TMPro.TextAlignmentOptions.Right)  //right middle
                        {
                            unityAligment = TextAnchor.MiddleRight;
                        }
                        else if (alignment == TMPro.TextAlignmentOptions.Midline) //center middle
                        {
                            unityAligment = TextAnchor.MiddleCenter;
                        }
                        else if (alignment == TMPro.TextAlignmentOptions.Top)   //center top
                        {
                            unityAligment = TextAnchor.UpperCenter;
                        }
                        else if (alignment == TMPro.TextAlignmentOptions.BottomLeft ||
                            alignment == TMPro.TextAlignmentOptions.BottomJustified)
                        {
                            unityAligment = TextAnchor.LowerLeft;
                        }
                        else if (alignment == TMPro.TextAlignmentOptions.BottomRight)
                        {
                            unityAligment = TextAnchor.LowerRight;
                        }
                        else if (alignment == TMPro.TextAlignmentOptions.Bottom)
                        {
                            unityAligment = TextAnchor.LowerCenter;
                        }
                        else
                        {
                            Debug.LogError("Text Mesh Pro UGUI 存在未知文本对齐方式，请手动调整");
                        }
                        tx.alignment = unityAligment;
                        var overflow = meshText.overflowMode;
                        var wrap = meshText.enableWordWrapping;
                        HorizontalWrapMode hOv = HorizontalWrapMode.Overflow;
                        VerticalWrapMode vOv = VerticalWrapMode.Overflow;
                        //水平
                        hOv = wrap == true ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;

                        //垂直
                        if (overflow == TMPro.TextOverflowModes.Overflow)
                        {
                            vOv = VerticalWrapMode.Overflow;
                        }
                        else if (overflow == TMPro.TextOverflowModes.Masking)
                        {
                            vOv = VerticalWrapMode.Overflow;
                        }
                        else if (overflow == TMPro.TextOverflowModes.Truncate)
                        {
                            vOv = VerticalWrapMode.Truncate;
                        }
                        else if (overflow == TMPro.TextOverflowModes.Ellipsis)
                        {
                            vOv = VerticalWrapMode.Truncate;
                        }
                        else if (overflow == TMPro.TextOverflowModes.ScrollRect)
                        {
                            vOv = VerticalWrapMode.Overflow;
                        }

                        tx.horizontalOverflow = hOv;
                        tx.verticalOverflow = vOv;
                        tx.color = fontVertexColor;
                        tx.raycastTarget = raycast;
                        tx.supportRichText = richText;
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(instanceObj, assetPath);
                PrefabUtility.UnloadPrefabContents(instanceObj);
            }

            EditorUtility.ClearProgressBar();
            Debug.Log("完成");
        }
        catch (System.Exception e)
        {
            Debug.Log(e.ToString());
        }
       
    }
    [MenuItem("XXL/Prefab/CollectTextMeshProFontChars")]
    public static void CollectTextMeshProFontChars()
    {
        string fontPath = "Assets/Res/Gui/Font/FZLTCH.TTF";
        var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        if (font == null)
        {
            Debug.Log("字体 FZLTCH.TTF 不存在");
            return;
        }
        List<char> allchar = new List<char>();

        var sele = Selection.objects;
        for (int i = 0; i < sele.Length; i++)
        {
            var assetPath = AssetDatabase.GetAssetPath(sele[i]);
            EditorUtility.DisplayProgressBar("Hold On...", assetPath, (i + 1) * 1f / sele.Length);
            GameObject instanceObj = PrefabUtility.LoadPrefabContents(assetPath);
            var meshTexts = instanceObj.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
           
            foreach (var meshText in meshTexts)
            {
                if (meshText.font == null)
                {
                    Debug.LogError("丢字体。。" + assetPath + "  " + meshText.gameObject.name);
                    continue;
                }
                if (meshText.font.name.Equals("JDJZONGYI SDF"))
                {
                    Debug.Log(meshText.gameObject.name);

                    var msg = meshText.text;
                    var chars = msg.ToCharArray();
                    foreach (var ch in chars)
                    {
                        if (!allchar.Contains(ch))
                        {
                            allchar.Add(ch);
                        }
                    }
                }
            }
            PrefabUtility.UnloadPrefabContents(instanceObj);
        }
        

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var ch in allchar)
        {
            sb.Append(ch).Append(" ");
        }

        using (StreamWriter sw = new StreamWriter("Assets/allchars.txt"))
        {
            sw.Write(sb.ToString());
        }

        EditorUtility.ClearProgressBar();
        Debug.Log("完成");
    }
    [MenuItem("XXL/Scene/Scene目录下SkinMeshRender组件cast Shadow全部关闭")]
    public static void ClosePrefabCastShadow()
    {
        var prefabs = AssetDatabase.FindAssets("t:Prefab", new string[] { "Assets/Res/Scene" });
        for (int i=0;i<prefabs.Length;i++)
        {
            var pref = prefabs[i];
            var assetPath = AssetDatabase.GUIDToAssetPath(pref);
            EditorUtility.DisplayProgressBar("Hold On...", assetPath, (i + 1) * 1f / prefabs.Length);
            GameObject instanceObj = PrefabUtility.LoadPrefabContents(assetPath);
            var renders = instanceObj.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renders != null&&renders.Length>0)
            {
                Debug.Log(assetPath);
                foreach (var render in renders)
                {
                    render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                PrefabUtility.SaveAsPrefabAsset(instanceObj, assetPath);
            }
            PrefabUtility.UnloadPrefabContents(instanceObj);
        }
        EditorUtility.ClearProgressBar();
        Debug.Log("完成");
    }
    [MenuItem("XXL/FBX/Animation Compression调整为Optimal")]
    public static void ChangeAnimationCompression()
    {
        var models = AssetDatabase.FindAssets("t:Model", new string[] { "Assets/Res" });
        for (int i = 0; i < models.Length; i++)
        {
            var pref = models[i];
            var assetPath = AssetDatabase.GUIDToAssetPath(pref);
            EditorUtility.DisplayProgressBar("Hold On...", assetPath, (i + 1) * 1f / models.Length);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }
        EditorUtility.ClearProgressBar();
        Debug.Log("完成");
    }
}
