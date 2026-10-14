# AssetBundle Push Pull

A generic Unity **Editor-only** tool that pushes a project's AssetBundles and shared scripts/assets to a shared server folder (usually a NAS or network share), and lets other projects pull them in.

- Menu: **Tools > AssetBundle Push Pull**
- Namespace: `AssetBundlePushPull`
- Every file is wrapped in `#if UNITY_EDITOR`, and there is no `.asmdef`, so it compiles into `Assembly-CSharp` and is stripped from player builds.
- Nothing outside this folder references it, so you can drop the folder into any project.

---

## Server layout

```
<ServerRoot>/                      default: \\NAS\Gaming\AssetBundlesStorage
  <GameName>/
    AssetBundles/                  exact mirror of the pushing project's AB folder
    Scripts/                       merged contents of the "Scripts / Assets to Share" list
```

Every sub-folder of `<ServerRoot>` is treated as a "game" in the Pull tab, including folders the tool didn't create.

Tool Sharing uses a **separate** root. If it lived inside `<ServerRoot>`, it would show up as a game:

```
<ToolsRoot>/                       default: \\NAS\Gaming\ToolsStorage
  <ToolName>/                      exact mirror of the pusher's tool folder (incl. .meta)
  <ToolName>.meta                  the tool folder's own .meta (keeps its GUID on pull)
```

---

## Files

| File | Role |
|---|---|
| `AssetBundlePushPullWindow.cs` | The main `EditorWindow`, with **Push**, **Pull**, and **Tool Sharing** tabs. |
| `PullConfirmWindow.cs` | The "Import from Server" window: a per-file diff tree (NEW / REPLACE / REMOVE) with checkboxes. It opens from the per-game **Pull &lt;game&gt;** button, and from a tool's **Pull/Update** button (`OpenTool`). |
| `ServerScanner.cs` | Background scan. Every server read the window needs (online checks, game and tool lists, dates, comparisons) happens here, on a worker thread. See *Threading*. |
| `ToolManager.cs` | Tool Sharing: scans tools, pushes a tool, works out each tool's status and last-updated date, and validates tool names. |
| `PushManager.cs` | Push logic. Also holds `Report()`, the shared completion/error dialog. |
| `PullManager.cs` | Scans games, reads folder dates, detects updates, and runs the non-interactive pull used by **Pull All Selected**. |
| `FileUtility.cs` | Copy, mirror, and delete helpers. Handles incremental copies, the progress bar, and error collection. |
| `Settings.cs` | Key/value settings stored in **`ProjectSettings/AssetBundlePushPull.json`**, not EditorPrefs, so each project keeps its own settings. |

---

## Push tab

Inputs:
- **Server Root Path** (shared with the Pull tab)
- **Game Name**
- **AssetBundle Folder** (**optional**: leave it empty to push scripts only)
- **Scripts / Assets to Share**: an ordered list of folders or files. You can drag and drop them, or use **Add Folder** / **Add File**. The list is per project, not per game.

Clicking **Push** does the following:
1. Checks that the server root exists and the game name isn't empty.
   - If an AB folder is set, it must exist. A wrong path is an error, not silently skipped. Clear it to push scripts only.
   - At least one thing must be pushed (an AB folder or a script entry), otherwise you get "Nothing to push".
2. **AssetBundles**: skipped when the AB folder is empty, so the server's `AssetBundles/` is left alone (it isn't deleted). Otherwise it runs `FileUtility.MirrorDirectory(abFolder, <game>/AssetBundles)`. The server folder becomes an exact recursive mirror, so anything deleted locally **is deleted on the server**.
3. **Scripts**: skipped if the list is empty, so the server's `Scripts/` folder is left alone. Otherwise it runs `FileUtility.MirrorInto(<game>/Scripts, list)`:
   - For a folder entry, its *contents* are merged in, not the folder itself.
   - A file entry is copied in by name.
   - Top-level server entries that no source provides any more are deleted.
   - If two sources have an entry with the same name, the **later list entry wins**. That's why the list has ▲/▼ buttons.
4. `.meta` files are pushed as well, which keeps GUIDs stable for anyone who pulls.

## Pull tab

- Lists every game folder on the server. Each game has:
  - a checkbox to select it for **Pull All Selected**
  - a foldout containing:
    - AssetBundles row: server date, **status label**, destination path, include toggle
    - Scripts row: server date, **status label**, destination path, include toggle
    - a **Pull &lt;game&gt;** button
- Status labels are explained in *Update detection* below. Hover over any label to see the counts.
- **Pull &lt;game&gt;** opens `PullConfirmWindow` (interactive, selective).
- **Pull All Selected** runs `PullManager.Pull(..., mirror:false)` for each ticked game with **no confirmation window**, then calls `AssetDatabase.Refresh()` once.

