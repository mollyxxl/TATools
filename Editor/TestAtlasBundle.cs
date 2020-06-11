using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Sprites;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public class TestAtlasBundle
{
    [MenuItem("XXL/TEstAB")]
    public static void TestABBundle()
    {
        var chat = new AssetBundleBuild();
        chat.assetBundleName = "NewChat";
        chat.assetNames = new string[] { "Assets/Res/Gui/Share/Texture/New_Chat/btn_liaotian_dengji.png",
            "Assets/Res/Gui/Share/Texture/New_Chat/btn_liaotian_duigou.png",
            "Assets/Res/Gui/Share/Texture/New_Chat/btn_liaotian_fasong.png",
            //"Assets/Res/Gui/Share/Texture/New_Chat/btn_liaotian_duigoukuang.png",
            //"Assets/Res/Gui/Share/Texture/New_Chat/btn_liaotian_duihuakuang1.png",
            //"Assets/Res/Gui/Share/Texture/New_Chat/btn_liaotian_duihuakuang2.png",
            //"Assets/Res/Gui/Share/Texture/New_Chat/btn_liaotian_shangtubiao2.png",
           };
        BuildPipeline.BuildAssetBundles("Assets/TestAB", new AssetBundleBuild[] { chat }, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows);
    }
    [MenuItem("XXL/RebuildAtlas")]
    public static void RebuildSelectAtlas()
    {
        //SpriteAtlasUtility.PackAllAtlases(BuildTarget.StandaloneWindows);
        Packer.RebuildAtlasCacheIfNeeded(BuildTarget.StandaloneWindows, true, Packer.Execution.ForceRegroup);
    }
}
