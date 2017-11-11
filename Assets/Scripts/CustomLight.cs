using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
[CanEditMultipleObjects]
[CustomEditor(typeof(CustomLight))]
public class CustomLightEditor : Editor {
    public override void OnInspectorGUI() {
        CustomLight l = target as CustomLight;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("color"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("intensity"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cullingMask"));

        CustomLight.CustomLightType t = l.type;
        bool same = true;
        foreach (Object cl in targets) 
            if (((CustomLight)cl).type != t) {
                same = false;
                break;
            }
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("type"));
        if (same) {
            switch (l.type) {
                case CustomLight.CustomLightType.Box:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("boxRadius"), new GUIContent("Extents"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("innerBoxRadius"), new GUIContent("Inner Extents"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("boxEdgeRadius"), new GUIContent("Edge Radius"));
                    break;
                case CustomLight.CustomLightType.Capsule:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("capsuleRadius"), new GUIContent("Radius"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("capsuleLength"), new GUIContent("Length"));
                    break;
            }
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif

[ExecuteInEditMode]
public class CustomLight : MonoBehaviour {
    public static List<CustomLight> activeLights = new List<CustomLight>();

    public enum CustomLightType {
        Box, Capsule
    }

    public LayerMask cullingMask = ~0;
    public CustomLightType type;
    public Vector3 boxRadius = Vector3.one;
    public Vector3 innerBoxRadius = Vector3.one;
    public float boxEdgeRadius = .25f;
    public float frustumEdgeRadius = .25f;
    public float capsuleRadius = 1;
    public float capsuleLength = 2;

    public Color color = Color.white;
    public float intensity = 1f;
    
    Mesh capsuleMesh;

    void OnValidate() {
        if (boxEdgeRadius > innerBoxRadius.x) boxEdgeRadius = innerBoxRadius.x;
        if (boxEdgeRadius > innerBoxRadius.y) boxEdgeRadius = innerBoxRadius.y;
        if (boxEdgeRadius > innerBoxRadius.z) boxEdgeRadius = innerBoxRadius.z;
        if (boxEdgeRadius < 0) boxEdgeRadius = 0;
        if (frustumEdgeRadius < 0) frustumEdgeRadius = 0;
        if (innerBoxRadius.x < 0) innerBoxRadius.x = 0;
        if (innerBoxRadius.y < 0) innerBoxRadius.y = 0;
        if (innerBoxRadius.z < 0) innerBoxRadius.z = 0;
        if (boxRadius.x < innerBoxRadius.x + .01f) boxRadius.x = innerBoxRadius.x + .01f;
        if (boxRadius.y < innerBoxRadius.y + .01f) boxRadius.y = innerBoxRadius.y + .01f;
        if (boxRadius.z < innerBoxRadius.z + .01f) boxRadius.z = innerBoxRadius.z + .01f;
        if (intensity < 0) intensity = 0;
        if (capsuleRadius < 0) capsuleRadius = 0;
    }

    void OnEnable() {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsuleMesh = g.GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(g);

        activeLights.Add(this);
    }
    void OnDisable() {
        activeLights.Remove(this);
    }

    void OnDrawGizmos() {
        Gizmos.DrawIcon(transform.position, "AreaLight Gizmo", true);
    }
    void OnDrawGizmosSelected() {
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.color = color;
        
        switch (type) {
            case CustomLightType.Box:
                Gizmos.DrawWireCube(Vector3.zero, 2f * boxRadius);
                Gizmos.DrawWireCube(Vector3.zero, 2f * innerBoxRadius);
                break;
            case CustomLightType.Capsule:
                DebugExtension.DrawCapsule(new Vector3(0f, 0f, -capsuleLength * .5f - capsuleRadius), new Vector3(0f, 0f, capsuleLength * .5f + capsuleRadius), capsuleRadius);
                Gizmos.DrawLine(new Vector3(0f, 0f, -capsuleLength * .5f), new Vector3(0f, 0f, capsuleLength * .5f));
                break;
        }

        Gizmos.matrix = Matrix4x4.identity;
    }
}
