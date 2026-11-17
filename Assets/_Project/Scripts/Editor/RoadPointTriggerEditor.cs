using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoadPointTrigger))]
public class RoadPointTriggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Поставить на дорогу", GUILayout.Height(28f)))
        {
            var trigger = (RoadPointTrigger)target;
            if (trigger.EditorSnapToRoad())
                Debug.Log($"{trigger.name}: поставлен на {trigger.DistanceMeters:0} м");
        }
    }
}
