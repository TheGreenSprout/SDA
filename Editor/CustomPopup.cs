/*
⚠️‼️ AI ASSISTED CODE

This code was written with the assistance of AI.
*/



using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

public class CustomPopup : EditorWindow
{
    private Texture2D icon;


    private string message;


    private const string defaultAudioLocation = "finishedSFX";

    private AudioClip soundClip;




    #region XML doc
    /// <summary>
    /// Shows a utility popup and plays an optional sound.
    /// </summary>
    /// <param name="icon">Icon to display in the popup.</param>
    /// <param name="message">Message text.</param>
    /// <param name="soundClip">(Optional) AudioClip to play. If null, EditorApplication.Beep() will be used.</param>
    #endregion
    public static void ShowPopup(Texture2D icon, string message, AudioClip soundClip = null, string soundLocation = null)
    {
        var w = CreateInstance<CustomPopup>();

        w.icon = icon;
        w.message = message;
        w.soundClip = soundClip;

        w.titleContent = new GUIContent("Success");


        const float popupWidth = 210f;
        const float popupHeight = 165f;

        w.position = new Rect(
            (Screen.width - popupWidth) / 2f,
            (Screen.height - popupHeight) / 2f,
            popupWidth,
            popupHeight
        );

        w.minSize = w.maxSize = new Vector2(popupWidth, popupHeight);

        w.ShowUtility();


        if (soundLocation != null)
        {
            w.PlaySound(soundLocation);
        }
        else
        {
            w.PlaySound(defaultAudioLocation);
        }
    }



    #region XML doc
    /// <summary>
    /// Uses UnityEditor.AudioUtil via reflection to play the clip, or falls back to a beep.
    /// </summary>
    #endregion
    private void PlaySound(string soundLocation)
    {
        // Get audio clip
        AudioClip clip = soundClip != null
            ? soundClip
            : Resources.Load<AudioClip>(soundLocation);


        // If no sound is found, use default beep
        if (clip == null)
        {
            Debug.LogWarning($"Could not find sound clip at Resources/{soundLocation}; playing default beep.");

            EditorApplication.Beep();

            return;
        }


        // Try UnityEditor.AudioUtil via reflection
        var audioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");

        if (audioUtilType != null)
        {
            var methodNames = new[] { "PlayPreviewClip", "PlayClip" };

            foreach (var name in methodNames)
            {
                var m = audioUtilType.GetMethod(
                    name,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
                    null
                );


                if (m != null)
                {
                    m.Invoke(null, new object[] { clip, 0, false });

                    return;
                }
            }
        }


        // Fallback, play default beep
        Debug.LogWarning("Could not find AudioUtil.PlayPreviewClip; playing default beep.");

        EditorApplication.Beep();
    }



    void OnGUI()
    {
        GUILayout.Space(20);


        if (icon != null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.Label(icon, GUILayout.Width(64), GUILayout.Height(64));

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }


        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(
                message,
                new GUIStyle(EditorStyles.wordWrappedLabel) { alignment = TextAnchor.MiddleCenter },
                GUILayout.Width(210)
            );
            GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();


        GUILayout.FlexibleSpace();

        if (GUILayout.Button("OK"))
        {
            Close();
        }
    }
}