#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;

namespace AssetBundlePushPull
{
    public struct PullPlan
    {
        public List<string> added, replaced, removed;
    }

    public enum SectionState { NotInProject, NotOnServer, UpToDate, Changed, Unknown }

    public struct SectionStatus
    {
        public SectionState state;
        public int added, changed, removed;
    }

    public static class PullManager
    {
        private static HashSet<string> Names(string dir)
        {
            var set = new HashSet<string>();
            if (Directory.Exists(dir))
                foreach (string e in Directory.GetFileSystemEntries(dir))
                {
                    string n = Path.GetFileName(e);
                    if (!n.EndsWith(".meta")) set.Add(n);
                }
            return set;
        }

        public static PullPlan Plan(string serverRoot, string game, string sub, string dest)
        {
            string source = Path.Combine(Path.Combine(serverRoot, game), sub);
            var p = new PullPlan { added = new List<string>(), replaced = new List<string>(), removed = new List<string>() };
            var src = Names(source);
            var dst = Names(dest);
            foreach (string n in src) { if (dst.Contains(n)) p.replaced.Add(n); else p.added.Add(n); }
            foreach (string n in dst) if (!src.Contains(n)) p.removed.Add(n);
            return p;
        }

        public static string[] ScanGames(string serverRoot)
        {
            if (string.IsNullOrEmpty(serverRoot) || !Directory.Exists(serverRoot))
                return new string[0];

            string[] dirs = Directory.GetDirectories(serverRoot);
            var names = new string[dirs.Length];
            for (int i = 0; i < dirs.Length; i++)
                names[i] = Path.GetFileName(dirs[i]);
            return names;
        }

        public static string FolderDate(string serverRoot, string gameName, string sub)
        {
            string p = Path.Combine(Path.Combine(serverRoot, gameName), sub);
            if (!Directory.Exists(p)) return "not available";
            return Directory.GetLastWriteTime(p).ToString("yyyy-MM-dd HH:mm");
        }

        public static long SubTicks(string serverRoot, string gameName, string sub)
        {
            string p = Path.Combine(Path.Combine(serverRoot, gameName), sub);
            return Directory.Exists(p) ? Directory.GetLastWriteTime(p).Ticks : 0;
        }

        public static void RecordPull(string serverRoot, string gameName, bool didAb, bool didScript)
        {
            if (didAb) Settings.SetGameLastPullAb(gameName, SubTicks(serverRoot, gameName, "AssetBundles"));
            if (didScript) Settings.SetGameLastPullScript(gameName, SubTicks(serverRoot, gameName, "Scripts"));
            Settings.SetGamePulledOnce(gameName, true);
        }

        // ---------- change detection (server vs. this project's destination) ----------

        // Compares the actual files on the server with the files in the local
        // destination folder, recursively, so adds/removes/edits at any depth are
        // seen (a folder's own timestamp only reflects its direct children).
        // 'sharedSources' = other server folders pulled into this same destination
        // (e.g. AssetBundles and Scripts pointed at one folder, or two games sharing
        // one). Their files are expected locally, so they don't count as removed.
        public static SectionStatus Compare(string serverRoot, string gameName, string sub, string dest, List<string> sharedSources = null)
        {
            var st = new SectionStatus();
            if (string.IsNullOrEmpty(dest)) { st.state = SectionState.NotInProject; return st; }

            string source = Path.Combine(Path.Combine(serverRoot, gameName), sub);
            if (!Directory.Exists(source)) { st.state = SectionState.NotOnServer; return st; }

            try
            {
                var src = Snapshot(source);
                var dst = Directory.Exists(dest) ? Snapshot(dest) : new Dictionary<string, FileInfo>(PathComparer);

                foreach (var kv in src)
                {
                    if (!dst.TryGetValue(kv.Key, out var local)) st.added++;
                    else if (Differs(kv.Value, local)) st.changed++;
                }
                var others = new List<Dictionary<string, FileInfo>>();
                if (sharedSources != null)
                    foreach (string s in sharedSources)
                        if (Directory.Exists(s)) others.Add(Snapshot(s));

                foreach (string k in dst.Keys)
                {
                    if (src.ContainsKey(k)) continue;
                    bool providedElsewhere = false;
                    foreach (var o in others) if (o.ContainsKey(k)) { providedElsewhere = true; break; }
                    if (!providedElsewhere) st.removed++;
                }

                if (st.added + st.changed + st.removed > 0) st.state = SectionState.Changed;
                // Server folder exists but holds no files (nothing was pushed) ->
                // not "up to date", there's simply nothing to pull.
                else if (src.Count == 0) st.state = SectionState.NotOnServer;
                else st.state = SectionState.UpToDate;
            }
            catch { st.state = SectionState.Unknown; }   // share dropped / access denied
            return st;
        }

        // Header label for a game, from its two section states.
        public static string GameTag(SectionStatus ab, SectionStatus scr)
        {
            // No destination set for either -> the game isn't in this project yet.
            if (ab.state == SectionState.NotInProject && scr.state == SectionState.NotInProject) return "NEW";

            bool abNew = ab.state == SectionState.Changed;
            bool scrNew = scr.state == SectionState.Changed;
            if (abNew && scrNew) return "NEW UPDATE";
            if (abNew) return "NEW ASSETBUNDLE";
            if (scrNew) return "NEW SCRIPT";
            return "";
        }

