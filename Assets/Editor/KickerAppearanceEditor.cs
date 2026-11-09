#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(KickerAppearance))]
[CanEditMultipleObjects]
public sealed class KickerAppearanceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Choose Helix Color and Boot Color, then apply custom colors. " +
            "Use Theme Colors restores the current competition palette. " +
            "Changes made in Play Mode are temporary.", MessageType.Info);

        if (GUILayout.Button("Apply Custom Colors"))
        {
            foreach (Object item in targets)
                ((KickerAppearance)item).ApplyCustomColors();
        }

        if (GUILayout.Button("Use Theme Colors"))
        {
            foreach (Object item in targets)
                ((KickerAppearance)item).UseThemeColors();
        }
    }
}
#endif
