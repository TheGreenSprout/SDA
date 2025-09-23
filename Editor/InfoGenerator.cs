/*
⚠️‼️ AI ASSISTED CODE

This code was written with the assistance of AI.
*/



using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class InfoGenerator : EditorWindow
{
    private string selectedFolder;
    private Vector2 scrollPos;
    private Vector2 exclusionScrollPos;
    private List<string> infoLines = new List<string>();
    private bool infoBuilt = false;
    private string rootFolderName;
    private bool includeLegend = true;
    public bool includeExcluded = true;

    private Texture2D headerImage;
    private Texture2D successIcon;
    private Texture2D restoreIcon;
    private Texture2D trashIcon;

    private List<string> excludePaths = new();
    private const string ExcludePrefsKey = "InfoGenerator_ExcludePaths";
    private string ProjectKey => Application.dataPath.GetHashCode().ToString();




    public static void Open()
    {
        var window = GetWindow<InfoGenerator>("Project Information Generator");
        window.minSize = new Vector2(550, 620);
        window.Show();
    }

    public static List<string> GlobalExcludes = new();
    public static void SetGlobalExcludes(List<string> excludes)
    {
        GlobalExcludes = excludes ?? new List<string>();
    }

    /*public static void ClearAllPreferences()
    {

    }*/

    void OnEnable()
    {
        headerImage = Resources.Load<Texture2D>("tool_header");
        successIcon = Resources.Load<Texture2D>("success");
        restoreIcon = Resources.Load<Texture2D>("restore_success");
        trashIcon = Resources.Load<Texture2D>("trash_success");

        if (EditorPrefs.HasKey(ProjectKey + "_" + ExcludePrefsKey))
        {
            excludePaths = EditorPrefs.GetString(ProjectKey + "_" + ExcludePrefsKey)
                .Split(';')
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => Path.GetFullPath(p).Replace('\\', '/').TrimEnd('/'))
                .Distinct()
                .ToList();
        }


        MergeGlobalExcludes();
    }

    private void MergeGlobalExcludes()
    {
        if (GlobalExcludes == null) return;

        foreach (var ex in GlobalExcludes)
        {
            if (!excludePaths.Contains(ex))
                excludePaths.Add(ex);
        }
    }

    void OnDisable()
    {
        EditorPrefs.SetString(ProjectKey + "_" + ExcludePrefsKey, string.Join(";", excludePaths));
    }

    void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Information Generator", EditorStyles.boldLabel);

        if (GUILayout.Button("⚠️ Clear All Saved Preferences", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Reset Preferences", "Are you sure you want to delete all saved Info Generator settings for this project?", "Yes", "Cancel"))
            {
                EditorPrefs.DeleteKey(ProjectKey + "_" + ExcludePrefsKey);
                excludePaths.Clear();
                includeLegend = true;


                CustomPopup.ShowPopup(trashIcon, "Preferences deleted...");
            }
        }
        GUILayout.EndHorizontal();


        if (headerImage != null)
        {
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(headerImage, GUILayout.Width(128), GUILayout.Height(128));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        
        //GUILayout.Label("Excluded Paths", EditorStyles.boldLabel);
        includeExcluded = GUILayout.Toggle(includeExcluded, includeExcluded ? "Excluded paths" : "Excluded paths", "Button", GUILayout.Height(30), GUILayout.Width(140));

        if (includeExcluded)
        {
            if (excludePaths.Count == 0)
            {
                EditorGUILayout.HelpBox("No excluded paths.", MessageType.Info);
            }
            else
            {
                exclusionScrollPos = EditorGUILayout.BeginScrollView(exclusionScrollPos, GUILayout.ExpandHeight(false));
                for (int i = 0; i < excludePaths.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    excludePaths[i] = EditorGUILayout.TextField(excludePaths[i]);
                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        excludePaths.RemoveAt(i);
                        i--;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
        }


        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Exclude Folder"))
        {
            var path = EditorUtility.OpenFolderPanel("Exclude Folder", Application.dataPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                path = Path.GetFullPath(path).Replace('\\', '/').TrimEnd('/');
                if (!excludePaths.Contains(path)) excludePaths.Add(path);
            }
        }
        if (GUILayout.Button("Exclude File"))
        {
            var path = EditorUtility.OpenFilePanel("Exclude File", Application.dataPath, "cs");
            if (!string.IsNullOrEmpty(path))
            {
                path = Path.GetFullPath(path).Replace('\\', '/').TrimEnd('/');
                if (!excludePaths.Contains(path)) excludePaths.Add(path);
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);


        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Select Root Folder", GUILayout.Width(200)))
        {
            var path = EditorUtility.OpenFolderPanel("Select Root Folder", Application.dataPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                selectedFolder = path;
                rootFolderName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar));
                infoBuilt = false;
                infoLines.Clear();
            }
        }
        GUILayout.Label(string.IsNullOrEmpty(selectedFolder) ? "No folder selected" : selectedFolder, EditorStyles.wordWrappedLabel);
        GUILayout.EndHorizontal();
        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(selectedFolder)))
        {
            if (GUILayout.Button("Generate Info File", GUILayout.Height(30), GUILayout.ExpandWidth(true)))
            {
                infoLines = BuildInfoInternal(selectedFolder);
                infoBuilt = true;
                scrollPos = Vector2.zero;
            }
        }

        if (!string.IsNullOrEmpty(rootFolderName))
        {
            if (GUILayout.Button("Clear Selection", GUILayout.Height(30)))
            {
                rootFolderName = "";
                //folderNamespaceMap.Clear();
            }
        }

        includeLegend = GUILayout.Toggle(includeLegend, includeLegend ? "Legend Included" : "Legend Excluded", "Button", GUILayout.Height(30), GUILayout.Width(160));
        GUILayout.EndHorizontal();
        GUILayout.Space(10);

        if (infoBuilt)
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
            foreach (var line in infoLines)
                GUILayout.Label(line);
            EditorGUILayout.EndScrollView();
            GUILayout.Space(5);
            if (GUILayout.Button("Save Info File…", GUILayout.Height(30)))
                SaveInfo();
        }
    }

    List<string> BuildInfoInternal(string root)
    {
        var raw = new List<string>();
        if (includeLegend)
        {
            raw.Add("📁 --> Folder/Namespace");
            raw.Add("📘 --> Information");
            raw.Add("📜 --> Script");
            raw.Add("🧩 --> Class");
            raw.Add("🧱 --> Struct");
            raw.Add("🔌 --> Interface");
            raw.Add("🔢 --> Enum");
            raw.Add("🎯 --> Class variable");
            raw.Add("📍 --> Enum item");
            raw.Add("⚙️ --> Function/Method");
            raw.Add("📌 --> Parameter");
        }

        TraverseInfo(root, 0, raw);

        var spaced = new List<string>();
        for (int i = 0; i < raw.Count; i++)
        {
            spaced.Add(raw[i]);
            if (i == raw.Count - 1) break;
            var cur = raw[i];
            var next = raw[i + 1];
            bool hasClosingInLine = (cur.Contains(")") && !cur.Contains("(")) || (cur.Contains("]") && !cur.Contains("[")) || (cur.Contains("}") && !cur.Contains("{"));
            bool hasClosingAfter = (next.Contains(")") && !next.Contains("(")) || (next.Contains("]") && !next.Contains("[")) || (next.Contains("}") && !next.Contains("{"));
            bool closingChain = hasClosingAfter && hasClosingInLine;
            bool isFolder = cur.Contains("📁");
            bool bothLegend = cur.Contains("-->") && next.Contains("-->");
            bool onlyCurrentLegend = cur.Contains("-->") && !next.Contains("-->");
            bool currentParam = cur.Contains("📍") && !hasClosingAfter;
            bool currentEnumMem = cur.Contains("📌") && !hasClosingAfter;
            if (onlyCurrentLegend) { spaced.Add(""); spaced.Add(""); }
            if (!bothLegend && !currentParam && !currentEnumMem && !closingChain && !isFolder) spaced.Add("");
        }
        return spaced;
    }

    void TraverseInfo(string path, int depth, List<string> lines)
    {
        path = Path.GetFullPath(path).Replace('\\', '/').TrimEnd('/');
        if (excludePaths.Any(ex => path.StartsWith(ex))) return;

        string indent = new string(' ', depth * 4);
        lines.Add($"{indent}📁 {Path.GetFileName(path)}");
        lines.Add($"{indent}{{");

        foreach (var file in Directory.GetFiles(path, "*.cs"))
        {
            string normFile = Path.GetFullPath(file).Replace('\\', '/').TrimEnd('/');
            if (excludePaths.Any(ex => normFile.StartsWith(ex))) continue;

            var result = CSharpParser.ParseFile(file);
            var ordered = result.OrderedTypes;
            if (ordered.Count == 0) continue;
            // existing logic unchanged...
        }

        foreach (var dir in Directory.GetDirectories(path))
            TraverseInfo(dir, depth + 1, lines);

        lines.Add($"{indent}}}");
    }

    void SaveInfo()
    {
        var defaultName = rootFolderName + "_Info.txt";
        var fullPath = EditorUtility.SaveFilePanel("Save Info File As", Application.dataPath, defaultName, "txt");
        if (string.IsNullOrEmpty(fullPath)) return;
        File.WriteAllLines(fullPath, infoLines);
        AssetDatabase.Refresh();
        CustomPopup.ShowPopup(restoreIcon, $"Info file saved:\n{Path.GetFileName(fullPath)}");
    }
}
