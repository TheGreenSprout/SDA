using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class ToolWindow : EditorWindow
{
    private Texture2D headerImage;
    private Texture2D successIcon;
    private Texture2D restoreIcon;
    private Texture2D trashIcon;
    private GUIStyle titleStyle;

    private Vector2 exclusionScrollPos;

    public bool includeExcluded = true;

    private const string websiteUrl = "https://sprouts-garden.netlify.app";

    private List<string> globalExcludes = new();
    private const string GlobalExcludePrefsKey = "Global_Excluded_Paths";
    private string ProjectKey => Application.dataPath.GetHashCode().ToString();

    [MenuItem("Tools/Sprout's Doc Assistant")]
    public static void Open()
    {
        var window = GetWindow<ToolWindow>("Sprout's Doc Assistant");
        window.minSize = new Vector2(500, 600);
        window.Show();
    }

    void OnEnable()
    {
        headerImage = Resources.Load<Texture2D>("tool_header");
        successIcon = Resources.Load<Texture2D>("success");
        restoreIcon = Resources.Load<Texture2D>("restore_success");
        trashIcon = Resources.Load<Texture2D>("trash_success");

        if (EditorPrefs.HasKey(ProjectKey + "_" + GlobalExcludePrefsKey))
        {
            globalExcludes = EditorPrefs.GetString(ProjectKey + "_" + GlobalExcludePrefsKey)
                .Split(';')
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => Path.GetFullPath(p).Replace('\\', '/').TrimEnd('/'))
                .Distinct()
                .ToList();
        }
    }

    void OnDisable()
    {
        EditorPrefs.SetString(ProjectKey + "_" + GlobalExcludePrefsKey, string.Join(";", globalExcludes));
    }

    void OnGUI()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };

            // Force green text
            titleStyle.normal.textColor = Color.green;
            titleStyle.active.textColor = Color.green;
            titleStyle.focused.textColor = Color.green;
            titleStyle.hover.textColor = Color.green;
            titleStyle.onNormal.textColor = Color.green;
            titleStyle.onActive.textColor = Color.green;
        }

        

        GUILayout.BeginHorizontal();
        GUILayout.Label("SDA Tool Menu", EditorStyles.boldLabel);

        if (GUILayout.Button("⚠️ Clear All Tool Preferences", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Master Reset",
                "This will clear ALL saved preferences for ALL tools in this project.\n\nThis includes:\n- Global excludes\n- XML Generator\n- Info Generator\n- Structure Generator\n- Namespace Generator\n\nAre you sure?",
                "Yes, Clear Everything", "Cancel"))
            {
                // Global
                EditorPrefs.DeleteKey(ProjectKey + "_" + GlobalExcludePrefsKey);
                globalExcludes.Clear();

                // XmlGenerator
                var prefix = ProjectKey + "_";
                EditorPrefs.DeleteKey(prefix + "XmlGenerator_ExcludePaths");
                EditorPrefs.DeleteKey(prefix + "XmlGenerator_SelectedFiles");
                EditorPrefs.DeleteKey(prefix + "XmlGenerator_BaseFolder");
                EditorPrefs.DeleteKey(prefix + "XmlGen_ClassFoldouts");
                EditorPrefs.DeleteKey(prefix + "XmlGen_MethodFoldouts");
                EditorPrefs.DeleteKey(prefix + "XmlGen_EnumFoldouts");
                EditorPrefs.DeleteKey(prefix + "XmlGen_FileFoldouts");
                EditorPrefs.DeleteKey(prefix + "XmlGenerator_AutoSave");

                // StructureGenerator
                EditorPrefs.DeleteKey(prefix + "StructureGenerator_ExcludePaths");

                // InfoGenerator
                EditorPrefs.DeleteKey(prefix + "InfoGenerator_ExcludePaths");

                // NamespaceGenerator
                EditorPrefs.DeleteKey(prefix + "NamespaceAssigner_ExcludePaths");
                EditorPrefs.DeleteKey(prefix + "NamespaceAssigner_IncludeRoot");
                EditorPrefs.DeleteKey(prefix + "NamespaceAssigner_BaseFolder");
                EditorPrefs.DeleteKey(prefix + "NamespaceAssigner_FolderNamespaceMap");

                /*XmlGenerator.ClearAllPreferences();
                InfoGenerator.ClearAllPreferences();
                StructureGenerator.ClearAllPreferences();
                NamespaceGenerator.ClearAllPreferences();*/

                //EditorUtility.DisplayDialog("Preferences Cleared", "All tool preferences have been reset.", "OK");
                CustomPopup.ShowPopup(trashIcon, "Global preferences deleted...");
            }
        }
        GUILayout.EndHorizontal();


        GUILayout.FlexibleSpace();

        // Header image
        if (headerImage != null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(headerImage, GUILayout.Width(128), GUILayout.Height(128));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        // Title
        GUILayout.Label("Sprout's Doc Assistant", titleStyle);
        GUILayout.Space(20);

        // Tool buttons
        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("XML Generator", GUILayout.Width(300), GUILayout.Height(50)))
        {
            XmlGenerator.SetGlobalExcludes(globalExcludes);
            XmlGenerator.Open();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Structure Generator", GUILayout.Width(300), GUILayout.Height(50)))
        {
            StructureGenerator.SetGlobalExcludes(globalExcludes);
            StructureGenerator.Open();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Information Generator", GUILayout.Width(300), GUILayout.Height(50)))
        {
            InfoGenerator.SetGlobalExcludes(globalExcludes);
            InfoGenerator.Open();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Namespace Generator", GUILayout.Width(300), GUILayout.Height(50)))
        {
            NamespaceGenerator.SetGlobalExcludes(globalExcludes);
            NamespaceGenerator.Open();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(10);

        GUILayout.EndVertical();

        GUILayout.Space(15);
        //GUILayout.Label("Global Excluded Paths", EditorStyles.boldLabel);
        
        includeExcluded = GUILayout.Toggle(includeExcluded, includeExcluded ? "Global Excluded paths" : "Global Excluded paths", "Button", GUILayout.Height(30), GUILayout.Width(140));

        if (includeExcluded)
        {
            if (globalExcludes.Count == 0)
            {
                EditorGUILayout.HelpBox("No excluded paths.", MessageType.Info);
            }
            else
            {
                exclusionScrollPos = EditorGUILayout.BeginScrollView(exclusionScrollPos, GUILayout.ExpandHeight(false));
                for (int i = 0; i < globalExcludes.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    globalExcludes[i] = EditorGUILayout.TextField(globalExcludes[i]);
                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        globalExcludes.RemoveAt(i);
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
                if (!globalExcludes.Contains(path)) globalExcludes.Add(path);
            }
        }
        if (GUILayout.Button("Exclude File"))
        {
            var path = EditorUtility.OpenFilePanel("Exclude File", Application.dataPath, "cs");
            if (!string.IsNullOrEmpty(path))
            {
                path = Path.GetFullPath(path).Replace('\\', '/').TrimEnd('/');
                if (!globalExcludes.Contains(path)) globalExcludes.Add(path);
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();

        // Website button
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Visit My Website!", EditorStyles.miniButton, GUILayout.Width(200), GUILayout.Height(30)))
        {
            Application.OpenURL(websiteUrl);
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }
}