        // True when two destination paths point at the same folder, regardless of
        // slash style, trailing separator or relative ("Assets/...") vs absolute.
        public static bool SameFolder(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            try
            {
                string fa = Path.GetFullPath(a).Replace('\\', '/').TrimEnd('/');
                string fb = Path.GetFullPath(b).Replace('\\', '/').TrimEnd('/');
                return PathComparer.Equals(fa, fb);
            }
            catch { return false; }
        }

        // Windows / macOS volumes are case-insensitive by default; Linux is not.
        // (No Unity API here: this runs on the background scanner thread.)
        private static readonly StringComparer PathComparer =
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

        private const int MaxDepth = 64;

        // All files under root. Unlike SearchOption.AllDirectories it never follows
        // symlinked folders (a symlink loop on macOS would otherwise never end) and
        // stops at MaxDepth as a hard backstop.
        public static IEnumerable<FileInfo> AllFiles(DirectoryInfo root)
        {
            var stack = new Stack<KeyValuePair<DirectoryInfo, int>>();
            stack.Push(new KeyValuePair<DirectoryInfo, int>(root, 0));
            while (stack.Count > 0)
            {
                var top = stack.Pop();
                foreach (var f in top.Key.GetFiles()) yield return f;
                if (top.Value >= MaxDepth) continue;
                foreach (var d in top.Key.GetDirectories())
                    if ((d.Attributes & FileAttributes.ReparsePoint) == 0)
                        stack.Push(new KeyValuePair<DirectoryInfo, int>(d, top.Value + 1));
            }
        }

        // Relative path ('/'-separated on every OS) -> file info, for all files under root.
        private static Dictionary<string, FileInfo> Snapshot(string root)
        {
            var map = new Dictionary<string, FileInfo>(PathComparer);
            var rootInfo = new DirectoryInfo(root);
            string rootFull = rootInfo.FullName.TrimEnd('/', '\\');
            foreach (var f in AllFiles(rootInfo))
            {
                if (Ignored(f.Name)) continue;
                string rel = f.FullName.Substring(rootFull.Length).TrimStart('/', '\\').Replace('\\', '/');
                map[rel] = f;
            }
            return map;
        }

        // .meta files are regenerated by Unity locally; the rest is OS clutter
        // (macOS writes .DS_Store and "._" AppleDouble files onto SMB shares).
        public static bool Ignored(string name)
        {
            return name.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("._", StringComparison.Ordinal)
                || string.Equals(name, ".DS_Store", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Thumbs.db", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "desktop.ini", StringComparison.OrdinalIgnoreCase);
        }

        // Copies keep the source timestamp, so an unchanged file matches. 2 s
        // tolerance absorbs coarse timestamps (Mono on macOS copies whole seconds,
        // FAT/some NAS round to 2 s).
        private static bool Differs(FileInfo a, FileInfo b)
        {
            return a.Length != b.Length
                || Math.Abs((a.LastWriteTimeUtc - b.LastWriteTimeUtc).TotalSeconds) > 2.0;
        }

        public static void Pull(string serverRoot, string gameName, string abDest, string scriptDest, bool pullAb, bool pullScript, bool mirror)
        {
            if (!pullAb && !pullScript)
            {
                EditorUtility.DisplayDialog("Pull", "Select AssetBundles and/or Scripts.", "OK");
                return;
            }
            if (string.IsNullOrEmpty(serverRoot) || !Directory.Exists(serverRoot))
            {
                EditorUtility.DisplayDialog("Pull", "Invalid Server Root.", "OK");
                return;
            }
            if (string.IsNullOrEmpty(gameName))
            {
                EditorUtility.DisplayDialog("Pull", "No game selected.", "OK");
                return;
            }

            string gameRoot = Path.Combine(serverRoot, gameName);
            string abSource = Path.Combine(gameRoot, "AssetBundles");
            string scriptSource = Path.Combine(gameRoot, "Scripts");

            bool doAb = pullAb && Directory.Exists(abSource) && !string.IsNullOrEmpty(abDest);
            bool doScript = pullScript && Directory.Exists(scriptSource) && !string.IsNullOrEmpty(scriptDest);

            int total = (doAb ? FileUtility.CountFiles(abSource) : 0) +
                        (doScript ? FileUtility.CountFiles(scriptSource) : 0);

            FileUtility.Begin("Pulling...", total);
            try
            {
                if (doAb) { if (mirror) FileUtility.MirrorContents(abSource, abDest); else FileUtility.CopyDirectory(abSource, abDest); }
                if (doScript) { if (mirror) FileUtility.MirrorContents(scriptSource, scriptDest); else FileUtility.CopyContentsReplacing(scriptSource, scriptDest); }
            }
            finally { FileUtility.End(); }

            if (FileUtility.Errors.Count == 0)
            {
                if (doAb) Settings.SetGameLastPullAb(gameName, SubTicks(serverRoot, gameName, "AssetBundles"));
                if (doScript) Settings.SetGameLastPullScript(gameName, SubTicks(serverRoot, gameName, "Scripts"));
                Settings.SetGamePulledOnce(gameName, true);
            }

            PushManager.Report("Pull");
        }
    }
}
#endif

