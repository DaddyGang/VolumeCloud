﻿using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WeatherMap))]
public class WeatherMapEditor : Editor
{
    WeatherMap weather;
    Editor noiseSettingsEditor;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (weather.noiseSettings != null)
        {
            DrawSettingsEditor(weather.noiseSettings, ref weather.showSettingsEditor, ref noiseSettingsEditor);
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Update"))
        {
            weather.ManualUpdate();
            EditorApplication.QueuePlayerLoopUpdate();
        }
        if (GUILayout.Button("Save"))
        {

        }
        if (GUILayout.Button("Load"))
        {

        }
        GUILayout.EndHorizontal();

    }



    void DrawSettingsEditor(Object settings, ref bool foldout, ref Editor editor)
    {
        if (settings != null)
        {
            foldout = EditorGUILayout.InspectorTitlebar(foldout, settings);
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                if (foldout)
                {
                    CreateCachedEditor(settings, null, ref editor);
                    editor.OnInspectorGUI();
                }
            }
        }
    }

    void OnEnable()
    {
        weather = (WeatherMap)target;
    }
}