### Pull &lt;game&gt;: confirm window behaviour
- Builds a tree from the union of server entries and destination entries. `.meta` files are hidden.
  - **New**: exists only on the server. Checked by default.
  - **Replace**: a file that exists on both sides, whether or not it changed. Checked by default. Identical files are skipped at copy time anyway.
  - **Remove**: exists only in the destination. **Unchecked by default.** If checked, it is deleted along with its `.meta`.
  - A folder that exists on both sides has no tag. Its checkbox is tri-state, based on its children.
- **Import Selected** copies or deletes each checked leaf (`CopySingle` / `DeletePath`, which carry the `.meta` along), records the pull, runs `AssetDatabase.Refresh()`, and shows the report.

### Pull All Selected: what gets copied
The mirror flag is always `false`, so nothing is ever removed from your project:
- **AssetBundles**: `CopyDirectory` merges new and changed files. Stale local files stay.
- **Scripts**: `CopyContentsReplacing` overwrites top-level files. Each top-level **sub-folder is deleted and re-copied whole**, so local-only files inside those sub-folders are lost. Stale top-level files stay.

## Tool Sharing tab

Shares reusable tools (any folder of scripts and assets) between projects.

- **Tools Root**: the path, a folder picker, **Scan**, and a connection status line with **Refresh**. If the root doesn't exist but its parent does (the share is reachable), a **Create** button appears. It asks for confirmation before creating the folder.
- **Share a Tool** (foldout):
  - **Tool Folder**: pick it, type it, or drag a folder from the Project window or the OS onto the row.
  - **Tool Name**: auto-filled from the folder name, and stays in sync until you type your own. A hint shows whether this is a *New tool* or *Updates existing tool*.
  - Names containing `\ / : * ? " < > |`, starting with `. # @`, or ending with a dot/space are rejected, so the same name works on both Windows and macOS.
  - **Push Tool** runs `ToolManager.Push`:
    - It mirrors the folder to `<ToolsRoot>/<ToolName>` and copies the folder's own `.meta` alongside it.
    - If the tool already exists, it asks before updating. Files that aren't in your folder are removed from the server copy.
    - It refuses if the local folder and the server copy overlap (one inside the other).
