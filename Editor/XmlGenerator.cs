/*
⚠️‼️ AI ASSISTED CODE

This code was written with the assistance of AI.
*/



using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class XmlGenerator : EditorWindow
{
    [SerializeField] private List<string> selectedFiles = new List<string>();
    private Dictionary<string, ParseResult> fileParseMap = new Dictionary<string, ParseResult>();
    private Vector2 scrollPos;
    private Vector2 exclusionScrollPos;

    public bool includeExcluded = true;

    private List<string> excludePaths = new List<string>();

    private string backupFolder => Path.Combine(Application.dataPath, "..", ".xmldoc_backups");

    private Texture2D headerImage;
    private Texture2D successIcon;
    private Texture2D restoreIcon;
    private Texture2D trashIcon;

    private Dictionary<string, bool> classFoldouts = new Dictionary<string, bool>();
    private Dictionary<string, Dictionary<string, bool>> methodFoldouts = new Dictionary<string, Dictionary<string, bool>>();
    private Dictionary<string, bool> enumFoldouts = new Dictionary<string, bool>();
    private Dictionary<string, bool> fileFoldouts = new Dictionary<string, bool>();

    private const string AutoSaveKey = "XmlGenerator_AutoSave";
    private const string SelectedFilesKey = "XmlGenerator_SelectedFiles";
    private const string BaseFolderKey = "XmlGenerator_BaseFolder";

    private const string ClassFoldoutsKey = "XmlGen_ClassFoldouts";
    private const string MethodFoldoutsKey = "XmlGen_MethodFoldouts";
    private const string EnumFoldoutsKey = "XmlGen_EnumFoldouts";
    private const string FileFoldoutsKey = "XmlGen_FileFoldouts";
    private const string ExcludePathsKey = "XmlGenerator_ExcludePaths";

    private static string ProjectKeyPrefix => Application.dataPath.GetHashCode().ToString();

    private string baseFolder = "";

    private bool autoSave
    {
        get => EditorPrefs.GetBool(ProjectKeyPrefix + "_" + AutoSaveKey, false);
        set => EditorPrefs.SetBool(ProjectKeyPrefix + "_" + AutoSaveKey, value);
    }




    public static void Open()
    {
        var window = GetWindow<XmlGenerator>("XML Doc Generator");
        window.minSize = new Vector2(650, 700);
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

        if ((selectedFiles == null || selectedFiles.Count == 0) && EditorPrefs.HasKey(ProjectKeyPrefix + "_" + SelectedFilesKey))
        {
            var saved = EditorPrefs.GetString(ProjectKeyPrefix + "_" + SelectedFilesKey);
            selectedFiles = new List<string>(saved.Split(';'));
            baseFolder = EditorPrefs.GetString(ProjectKeyPrefix + "_" + BaseFolderKey, "");
        }

        if (EditorPrefs.HasKey(ProjectKeyPrefix + "_" + ExcludePathsKey))
        {
            excludePaths = EditorPrefs.GetString(ProjectKeyPrefix + "_" + ExcludePathsKey)
                .Split(';')
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => Path.GetFullPath(p).Replace('\\', '/').TrimEnd('/'))
                .Distinct()
                .ToList();
        }

        if (selectedFiles != null && selectedFiles.Count > 0)
        {
            ParseFiles();
        }

        if (EditorPrefs.HasKey(ProjectKeyPrefix + "_" + ClassFoldoutsKey))
            classFoldouts = JsonUtility.FromJson<SerializableFoldoutState>(EditorPrefs.GetString(ProjectKeyPrefix + "_" + ClassFoldoutsKey)).ToDictionary();
        if (EditorPrefs.HasKey(ProjectKeyPrefix + "_" + MethodFoldoutsKey))
            methodFoldouts = JsonUtility.FromJson<SerializableNestedFoldoutState>(EditorPrefs.GetString(ProjectKeyPrefix + "_" + MethodFoldoutsKey)).ToNestedDictionary();
        if (EditorPrefs.HasKey(ProjectKeyPrefix + "_" + EnumFoldoutsKey))
            enumFoldouts = JsonUtility.FromJson<SerializableFoldoutState>(EditorPrefs.GetString(ProjectKeyPrefix + "_" + EnumFoldoutsKey)).ToDictionary();
        if (EditorPrefs.HasKey(ProjectKeyPrefix + "_" + FileFoldoutsKey))
            fileFoldouts = JsonUtility.FromJson<SerializableFoldoutState>(EditorPrefs.GetString(ProjectKeyPrefix + "_" + FileFoldoutsKey)).ToDictionary();
        


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

    void OnDestroy()
    {
        EndSelection();

        EditorPrefs.DeleteKey(ProjectKeyPrefix + "_" + SelectedFilesKey);
        EditorPrefs.DeleteKey(ProjectKeyPrefix + "_" + BaseFolderKey);
        EditorPrefs.DeleteKey(ProjectKeyPrefix + "_" + ClassFoldoutsKey);
        EditorPrefs.DeleteKey(ProjectKeyPrefix + "_" + MethodFoldoutsKey);
        EditorPrefs.DeleteKey(ProjectKeyPrefix + "_" + EnumFoldoutsKey);
        EditorPrefs.DeleteKey(ProjectKeyPrefix + "_" + FileFoldoutsKey);
    }

    void OnDisable()
    {
        EditorPrefs.SetString(ProjectKeyPrefix + "_" + SelectedFilesKey, string.Join(";", selectedFiles));
        EditorPrefs.SetString(ProjectKeyPrefix + "_" + BaseFolderKey, baseFolder);
        EditorPrefs.SetString(ProjectKeyPrefix + "_" + ExcludePathsKey, string.Join(";", excludePaths));

        var sClass = new SerializableFoldoutState();
        sClass.FromDictionary(classFoldouts);
        EditorPrefs.SetString(ProjectKeyPrefix + "_" + ClassFoldoutsKey, JsonUtility.ToJson(sClass));

        var sMethod = new SerializableNestedFoldoutState();
        sMethod.FromNestedDictionary(methodFoldouts);
        EditorPrefs.SetString(ProjectKeyPrefix + "_" + MethodFoldoutsKey, JsonUtility.ToJson(sMethod));

        var sEnum = new SerializableFoldoutState();
        sEnum.FromDictionary(enumFoldouts);
        EditorPrefs.SetString(ProjectKeyPrefix + "_" + EnumFoldoutsKey, JsonUtility.ToJson(sEnum));

        var sFile = new SerializableFoldoutState();
        sFile.FromDictionary(fileFoldouts);
        EditorPrefs.SetString(ProjectKeyPrefix + "_" + FileFoldoutsKey, JsonUtility.ToJson(sFile));

        EndSelection();
    }

    private void EndSelection()
    {
        if (autoSave && selectedFiles.Count > 0)
        {
            GenerateXML();
        }

        selectedFiles.Clear();
        fileParseMap.Clear();
        AssetDatabase.Refresh();
    }

    private void GenerateXML()
    {
        Directory.CreateDirectory(backupFolder);
        foreach (var file in selectedFiles)
        {
            if (!fileParseMap.ContainsKey(file)) continue;
            var bak = Path.Combine(backupFolder, Path.GetFileName(file));
            File.Copy(file, bak, true);

            var parsed = fileParseMap[file];
            DocInjector.InjectToFile(file, parsed.Classes, parsed.Enums, parsed.Structs, parsed.Interfaces);
        }

        CustomPopup.ShowPopup(successIcon, "XML documentation injected successfully!");
    }

    void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("XML Generator", EditorStyles.boldLabel);

        if (GUILayout.Button("⚠️ Clear All Saved Preferences", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Reset Preferences",
                "Are you sure you want to delete all saved XML generator settings for this project?",
                "Yes", "Cancel"))
            {

                EditorPrefs.DeleteKey(ProjectKeyPrefix + "_" + SelectedFilesKey);
                EditorPrefs.DeleteKey(ProjectKeyPrefix + "_" + BaseFolderKey);
                EditorPrefs.DeleteKey(ProjectKeyPrefix + "_" + ClassFoldoutsKey);
                EditorPrefs.DeleteKey(ProjectKeyPrefix + "_" + MethodFoldoutsKey);
                EditorPrefs.DeleteKey(ProjectKeyPrefix + "_" + EnumFoldoutsKey);
                EditorPrefs.DeleteKey(ProjectKeyPrefix + "_" + FileFoldoutsKey);

                if (autoSave && selectedFiles.Count > 0)
                {
                    GenerateXML();
                }

                selectedFiles.Clear();
                fileParseMap.Clear();
                AssetDatabase.Refresh();

                EndSelection();
                excludePaths.Clear();

                autoSave = false;

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
            if (!string.IsNullOrEmpty(path) && !excludePaths.Contains(path))
                excludePaths.Add(path);
        }
        if (GUILayout.Button("Exclude File"))
        {
            var path = EditorUtility.OpenFilePanel("Exclude File", Application.dataPath, "cs");
            if (!string.IsNullOrEmpty(path) && !excludePaths.Contains(path))
                excludePaths.Add(path);
        }
        GUILayout.EndHorizontal();


        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Select Single File")) PickSingleFile();
        if (GUILayout.Button("Select Folder (Top-Level Only)")) PickFolderAndParse(SearchOption.TopDirectoryOnly);
        if (GUILayout.Button("Select Folder (All Subdirectories)")) PickFolderAndParse(SearchOption.AllDirectories);
        GUILayout.EndHorizontal();

        autoSave = EditorGUILayout.ToggleLeft("Auto-Save on Clear or Close", autoSave);

        GUILayout.Space(10);

        EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
        if (selectedFiles.Count > 0)
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));

            foreach (var filePath in selectedFiles)
            {
                if (!fileParseMap.ContainsKey(filePath)) continue;
                var result = fileParseMap[filePath];

                var relPath = Path.Combine(Path.GetFileName(baseFolder), Path.GetRelativePath(baseFolder, filePath)).Replace("\\", "/");
                if (!fileFoldouts.ContainsKey(relPath)) fileFoldouts[relPath] = true;

                fileFoldouts[relPath] = EditorGUILayout.Foldout(fileFoldouts[relPath], relPath, true);
                if (!fileFoldouts[relPath]) continue;

                foreach (var t in result.OrderedTypes)
                {
                    if (t is ClassInfo cls)
                    {
                        string tempSummary = cls.Summary;
                        DrawTypeEditor(filePath, cls.Name, ref tempSummary, cls.Methods, true, ref classFoldouts, ref methodFoldouts);
                        cls.Summary = tempSummary;
                    }
                    else if (t is StructInfo strct)
                    {
                        string tempSummary = strct.Summary;
                        DrawTypeEditor(filePath, strct.Name, ref tempSummary, strct.Methods, false, ref classFoldouts, ref methodFoldouts);
                        strct.Summary = tempSummary;
                    }
                    else if (t is InterfaceInfo iface)
                    {
                        string tempSummary = iface.Summary;
                        DrawTypeEditor(filePath, iface.Name, ref tempSummary, iface.Methods, false, ref classFoldouts, ref methodFoldouts, "Interface");
                        iface.Summary = tempSummary;
                    }
                    else if (t is EnumInfo en)
                    {
                        string enumKey = filePath + "::enum::" + en.Name;
                        if (!enumFoldouts.ContainsKey(enumKey)) enumFoldouts[enumKey] = true;

                        bool hasAny = !string.IsNullOrWhiteSpace(en.Summary) || en.Members.Exists(m => !string.IsNullOrWhiteSpace(m.Summary));
                        var prevColor = GUI.color;
                        if (!hasAny) GUI.color = new Color(1, 1, 1, 0.5f);

                        enumFoldouts[enumKey] = EditorGUILayout.Foldout(enumFoldouts[enumKey], $"Enum: {en.Name}", true);

                        GUI.color = prevColor;
                        if (!enumFoldouts[enumKey]) continue;

                        EditorGUILayout.BeginVertical("box");
                        prevColor = GUI.color;
                        if (string.IsNullOrWhiteSpace(en.Summary)) GUI.color = new Color(1, 1, 1, 0.5f);
                        en.Summary = EditorGUILayout.TextField("Summary", en.Summary);
                        GUI.color = prevColor;

                        foreach (var m in en.Members)
                        {
                            prevColor = GUI.color;
                            if (string.IsNullOrWhiteSpace(m.Summary)) GUI.color = new Color(1, 1, 1, 0.5f);

                            m.Summary = EditorGUILayout.TextField(
                                new GUIContent($"Member: {m.Name}", m.Name),
                                m.Summary
                            );

                            GUI.color = prevColor;
                        }
                        EditorGUILayout.EndVertical();
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);
        if (selectedFiles.Count > 0)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate XML", GUILayout.Height(30))) GenerateXML();
            if (GUILayout.Button("Clear Selection", GUILayout.Height(30)))
            {
                EndSelection();
                EditorPrefs.DeleteKey(ProjectKeyPrefix + "_" + SelectedFilesKey);
                EditorPrefs.DeleteKey(ProjectKeyPrefix + "_" + BaseFolderKey);
                EditorPrefs.DeleteKey(ProjectKeyPrefix + "_" + ClassFoldoutsKey);
                EditorPrefs.DeleteKey(ProjectKeyPrefix + "_" + MethodFoldoutsKey);
                EditorPrefs.DeleteKey(ProjectKeyPrefix + "_" + EnumFoldoutsKey);
                EditorPrefs.DeleteKey(ProjectKeyPrefix + "_" + FileFoldoutsKey);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Restore Single Backup"))
            {
                var path = EditorUtility.OpenFilePanel("Select Script to Restore", Application.dataPath, "cs");
                if (!string.IsNullOrEmpty(path))
                {
                    var bak = Path.Combine(backupFolder, Path.GetFileName(path));
                    if (File.Exists(bak))
                    {
                        File.Copy(bak, path, true);
                        CustomPopup.ShowPopup(restoreIcon, $"Backup restored for:\n{Path.GetFileName(path)}");
                        AssetDatabase.Refresh();
                    }
                    else EditorUtility.DisplayDialog("Restore Failed", "No backup found for that script.", "OK");
                }
            }
            if (GUILayout.Button("Restore Batch (Top-Level Only)"))
                PickFolderAndRestore(SearchOption.TopDirectoryOnly);
            if (GUILayout.Button("Restore Batch (All Subdirectories)"))
                PickFolderAndRestore(SearchOption.AllDirectories);
            GUILayout.EndHorizontal();
        }
    }

    void DrawTypeEditor(
        string filePath,
        string typeName,
        ref string summary,
        List<MethodInfo> methods,
        bool isClass,
        ref Dictionary<string, bool> foldouts,
        ref Dictionary<string, Dictionary<string, bool>> methodFolds,
        string labelPrefix = null
    )
    {
        string key = filePath + "::" + typeName;
        if (!foldouts.ContainsKey(key)) foldouts[key] = true;
        if (!methodFolds.ContainsKey(key)) methodFolds[key] = new();

        bool hasDoc = !string.IsNullOrWhiteSpace(summary);
        var prevColor = GUI.color;
        if (!hasDoc) GUI.color = new Color(1, 1, 1, 0.5f);

        string label = (labelPrefix ?? (isClass ? "Class" : "Struct")) + ": " + typeName;
        foldouts[key] = EditorGUILayout.Foldout(foldouts[key], new GUIContent(label), true);

        GUI.color = prevColor;
        if (!foldouts[key]) return;

        EditorGUILayout.BeginVertical("box");

        prevColor = GUI.color;
        if (!hasDoc) GUI.color = new Color(1, 1, 1, 0.5f);
        summary = EditorGUILayout.TextField("Summary", summary);
        GUI.color = prevColor;

        foreach (var m in methods)
        {
            string mKey = key + "::" + m.UniqueSignature;
            if (!methodFolds[key].ContainsKey(mKey)) methodFolds[key][mKey] = true;

            bool hasAny = !string.IsNullOrWhiteSpace(m.Summary) ||
                        (!m.ReturnType.Equals("void", System.StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(m.ReturnDescription)) ||
                        m.Parameters.Exists(p => !string.IsNullOrWhiteSpace(p.Description));

            prevColor = GUI.color;
            if (!hasAny) GUI.color = new Color(1, 1, 1, 0.5f);
            methodFolds[key][mKey] = EditorGUILayout.Foldout(methodFolds[key][mKey], $"Method: {m.Name} ({m.ReturnType})", true);
            GUI.color = prevColor;

            if (!methodFolds[key][mKey]) continue;

            EditorGUILayout.BeginVertical("box");

            prevColor = GUI.color;
            if (string.IsNullOrWhiteSpace(m.Summary)) GUI.color = new Color(1, 1, 1, 0.5f);
            m.Summary = EditorGUILayout.TextField("Summary", m.Summary);
            GUI.color = prevColor;

            foreach (var p in m.Parameters)
            {
                prevColor = GUI.color;
                if (string.IsNullOrWhiteSpace(p.Description)) GUI.color = new Color(1, 1, 1, 0.5f);

                p.Description = EditorGUILayout.TextField(
                    new GUIContent($"Param: {p.Type} {p.Name}", $"{p.Type} {p.Name}"),
                    p.Description
                );

                GUI.color = prevColor;
            }

            if (!m.ReturnType.Equals("void", System.StringComparison.OrdinalIgnoreCase))
            {
                prevColor = GUI.color;
                if (string.IsNullOrWhiteSpace(m.ReturnDescription)) GUI.color = new Color(1, 1, 1, 0.5f);
                m.ReturnDescription = EditorGUILayout.TextField("Return Description", m.ReturnDescription);
                GUI.color = prevColor;
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndVertical();
    }

    void PickSingleFile()
    {
        var path = EditorUtility.OpenFilePanel("Select C# Script", Application.dataPath, "cs");
        if (!string.IsNullOrEmpty(path))
        {
            selectedFiles = new List<string> { path };
            baseFolder = Path.GetDirectoryName(path);
            ParseFiles();
        }
    }

    void PickFolderAndParse(SearchOption opt)
    {
        var folder = EditorUtility.OpenFolderPanel("Select Folder", Application.dataPath, "");
        if (string.IsNullOrEmpty(folder)) return;
        baseFolder = folder;
        selectedFiles = new List<string>(Directory.GetFiles(folder, "*.cs", opt));
        ParseFiles();
    }

    void ParseFiles()
    {
        fileParseMap.Clear();
        List<string> validFiles = new();

        foreach (var file in selectedFiles)
        {
            if (!Path.GetExtension(file).Equals(".cs", System.StringComparison.OrdinalIgnoreCase))
                continue;

            string normalizedFile = Path.GetFullPath(file).Replace('\\', '/');
            bool shouldExclude = excludePaths.Any(ex =>
                normalizedFile.StartsWith(Path.GetFullPath(ex).Replace('\\', '/')) ||
                normalizedFile.Contains(ex.Replace('\\', '/'))
            );

            if (shouldExclude)
                continue;

            validFiles.Add(file);
        }

        if (validFiles.Count == 0)
        {
            selectedFiles.Clear();
            baseFolder = "";
            return;
        }

        foreach (var file in validFiles)
        {
            fileParseMap[file] = CSharpParser.ParseFile(file);
        }
    }

    void PickFolderAndRestore(SearchOption opt)
    {
        var folder = EditorUtility.OpenFolderPanel("Select Folder of Scripts to Restore", Application.dataPath, "");
        if (string.IsNullOrEmpty(folder)) return;
        int restored = 0;
        foreach (var file in Directory.GetFiles(folder, "*.cs", opt))
        {
            var bak = Path.Combine(backupFolder, Path.GetFileName(file));
            if (File.Exists(bak))
            {
                File.Copy(bak, file, true);
                restored++;
            }
        }
        if (restored > 0)
            CustomPopup.ShowPopup(restoreIcon, $"{restored} backup(s) restored!");
        else
            EditorUtility.DisplayDialog("Restore Failed", "No backups found in selected folder.", "OK");
        AssetDatabase.Refresh();
    }
}



[System.Serializable]
public class SerializableFoldoutState
{
    public List<string> keys = new();
    public List<bool> values = new();

    public void FromDictionary(Dictionary<string, bool> dict)
    {
        keys = new List<string>(dict.Keys);
        values = new List<bool>(dict.Values);
    }

    public Dictionary<string, bool> ToDictionary()
    {
        var dict = new Dictionary<string, bool>();
        for (int i = 0; i < keys.Count; i++)
            dict[keys[i]] = (i < values.Count) ? values[i] : true;
        return dict;
    }
}

[System.Serializable]
public class SerializableNestedFoldoutState
{
    public List<string> outerKeys = new();
    public List<SerializableFoldoutState> innerStates = new();

    public void FromNestedDictionary(Dictionary<string, Dictionary<string, bool>> dict)
    {
        outerKeys.Clear();
        innerStates.Clear();
        foreach (var kv in dict)
        {
            outerKeys.Add(kv.Key);
            var s = new SerializableFoldoutState();
            s.FromDictionary(kv.Value);
            innerStates.Add(s);
        }
    }

    public Dictionary<string, Dictionary<string, bool>> ToNestedDictionary()
    {
        var dict = new Dictionary<string, Dictionary<string, bool>>();
        for (int i = 0; i < outerKeys.Count; i++)
        {
            dict[outerKeys[i]] = (i < innerStates.Count) ? innerStates[i].ToDictionary() : new Dictionary<string, bool>();
        }
        return dict;
    }
}
