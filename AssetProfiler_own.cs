#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

using UnityEditor;


public class AssetProfiler_own : MonoBehaviour
{
    public int instantiateCountForEach = 100;
    public float testTimeForEach = 3;
    public int topFrameCount = 10;
    public List<string> testAssetPathList = new List<string>();

    ProfilingReport mTempReport = null;
    string mSavePath;
    int mProgress = -1;

    public class TextureInfo
    {
        public string name;
        public int width;
        public int height;
        public long Bytes;
    }
    public Dictionary<IntPtr, TextureInfo> mInitTextureSamples = null;
    public static string FP_Effect = "Assets/Res/Effect";
    public class ProfilingReport
    {
        public string path;                 //资源路径
        public float loadTime;              //加载耗时，单位ms
        public float instTime;              //实例化耗时,单位ms
        public float frameTimeMin;          //最小帧耗时
        public float frameTimeMax;          //最大帧耗时
        public float frameTimeAvgTopX;      //耗时最大的X帧平均值
        public int rendererCount;           //激活的渲染器个数
        public int materialCount;           //材质个数
        public int maxDrawCall;                //drawCall
        public int texMemCount;             //所用贴图个数
        public long texMemKBytes;           //所用贴图内存
        public int particleSystemCount;     //粒子系统总数
        public int particleAliveCountMax;   //最大粒子共存数
        public int meshVertexCount;         //网格顶点总数
        public int meshTriangleCount;       //网格三角面总数
        public int animationCount;          //旧动画组件数量
        public int animatorCount;           //新动画组件数量
        public Dictionary<IntPtr, TextureInfo> textureInfoDetails;

        public void Export(System.Text.StringBuilder builder)
        {
            builder.AppendFormat("{0}, ", path);
            builder.AppendFormat("{0}, ", loadTime);
            builder.AppendFormat("{0}, ", instTime);
            builder.AppendFormat("{0}, ", frameTimeAvgTopX);
            builder.AppendFormat("{0}, ", frameTimeMax);
            builder.AppendFormat("{0}, ", frameTimeMin);
            builder.AppendFormat("{0}, ", rendererCount);
            builder.AppendFormat("{0}, ", materialCount);
            builder.AppendFormat("{0}, ", maxDrawCall);
            builder.AppendFormat("{0}, ", particleSystemCount);
            builder.AppendFormat("{0}, ", particleAliveCountMax);
            builder.AppendFormat("{0}, ", meshVertexCount);
            builder.AppendFormat("{0}, ", meshTriangleCount);
            builder.AppendFormat("{0}, ", animationCount);
            builder.AppendFormat("{0}, ", animatorCount);
            builder.AppendFormat("{0}, ", texMemCount);
            builder.AppendFormat("{0}, ", texMemKBytes / 1024);
            System.Text.StringBuilder names = new System.Text.StringBuilder();
            foreach(var t in textureInfoDetails.Values)
            {
                names.AppendFormat("{0}({1}_{2}_{3})  ", t.name, t.width, t.height, t.Bytes / 1024);
            }
            builder.AppendFormat("{0}, ", names.ToString());
            builder.Append("\n");
        }

        public static void ExportColomnName(System.Text.StringBuilder builder,int topX)
        {
            builder.AppendFormat("{0}, ", "资源路径");
            builder.AppendFormat("{0}, ", "加载耗时(ms)");
            builder.AppendFormat("{0}, ", "实例化耗时(ms)");
            builder.AppendFormat("最大{0}帧平均耗时(ms), ", topX.ToString());
            builder.AppendFormat("{0}, ", "最大单帧耗时(ms)");
            builder.AppendFormat("{0}, ", "最小单帧耗时(ms)");
            builder.AppendFormat("{0}, ", "渲染器总数");
            builder.AppendFormat("{0}, ", "材质总数");
            builder.AppendFormat("{0}, ", "DrawCall(建议<10)");
            builder.AppendFormat("{0}, ", "粒子系统总数");
            builder.AppendFormat("{0}, ", "最大粒子共存数");
            builder.AppendFormat("{0}, ", "网格顶点总数");
            builder.AppendFormat("{0}, ", "网格三角面总数");
            builder.AppendFormat("{0}, ", "旧动画组件总数");
            builder.AppendFormat("{0}, ", "新动画组件总数");
            builder.AppendFormat("{0}, ", "纹理总数");
            builder.AppendFormat("{0}, ", "纹理总内存(KB)");
            builder.AppendFormat("{0}, ", "纹理详情(长_宽_内存KB)");
            builder.Append("\n");
        }
    }

