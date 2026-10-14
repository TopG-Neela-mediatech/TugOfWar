#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace AssetBundlePushPull
{
    // Tool Sharing: <ToolsRoot>/<ToolName>/... holds each shared tool's files.
    // Pulling places the tool folder at <chosen destination>/<ToolName>.
    public static class ToolManager
    {
        // Characters invalid in a folder name on Windows OR macOS, so a tool
        // pushed from one OS can always be pulled on the other.
        private static readonly char[] BadNameChars = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

        public static string[] ScanTools(string toolsRoot)
        {
            if (string.IsNullOrEmpty(toolsRoot) || !Directory.Exists(toolsRoot)) return new string[0];
            var list = new List<string>();
            try
            {
                foreach (string d in Directory.GetDirectories(toolsRoot))
                {
                    string n = Path.GetFileName(d);
                    // Skip hidden / NAS system folders (.Trashes, #recycle, @eaDir).
                    if (n.Length == 0 || n[0] == '.' || n[0] == '#' || n[0] == '@') continue;
                    list.Add(n);
                }
            }
            catch { }
            list.Sort(StringComparer.OrdinalIgnoreCase);   // GetDirectories order differs per OS
            return list.ToArray();
        }

        // Where the tool lives locally once pulled.
        public static string TargetPath(string dest, string tool) =>
            string.IsNullOrEmpty(dest) ? "" : Path.Combine(dest, tool);

        // Newest file time inside the tool on the server = "last updated".
        public static string LastUpdated(string toolsRoot, string tool)
        {
            try
            {
                var dir = new DirectoryInfo(Path.Combine(toolsRoot, tool));
                if (!dir.Exists) return "not available";
                DateTime newest = DateTime.MinValue;
                foreach (var f in PullManager.AllFiles(dir))
                    if (!PullManager.Ignored(f.Name) && f.LastWriteTime > newest) newest = f.LastWriteTime;
                return newest == DateTime.MinValue ? "empty" : newest.ToString("yyyy-MM-dd HH:mm");
            }
            catch { return "?"; }
        }

        public static SectionStatus Status(string toolsRoot, string tool, string dest)
        {
            string target = TargetPath(dest, tool);
            // No destination, or not pulled into this project yet.
            if (string.IsNullOrEmpty(target) || !Directory.Exists(target))
                return new SectionStatus { state = SectionState.NotInProject };
            return PullManager.Compare(toolsRoot, tool, "", target);
        }

        public static string ValidateName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Trim().Length == 0) return "Tool Name is empty.";
            if (name != name.Trim()) return "Tool Name can't start or end with a space.";
            if (name.IndexOfAny(BadNameChars) >= 0) return "Tool Name can't contain  \\ / : * ? \" < > |";
            if (name[0] == '.' || name[0] == '#' || name[0] == '@') return "Tool Name can't start with . # or @";
            if (name.EndsWith(".")) return "Tool Name can't end with a dot.";
            return null;
        }

        // Mirrors the local tool folder to <ToolsRoot>/<toolName>. Returns true if pushed.
        public static bool Push(string toolsRoot, string toolName, string localFolder)
        {
            if (string.IsNullOrEmpty(toolsRoot) || !Directory.Exists(toolsRoot))
            { EditorUtility.DisplayDialog("Push Tool", "Invalid Tools Root.", "OK"); return false; }
            if (string.IsNullOrEmpty(localFolder) || !Directory.Exists(localFolder))
            { EditorUtility.DisplayDialog("Push Tool", "Select the tool folder to share.", "OK"); return false; }
            string err = ValidateName(toolName);
            if (err != null) { EditorUtility.DisplayDialog("Push Tool", err, "OK"); return false; }

            string dest = Path.Combine(toolsRoot, toolName);
            if (IsSameOrInside(localFolder, dest) || IsSameOrInside(dest, localFolder))
            {
                EditorUtility.DisplayDialog("Push Tool", "The tool folder and the server copy overlap. Pick the tool's folder in your project.", "OK");
                return false;
            }
            if (Directory.Exists(dest) && !EditorUtility.DisplayDialog("Push Tool",
                    "'" + toolName + "' already exists on the server.\n\nUpdate it with your folder? Files that aren't in your folder will be removed from the server copy.",
                    "Update", "Cancel"))
                return false;

            FileUtility.Begin("Pushing tool...", FileUtility.CountFiles(localFolder));
            try
            {
                FileUtility.MirrorDirectory(localFolder, dest);
                // The tool folder's own .meta sits beside it on the server, so a
                // pull keeps the folder GUID. ScanTools lists folders only.
                CopyIfNewer(localFolder.TrimEnd('/', '\\') + ".meta", dest + ".meta");
            }
            finally { FileUtility.End(); }

            PushManager.Report("Push Tool");
            return true;
        }

        // Called after a pull: bring the tool folder's .meta along if the project
        // doesn't have one yet (never overwrite an existing local folder GUID).
        public static void CopyFolderMeta(string toolsRoot, string tool, string target)
        {
            string src = Path.Combine(toolsRoot, tool) + ".meta";
            string dst = target.TrimEnd('/', '\\') + ".meta";
            try { if (Directory.Exists(target) && File.Exists(src) && !File.Exists(dst)) File.Copy(src, dst); } catch { }
        }

        private static void CopyIfNewer(string src, string dst)
        {
            try
            {
                if (File.Exists(src) && (!File.Exists(dst) || File.GetLastWriteTimeUtc(src) != File.GetLastWriteTimeUtc(dst)))
                    File.Copy(src, dst, true);
            }
            catch (Exception ex) { FileUtility.Errors.Add(src + " : " + ex.Message); }
        }

        private static bool IsSameOrInside(string path, string parent)
        {
            try
            {
                string p = Path.GetFullPath(path).Replace('\\', '/').TrimEnd('/') + "/";
                string r = Path.GetFullPath(parent).Replace('\\', '/').TrimEnd('/') + "/";
                return p.StartsWith(r, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }
    }
}
#endif
