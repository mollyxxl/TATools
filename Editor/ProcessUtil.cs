using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;

public static class ProcessUtil
{
    public class ProcessOutput
    {
        public int ExitCode { get; private set; }
        public double Duration { get; private set; }
        public string StandardOutput { get; private set; }
        public string StandardError { get; private set; }

        private System.Text.StringBuilder output = new System.Text.StringBuilder();
        private System.Text.StringBuilder error = new System.Text.StringBuilder();

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendFormat("ExitCode:{0}, Duration:{1}", ExitCode, Duration).AppendLine();
            sb.AppendLine(StandardOutput);
            if (!string.IsNullOrEmpty(StandardError))
            {
                sb.AppendLine("----StandardError----");
                sb.Append(StandardError);
            }
            return sb.ToString();
        }

        public void OutputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            if (output != null && !string.IsNullOrEmpty(e.Data))
            {
                output.AppendLine(e.Data);
            }
        }

        public void ErrorDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            if (output != null && !string.IsNullOrEmpty(e.Data))
            {
                error.AppendLine(e.Data);
            }
        }

        public void OnExit(int exitCode, double time)
        {
            ExitCode = exitCode;
            Duration = time;

            StandardOutput = output.ToString();
            output.Remove(0, output.Length);
            output = null;

            StandardError = error.ToString();
            error.Remove(0, error.Length);
            error = null;
        }
    }

    public static ProcessOutput StartProcess(string fileName, string command, string workingDir = null, int timeoutMiliSeconds = 0)
    {
        var startinfo = new System.Diagnostics.ProcessStartInfo();
        startinfo.FileName = fileName;
        startinfo.UseShellExecute = false;
        startinfo.RedirectStandardOutput = true;
        startinfo.RedirectStandardError = true;
        startinfo.CreateNoWindow = true;
        startinfo.Arguments = command;

        if (!string.IsNullOrEmpty(workingDir) && Directory.Exists(workingDir))
        {
            startinfo.WorkingDirectory = workingDir;
        }

        /*
         * Process的StandardOutput和WaitForExit的使用不当，有可能导致死锁，参考如下：
         * https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.standardoutput?redirectedfrom=MSDN&view=netframework-4.8#System_Diagnostics_Process_StandardOutput
         */
        var startTime = DateTime.Now;
        using (var process = System.Diagnostics.Process.Start(startinfo))
        {
            var output = new ProcessOutput();
            process.OutputDataReceived += output.OutputDataReceived;
            process.ErrorDataReceived += output.OutputDataReceived;
            process.BeginOutputReadLine();

            if (timeoutMiliSeconds > 0)
            {
                process.WaitForExit(timeoutMiliSeconds);
            }
            else
            {
                process.WaitForExit();
            }
            //force process to exit when timeout
            if (!process.HasExited)
            {
                process.Kill();
                process.WaitForExit();
            }

            process.CancelOutputRead();
            output.OnExit(process.ExitCode, (process.ExitTime - startTime).TotalSeconds);
            return output;
        }
    }

    public static class SVN
    {
        public static ProcessOutput Execute(string command, string wcpath)
        {
            var SVN_USER = System.Environment.GetEnvironmentVariable("SVN_USER");
            var SVN_PWD = System.Environment.GetEnvironmentVariable("SVN_PWD");
            var globalOptions = "--trust-server-cert-failures=unknown-ca --no-auth-cache --non-interactive ";
            if (!string.IsNullOrEmpty(SVN_USER) && !string.IsNullOrEmpty(SVN_PWD))
            {
                globalOptions = string.Format("{0}--username {1} --password {2} ", globalOptions, SVN_USER, SVN_PWD);
            }
            var output = StartProcess("svn", globalOptions + command, wcpath);
            if (output.ExitCode != 0)
            {
                Debug.LogErrorFormat("svn execution failed with exit code:{0}", output.ExitCode);
            }
            return output;
        }

        public struct ProgressInfo
        {
            public string title;
            public string message;
            public ProgressInfo(string t, string m)
            {
                title = t;
                message = m;
            }
        }

        public static IEnumerable<ProgressInfo> ResetWorkingCopy(string wcpath)
        {
            //break all locks
            yield return new ProgressInfo("cleanup", wcpath);
            Execute("cleanup -q --include-externals ./", wcpath);

            //resolve conflict status
            yield return new ProgressInfo("resolve", wcpath);
            Execute("resolve -q -R --accept working ./", wcpath);
            //revert all changes
            yield return new ProgressInfo("revert", wcpath);
            Execute("revert -q -R ./", wcpath);

            //remove unversioned files out of externals
            yield return new ProgressInfo("remove unversioned", wcpath);
            var output = Execute("status --ignore-externals ./", wcpath);
            if (output.ExitCode == 0)
            {
                using (var rd = new StringReader(output.StandardOutput))
                {
                    //7 status + 1 space + path
                    const int COLUMNS_HEADER_COUNT = 7 + 1;
                    while (rd.Peek() > 0)
                    {
                        var line = rd.ReadLine();
                        if (line.Length > 0)
                        {
                            var isUnversioned = line[0] == '?';
                            if (isUnversioned)
                            {
                                line = line.Substring(COLUMNS_HEADER_COUNT);
                                if (Directory.Exists(line))
                                {
                                    Directory.Delete(line, true);
                                }
                                else if (File.Exists(line))
                                {
                                    File.Delete(line);
                                }
                            }
                        }
                    }
                }
            }

            var externals = GetExternals(wcpath);
            foreach (var key in externals.Keys)
            {
                var path = Path.Combine(wcpath, key);
                //resolve conflict status
                yield return new ProgressInfo("resolve", key);
                Execute("resolve -q -R --accept working ./", path);
                //revert all changes
                yield return new ProgressInfo("revert", key);
                Execute("revert -q -R ./", path);
                //remove unversioned files
                yield return new ProgressInfo("cleanup", key);
                Execute("cleanup -q --remove-unversioned ./", path);
            }
            yield return new ProgressInfo("complete", "working copy reset");
        }

        static Dictionary<string, string> GetExternals(string wcpath)
        {
            var result = new Dictionary<string, string>();
            var output = Execute("propget svn:externals ./", wcpath);
            if (output.ExitCode == 0)
            {
                using (var rd = new StringReader(output.StandardOutput))
                {
                    while (rd.Peek() > 0)
                    {
                        var line = rd.ReadLine();
                        if (line.Length > 0)
                        {
                            var tokens = line.Split(' ');
                            if (tokens.Length == 2)
                            {
                                var url = tokens[0];
                                var path = tokens[1];
                                result.Add(path, url);
                            }
                        }
                    }
                }
            }
            return result;
        }
    }

    static class EditorMenus
    {
        [MenuItem("Tools/SVN/更新工程")]
        public static void SVN_Update()
        {
            try
            {
                EditorUtility.DisplayProgressBar("SVN", "Update", 0.5f);
                var output = SVN.Execute("update --force ./", Path.GetDirectoryName(Application.dataPath));
                Debug.Log(output);
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Tools/SVN/清理工程")]
        public static void SVN_Cleanup()
        {
            try
            {
                EditorUtility.DisplayProgressBar("SVN", "Cleanup", 0.5f);
                var output = SVN.Execute("cleanup --include-externals ./", Path.GetDirectoryName(Application.dataPath));
                Debug.Log(output);
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Tools/SVN/重置工程")]
        public static void SVN_Reset()
        {
            try
            {
                if (EditorUtility.DisplayDialog(
                    "重置工程", "老铁！需要重置工程吗？将会删除【所有】未提交文件，回退【所有】本地修改，注意防止丢失工作内容。", "搞起", "取消"))
                {
                    EditorUtility.DisplayProgressBar("SVN", "Reset", 0.5f);
                    foreach (var info in SVN.ResetWorkingCopy(Path.GetDirectoryName(Application.dataPath)))
                    {
                        EditorUtility.DisplayProgressBar(info.title, info.message, 0.5f);
                    }
                    AssetDatabase.Refresh();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}