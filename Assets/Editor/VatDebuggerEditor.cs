using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VATDebugger))]
public class VATDebuggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        VATDebugger dbg = (VATDebugger)target;

        if (dbg.vat == null)
        {
            EditorGUILayout.HelpBox("VATController non assigné", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("VAT Debug Controls", EditorStyles.boldLabel);

        if (GUILayout.Button("Pause"))
            dbg.vat.Pause();
        if (GUILayout.Button("Resume"))
            dbg.vat.Resume();
        if (GUILayout.Button("Toggle Pause"))
            dbg.vat.TogglePause();

        EditorGUILayout.Space();

        if (GUILayout.Button("Stop (Default)"))
            dbg.vat.Stop();
        EditorGUILayout.BeginHorizontal();
        dbg.stopIndex = EditorGUILayout.IntField("Index", dbg.stopIndex);
        if (GUILayout.Button("Stop Index"))
            dbg.vat.Stop(dbg.stopIndex);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Speed", EditorStyles.boldLabel);

        if (Application.isPlaying)
        {
            EditorGUILayout.BeginHorizontal();
            dbg.vat.Speed = EditorGUILayout.Slider(dbg.vat.Speed, 0f, 3f);
            if (GUILayout.Button("Reset", GUILayout.Width(50)))
                dbg.vat.Speed = 1f;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("x0.25")) dbg.vat.Speed = 0.25f;
            if (GUILayout.Button("x0.5")) dbg.vat.Speed = 0.5f;
            if (GUILayout.Button("x1")) dbg.vat.Speed = 1f;
            if (GUILayout.Button("x2")) dbg.vat.Speed = 2f;
            if (GUILayout.Button("x3")) dbg.vat.Speed = 3f;
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("Speed disponible uniquement en Play Mode", MessageType.Info);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("State", dbg.vat.IsPaused ? "⏸ Paused" : "▶ Playing");
    }
}