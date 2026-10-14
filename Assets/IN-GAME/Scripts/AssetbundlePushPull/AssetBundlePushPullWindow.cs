#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetBundlePushPull
{
    public class AssetBundlePushPullWindow : EditorWindow
    {
        private int _tab;
        private readonly string[] _tabs = { "Push", "Pull", "Tool Sharing" };

        private List<string> _scripts;
        private Vector2 _scroll;

        private string[] _games = new string[0];
        private bool _pullAb = true;
        private bool _pullScript = true;
        private string _scannedRoot;
        private Vector2 _gameScroll;

        // Cached so OnGUI never hits disk / allocates styles every repaint.
        private bool _online;
        private bool _stylesReady;
        private GUIStyle _statusStyle, _tagStyle, _notInProjectStyle, _upToDateStyle;
        private struct GameInfo
        {
            public string abDate, scrDate;
            public SectionStatus ab, scr;
            public GUIContent tag, abLabel, scrLabel;   // cached; tooltips carry the details
        }
        private readonly Dictionary<string, GameInfo> _info = new Dictionary<string, GameInfo>();

        // Tool Sharing tab state (same caching rules as games).
        private string[] _tools = new string[0];
        private string _scannedToolsRoot;
        private bool _toolsOnline, _toolsParentExists;
        private Vector2 _toolScroll;
        private struct ToolInfo { public string date; public SectionStatus st; public GUIContent label; }
        private readonly Dictionary<string, ToolInfo> _toolInfo = new Dictionary<string, ToolInfo>();

        // ---- background scanning ----
        // ALL server reads happen on a worker thread (ServerScanner); OnGUI only
        // draws the last result. One scan runs at a time; requests made while one
        // is running are coalesced into a single follow-up scan.
        private const double RecheckDelay = 0.6;   // after typing a path
        private const double SlowAfter = 8.0;      // "not responding" hint
        private int _scanGen;
        private bool _scanning, _rescanWanted, _hasResult;
        private double _scanStarted, _scanDueAt = -1, _lastScanDone;
        private string _resultServerRoot, _resultToolsRoot;
        private volatile ScanResult _incoming;

        [MenuItem("Tools/AssetBundle Push Pull")]
        private static void Open()
        {
            GetWindow<AssetBundlePushPullWindow>("AssetBundle Push Pull").minSize = new Vector2(500, 400);
        }

        // Called after an import elsewhere (confirm window) so labels update at once.
        public static void RefreshOpenWindows()
        {
            foreach (var w in Resources.FindObjectsOfTypeAll<AssetBundlePushPullWindow>())
                w.RequestScan();
        }

        private void OnEnable()
        {
            _scripts = Settings.ScriptList;
            RequestScan();
        }

        // Re-check when returning to the window (e.g. after a Pull). Throttled so
        // focus flicker (dialogs, macOS) doesn't queue scans back to back.
        private void OnFocus()
        {
            if (!_scanning && EditorApplication.timeSinceStartup - _lastScanDone > 1.5) RequestScan();
        }

        private void OnInspectorUpdate()
        {
            double now = EditorApplication.timeSinceStartup;

            var r = _incoming;
            if (r != null)
            {
                _incoming = null;
                _scanning = false;
                _lastScanDone = now;
                if (r.gen == _scanGen) ApplyResult(r);
                if (_rescanWanted) StartScan();
                Repaint();
            }

            if (_scanDueAt >= 0 && now >= _scanDueAt) { _scanDueAt = -1; RequestScan(); }

            // Keep the "checking / not responding" text current while waiting.
            if (_scanning && now - _scanStarted > 0.5) Repaint();
        }

        // Scan now, or right after the running scan finishes.
        private void RequestScan()
        {
            if (_scanning) { _rescanWanted = true; return; }
            StartScan();
        }

        // Scan shortly, restarting the delay on every call (used while typing).
        private void ScheduleScan() => _scanDueAt = EditorApplication.timeSinceStartup + RecheckDelay;
        private void ScheduleRecheck(string g) => ScheduleScan();
        private void ScheduleToolRecheck(string t) => ScheduleScan();

        private void StartScan()
        {
            _scanning = true;
            _rescanWanted = false;
            _scanDueAt = -1;
            _scanStarted = EditorApplication.timeSinceStartup;
            int gen = ++_scanGen;
            // Inputs are captured here, on the main thread.
            string serverRoot = Settings.ServerRoot, toolsRoot = Settings.ToolsRoot;
            var snapshot = Settings.Snapshot();
            _scannedRoot = serverRoot;
            _scannedToolsRoot = toolsRoot;

            var self = this;
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                ScanResult res;
                try { res = ServerScanner.Run(gen, serverRoot, toolsRoot, snapshot); }
                catch { res = new ScanResult { gen = gen, serverRoot = serverRoot, toolsRoot = toolsRoot }; }
                self._incoming = res;   // picked up by OnInspectorUpdate on the main thread
            });
        }

        private void ApplyResult(ScanResult r)
        {
            _hasResult = true;
            _resultServerRoot = r.serverRoot;
            _resultToolsRoot = r.toolsRoot;

            _online = r.online;
            _games = r.games;
            _info.Clear();
            foreach (var kv in r.gameData) _info[kv.Key] = MakeGameInfo(kv.Value);

            _toolsOnline = r.toolsOnline;
            _toolsParentExists = r.toolsParentExists;
            _tools = r.tools;
            _toolInfo.Clear();
            foreach (var kv in r.toolData) _toolInfo[kv.Key] = MakeToolInfo(kv.Value, Settings.ToolDest(kv.Key));
        }

        // Status line text/colour shared by the Pull and Tools tabs.
        private string StatusText(string root, string resultRoot, bool online, string what, out Color color)
        {
            color = new Color(0.9f, 0.3f, 0.3f);
            if (string.IsNullOrEmpty(root)) return what + " root not set.";
            bool pending = !_hasResult || resultRoot != root;
            if (pending || (_scanning && !online))
            {
                color = new Color(0.95f, 0.75f, 0.3f);
                return EditorApplication.timeSinceStartup - _scanStarted > SlowAfter && _scanning
                    ? "● " + what + " not responding... still trying"
                    : "● Checking " + what.ToLowerInvariant() + "...";
            }
            if (!online) return "● " + what + " offline / not connected";
            color = new Color(0.3f, 0.8f, 0.3f);
            return "● " + what + " connected" + (_scanning ? "  (refreshing...)" : "");
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;
            _statusStyle = new GUIStyle(EditorStyles.miniBoldLabel);
            _tagStyle = new GUIStyle(EditorStyles.miniBoldLabel) { normal = { textColor = new Color(0.3f, 0.8f, 0.3f) } };
            _notInProjectStyle = new GUIStyle(EditorStyles.miniBoldLabel) { normal = { textColor = new Color(0.95f, 0.6f, 0.25f) } };
            _upToDateStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.5f, 0.5f, 0.5f) } };
        }

        private void OnGUI()
        {
            EnsureStyles();
            _tab = GUILayout.Toolbar(_tab, _tabs);
            EditorGUILayout.Space();

            if (_tab == 0) DrawPush();
            else if (_tab == 1) DrawPull();
            else DrawTools();
        }

        private void DrawPush()
        {
            Settings.ServerRoot = PathField("Server Root Path", Settings.ServerRoot);
            DrawServerStatus();
            Settings.GameName = EditorGUILayout.TextField("Game Name", Settings.GameName);
            Settings.AssetBundleFolder = PathField("AssetBundle Folder", Settings.AssetBundleFolder);
            if (string.IsNullOrEmpty(Settings.AssetBundleFolder))
                EditorGUILayout.LabelField(" ", "Optional: empty = push scripts only (server AssetBundles untouched).", EditorStyles.miniLabel);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scripts / Assets to Share", EditorStyles.boldLabel);
            DrawScriptList();

            EditorGUILayout.Space();
            if (GUILayout.Button("Push", GUILayout.Height(30)))
            {
                Settings.ScriptList = _scripts;
                PushManager.Push(Settings.ServerRoot, Settings.GameName, Settings.AssetBundleFolder, _scripts);
            }
        }

        private void DrawScriptList()
        {
            Rect drop = EditorGUILayout.GetControlRect(false, 40);
            GUI.Box(drop, "Drag & Drop folders / assets here");
            HandleDrop(drop);

            int up = -1, down = -1, remove = -1;
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(160));
            for (int i = 0; i < _scripts.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(_scripts[i]);
                using (new EditorGUI.DisabledScope(i == 0))
                    if (GUILayout.Button("▲", GUILayout.Width(24))) up = i;
                using (new EditorGUI.DisabledScope(i == _scripts.Count - 1))
                    if (GUILayout.Button("▼", GUILayout.Width(24))) down = i;
                if (GUILayout.Button("X", GUILayout.Width(24))) remove = i;
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            if (up > 0) Swap(up, up - 1);
            else if (down >= 0) Swap(down, down + 1);
            else if (remove >= 0) { _scripts.RemoveAt(remove); Save(); }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Folder")) AddPath(EditorUtility.OpenFolderPanel("Add Folder", "", ""));
            if (GUILayout.Button("Add File")) AddPath(EditorUtility.OpenFilePanel("Add File", "", ""));
            EditorGUILayout.EndHorizontal();
        }

        private void HandleDrop(Rect rect)
        {
            Event e = Event.current;
            if (!rect.Contains(e.mousePosition)) return;
            if (e.type == EventType.DragUpdated) DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            else if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (string p in DragAndDrop.paths) AddPath(p);
                e.Use();
            }
        }

        private void AddPath(string p)
        {
            if (!string.IsNullOrEmpty(p) && !_scripts.Contains(p)) { _scripts.Add(p); Save(); }
        }

        private void Swap(int a, int b) { (_scripts[a], _scripts[b]) = (_scripts[b], _scripts[a]); Save(); }
        private void Save() => Settings.ScriptList = _scripts;

        private void DrawServerStatus()
        {
            // Root typed/changed (Push or Pull tab) -> rescan once typing pauses.
            if (Settings.ServerRoot != _scannedRoot && _scanDueAt < 0 && !_scanning) ScheduleScan();

            string msg = StatusText(Settings.ServerRoot, _resultServerRoot, _online, "Server", out Color c);
            _statusStyle.normal.textColor = c;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(msg, _statusStyle);
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(_scanning))
                if (GUILayout.Button("Refresh", EditorStyles.miniButton, GUILayout.Width(64)))
                    RequestScan();   // fresh connection check + comparison, in the background
            EditorGUILayout.EndHorizontal();
        }

        // Builds the display strings for a scanned game (main thread: GUIContent).
        private static GameInfo MakeGameInfo(GameData d)
        {
            var gi = new GameInfo { abDate = d.abDate, scrDate = d.scrDate, ab = d.ab, scr = d.scr };
            gi.abLabel = SectionLabel(gi.ab, "NEW ASSETBUNDLE", "AssetBundles");
            gi.scrLabel = SectionLabel(gi.scr, "NEW SCRIPT", "Scripts");

            string tag = PullManager.GameTag(gi.ab, gi.scr);
            string tip = tag == "NEW"
                ? "This game isn't in this project yet. Set a destination folder to pull it."
                : Join(Details(gi.ab, "AssetBundles"), Details(gi.scr, "Scripts"));
            gi.tag = string.IsNullOrEmpty(tag) ? GUIContent.none : new GUIContent("● " + tag, tip);
            return gi;
        }

        private static GUIContent SectionLabel(SectionStatus s, string newText, string what)
        {
            switch (s.state)
            {
                case SectionState.NotInProject: return new GUIContent("NOT IN PROJECT", "No destination folder set for " + what + " in this project.");
                case SectionState.Changed:      return new GUIContent(newText, Details(s, what));
                case SectionState.UpToDate:     return new GUIContent("Up to date", what + " match the server.");
                case SectionState.NotOnServer:  return new GUIContent("Nothing on server", "No " + what + " have been pushed to the server for this game.");
                case SectionState.Unknown:      return new GUIContent("?", "Couldn't read the server or destination folder.");
                default:                        return GUIContent.none;
            }
        }

        private static string Details(SectionStatus s, string what)
        {
            if (s.state != SectionState.Changed) return "";
            var parts = new List<string>();
            if (s.added > 0) parts.Add(s.added + " new");
            if (s.changed > 0) parts.Add(s.changed + " changed");
            if (s.removed > 0) parts.Add(s.removed + " removed on server");
            return what + ": " + string.Join(", ", parts);
        }

        private static string Join(string a, string b) =>
            string.IsNullOrEmpty(a) ? b : string.IsNullOrEmpty(b) ? a : a + "\n" + b;

        private GUIStyle LabelStyle(SectionState s) =>
            s == SectionState.NotInProject ? _notInProjectStyle : s == SectionState.Changed ? _tagStyle : _upToDateStyle;

        private static string PathField(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            value = EditorGUILayout.TextField(label, value);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string picked = EditorUtility.OpenFolderPanel(label, value, "");
                if (!string.IsNullOrEmpty(picked)) value = picked;
            }
            EditorGUILayout.EndHorizontal();
            return value;
        }

        private void DrawPull()
        {
            EditorGUILayout.BeginHorizontal();
            string rootBefore = Settings.ServerRoot;
            Settings.ServerRoot = EditorGUILayout.TextField("Server Root", rootBefore);
            if (Settings.ServerRoot != rootBefore) ScheduleScan();
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string picked = EditorUtility.OpenFolderPanel("Server Root", Settings.ServerRoot, "");
                if (!string.IsNullOrEmpty(picked)) { Settings.ServerRoot = picked; RequestScan(); }
            }
            if (GUILayout.Button("Scan", GUILayout.Width(50))) RequestScan();
            EditorGUILayout.EndHorizontal();

            DrawServerStatus();
            if (!_online) return;

            if (_games.Length == 0)
            {
                EditorGUILayout.HelpBox("No games found on server.", MessageType.Info);
                return;
            }

            _gameScroll = EditorGUILayout.BeginScrollView(_gameScroll);
            foreach (string g in _games) DrawGameBlock(g);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            if (GUILayout.Button("Pull All Selected", GUILayout.Height(30)))
                PullSelected();
        }

        // Batch-pulls every ticked game in one pass, then refreshes the
        // AssetDatabase a single time (instead of once per game).
        private void PullSelected()
        {
            string root = Settings.ServerRoot;
            int pulled = 0;
            foreach (string g in _games)
            {
                if (!Settings.GameSelected(g)) continue;
                bool ab = Settings.GamePullAb(g);
                bool scr = Settings.GamePullScript(g);
                if (!ab && !scr) continue;
                string abDest = Settings.GameAbDest(g), scrDest = Settings.GameScriptDest(g);
                if (ab && string.IsNullOrEmpty(abDest))
                { EditorUtility.DisplayDialog("Pull All Selected", g + ": AssetBundle destination path is empty. Set it before pulling.", "OK"); return; }
                if (scr && string.IsNullOrEmpty(scrDest))
                { EditorUtility.DisplayDialog("Pull All Selected", g + ": Script destination path is empty. Set it before pulling.", "OK"); return; }
                PullManager.Pull(root, g, abDest, scrDest, ab, scr, false);
                pulled++;
            }

            if (pulled == 0)
            {
                EditorUtility.DisplayDialog("Pull All Selected",
                    "Tick at least one game (with AssetBundles and/or Scripts enabled).", "OK");
                return;
            }

            AssetDatabase.Refresh();
            RequestScan();
            Repaint();
        }

        private void DrawGameBlock(string g)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            Settings.SetGameSelected(g, EditorGUILayout.Toggle(Settings.GameSelected(g), GUILayout.Width(18)));
            bool exp = EditorGUILayout.Foldout(Settings.GameExpanded(g), g, true, EditorStyles.foldoutHeader);
            Settings.SetGameExpanded(g, exp);
            _info.TryGetValue(g, out var gi);
            if (gi.tag != null && gi.tag != GUIContent.none)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(gi.tag, _tagStyle);
            }
            EditorGUILayout.EndHorizontal();
            if (!exp) { EditorGUILayout.EndVertical(); return; }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("AssetBundle", GUILayout.Width(75));
            EditorGUILayout.LabelField(gi.abDate, GUILayout.Width(110));
            GUILayout.Label(gi.abLabel ?? GUIContent.none, LabelStyle(gi.ab.state), GUILayout.Width(112));
            string abCur = Settings.GameAbDest(g);
            string abNew = EditorGUILayout.TextField(abCur);
            if (abNew != abCur) { Settings.SetGameAbDest(g, abNew); ScheduleRecheck(g); }
            if (GUILayout.Button("...", GUILayout.Width(26)))
            {
                string p = EditorUtility.OpenFolderPanel("AssetBundle Destination", Settings.GameAbDest(g), "");
                if (!string.IsNullOrEmpty(p)) { Settings.SetGameAbDest(g, p); ScheduleRecheck(g); }
            }
            Settings.SetGamePullAb(g, EditorGUILayout.Toggle(Settings.GamePullAb(g), GUILayout.Width(20)));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scripts", GUILayout.Width(75));
            EditorGUILayout.LabelField(gi.scrDate, GUILayout.Width(110));
            GUILayout.Label(gi.scrLabel ?? GUIContent.none, LabelStyle(gi.scr.state), GUILayout.Width(112));
            string scrCur = Settings.GameScriptDest(g);
            string scrNew = EditorGUILayout.TextField(scrCur);
            if (scrNew != scrCur) { Settings.SetGameScriptDest(g, scrNew); ScheduleRecheck(g); }
            if (GUILayout.Button("...", GUILayout.Width(26)))
            {
                string p = EditorUtility.OpenFolderPanel("Script Destination", Settings.GameScriptDest(g), "");
                if (!string.IsNullOrEmpty(p)) { Settings.SetGameScriptDest(g, p); ScheduleRecheck(g); }
            }
            Settings.SetGamePullScript(g, EditorGUILayout.Toggle(Settings.GamePullScript(g), GUILayout.Width(20)));
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Pull " + g))
            {
                bool ab = Settings.GamePullAb(g), scr = Settings.GamePullScript(g);
                if (!ab && !scr)
                    EditorUtility.DisplayDialog("Pull", "Select AssetBundles and/or Scripts.", "OK");
                else if (ab && string.IsNullOrEmpty(Settings.GameAbDest(g)))
                    EditorUtility.DisplayDialog("Pull", "AssetBundle destination path is empty. Set it before pulling.", "OK");
                else if (scr && string.IsNullOrEmpty(Settings.GameScriptDest(g)))
                    EditorUtility.DisplayDialog("Pull", "Script destination path is empty. Set it before pulling.", "OK");
                else
                    PullConfirmWindow.Open(Settings.ServerRoot, g, Settings.GameAbDest(g), Settings.GameScriptDest(g), ab, scr);
            }
            EditorGUILayout.EndVertical();
        }

        // ---------- Tool Sharing tab ----------

        private void DrawTools()
        {
            if (Settings.ToolsRoot != _scannedToolsRoot && _scanDueAt < 0 && !_scanning) ScheduleScan();

            EditorGUILayout.BeginHorizontal();
            string rootBefore = Settings.ToolsRoot;
            Settings.ToolsRoot = EditorGUILayout.TextField("Tools Root", rootBefore);
            if (Settings.ToolsRoot != rootBefore) ScheduleScan();
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string picked = EditorUtility.OpenFolderPanel("Tools Root", Settings.ToolsRoot, "");
                if (!string.IsNullOrEmpty(picked)) { Settings.ToolsRoot = picked; RequestScan(); }
            }
            if (GUILayout.Button("Scan", GUILayout.Width(50))) RequestScan();
            EditorGUILayout.EndHorizontal();

            DrawToolsStatus();
            if (!_toolsOnline) return;

            EditorGUILayout.Space();
            DrawShareTool();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Available Tools (" + _tools.Length + ")", EditorStyles.boldLabel);
            if (_tools.Length == 0)
            {
                EditorGUILayout.HelpBox("No tools on the server yet. Share one above.", MessageType.Info);
                return;
            }

            _toolScroll = EditorGUILayout.BeginScrollView(_toolScroll);
            foreach (string t in _tools) DrawToolBlock(t);
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolsStatus()
        {
            bool set = !string.IsNullOrEmpty(Settings.ToolsRoot);
            string msg = StatusText(Settings.ToolsRoot, _resultToolsRoot, _toolsOnline, "Tools server", out Color c);
            _statusStyle.normal.textColor = c;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(msg, _statusStyle);
            GUILayout.FlexibleSpace();
            // Share is reachable but the tools folder doesn't exist yet -> offer to
            // create it. (_toolsParentExists comes from the background scan.)
            bool settled = _hasResult && !_scanning && _resultToolsRoot == Settings.ToolsRoot;
            if (set && settled && !_toolsOnline && _toolsParentExists &&
                GUILayout.Button("Create", EditorStyles.miniButton, GUILayout.Width(64)) &&
                EditorUtility.DisplayDialog("Tools Root", "Create this folder?\n\n" + Settings.ToolsRoot, "Create", "Cancel"))
            {
                try { System.IO.Directory.CreateDirectory(Settings.ToolsRoot); }
                catch (System.Exception ex) { EditorUtility.DisplayDialog("Tools Root", "Couldn't create the folder:\n" + ex.Message, "OK"); }
                RequestScan();
            }
            using (new EditorGUI.DisabledScope(_scanning))
                if (GUILayout.Button("Refresh", EditorStyles.miniButton, GUILayout.Width(64)))
                    RequestScan();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawShareTool()
        {
            bool exp = EditorGUILayout.Foldout(Settings.ToolShareExpanded, "Share a Tool", true, EditorStyles.foldoutHeader);
            Settings.ToolShareExpanded = exp;
            if (!exp) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            string before = Settings.ToolPushFolder;
            string folder = PathField("Tool Folder", before);
            string dropped = FolderDrop(GUILayoutUtility.GetLastRect());
            if (dropped != null) folder = dropped;
            if (folder != before)
            {
                // Keep the name in sync with the folder unless the user typed their own.
                string oldName = FolderName(before);
                if (string.IsNullOrEmpty(Settings.ToolPushName) || Settings.ToolPushName == oldName)
                    Settings.ToolPushName = FolderName(folder);
                Settings.ToolPushFolder = folder;
            }

            Settings.ToolPushName = EditorGUILayout.TextField("Tool Name", Settings.ToolPushName);
            string name = Settings.ToolPushName;
            if (!string.IsNullOrEmpty(name))
            {
                bool exists = System.Array.IndexOf(_tools, name) >= 0;
                EditorGUILayout.LabelField(" ", (exists ? "Updates existing tool: " : "New tool: ") +
                    System.IO.Path.Combine(Settings.ToolsRoot, name), EditorStyles.miniLabel);
            }

            if (GUILayout.Button("Push Tool", GUILayout.Height(26)))
            {
                if (ToolManager.Push(Settings.ToolsRoot, Settings.ToolPushName, Settings.ToolPushFolder))
                    RequestScan();
            }
            EditorGUILayout.EndVertical();
        }

        private static string FolderName(string path) =>
            string.IsNullOrEmpty(path) ? "" : System.IO.Path.GetFileName(path.TrimEnd('/', '\\'));

        // Accepts a folder dragged from the Project window or the OS file browser.
        private static string FolderDrop(Rect rect)
        {
            Event e = Event.current;
            if (!rect.Contains(e.mousePosition)) return null;
            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform) return null;

            string dir = null;
            foreach (string p in DragAndDrop.paths)
                if (System.IO.Directory.Exists(p)) { dir = p; break; }
            if (dir == null) return null;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (e.type == EventType.DragUpdated) { e.Use(); return null; }
            DragAndDrop.AcceptDrag();
            e.Use();
            return dir;
        }

        private void DrawToolBlock(string t)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            bool exp = EditorGUILayout.Foldout(Settings.ToolExpanded(t), t, true, EditorStyles.foldoutHeader);
            Settings.SetToolExpanded(t, exp);
            _toolInfo.TryGetValue(t, out var ti);
            if (ti.label != null && ti.label != GUIContent.none)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(ti.label, LabelStyle(ti.st.state));
            }
            EditorGUILayout.EndHorizontal();
            if (!exp) { EditorGUILayout.EndVertical(); return; }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Updated", GUILayout.Width(55));
            EditorGUILayout.LabelField(ti.date, GUILayout.Width(110));
            string cur = Settings.ToolDest(t);
            string nv = EditorGUILayout.TextField(cur);
            if (nv != cur) { Settings.SetToolDest(t, nv); ScheduleToolRecheck(t); }
            if (GUILayout.Button("...", GUILayout.Width(26)))
            {
                string p = EditorUtility.OpenFolderPanel("Pull '" + t + "' into folder", Settings.ToolDest(t), "");
                if (!string.IsNullOrEmpty(p)) { Settings.SetToolDest(t, p); ScheduleToolRecheck(t); }
            }
            if (GUILayout.Button(ti.st.state == SectionState.Changed ? "Update" : "Pull", GUILayout.Width(60)))
            {
                string dest = Settings.ToolDest(t);
                if (string.IsNullOrEmpty(dest))
                    EditorUtility.DisplayDialog("Pull Tool", "Set the folder to pull '" + t + "' into first.", "OK");
                else
                    PullConfirmWindow.OpenTool(Settings.ToolsRoot, t, ToolManager.TargetPath(dest, t));
            }
            EditorGUILayout.EndHorizontal();

            string target = ToolManager.TargetPath(Settings.ToolDest(t), t);
            if (!string.IsNullOrEmpty(target))
                EditorGUILayout.LabelField("Pulls to: " + target, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        // Builds the display strings for a scanned tool (main thread: GUIContent).
        private static ToolInfo MakeToolInfo(ToolData d, string dest)
        {
            var ti = new ToolInfo { date = d.date, st = d.st };
            switch (ti.st.state)
            {
                case SectionState.NotInProject:
                    ti.label = new GUIContent("NOT IN PROJECT", string.IsNullOrEmpty(dest)
                        ? "Set the folder to pull this tool into, then Pull."
                        : "Not pulled into this project yet.");
                    break;
                case SectionState.Changed:  ti.label = new GUIContent("● UPDATE", Details(ti.st, "Tool")); break;
                case SectionState.UpToDate: ti.label = new GUIContent("Up to date", "Your copy matches the server."); break;
                case SectionState.NotOnServer: ti.label = new GUIContent("Empty on server", "This tool's folder on the server has no files."); break;
                case SectionState.Unknown:  ti.label = new GUIContent("?", "Couldn't read the server or local folder."); break;
                default:                    ti.label = GUIContent.none; break;
            }
            return ti;
        }
    }
}
#endif

