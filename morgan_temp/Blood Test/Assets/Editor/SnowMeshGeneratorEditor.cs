using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SnowMeshGenerator))]
public class SnowMeshGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        SnowMeshGenerator generator = (SnowMeshGenerator)target;

        if (GUILayout.Button("Generate Snow Mesh"))
        {
            generator.Generate();
        }
    }
}
