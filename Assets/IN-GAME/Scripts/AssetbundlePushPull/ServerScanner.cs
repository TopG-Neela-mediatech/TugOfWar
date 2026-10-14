#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;

namespace AssetBundlePushPull
{
    public struct GameData { public string abDate, scrDate; public SectionStatus ab, scr; }
    public struct ToolData { public string date; public SectionStatus st; }

    // Everything the window shows about the servers, gathered in one pass.
    public sealed class ScanResult
    {
        public int gen;
        public string serverRoot, toolsRoot;   // the inputs this result belongs to
        public bool online, toolsOnline, toolsParentExists;
        public string[] games = new string[0], tools = new string[0];
        public readonly Dictionary<string, GameData> gameData = new Dictionary<string, GameData>();
        public readonly Dictionary<string, ToolData> toolData = new Dictionary<string, ToolData>();
    }

    // Does every server read (online checks, game/tool listing, file comparisons)
    // and is meant to run OFF the main thread. Network mounts can answer slowly
    // or hang (SMB on macOS especially, or a sleeping NAS); doing this on the main
    // thread froze the whole Unity editor. Uses no Unity API and no live Settings
    // (only the snapshot it's given), so it's safe on a worker thread.
    public static class ServerScanner
    {
        public static ScanResult Run(int gen, string serverRoot, string toolsRoot, Dictionary<string, string> settings)
        {
            var r = new ScanResult { gen = gen, serverRoot = serverRoot, toolsRoot = toolsRoot };

            try
            {
                r.online = !string.IsNullOrEmpty(serverRoot) && Directory.Exists(serverRoot);
                if (r.online)
                {
                    r.games = PullManager.ScanGames(serverRoot);
                    foreach (string g in r.games)
                    {
                        string ad = Settings.Get(settings, Settings.AbDestKey + g);
                        string sd = Settings.Get(settings, Settings.ScrDestKey + g);
                        r.gameData[g] = new GameData
                        {
                            abDate = PullManager.FolderDate(serverRoot, g, "AssetBundles"),
                            scrDate = PullManager.FolderDate(serverRoot, g, "Scripts"),
                            ab = PullManager.Compare(serverRoot, g, "AssetBundles", ad, SharedSources(serverRoot, r.games, settings, g, "AssetBundles", ad)),
                            scr = PullManager.Compare(serverRoot, g, "Scripts", sd, SharedSources(serverRoot, r.games, settings, g, "Scripts", sd))
                        };
                    }
                }
            }
            catch { r.online = false; r.games = new string[0]; r.gameData.Clear(); }

            try
            {
                r.toolsOnline = !string.IsNullOrEmpty(toolsRoot) && Directory.Exists(toolsRoot);
                if (r.toolsOnline)
                {
                    r.tools = ToolManager.ScanTools(toolsRoot);
                    foreach (string t in r.tools)
                        r.toolData[t] = new ToolData
                        {
                            date = ToolManager.LastUpdated(toolsRoot, t),
                            st = ToolManager.Status(toolsRoot, t, Settings.Get(settings, Settings.ToolDestKey + t))
                        };
                }
                else r.toolsParentExists = ParentExists(toolsRoot);
            }
            catch { r.toolsOnline = false; r.tools = new string[0]; r.toolData.Clear(); }

            return r;
        }

        // Server folders of every OTHER game/section whose destination is the same
        // local folder as (g, sub). Empty in the usual one-folder-per-section setup.
        private static List<string> SharedSources(string root, string[] games, Dictionary<string, string> s, string g, string sub, string dest)
        {
            if (string.IsNullOrEmpty(dest)) return null;
            List<string> list = null;
            foreach (string o in games)
            {
                if (!(o == g && sub == "AssetBundles") && PullManager.SameFolder(dest, Settings.Get(s, Settings.AbDestKey + o)))
                    (list ?? (list = new List<string>())).Add(Path.Combine(Path.Combine(root, o), "AssetBundles"));
                if (!(o == g && sub == "Scripts") && PullManager.SameFolder(dest, Settings.Get(s, Settings.ScrDestKey + o)))
                    (list ?? (list = new List<string>())).Add(Path.Combine(Path.Combine(root, o), "Scripts"));
            }
            return list;
        }

        private static bool ParentExists(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            try
            {
                string parent = Path.GetDirectoryName(path.TrimEnd('/', '\\'));
                return !string.IsNullOrEmpty(parent) && Directory.Exists(parent);
            }
            catch { return false; }
        }
    }
}
#endif
