using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public class CgincDefineChange : UnityEditor.AssetModificationProcessor
{
    //private static void OnWillCreateAsset(string path)
    //{
    //    Debug.Log(path);
    //    path = path.Replace(".meta", "");
    //    if (path.EndsWith(".cginc"))
    //    {
    //        var name = Path.GetFileNameWithoutExtension(path);
    //        string sb = CalcDefine(name);

    //        string allText = File.ReadAllText(path);
    //        allText = allText.Replace("#XXLNAME#", sb);  //替换自定义宏
    //        File.WriteAllText(path, allText);
    //    }
    //}
    /// <summary>
    /// 根据大写字母和空格拆分字符串，然后拼接成字符串(Shader 中宏定义)
    /// 输入：MyLight 1       输出： MY_LIGHT_1
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private static string CalcDefine(string name)
    {
        Regex rgx = new Regex("[A-Z]");
        var s = rgx.Matches(name);
        StringBuilder sb = new StringBuilder();
        int start = 0;
        for (int i = 0; i < s.Count; i++)
        {
            var current = s[i];
            var len = s[i].Length;
            var next = s[i].NextMatch();
            var split = "_";
            if (next.Success)
            {
                len = next.Index - current.Index;
            }
            else
            {
                len = name.Length - current.Index;
            }
            sb.Append(name.Substring(start, len).ToUpper().Replace(" ", split) + split);
            start += len;
        }

        return sb.ToString();
    }
}
