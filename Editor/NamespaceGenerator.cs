/*
⚠️‼️ AI ASSISTED CODE

This code was written with the assistance of AI.
*/



using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class NamespaceGenerator : EditorWindow
{
    private string baseFolder = "";
    private bool includeRootFolder = true;

    private Dictionary<string, string> folderNamespaceMap = new();
    private Vector2 scrollPos;
    private Vector2 exclusionScrollPos;

    public bool includeExcluded = true;

    private static Regex namespaceRegex = new(@"^\s*namespace\s+[\w\.]+", RegexOptions.Multiline);

    private Texture2D headerImage;
    private Texture2D successIcon;
    private Texture2D restoreIcon;
    private Texture2D trashIcon;

    private List<string> excludePaths = new();
    private const string ExcludePrefsKey = "NamespaceAssigner_ExcludePaths";
    private const string RootIncludePrefsKey = "NamespaceAssigner_IncludeRoot";
    private const string RootPathPrefsKey = "NamespaceAssigner_BaseFolder";
    private const string NamespaceMapPrefsKey = "NamespaceAssigner_FolderNamespaceMap";

    private string ProjectKey => Application.dataPath.GetHashCode().ToString();




    public static void Open()
    {
        var window = GetWindow<NamespaceGenerator>("Namespace Assigner");
        window.minSize = new Vector2(600, 500);
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



        includeRootFolder = EditorPrefs.GetBool(ProjectKey + "_" + RootIncludePrefsKey, true);
        baseFolder = EditorPrefs.GetString(ProjectKey + "_" + RootPathPrefsKey, "");

        if (EditorPrefs.HasKey(ProjectKey + "_" + ExcludePrefsKey))
        {
            excludePaths = EditorPrefs.GetString(ProjectKey + "_" + ExcludePrefsKey)
                .Split(';')
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(NormalizePath)
                .Distinct()
                .ToList();
        }

        string mapJson = EditorPrefs.GetString(ProjectKey + "_" + NamespaceMapPrefsKey, "");
        if (!string.IsNullOrEmpty(mapJson))
        {
            var loaded = JsonUtility.FromJson<SerializableMap>(mapJson);
            folderNamespaceMap = loaded.ToDictionary();
        }

        if (!string.IsNullOrEmpty(baseFolder))
            RefreshFolderNamespaces();
        


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
        EditorPrefs.SetBool(ProjectKey + "_" + RootIncludePrefsKey, includeRootFolder);
        EditorPrefs.SetString(ProjectKey + "_" + RootPathPrefsKey, baseFolder);
        EditorPrefs.SetString(ProjectKey + "_" + ExcludePrefsKey, string.Join(";", excludePaths));

        string mapJson = JsonUtility.ToJson(new SerializableMap(folderNamespaceMap));
        EditorPrefs.SetString(ProjectKey + "_" + NamespaceMapPrefsKey, mapJson);
    }

    void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Namespace Generator", EditorStyles.boldLabel);

        if (GUILayout.Button("⚠️ Clear All Saved Preferences", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Reset Preferences",
                "Are you sure you want to delete all saved Namespace generator settings for this project?",
                "Yes", "Cancel"))
            {
                EditorPrefs.DeleteKey(ProjectKey + "_" + RootIncludePrefsKey);
                EditorPrefs.DeleteKey(ProjectKey + "_" + RootPathPrefsKey);
                EditorPrefs.DeleteKey(ProjectKey + "_" + ExcludePrefsKey);
                EditorPrefs.DeleteKey(ProjectKey + "_" + NamespaceMapPrefsKey);
                includeRootFolder = true;

                baseFolder = "";
                folderNamespaceMap.Clear();
                excludePaths.Clear();

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


        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Exclude Folder"))
        {
            var path = EditorUtility.OpenFolderPanel("Exclude Folder",
                string.IsNullOrEmpty(baseFolder) ? Application.dataPath : baseFolder,
                "");
            if (!string.IsNullOrEmpty(path))
            {
                path = NormalizePath(path);
                if (!excludePaths.Contains(path)) excludePaths.Add(path);
            }
        }
        if (GUILayout.Button("Exclude File"))
        {
            var path = EditorUtility.OpenFilePanel("Exclude File",
                string.IsNullOrEmpty(baseFolder) ? Application.dataPath : baseFolder,
                "cs");
            if (!string.IsNullOrEmpty(path))
            {
                path = NormalizePath(path);
                if (!excludePaths.Contains(path)) excludePaths.Add(path);
            }
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);


        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select Root Folder", GUILayout.Width(200)))
        {
            var folder = EditorUtility.OpenFolderPanel("Select Root Folder",
                string.IsNullOrEmpty(baseFolder) ? Application.dataPath : baseFolder,
                "");
            if (!string.IsNullOrEmpty(folder))
            {
                baseFolder = NormalizePath(folder);
                RefreshFolderNamespaces();
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(baseFolder)))
        {
            if (GUILayout.Button("Generate Namespaces", GUILayout.Height(30)))
            {
                ApplyNamespaces();
            }
        }

        if (!string.IsNullOrEmpty(baseFolder))
        {
            if (GUILayout.Button("Clear Selection", GUILayout.Height(30)))
            {
                baseFolder = "";
                folderNamespaceMap.Clear();
            }
        }

        includeRootFolder = GUILayout.Toggle(
            includeRootFolder,
            includeRootFolder ? "Root Name Included" : "Root Name Excluded",
            "Button",
            GUILayout.Height(30),
            GUILayout.Width(180)
        );
        GUILayout.EndHorizontal();
        GUILayout.Space(10);

        if (!string.IsNullOrEmpty(baseFolder))
        {
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Base Folder:", baseFolder, EditorStyles.wordWrappedLabel);

            GUILayout.Space(10);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var folder in folderNamespaceMap.Keys.ToList())
            {
                if (IsExcluded(folder)) continue;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Folder:", GetRelativePath(folder), EditorStyles.boldLabel);
                folderNamespaceMap[folder] = EditorGUILayout.TextField("Namespace", folderNamespaceMap[folder]);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();

            /*GUILayout.Space(10);
            if (GUILayout.Button("Apply Namespaces to .cs Files", GUILayout.Height(30)))
            {
                ApplyNamespaces();
            }*/
        }
    }

    private void RefreshFolderNamespaces()
    {
        folderNamespaceMap ??= new();
        if (string.IsNullOrEmpty(baseFolder)) return;

        var allFolders = Directory.GetDirectories(baseFolder, "*", SearchOption.AllDirectories)
                                  .Select(NormalizePath)
                                  .Where(f => !IsExcluded(f))
                                  .ToList();

        allFolders.Insert(0, NormalizePath(baseFolder));

        foreach (var folder in allFolders)
        {
            if (IsExcluded(folder)) continue;

            var csFiles = Directory.GetFiles(folder, "*.cs", SearchOption.TopDirectoryOnly)
                                   .Select(NormalizePath)
                                   .Where(f => !IsExcluded(f))
                                   .ToArray();

            var subFolders = Directory.GetDirectories(folder, "*", SearchOption.TopDirectoryOnly)
                                      .Select(NormalizePath)
                                      .Where(f => !IsExcluded(f))
                                      .ToArray();

            if (csFiles.Length == 0 && subFolders.Length == 0)
                continue;

            if (!folderNamespaceMap.ContainsKey(folder))
                folderNamespaceMap[folder] = BuildNamespace(baseFolder, folder, includeRootFolder);
        }
    }

    private string BuildNamespace(string root, string folder, bool includeRoot)
    {
        string rel = Path.GetRelativePath(root, folder);
        var parts = rel.Split(Path.DirectorySeparatorChar)
                       .Where(p => !string.IsNullOrWhiteSpace(p) && p != "." && p != "..")
                       .ToList();

        if (includeRoot)
        {
            var rootName = Path.GetFileName(root);
            if (!string.IsNullOrWhiteSpace(rootName))
                parts.Insert(0, rootName);
        }

        return string.Join(".", parts);
    }

    private void ApplyNamespaces()
    {
        int changed = 0;

        foreach (var kvp in folderNamespaceMap)
        {
            var folder = kvp.Key;
            var ns = kvp.Value;

            if (IsExcluded(folder)) continue;

            var csFiles = Directory.GetFiles(folder, "*.cs", SearchOption.TopDirectoryOnly)
                                   .Select(NormalizePath)
                                   .Where(f => !IsExcluded(f))
                                   .ToArray();

            foreach (var file in csFiles)
            {
                string code = File.ReadAllText(file);
                string newCode;

                if (namespaceRegex.IsMatch(code))
                {
                    newCode = namespaceRegex.Replace(code, $"namespace {ns}");
                }
                else
                {
                    newCode = $"namespace {ns}\n{{\n{IndentCode(code)}\n}}";
                }

                File.WriteAllText(file, newCode);
                changed++;
            }
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Namespaces Applied", $"{changed} file(s) updated.", "OK");
    }

    private bool IsExcluded(string path)
    {
        string fullPath = NormalizePath(path);
        return excludePaths.Any(ex => fullPath.StartsWith(ex));
    }

    private string NormalizePath(string path)
    {
        return Path.GetFullPath(path).Replace('\\', '/').TrimEnd('/');
    }

    private string GetRelativePath(string fullPath)
    {
        if (string.IsNullOrEmpty(baseFolder)) return fullPath;

        string rel = Path.GetRelativePath(baseFolder, fullPath).Replace("\\", "/");
        string rootName = Path.GetFileName(baseFolder);

        return string.IsNullOrEmpty(rel) || rel == "." ? rootName : $"{rootName}/{rel}";
    }

    private string IndentCode(string code)
    {
        var lines = code.Split('\n');
        return string.Join("\n", lines.Select(l => "    " + l));
    }
}

[System.Serializable]
public class SerializableMap
{
    public List<string> keys = new();
    public List<string> values = new();

    public SerializableMap() { }

    public SerializableMap(Dictionary<string, string> dict)
    {
        foreach (var kvp in dict)
        {
            keys.Add(kvp.Key);
            values.Add(kvp.Value);
        }
    }

    public Dictionary<string, string> ToDictionary()
    {
        var result = new Dictionary<string, string>();
        for (int i = 0; i < keys.Count && i < values.Count; i++)
        {
            result[keys[i]] = values[i];
        }
        return result;
    }
}