    // Use this for initialization
    IEnumerator Start()
    {
        yield return FullUnloadAndGC();

        Time.maximumDeltaTime = 10;
        Application.targetFrameRate = 1000;

        //计算每帧Overhead
        float baseFrameTime = 0;
        int baseFrameCount = 33;
        for (int i = 0; i < baseFrameCount; i++)
        {
            yield return UtilityOwn.Yield.WaitForEndOfFrame;
            baseFrameTime += Time.deltaTime;
        }
        baseFrameTime /= baseFrameCount;
        Debug.LogFormat("average delta time of {0} frames: {1}", baseFrameCount, baseFrameTime);

        mInitTextureSamples = GetTextureMemorySamples();

        if (testAssetPathList.Count == 0)
        {
            ReloadAssetPath(ref testAssetPathList);
        }

        var now = DateTime.Now;
        string fileName = string.Format("AssetProfilingReport_{0:d4}{1:d2}{2:d2}_{3:d2}{4:d2}{5:d2}.csv",
            now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
        mSavePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), fileName);

        mProgress = 0;
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        StreamWriter sw = new StreamWriter(mSavePath, false, System.Text.Encoding.UTF8);
        ProfilingReport.ExportColomnName(builder,topFrameCount);
        sw.Write(builder.ToString());
        builder.Length = 0;
        for (mProgress = 0; mProgress < testAssetPathList.Count; mProgress++)
        {
            builder.Length = 0;
            yield return AnalyzeSingleAsset(testAssetPathList[mProgress], baseFrameTime);
            mTempReport.Export(builder);
            sw.Write(builder.ToString());
            sw.Flush();
        }
        sw.Close();
    }

    void OnGUI()
    {
        if (mProgress < 0)
        {
            GUILayout.Label("not start");
        }
        else if (mProgress >= testAssetPathList.Count)
        {
            GUILayout.Label("finish");
        }
        else
        {
            GUILayout.Label(string.Format("{0}/{1}:{2}", mProgress, testAssetPathList.Count, testAssetPathList[mProgress]));
        }
        GUILayout.Label("report will save to:" + mSavePath);
    }

    IEnumerator FullUnloadAndGC()
    {
        yield return Resources.UnloadUnusedAssets();
        GC.Collect();
        yield return UtilityOwn.Yield.WaitForEndOfFrame;
        yield return UtilityOwn.Yield.WaitForEndOfFrame;
        yield return UtilityOwn.Yield.WaitForEndOfFrame;
    }

    IEnumerator AnalyzeSingleAsset(string assetPath, float baseFrameTime)
    {
        mTempReport = new ProfilingReport();
        mTempReport.path = assetPath;

        //清除缓存资源
        yield return FullUnloadAndGC();

        //独立的代码块来声明局部变量，限定作用域
        {
            //加载
            float t1 = Time.realtimeSinceStartup;
            GameObject targetObject = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            float t2 = Time.realtimeSinceStartup;
            mTempReport.loadTime = (t2 - t1) * 1000;
            yield return UtilityOwn.Yield.WaitForEndOfFrame;
            yield return UtilityOwn.Yield.WaitForEndOfFrame;
            if (targetObject == null)
            {
                Debug.LogError("load asset failed:" + assetPath);
                yield break;
            }

            var objRenderers = targetObject.GetComponentsInChildren<Renderer>(true);
            var objMaterials = new HashSet<Material>();
            foreach (var r in objRenderers)
            {
                foreach (var mat in r.sharedMaterials)
                {
                    objMaterials.Add(mat);
                }
                if (r is MeshRenderer)
                {
                    var mr = r as MeshRenderer;
                    var mesh = mr.GetComponent<MeshFilter>().sharedMesh;
                    if (mesh)
                    {
                        mTempReport.meshVertexCount += mesh.vertexCount;
                        mTempReport.meshTriangleCount += mesh.triangles.Length / 3;
                    }
                }
                else if (r is SkinnedMeshRenderer)
                {
                    var smr = r as SkinnedMeshRenderer;
                    var mesh = smr.sharedMesh;
                    if (mesh)
                    {
                        mTempReport.meshVertexCount += smr.sharedMesh.vertexCount;
                        mTempReport.meshTriangleCount += smr.sharedMesh.triangles.Length / 3;
                    }
                }
                else if (r is ParticleSystemRenderer)
                {
                    var psr = r as ParticleSystemRenderer;
                    var mesh = psr.mesh;
                    if (mesh)
                    {
                        mTempReport.meshVertexCount += mesh.vertexCount;
                        mTempReport.meshTriangleCount += mesh.triangles.Length / 3;
                    }
                }
            }
            mTempReport.rendererCount = objRenderers.Length;
            mTempReport.materialCount = objMaterials.Count;

            mTempReport.particleSystemCount = targetObject.GetComponentsInChildren<ParticleSystem>(true).Length;
            mTempReport.animationCount = targetObject.GetComponentsInChildren<Animation>(true).Length;
            mTempReport.animatorCount = targetObject.GetComponentsInChildren<Animator>(true).Length;

            yield return UtilityOwn.Yield.WaitForEndOfFrame;
            yield return UtilityOwn.Yield.WaitForEndOfFrame;


            t2 = Time.realtimeSinceStartup;
            //实例化
            var instList = new List<GameObject>();
            for (int i = 0; i < instantiateCountForEach; i++)
            {
                GameObject go = Instantiate(targetObject);
                go.transform.position = Vector3.zero;
                instList.Add(go);
            }
            float t3 = Time.realtimeSinceStartup;
            mTempReport.instTime = (t3 - t2) * 1000f / instantiateCountForEach;

            //渲染
            ParticleSystem[] psOfOneInst = instList[0].GetComponentsInChildren<ParticleSystem>(true);
            yield return UtilityOwn.Yield.WaitForEndOfFrame;
            yield return UtilityOwn.Yield.WaitForEndOfFrame;
            yield return UtilityOwn.Yield.WaitForEndOfFrame;
            
            float t4 = Time.realtimeSinceStartup;
            mTempReport.frameTimeMin = float.MaxValue;
            mTempReport.frameTimeMax = float.MinValue;
            int frame = 0;
            List<float> frameTimeList = new List<float>();
            while (Time.realtimeSinceStartup - t4 < testTimeForEach)
            {
                var dtMs = Time.deltaTime * 1000;
                frame++;
                mTempReport.frameTimeMin = Math.Min(mTempReport.frameTimeMin, dtMs);
                mTempReport.frameTimeMax = Math.Max(mTempReport.frameTimeMax, dtMs);
                frameTimeList.Add(dtMs);
                int particleCount = 0;
                for (int i = 0; i < psOfOneInst.Length; ++i)
                {
                    particleCount += psOfOneInst[i].particleCount;
                }
                if (mTempReport.particleAliveCountMax < particleCount)
                    mTempReport.particleAliveCountMax = particleCount;
                yield return UtilityOwn.Yield.WaitForEndOfFrame;
            }
            mTempReport.frameTimeMin = mTempReport.frameTimeMin / instantiateCountForEach - baseFrameTime;
            mTempReport.frameTimeMax = mTempReport.frameTimeMax / instantiateCountForEach - baseFrameTime;
            frameTimeList.Sort();
            frameTimeList.Reverse();
            float avg = 0f;
            int topX = Math.Min(topFrameCount, frameTimeList.Count);
            for (int i = 0; i < topX; ++i)
            {
                avg += frameTimeList[i];
            }
            mTempReport.frameTimeAvgTopX = avg / topX / instantiateCountForEach - baseFrameTime;
            if (topX < topFrameCount)
            {
                Debug.LogWarning("frame count lower than expectation:" + assetPath);
            }

            //统计纹理信息
            yield return UtilityOwn.Yield.WaitForEndOfFrame;
            mTempReport.textureInfoDetails = new Dictionary<IntPtr, TextureInfo>();
            var texSamples = GetTextureMemorySamples();
            foreach (var t in texSamples)
            {
                if (mInitTextureSamples.ContainsKey(t.Key) == false)
                {
                    mTempReport.textureInfoDetails.Add(t.Key, t.Value);
                    mTempReport.texMemKBytes += t.Value.Bytes;
                    mTempReport.texMemCount++;
                }
            }

            //清理
            foreach (var obj in instList)
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
            instList.Clear();
            texSamples.Clear();
        }

        yield return FullUnloadAndGC();
    }

    [ContextMenu("重新加载测试资源路径")]
    void ReloadAssetPathList()
    {
        ReloadAssetPath(ref testAssetPathList);
    }

    static void ReloadAssetPath(ref List<string> assetPathList)
    {
        assetPathList.Clear();
        string[] dirList = Directory.GetFiles(FP_Effect, "*.prefab", SearchOption.AllDirectories);
        for (int i = 0; i < dirList.Length; ++i)
        {
            string ss = dirList[i];
            if (ss.Contains("ui_prefab"))
                continue;
            //ss = ss.Replace(".prefab", "");
            assetPathList.Add(ss);
        }
    }

    static Dictionary<IntPtr, TextureInfo> GetTextureMemorySamples()
    {
        var textSamples = new Dictionary<IntPtr, TextureInfo>();
        var loadedTextures = Resources.FindObjectsOfTypeAll(typeof(Texture));
        foreach (Texture t in loadedTextures)
        {
            var ptr = t.GetNativeTexturePtr();
            if (textSamples.ContainsKey(ptr))
            {
                continue;
            }
            TextureInfo texInfo = new TextureInfo();
            texInfo.name = t.name;
            texInfo.width = t.width;
            texInfo.height = t.height;
            texInfo.Bytes = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t);
            textSamples.Add(ptr, texInfo);
        }
        return textSamples;
    }
    private void LateUpdate()
    {
        int drawCall = UnityEditor.UnityStats.batches;
        if (mTempReport != null)
        {
            if (mTempReport.maxDrawCall < drawCall)
            {
                mTempReport.maxDrawCall = drawCall;
            }
        }
    }
    static void DeleteInactiveChildLeafNode(Transform n)
    {
        for(int i = n.childCount-1; i >= 0; i--)
        {
            Transform child = n.GetChild(i);
            DeleteInactiveChildLeafNode(child);
        }
        if (n.gameObject.activeSelf == false)
            UnityEngine.Object.DestroyImmediate(n.gameObject);
        else
        {
            ParticleSystem ps = n.GetComponent<ParticleSystem>();
            ParticleSystemRenderer psr = n.GetComponent<ParticleSystemRenderer>();
            if ((ps != null && ps.emission.enabled == false) || 
                (psr != null && psr.enabled == false))
            {
                UnityEngine.Object.DestroyImmediate(psr);
                UnityEngine.Object.DestroyImmediate(ps);
            }
        }
    }

    static int GetValidPSCount(GameObject fx, string fxPath)
    {
        if (fx == null || fx.activeInHierarchy == false)
        {
            Debug.Log( string.Format("fx in inactive:{0}", fxPath));
            return 0;
        }
        ParticleSystem[] ps = fx.GetComponentsInChildren<ParticleSystem>();
        int count = 0;
        foreach (var p in ps)
        {
            ParticleSystemRenderer psr = p.GetComponent<ParticleSystemRenderer>();
            if (psr.enabled && p.emission.enabled && p.gameObject.activeInHierarchy)
                count++;
        }
        return count;
    }
}

namespace UtilityOwn
{
    public static class Yield
    {
        public static readonly WaitForSecondsRealtime WaitForSecondsRealtime_1S = new WaitForSecondsRealtime(1f);
        public static readonly WaitForSeconds WaitForSeconds_Dot1S = new WaitForSeconds(0.1f);
        public static readonly WaitForFixedUpdate WaitForFixedUpdate = new WaitForFixedUpdate();
        public static readonly WaitForEndOfFrame WaitForEndOfFrame = new WaitForEndOfFrame();
    }
}
#endif