- **Available Tools**: one block per tool folder on the server.
  - Hidden and NAS system folders (`.*`, `#recycle`, `@eaDir`) are skipped.
  - The list is sorted by name, ignoring case.
  - Header: tool name and status label.
  - Expanded: **Updated** (the newest file time on the server), a destination field with a `...` picker, a **Pull** button (shown as **Update** when there's an update), and a *Pulls to:* hint.
  - The destination is the **parent** folder. The tool lands at `<destination>/<ToolName>` (`ToolManager.TargetPath`).
  - Pull/Update opens `PullConfirmWindow.OpenTool`, the same diff tree as game pulls, with Remove unchecked by default. After import, the tool folder's `.meta` is copied if the project doesn't have one yet, and the labels refresh.

Tool status label (`ToolManager.Status`, which uses `PullManager.Compare` with `sub = ""`):

| State | Label | When |
|---|---|---|
| `NotInProject` | **NOT IN PROJECT** (orange) | No destination set, **or** `<destination>/<ToolName>` doesn't exist yet |
| `Changed` | **● UPDATE** (green) | Something was added, changed, or removed in the tool on the server since your copy. Hover for counts. |
| `UpToDate` | Up to date (grey) | Your copy matches the server |
| `NotOnServer` | Empty on server (grey) | The tool's server folder has no files |
| `Unknown` | `?` | A folder couldn't be read |

The same comparison rules apply as for games (see below): recursive, 2 s timestamp tolerance, and `.meta` / OS junk ignored. Labels refresh on the same triggers: open, focus, Scan/Refresh, after an import, and 0.6 s after editing a destination.

---

## Update detection

`PullManager.Compare(serverRoot, game, sub, dest, sharedSources)` compares the **actual files** on the server (`<game>/<sub>`) with the files in the local destination folder. It's recursive, so changes at any depth are detected. It returns a `SectionStatus`, which holds a `SectionState` plus the added / changed / removed counts.

Per-row label (AssetBundles / Scripts):

| State | Label | When |
|---|---|---|
| `NotInProject` | **NOT IN PROJECT** (orange) | No destination path is set for this section. |
| `Changed` | **NEW ASSETBUNDLE** / **NEW SCRIPT** (green) | At least one file is new on the server, was changed on the server, or was removed on the server but still exists locally. A missing destination folder counts as everything new. |
| `UpToDate` | Up to date (grey) | The destination matches the server. |
| `NotOnServer` | Nothing on server (grey) | The server folder is missing or holds no files, so nothing was pushed for this section. If the server is empty but the local folder still has files from an earlier pull, the state is `Changed` instead (removed on server). |
| `Unknown` | `?` | The server or destination folder couldn't be read (share dropped, access denied). |

Header label (next to the game name), from `PullManager.GameTag`:
- **NEW**: neither destination is set, so this game isn't in the project yet.
- **NEW UPDATE**: both rows are `Changed`.
- **NEW ASSETBUNDLE** / **NEW SCRIPT**: only one row is `Changed`.
- Otherwise the header has no label. A section that is `NOT IN PROJECT` never counts as an update.

How files are compared:
- **Paths**: relative paths use `/` on every OS.
  - Case-insensitive on Windows and macOS, case-sensitive on Linux.
- **Changed**: the file size differs, **or** the last-write time (UTC) differs by **more than 2 s**.
  - Copies keep timestamps, so a file that hasn't changed matches.
  - The 2 s tolerance covers Mono on macOS (which copies whole seconds) and FAT/NAS 2 s rounding.
  - The copy engine's exact-match skip (`FileUtility.SameFile`) is unchanged. Anything flagged as changed is always copied on pull.
- **Ignored**: `*.meta` (Unity regenerates these), `.DS_Store`, `._*` (macOS AppleDouble files on SMB shares), `Thumbs.db`, and `desktop.ini`.
- **Shared destinations**: if the other section, or another game, pulls into the **same folder** (checked by `PullManager.SameFolder`), that section's server files don't count as "removed". Without this, sections that share a folder would never show "Up to date".

When the labels refresh (each time as one background scan, see *Threading*):
- when the window opens
- when it gains focus, at most once every 1.5 s
- when **Refresh** / **Scan** is clicked, or after **Pull All Selected** or **Push Tool**
- right after **Import Selected** in the confirm window (`AssetBundlePushPullWindow.RefreshOpenWindows()`)
- 0.6 s after you stop typing in a destination or root path, or right after picking one with `...`

## Threading (why the editor never waits on the NAS)

Network shares can answer slowly or hang, especially SMB on macOS or a sleeping or disconnected NAS. The window used to read the server on Unity's main thread: on open, on focus, on every keystroke in the root fields, and on every repaint while the tools root was offline. On a Mac this froze the editor.

Now:
- The window never touches the server from `OnGUI`, `OnEnable`, or `OnFocus`.
- `StartScan()` captures its inputs on the main thread: `ServerRoot`, `ToolsRoot`, and `Settings.Snapshot()` (a copy of the settings map, because the live map isn't thread-safe).
- It then runs `ServerScanner.Run` on a `ThreadPool` thread. The worker uses **no Unity API**. That's why `PullManager.PathComparer` uses `RuntimeInformation` instead of `Application.platform`.
- The finished `ScanResult` is handed back through a `volatile` field and applied in `OnInspectorUpdate` (about 10 Hz), where the `GUIContent` labels are built.
- **Only one scan runs at a time.** Requests made during a scan are merged into one follow-up scan (`_rescanWanted`). Typing is debounced (`ScheduleScan`).
- The status line shows **Checking...**, then **not responding... still trying** after 8 s, then connected or offline. Existing results stay visible while a refresh runs, marked "(refreshing...)". The Refresh buttons are disabled while a scan runs.
- A scan stuck on a dead mount can't be cancelled from .NET. The UI stays usable, and the next scan starts when it returns. (Windows gives up on a missing host after roughly 20–40 s.)
- Folder walking (`PullManager.AllFiles`) doesn't follow symlinked folders and stops at depth 64, so a symlink loop (possible on macOS) can't make a scan run forever.

User-initiated actions still run on the main thread with a progress bar, as before: Push, Pull, Import, Push Tool, and Create.

Cost: one recursive file listing of the server folder and the destination, per assigned section. That was about 90 ms for `Phonic` over the NAS. Sections with no destination set cost nothing.

`Settings` still records `LastPull*` / `Pulled_*` after each pull, but the labels no longer read them.

## Incremental copy

`FileUtility.CopyFile` skips a file when the destination has the **same length and last-write time (UTC)**. `File.Copy` keeps timestamps, so files that haven't changed are never rewritten. The progress bar only redraws when the percentage changes.

## Errors

Copy and delete failures are collected in `FileUtility.Errors`. They don't abort the operation. `PushManager.Report(title)`:
- logs `"<title> completed."` if there were no errors
- otherwise shows a dialog listing the first 10 errors

The pull timestamp is only recorded when there were no errors.

---

## Settings keys (`ProjectSettings/AssetBundlePushPull.json`)

| Key | Meaning |
|---|---|
| `ServerRoot`, `GameName`, `AssetBundleFolder` | Push inputs. `ServerRoot` is also used by Pull. |
| `ScriptList` | Push share list, newline-separated |
| `ABDest_<g>`, `ScrDest_<g>` | Per-game pull destinations |
| `PullAb_<g>`, `PullScr_<g>` | Per-game include toggles (default on) |
| `Sel_<g>`, `Exp_<g>` | Per-game selected / expanded UI state |
| `LastPullAb_<g>`, `LastPullScr_<g>`, `Pulled_<g>` | Last-pull bookkeeping. Still written, but no longer used for labels. |
| `ToolsRoot` | Tool Sharing root |
| `ToolPushFolder`, `ToolPushName`, `ToolShareExp` | Share-a-Tool inputs and foldout state |
| `ToolDest_<t>`, `ToolExp_<t>` | Per-tool pull destination (parent folder) and expanded state |

The setters only write to disk when a value actually changes (they're called on every OnGUI repaint).

---

## Known issues / notes for future changes

- **Local-only files keep the label on**: "Removed on server" counts files that exist locally but aren't on the server. If you deliberately keep your own extra files in a pull destination, that row stays `NEW ...` until they're removed. (The confirm window lists the same files as REMOVE.)
- **Tools pulled twice cause duplicate classes**: if a tool is pulled into a second location in the same project (or already exists under another path), Unity will report duplicate class errors. The tool doesn't detect this.
- **Tools and OS junk files**: a tool pushed from macOS carries `.DS_Store` / `._*` files to the server (the mirror copies everything). They're ignored by the labels, and Unity ignores dot-files.
- **Tools scan cost**: each tool costs one recursive listing on refresh for its "Updated" date, plus one comparison if a destination is set.
- **Unused code** (safe to remove or reuse):
  - `PullManager.Plan` / `PullPlan`
  - `Settings.PullServerRoot`, `PullGameName`, `AssetBundleDestination`, `ScriptDestination`
  - `FileUtility.MirrorContents`, `CopyPath`, `CountEntry`, `CopyEntry`, `RemoveEntry`: nothing ever passes `mirror:true`, and the rest are never called
- **Duplicated logic**: `PullManager.Pull` re-implements `RecordPull`.
- **Pull All Selected**:
  - shows one report per game
  - stops at the first game that has a missing destination, but games before it have already been pulled
- **Summary counts**: the confirm window's "replace" count includes files that haven't changed.
- **Fragile parsing**: `long.Parse` in `GameLastPull*` throws if the JSON is corrupted by hand.
- **Path formats**: drag-and-drop gives project-relative paths (`Assets/...`), while the file pickers give absolute paths. Both work because the editor's working directory is the project root.
- **Refresh scope**: `AssetDatabase.Refresh()` only matters when the pull destinations are inside `Assets/`.

---

## Changelog

- **2026-09-24**: Fixed the editor hanging on macOS.
  - All server reads moved to a background thread (new `ServerScanner.cs`, `Settings.Snapshot()`), with a "Checking / not responding" status line.
  - Symlink-safe, depth-capped folder walk (`PullManager.AllFiles`).
  - `PullManager` no longer uses Unity APIs.
  - Removed the unused `_gameIndex`.
  - Push and Pull behaviour is unchanged.

- **2026-09-24**: Push no longer requires an AssetBundle folder.
  - Empty means scripts-only push, and the server's `AssetBundles/` is left untouched.
  - A hint under the field says so.
  - A set-but-missing path still errors, and pushing with nothing set shows "Nothing to push".

- **2026-09-24**: Fixed an empty server folder showing "Up to date".
  - Example: Phonic's `Scripts/` exists on the server but has no files.
  - It now shows **Nothing on server** (games) or **Empty on server** (tools).
  - The status label column was widened from 100 to 112 px so "NEW ASSETBUNDLE" isn't cut off.

- **2026-09-24**: Added the **Tool Sharing** tab.
  - New `ToolManager.cs` and `PullConfirmWindow.OpenTool`.
  - New Tool settings keys, plus `DefaultToolsRoot` (`\\NAS\Gaming\ToolsStorage`).
  - `PullManager.Ignored` is now public.
  - Push and Pull behaviour is unchanged.

- **2026-09-24**: Rewrote the Pull tab labels.
  - Labels now come from a real recursive file comparison between the server and each destination (`PullManager.Compare`), replacing the folder-timestamp check (`UpdateTag`, removed).
  - Added per-row labels: NOT IN PROJECT / NEW ASSETBUNDLE / NEW SCRIPT / Up to date.
  - The header shows **NEW** when no destination is set. Hover tooltips show the counts.
  - Destinations shared between sections or games are handled.
  - Works the same on Windows and macOS: path normalization, case rules, timestamp tolerance, and macOS junk files.
  - The confirm window refreshes the main window after an import.
  - Window min width changed from 420 to 500 to fit the new column.
- **2026-09-24**: README created from a full read of the current source. No code changes.
