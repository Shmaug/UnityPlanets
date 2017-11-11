using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
[ImageEffectAllowedInSceneView]
public class CustomLightRenderer : MonoBehaviour {
    Camera cam;
    Material deferredMat;
    CommandBuffer customLightBuf;
    Mesh boxMesh;
    Mesh quadMesh;
    float nearRadiusSqr;

    void OnEnable() {
        cam = GetComponent<Camera>();
        customLightBuf = new CommandBuffer();
        customLightBuf.name = "Deferred Custom Lights";
        cam.AddCommandBuffer(CameraEvent.AfterLighting, customLightBuf);
    }
    void OnDisable() {
        if (deferredMat) DestroyImmediate(deferredMat);
        if (customLightBuf != null) {
            cam.RemoveCommandBuffer(CameraEvent.AfterLighting, customLightBuf);
            customLightBuf.Dispose();
            customLightBuf = null;
        }
        if (quadMesh)
            DestroyImmediate(quadMesh);
    }
    
    void SetupQuad() {
        if (!quadMesh) {
            quadMesh = new Mesh();
            quadMesh.SetVertices(new List<Vector3>() {
                new Vector3(0, 1, 0),
                new Vector3(1, 1, 0),
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0) });
            quadMesh.SetIndices(new int[] { 0, 1, 2, 2, 1, 3 }, MeshTopology.Triangles, 0);
        }
        
        float y = Mathf.Tan(Mathf.Deg2Rad * cam.fieldOfView * .5f);
        float x = y * cam.aspect;
        Vector3 n1 = new Vector3(x, -y, -1f).normalized;
        Vector3 n2 = new Vector3(-x, -y, -1f).normalized;
        Vector3 n3 = new Vector3(x, y, -1f).normalized;
        Vector3 n4 = new Vector3(-x, y, -1f).normalized;
        quadMesh.SetNormals(new List<Vector3>() { n1, n2, n3, n4 });

        // radius^2 of a sphere that has a radius of length from the camera position to a corner of the near plane
        nearRadiusSqr = (x * x + y * y + 1f) * cam.nearClipPlane * cam.nearClipPlane;
    }

    void DrawQuad() {
        customLightBuf.SetGlobalFloat("_LightAsQuad", 1f);
        customLightBuf.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.Ortho(0, 1, 0, 1, -1, 100));
        customLightBuf.DrawMesh(quadMesh, Matrix4x4.identity, deferredMat, 0, 0);
        customLightBuf.SetViewProjectionMatrices(cam.worldToCameraMatrix, cam.projectionMatrix);
        customLightBuf.SetGlobalFloat("_LightAsQuad", 0f);
    }
    
    void DrawLight(CustomLight l, Vector3 pos, Quaternion rot, Vector3 size) {
        Vector3 relpos = Quaternion.Inverse(rot) * (transform.position - pos); // oriented bounding box cheat
        
        // get box closest point to sphere center by clamping
        float x = Mathf.Clamp(relpos.x, -size.x * .5f, size.x * .5f) - relpos.x;
        float y = Mathf.Clamp(relpos.y, -size.y * .5f, size.y * .5f) - relpos.y;
        float z = Mathf.Clamp(relpos.z, -size.z * .5f, size.z * .5f) - relpos.z;

        if (x * x + y * y + z * z < nearRadiusSqr)
            DrawQuad(); // Draw a fullscreen quad if the light's box intersects with the camera near-plane boundingsphere (see SetupQuad())
        else
            customLightBuf.DrawMesh(boxMesh, Matrix4x4.TRS(pos, rot, size), deferredMat, 0, 0);
    }
    
    void OnPreRender() {
        if (!boxMesh) {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boxMesh = g.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(g);
        }
        
        if (!deferredMat) deferredMat = new Material(Shader.Find("Hidden/Internal-DeferredShading"));
        
        SetupQuad();
        
        customLightBuf.Clear();
        customLightBuf.BeginSample("Custom Light Draw");
        Vector4 lightpos;
        foreach (CustomLight l in CustomLight.activeLights) {
            if ((l.cullingMask & cam.cullingMask) == 0) continue;

            customLightBuf.SetGlobalMatrix("unity_WorldToLight", l.transform.worldToLocalMatrix);
            customLightBuf.SetGlobalColor("_LightColor", l.color * l.intensity);
            lightpos = l.transform.position;
            switch (l.type) {
                case CustomLight.CustomLightType.Capsule:
                    lightpos.w = 1f / (l.capsuleRadius * l.capsuleRadius);
                    customLightBuf.SetGlobalVector("_LightPos", lightpos);
                    customLightBuf.SetGlobalVector("_CustomLightParams0", l.transform.TransformPoint(new Vector4(0f, 0f, -l.capsuleLength * .5f)));
                    customLightBuf.SetGlobalVector("_CustomLightParams1", l.transform.TransformPoint(new Vector3(0f, 0f, l.capsuleLength * .5f)));

                    customLightBuf.EnableShaderKeyword("CAPSULE");
                    
                    DrawLight(l, l.transform.position, l.transform.rotation, new Vector3(l.capsuleRadius * 2f, l.capsuleRadius * 2f, l.capsuleLength + l.capsuleRadius * 2f));

                    customLightBuf.DisableShaderKeyword("CAPSULE");
                    break;

                case CustomLight.CustomLightType.Box:
                    lightpos.w = l.boxEdgeRadius;
                    customLightBuf.SetGlobalVector("_LightPos", lightpos);
                    customLightBuf.SetGlobalVector("_CustomLightParams0", l.innerBoxRadius - new Vector3(l.boxEdgeRadius, l.boxEdgeRadius, l.boxEdgeRadius));
                    Vector3 delta = l.boxRadius - l.innerBoxRadius;
                    customLightBuf.SetGlobalVector("_CustomLightParams1", new Vector3(1f / delta.x, 1f / delta.y, 1f / delta.z));

                    customLightBuf.EnableShaderKeyword("BOX");
                    
                    DrawLight(l, l.transform.position, l.transform.rotation, l.boxRadius * 2f);
                    
                    customLightBuf.DisableShaderKeyword("BOX");
                    break;
            }
        }
        customLightBuf.EndSample("Custom Light Draw");
    }
}
