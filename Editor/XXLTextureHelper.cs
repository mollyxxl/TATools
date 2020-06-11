using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class XXLTextureHelper
{
    [MenuItem("XXL/Texture/ARGB32")]
    public static void CheckARGB32()
    {
        var texures = AssetDatabase.FindAssets("t:Texture", new string[] { "Assets" });
        for (int i = 0; i < texures.Length; i++)
        {
            var textureGuid = texures[i];
            EditorUtility.DisplayProgressBar("Hold on...", textureGuid, (i + 1) * 1f / texures.Length);
            var texturePath = AssetDatabase.GUIDToAssetPath(textureGuid);

            var importer = AssetImporter.GetAtPath(texturePath);
            if (importer is TextureImporter)
            {
                var textureImporter = (TextureImporter)importer;
                var pcSetting = textureImporter.GetPlatformTextureSettings("Standalone");
                var androidSetting = textureImporter.GetPlatformTextureSettings("Android");
                var iosSetting = textureImporter.GetPlatformTextureSettings("iPhone");
                var defaultSetting = textureImporter.GetDefaultPlatformTextureSettings();
                if (pcSetting.format == TextureImporterFormat.ARGB32 ||
                    pcSetting.format == TextureImporterFormat.RGBA32 ||
                    androidSetting.format == TextureImporterFormat.ARGB32 ||
                    androidSetting.format == TextureImporterFormat.RGBA32 ||
                    iosSetting.format == TextureImporterFormat.ARGB32 ||
                    iosSetting.format == TextureImporterFormat.RGBA32)
                {
                    Debug.LogError(texturePath);
                }
                else if (defaultSetting.format == TextureImporterFormat.Automatic &&
                   defaultSetting.textureCompression == TextureImporterCompression.Uncompressed)
                {

                    var floders = texturePath.Split('/');
                    if (floders != null)
                    {
                        if (floders.Contains("Editor"))
                        {

                        }
                        else if (floders.Contains("Resources"))
                        {
                            Debug.LogError("Automatic  :" + texturePath);
                        }
                        else
                        {

                        }
                    }
                }

            }
            else
            {
                Debug.Log(importer.GetType() + "  " + texturePath);
            }

        }
        EditorUtility.ClearProgressBar();
    }
    [MenuItem("XXL/Prefab/Prefab上Animator组件AnimatorCullingMode状态检测",false,100)]
    public static void CheckPrefabAnimator()
    {
        var prefabs = AssetDatabase.FindAssets("t:Prefab", new string[] { "Assets/Res" });
        for (int i = 0; i < prefabs.Length; i++)
        {
            var prefab = prefabs[i];
            EditorUtility.DisplayProgressBar("Hold on...", prefab, (i + 1) * 1f / prefabs.Length);
            var prefabPath = AssetDatabase.GUIDToAssetPath(prefab);
            var cullingMode = AnimatorCullingMode.CullCompletely;
            if (prefabPath.StartsWith("Assets/Res/Character"))
            {
                cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            }
            else if (prefabPath.StartsWith("Assets/Res/Gui"))
            {
                //  cullingMode = AnimatorCullingMode.AlwaysAnimate;
                continue;
            }
            CheckPrefabAnimatorCore(prefabPath,cullingMode);
        }
        EditorUtility.ClearProgressBar();
    }

    private static void CheckPrefabAnimatorCore(string prefabPath,AnimatorCullingMode cullingMode= AnimatorCullingMode.CullCompletely)
    {
        GameObject instanceObj = PrefabUtility.LoadPrefabContents(prefabPath);
        var animators = instanceObj.GetComponentsInChildren<Animator>(true);
        bool change = false;
        if (animators != null)
        {
            foreach (var animator in animators)
            {
                if (animator.cullingMode == AnimatorCullingMode.AlwaysAnimate)
                {
                    animator.cullingMode = cullingMode;
                    change = true;
                    Debug.Log(prefabPath + "    " + animator.gameObject.name);
                }
            }
        }
        if(change)
            PrefabUtility.SaveAsPrefabAsset(instanceObj, prefabPath);
        PrefabUtility.UnloadPrefabContents(instanceObj);
    }

    [MenuItem("XXL/Prefab/Prefab上Animator组件AnimatorCullingMode状态检测(单个选择)",false,101)]
    public static void CheckPrefabAnimatorOne()
    {
        var prefab = Selection.activeGameObject;
        if (prefab != null)
        {
            var prefabPath = AssetDatabase.GetAssetPath(prefab);
            var cullingMode = AnimatorCullingMode.CullCompletely;
            if (prefabPath.StartsWith("Assets/Res/Character"))
            {
                cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            }
            CheckPrefabAnimatorCore(prefabPath, cullingMode);
        }
    }
    [MenuItem("XXL/Effect/粒子特效root空节点检查",false,100)]
    public static void CheckParticleRenderState()
    {
        var prefabs = AssetDatabase.FindAssets("t:Prefab", new string[] { "Assets/Res/Effect" });
        for (int i = 0; i < prefabs.Length; i++)
        {
            var prefabGUID = prefabs[i];
            EditorUtility.DisplayProgressBar("Hold on...", prefabGUID, (i + 1) * 1f / prefabs.Length);
            var prefabPath = AssetDatabase.GUIDToAssetPath(prefabGUID);
            GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            var rootParticle = prefab.GetComponent<ParticleSystemRenderer>();
            if (rootParticle != null)
            {
                var ps = prefab.GetComponent<ParticleSystem>();
                if (!rootParticle.enabled && !ps.emission.enabled)
                {
                    Debug.Log("需要剔除:"+prefabPath);
                    GameObject.DestroyImmediate(ps);
                    GameObject.DestroyImmediate(rootParticle);
                    PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
                }
            }
            PrefabUtility.UnloadPrefabContents(prefab);
        }
        EditorUtility.ClearProgressBar();
    }
    [MenuItem("XXL/Scene/Mesh信息分析")]
    public static void CheckSceneMeshInfos()
    {
        var meshs = AssetDatabase.FindAssets("t:Mesh", new string[] { "Assets/Res/Scene" });
        for (int i = 0; i < meshs.Length; i++)
        {
            var meshGUID = meshs[i];
            EditorUtility.DisplayProgressBar("Hold on...", meshGUID, (i + 1) * 1f / meshs.Length);
            var meshPath = AssetDatabase.GUIDToAssetPath(meshGUID);
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            var tru= AssetDatabase.IsMainAsset(mesh);
            if (mesh != null)
            {
                if (mesh.vertices.Length > 1500)
                {
                    Debug.Log(meshPath + "    " + mesh.vertices.Length+"    "+ tru);
                }
            }
            else 
            {
                Debug.Log(meshPath);
            }
        }
        EditorUtility.ClearProgressBar();
    }
    [MenuItem("XXL/Animation/移除动画文件中CurveBindingsInfo")]
    public static void RemoveCurveBindingsInfo()
    {
        var clips = AssetDatabase.FindAssets("t:AnimationClip", new string[] { "Assets/Res/Character" });
        if (clips != null)
        {
            for (int j = 0; j < clips.Length; j++)
            {
                var clip = clips[j];
                var clipPath = AssetDatabase.GUIDToAssetPath(clip);
                var animationClip =AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                //去除不用的animation曲线
                EditorCurveBinding[] theCurveBindings = AnimationUtility.GetCurveBindings(animationClip);
                for (int i = 0; i < theCurveBindings.Length; i ++)
                {
                    string name = theCurveBindings[i].propertyName.ToLower();
                    if (name.Contains("scale"))
                    {
                        AnimationUtility.SetEditorCurve(animationClip, theCurveBindings[i], null);
                    }
                }
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Done");
    }
    [MenuItem("XXL/Animation/移除动画文件中CurveBindingsInfo One")]
    public static void RemoveCurveBindingsInfoOne()
    {
        AnimationClip animationClip = Selection.activeObject as AnimationClip;
        //m_ScaleCurves
        SerializedObject so = new SerializedObject(animationClip);
        var scale= so.FindProperty("m_ScaleCurves");
        scale.animationCurveValue = null;
        int sss = 1;
        //if (animationClip != null)
        //{
        //    //去除不用的animation曲线
        //    EditorCurveBinding[] theCurveBindings = AnimationUtility.GetCurveBindings(animationClip);
        //    for (int i = 0; i < theCurveBindings.Length; i++)
        //    {
        //        var curveBind = theCurveBindings[i];
        //        string name = curveBind.propertyName.ToLower();
        //        if (name.Contains("scale"))
        //        {
        //            Debug.Log(theCurveBindings[i].path + "   " + name);

        //           var  curve = AnimationUtility.GetEditorCurve(animationClip, curveBind);
        //            AnimationCurve tempCure = new AnimationCurve();
        //            //while (tempCure.keys.Length > 0)
        //            //{
        //            //    tempCure.RemoveKey(0);
        //            //}
        //            animationClip.SetCurve(curveBind.path, curveBind.type, curveBind.propertyName, tempCure);
        //        }
        //    }
        //}

        AssetDatabase.SaveAssets();
        Debug.Log("Done");
    }

    [MenuItem("XXL/Animation/移除动画文件中精度降低为3位")]
    public static void RemoveCurveBindingsInfoFLoat()
    {
        try
        {
            string animationPath = Application.dataPath + "/Res";
            var files = Directory.GetFiles(animationPath, "*.anim", SearchOption.AllDirectories);

            for (int m = 0; m < files.Length; m += 1)
            {
                var file = files[m].Replace('\\', '/').Substring(Application.dataPath.Length - 6);
                if (file.StartsWith("Assets/Res/Character/player"))
                {
                    continue;   //player 目录已经处理过 直接跳过
                }
                Debug.Log(file);
                EditorUtility.DisplayProgressBar("Hold On...", file, (m + 1) * 1f / files.Length);
                var animationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(file);

                //if (animationClip != null)
                //{
                //    //压缩animationClip的文件精度
                //    EditorCurveBinding[] binds = AnimationUtility.GetCurveBindings(animationClip);
                //    Keyframe key;
                //    Keyframe[] keyFrames;
                //    for (int i = 0; i < binds.Length; i += 1)
                //    {
                //        EditorCurveBinding bind = binds[i];
                //        AnimationCurve curve = AnimationUtility.GetEditorCurve(animationClip, bind);
                //        if (curve == null || curve.keys == null)
                //        {
                //            continue;
                //        }
                //        keyFrames = curve.keys;

                //        for (int j = 0; j < keyFrames.Length; j += 1)
                //        {
                //            key = keyFrames[j];
                //            key.value = float.Parse(key.value.ToString("f3"));
                //            key.inTangent = float.Parse(key.inTangent.ToString("f3"));
                //            key.outTangent = float.Parse(key.outTangent.ToString("f3"));
                //            keyFrames[j] = key;
                //        }
                //        curve.keys = keyFrames;
                //        animationClip.SetCurve(bind.path, bind.type, bind.propertyName, curve);
                //    }
                //}

                if (animationClip != null)
                {
                    //压缩animationClip的文件精度
                    AnimationClipCurveData[] curves = AnimationUtility.GetAllCurves(animationClip);
                    Keyframe key;
                    Keyframe[] keyFrames;
                    for (int i = 0; i < curves.Length; i += 1)
                    {
                        AnimationClipCurveData curveData = curves[i];
                        if (curveData.curve == null || curveData.curve.keys == null)
                        {
                            continue;
                        }
                        keyFrames = curveData.curve.keys;
                        for (int j = 0; j < keyFrames.Length; j += 1)
                        {
                            key = keyFrames[j];
                            key.value = float.Parse(key.value.ToString("f3"));
                            key.inTangent = float.Parse(key.inTangent.ToString("f3"));
                            key.outTangent = float.Parse(key.outTangent.ToString("f3"));
                            keyFrames[j] = key;
                        }
                        curveData.curve.keys = keyFrames;
                        animationClip.SetCurve(curveData.path, curveData.type, curveData.propertyName, curveData.curve);
                    }
                }
            }
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
            Debug.Log("Done");
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.Log(e.ToString());
        }
        
    }
    [MenuItem("XXL/Mat/变体收集材质keyword自动显示")]
    public static void ChangeKeywordToFloat()
    {
        Dictionary<string, string> dic = new Dictionary<string, string>();
        dic.Add("ENABLE_CHANGECOLOR", "_EnableChangeColor");
        dic.Add("SYC_QUALITY_LOW", "_BumpEnable");
        dic.Add("SYC_TATTOO_ENABLE", "_TattooEnable");
        dic.Add("_ENABLE_DISSOLVE", "_EnableDissolve");
        dic.Add("SYC_ANISO_ENABLE", "_AnisoEnable");
        dic.Add("SYC_EMISSION_ENABLE", "_EmissionEnable");
        dic.Add("SYC_SSS_ENABLE", "_SSSEnable");
        dic.Add("_REFLECTIONENABLE_ON", "_ReflectionEnable");
        dic.Add("_NORMALMAP", "");

        var mats = AssetDatabase.FindAssets("t:Material", new string[] { "Assets/Res/shaderlib" });
        List<string> keyssss = new List<string>();
        if (mats != null)
        {
            for (int j = 0; j < mats.Length; j++)
            {
                var matgui = mats[j];
                EditorUtility.DisplayProgressBar("Hold On...", matgui, (j + 1) * 1f / mats.Length);
                var matPath = AssetDatabase.GUIDToAssetPath(matgui);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat.shader.name == "Genesis/Character/Character_PBR")
                {
                    var keywords = mat.shaderKeywords;
                    foreach (var di in dic)
                    {
                        float value = keywords.Contains(di.Key) ? 1f : 0;
                        if (!string.IsNullOrEmpty(di.Value))
                            mat.SetFloat(di.Value, value);
                    }
                }
            }
        }
        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        Debug.Log("Done");
    }
}
