using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

[ImageEffectAllowedInSceneView]
[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class PostClouds : MonoBehaviour {
    public float hr = .1332333f;
    public float hm = .02f;
    public float sunPower = 20.0f;

    [Space]

    public double scale = .0002;
    public double earthRadius = 1000000;
    public double atmosphereHeight = 10000;
    public double cloudMin = 3000;
    public double cloudMax = 4000;

    [Space]

    public Color cloudColor = Color.white;
    public float cloudScale = 1.0f;
    public float cloudScroll = 1.0f;
    public float cloudSparsity = .5f;

    [Space]

    public Light sunLight;
    public Texture2D noiseTex;


    Material material;
    Camera cam;

    void Start() {
        cam = GetComponent<Camera>();
        cam.depthTextureMode |= DepthTextureMode.Depth;
    }

    void OnDestroy() {
        if (material) DestroyImmediate(material);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        Profiler.BeginSample("Post-Process Clouds");
        if (!material)
            material = new Material(Shader.Find("Hidden/PostClouds"));
        
        material.SetVector("_CameraPos", (Vector3)(((Vector3d)transform.position + new Vector3d(0, earthRadius, 0)) * scale));
        material.SetFloat("_Far", (float)(cam.farClipPlane * scale));
        material.SetFloat("_InnerRadius", (float)(earthRadius * scale));
        material.SetFloat("_OuterRadius", (float)((earthRadius + atmosphereHeight) * scale));
        material.SetFloat("_CloudMin", (float)((earthRadius + cloudMin) * scale));
        material.SetFloat("_CloudMax", (float)((earthRadius + cloudMax) * scale));

        material.SetVector("_SunDir", sunLight.transform.forward);
        material.SetFloat("_SunPower", sunPower * sunLight.intensity);
        material.SetFloat("_Hr", hr);
        material.SetFloat("_Hm", hm);
        material.SetColor("_CloudColor", cloudColor);
        material.SetFloat("_CloudScale", cloudScale);
        material.SetFloat("_CloudScroll", cloudScroll);
        material.SetFloat("_CloudSparse", cloudSparsity);
        material.SetTexture("_NoiseTexture", noiseTex);

        Graphics.Blit(source, destination, material);
        Profiler.EndSample();
    }
}
