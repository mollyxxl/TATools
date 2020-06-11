using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class FindAssetReferences : EditorWindow
{
    class CheckRule
    {
        public bool check;
        public bool meta;
        public CheckRule(bool check, bool meta) { this.check = check; this.meta = meta; }
    }

    Dictionary<string, CheckRule> _checkPatterns = new Dictionary<string, CheckRule>()
    {
        { "*.prefab" , new CheckRule(true, false) },
        { "*.asset" , new CheckRule(true, false) },
        { "*.unity" , new CheckRule(true, false) },
        { "*.mat" , new CheckRule(true, false) },
        { "*.fontsettings" , new CheckRule(false, false) },
        { "*.shadervariants" , new CheckRule(false, false) },
        { "*.shader" , new CheckRule(false, true) },
        { "*.cs" , new CheckRule(false, true) },
    };
    HashSet<string> _targetAssetGuids = new HashSet<string>();
    public void OnGUI()
    {
        foreach (var item in _checkPatterns)
        {
            var pattern = item.Key;
            var rule = item.Value;
            if (rule.meta)
            {
                pattern += " (.meta)";
            }
            rule.check = GUILayout.Toggle(rule.check, pattern );
        }

        if (GUILayout.Button("Find References (Inversed Dependency: find objects depends on selected, only YAML format)"))
        {
            var searchPatterns = new HashSet<string>();
            foreach (var pattern in _checkPatterns)
            {
                if (pattern.Value.check)
                {
                    searchPatterns.Add(pattern.Key);
                }
            }
            FindReferencesThreading(_targetAssetGuids, searchPatterns);
        }

        if (GUILayout.Button("Ripgrep Match FileName"))
        {
            FindReferencesRipgrep(true, false);
        }

        if (GUILayout.Button("Ripgrep Match GUID"))
        {
            FindReferencesRipgrep(false, true);
        }

        if (GUILayout.Button("Ripgrep Match (FileName & Guid)"))
        {
            FindReferencesRipgrep(true, true);
        }

        foreach (var guid in _targetAssetGuids)
        {
            GUILayout.Label(AssetDatabase.GUIDToAssetPath(guid));
        }
    }

    void ShowWindow(Object[] selection)
    {
        _targetAssetGuids.Clear();
        foreach (var o in selection)
        {
            var path = AssetDatabase.GetAssetPath(o);
            if (!string.IsNullOrEmpty(path))
            {
                var guid = AssetDatabase.AssetPathToGUID(path);
                _targetAssetGuids.Add(guid);
            }
        }

        //TODO SetupFilter from selection asset type

        ShowPopup();
    }

    void FindReferences(HashSet<string> targetGuids, HashSet<string> searchPatterns)
    {
        if (targetGuids.Count == 0 || searchPatterns.Count == 0)
        {
            return;
        }

        var startNow = System.DateTime.Now;
        var totalCount = 0;
        Debug.LogFormat("[FindReferences] start");
        try
        {
            Dictionary<string, List<string>> results = new Dictionary<string, List<string>>();
            foreach (var guid in targetGuids)
            {
                results.Add(guid, new List<string>());
            }
            foreach (var pattern in searchPatterns)
            {
                var files = Directory.GetFiles(Application.dataPath, pattern, SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    var fn = files[i];
                    if (EditorUtility.DisplayCancelableProgressBar("Find References", fn, (float)i / (float)files.Length))
                    {
                        break;
                    }

                    var isMeta = _checkPatterns[pattern].meta;
                    if (isMeta)
                    {
                        fn += ".meta";
                    }
                    else
                    {
                        //check for YAML format, skip binaray files
                        using (var fs = new FileStream(fn, FileMode.Open))
                        {
                            if (fs.ReadByte() != '%' ||
                                fs.ReadByte() != 'Y' ||
                                fs.ReadByte() != 'A' ||
                                fs.ReadByte() != 'M' ||
                                fs.ReadByte() != 'L')
                            {
                                continue;
                            }
                        }
                    }

                    //for big file that can't read all content once
                    using (var fs = new StreamReader(fn))
                    {
                        while (!fs.EndOfStream)
                        {
                            var ln = fs.ReadLine();
                            foreach (var guid in targetGuids)
                            {
                                if (Regex.IsMatch(ln, guid))
                                {
                                    var path = UnityEditor.FileUtil.GetProjectRelativePath(fn);
                                    if (isMeta)
                                    {
                                        path = path.Remove(path.Length - ".meta".Length);
                                    }
                                    results[guid].Add(path);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            foreach (var item in results)
            {
                var guid = item.Key;
                totalCount += item.Value.Count;
                Debug.LogFormat("{0} references found for {1}", item.Value.Count, AssetDatabase.GUIDToAssetPath(item.Key));
                foreach (var path in item.Value)
                {
                    Debug.LogFormat(AssetDatabase.LoadAssetAtPath<Object>(path),
                        "{0} referenced by {1}", AssetDatabase.GUIDToAssetPath(guid), path);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            Debug.LogFormat("[FindReferences] total references count {0}, used in {1:N2} seconds", totalCount, (System.DateTime.Now - startNow).TotalSeconds);
        }
    }

    void FindReferencesThreading(HashSet<string> targetGuids, HashSet<string> searchPatterns)
    {
        if (targetGuids.Count == 0 || searchPatterns.Count == 0)
        {
            return;
        }

        var startNow = System.DateTime.Now;
        var totalCount = 0;
        Debug.LogFormat("[FindReferences] start");
        try
        {
            Dictionary<string, List<string>> results = new Dictionary<string, List<string>>();
            foreach (var guid in targetGuids)
            {
                results.Add(guid, new List<string>());
            }

            var checkList = new HashSet<string>();
            foreach (var pattern in searchPatterns)
            {
                var files = Directory.GetFiles(Application.dataPath, pattern, SearchOption.AllDirectories);
                foreach (var fn in files)
                {
                    checkList.Add(fn);
                }
            }

            var ProjectPathIndex = Path.GetDirectoryName(Application.dataPath).Length + 1;
            var checkedCount = 0;
            var mtx = new System.Threading.Mutex();
            foreach (var fn in checkList)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(state =>
                {
                    //check for YAML format, skip binaray files
                    using (var fs = new FileStream(fn, FileMode.Open))
                    {
                        if (fs.ReadByte() != '%' ||
                            fs.ReadByte() != 'Y' ||
                            fs.ReadByte() != 'A' ||
                            fs.ReadByte() != 'M' ||
                            fs.ReadByte() != 'L')
                        {
                            mtx.WaitOne();
                            ++checkedCount;
                            mtx.ReleaseMutex();
                            return;
                        }
                    }

                    //for big file that can't read all content once
                    using (var fs = new StreamReader(fn))
                    {
                        while (!fs.EndOfStream)
                        {
                            var ln = fs.ReadLine();
                            foreach (var guid in targetGuids)
                            {
                                if (Regex.IsMatch(ln, guid))
                                {
                                    //FileUtil.GetProjectRelativePath can only used in main thread
                                    results[guid].Add(fn.Substring(ProjectPathIndex).Replace('\\', '/'));
                                    break;
                                }
                            }
                        }
                    }

                    mtx.WaitOne();
                    ++checkedCount;
                    mtx.ReleaseMutex();
                });
            }

            while (true)
            {
                var progress = (float)checkedCount / checkList.Count;
                EditorUtility.DisplayCancelableProgressBar("Find References", string.Format("{0}/{1}", checkedCount, checkList.Count), progress);
                if (checkedCount >= checkList.Count)
                {
                    break;
                }
                System.Threading.Thread.Sleep(200);
            }

            foreach (var item in results)
            {
                var guid = item.Key;
                totalCount += item.Value.Count;
                Debug.LogFormat("{0} references found for {1}", item.Value.Count, AssetDatabase.GUIDToAssetPath(item.Key));
                foreach (var path in item.Value)
                {
                    Debug.LogFormat(AssetDatabase.LoadAssetAtPath<Object>(path),
                        "{0} referenced by {1}", AssetDatabase.GUIDToAssetPath(guid), path);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            Debug.LogFormat("[FindReferences] total references count {0}, used in {1:N2} seconds", totalCount, (System.DateTime.Now - startNow).TotalSeconds);
        }
    }

    void FindReferencesRipgrep(bool matchFileName, bool matchGuid)
    {
        if (_targetAssetGuids.Count > 0 && (matchFileName || matchGuid))
        {
            string extension = string.Empty;
            foreach (var item in _checkPatterns)
            {
                if (item.Value.check)
                {
                    extension += string.Format("-g \"{0}\" ", item.Key);
                }
            }
            var sb = new System.Text.StringBuilder();
            sb.Append("-e \"\\b(");
            var count = 1;
            foreach (var guid in _targetAssetGuids)
            {
                var fileName = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid));
                if (matchFileName && matchGuid)
                {
                    sb.Append(fileName).Append('|').Append(guid);
                }
                else if (matchFileName)
                {
                    sb.Append(fileName);
                }
                else if (matchGuid)
                {
                    sb.Append(guid);
                }
                if (count++ < _targetAssetGuids.Count)
                {
                    sb.Append('|');
                }
            }
            sb.AppendFormat(")\\b\" \"{0}\" {1} -g \"!*.meta\"", Application.dataPath,extension);
            string PROG =Path.Combine(Application.dataPath, "XXL/Tools/ripgrep/rg.exe");
           // Debug.Log("PROG: " + PROG);
            var command = sb.ToString();
            var beginTime = System.DateTime.Now;
            var result = ProcessUtil.StartProcess(PROG, command);
            sb.Insert(0, " ").Insert(0, PROG);
            sb.AppendLine().AppendLine().Append(result);
            Debug.Log(sb.ToString());
            Debug.LogFormat("FindReferences finished in:{0}s", (System.DateTime.Now - beginTime).TotalSeconds);
        }
    }

    [MenuItem("Assets/Find Asset References ALL", false, 30)]
    static void Find()
    {
        GetWindow<FindAssetReferences>(true).ShowWindow(Selection.objects);
    }

    [MenuItem("Assets/Find Asset References ALL", true)]
    static bool VFind()
    {
        var path = AssetDatabase.GetAssetPath(Selection.activeObject);
        return (!string.IsNullOrEmpty(path) && !AssetDatabase.IsValidFolder(path));
    }
}