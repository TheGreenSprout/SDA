/*
⚠️‼️ AI ASSISTED CODE

This code was written with the assistance of AI.
*/



using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class StructureGenerator : EditorWindow
{
    private string selectedFolder;
    private Vector2 scrollPos;
    private Vector2 exclusionScrollPos;
    private List<string> structureLines = new List<string>();
    private bool structureBuilt = false;
    private bool includeLegend = true;
    public bool includeExcluded = true;
    private string rootFolderName;

    private Texture2D headerImage;
    private Texture2D successIcon;
    private Texture2D restoreIcon;
    private Texture2D trashIcon;

    private List<string> excludePaths = new();
    private const string ExcludePrefsKey = "StructureGenerator_ExcludePaths";
    private string ProjectKey => Application.dataPath.GetHashCode().ToString();




    public static void Open()
    {
        var window = GetWindow<StructureGenerator>("Project Structure Generator");
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
        GUILayout.Label("Structure Generator", EditorStyles.boldLabel);

        if (GUILayout.Button("⚠️ Clear All Saved Preferences", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Reset Preferences", "Are you sure you want to delete all saved Structure Generator settings for this project?", "Yes", "Cancel"))
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
                structureBuilt = false;
                structureLines.Clear();
            }
        }
        GUILayout.Label(string.IsNullOrEmpty(selectedFolder) ? "No folder selected" : selectedFolder, EditorStyles.wordWrappedLabel);
        GUILayout.EndHorizontal();
        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(selectedFolder)))
        {
            if (GUILayout.Button("Generate Structure", GUILayout.Height(30)))
            {
                structureLines = BuildStructureInternal(selectedFolder);
                structureBuilt = true;
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
        
        includeLegend = GUILayout.Toggle(includeLegend, includeLegend ? "Legend Included" : "Legend Excluded", "Button", GUILayout.Height(30), GUILayout.Width(140));
        GUILayout.EndHorizontal();
        GUILayout.Space(10);

        if (structureBuilt)
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
            foreach (var line in structureLines)
            {
                GUILayout.Label(line);
                GUILayout.Space(2);
            }
            EditorGUILayout.EndScrollView();
            GUILayout.Space(5);
            if (GUILayout.Button("Save Structure…", GUILayout.Height(30)))
                SaveStructure();
        }
    }

    List<string> BuildStructureInternal(string root)
    {
        var lines = new List<string>();
        if (includeLegend)
        {
            lines.Add("📁 --> Folder/Namespace");
            lines.Add("🎨 --> Assets");
            lines.Add("🏗️ --> Prefabs");
            lines.Add("🛠️ --> Script folders");
            lines.Add("📜 --> Scripts");
            lines.Add("📝 --> Documentation");
            lines.Add("⚙️ --> Plugin");
            lines.Add("🧩 --> Class");
            lines.Add("🧱 --> Struct");
            lines.Add("🔌 --> Interface");
            lines.Add("🔢 --> Enum");
            lines.Add("");
        }

        lines.Add($"📁 {rootFolderName}");
        lines.Add("");
        TraverseInternal(root, 1, lines);

        for (int i = lines.Count - 1; i >= 0 && string.IsNullOrWhiteSpace(lines[i]); i--)
            lines.RemoveAt(i);

        return lines;
    }

    void TraverseInternal(string path, int depth, List<string> lines)
    {
        path = Path.GetFullPath(path).Replace('\\', '/').TrimEnd('/');
        if (excludePaths.Any(ex => path.StartsWith(ex))) return;

        string indent = new string(' ', depth * 4);

        foreach (var dir in Directory.GetDirectories(path))
        {
            string normDir = Path.GetFullPath(dir).Replace('\\', '/').TrimEnd('/');
            if (excludePaths.Any(ex => normDir.StartsWith(ex))) continue;

            lines.Add($"{indent}📁 {Path.GetFileName(dir)}");
            lines.Add("");
            TraverseInternal(dir, depth + 1, lines);
        }

        foreach (var file in Directory.GetFiles(path))
        {
            string normFile = Path.GetFullPath(file).Replace('\\', '/').TrimEnd('/');
            if (excludePaths.Any(ex => normFile.StartsWith(ex))) continue;

            var ext = Path.GetExtension(file).ToLower();
            if (ext == ".meta") continue;

            if (ext == ".cs")
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var result = CSharpParser.ParseFile(file);
                var ordered = result.OrderedTypes;
                bool singleClass = result.Classes.Count == 1 && result.Classes[0].Name == fileName && result.Enums.Count == 0;
                bool singleStruct = result.Structs.Count == 1 && result.Structs[0].Name == fileName && result.Enums.Count == 0;
                bool singleInterface = result.Interfaces.Count == 1 && result.Interfaces[0].Name == fileName && result.Enums.Count == 0;

                if (singleClass)
                {
                    var cls = (ClassInfo)ordered.First();
                    var vars = cls.Fields.Select(f => f.Type).Distinct().ToList();
                    var varBlock = vars.Count > 0 ? " {" + string.Join(", ", vars) + "}" : "";
                    lines.Add($"{indent}📜🧩 {fileName}{varBlock}");
                    lines.Add("");
                }
                else if (singleStruct)
                {
                    var strct = (StructInfo)ordered.First();
                    var vars = strct.Fields.Select(f => f.Type).Distinct().ToList();
                    var varBlock = vars.Count > 0 ? " {" + string.Join(", ", vars) + "}" : "";
                    lines.Add($"{indent}📜🧱 {fileName}{varBlock}");
                    lines.Add("");
                }
                else if (singleInterface)
                {
                    lines.Add($"{indent}📜🔌 {fileName}");
                    lines.Add("");
                }
                else
                {
                    lines.Add($"{indent}📜 {fileName}");
                    lines.Add("");
                    foreach (var t in ordered)
                    {
                        string subIndent = new string(' ', (depth + 1) * 4);
                        if (t is ClassInfo cls)
                        {
                            var vars = cls.Fields.Select(f => f.Type).Distinct().ToList();
                            var varBlock = vars.Count > 0 ? " {" + string.Join(", ", vars) + "}" : "";
                            lines.Add($"{subIndent}🧩 {cls.Name}{varBlock}");
                        }
                        else if (t is StructInfo st)
                        {
                            var vars = st.Fields.Select(f => f.Type).Distinct().ToList();
                            var varBlock = vars.Count > 0 ? " {" + string.Join(", ", vars) + "}" : "";
                            lines.Add($"{subIndent}🧱 {st.Name}{varBlock}");
                        }
                        else if (t is InterfaceInfo iface)
                        {
                            lines.Add($"{subIndent}🔌 {iface.Name}");
                        }
                        else if (t is EnumInfo en)
                        {
                            lines.Add($"{subIndent}🔢 {en.Name}");
                        }
                        lines.Add("");
                    }
                }
            }
            else
            {
                string icon = ext switch
                {
                    ".prefab" => "🏗️",
                    ".md" or ".txt" => "📝",
                    _ => "🎨"
                };
                lines.Add($"{indent}{icon} {Path.GetFileName(file)}");
                lines.Add("");
            }
        }
    }

    void SaveStructure()
    {
        var defaultName = rootFolderName + "_Structure.txt";
        var fullPath = EditorUtility.SaveFilePanel("Save Structure As", Application.dataPath, defaultName, "txt");
        if (string.IsNullOrEmpty(fullPath)) return;

        File.WriteAllLines(fullPath, structureLines);
        AssetDatabase.Refresh();
        CustomPopup.ShowPopup(restoreIcon, $"Project structure saved:\n{Path.GetFileName(fullPath)}");
    }
}
