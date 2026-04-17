using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VATSequencerDebugger))]
public class VATSequencerDebuggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VATSequencerDebugger dbg = (VATSequencerDebugger)target;

        if (dbg.sequencer == null)
        {
            EditorGUILayout.HelpBox("VATSequencer non assigné", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("VAT Sequencer Debug", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Disponible uniquement en Play Mode", MessageType.Info);
            return;
        }

        var seq = dbg.sequencer;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("State", EditorStyles.boldLabel);

        string stateLabel = !seq.IsStarted
            ? "⏹ Non démarré"
            : seq.IsFinished
                ? "✅ Terminé"
                : $"▶ Step {seq.CurrentStepIndex + 1} / {seq.steps.Count}";

        EditorGUILayout.LabelField("Status", stateLabel);

        if (seq.CurrentStep.HasValue)
        {
            var step = seq.CurrentStep.Value;
            EditorGUILayout.LabelField("VAT actif", step.vat != null ? step.vat.name : "—");
            EditorGUILayout.LabelField("Anim index", step.animIndex.ToString());
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);

        if (GUILayout.Button("▶  Start Sequence"))
            seq.StartSequence();

        if (GUILayout.Button("⏭  Next Step"))
            seq.NextStep();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Jump to Step", EditorStyles.boldLabel);

        for (int i = 0; i < seq.steps.Count; i++)
        {
            var step = seq.steps[i];
            string label = step.vat != null ? step.vat.name : $"Step {i}";
            bool isCurrent = i == seq.CurrentStepIndex;

            GUI.enabled = !isCurrent;
            if (GUILayout.Button(isCurrent ? $"● {i} — {label} (actif)" : $"  {i} — {label}"))
                seq.GoToStep(i);
            GUI.enabled = true;
        }
    }
}