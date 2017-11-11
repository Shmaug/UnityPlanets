using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class PostEffects : MonoBehaviour {
    public Light sunLight;
    public Texture2D noiseTex;
    public bool renderWireframe = false;
    public float wireframeThickness = .1f;
    public bool renderAtmosphere = false;

    Material atmoMat;

    Camera cam;
    ScaleCamera scaleCamera;
    bool wireframe = false;
    
    void OnEnable() {
        cam = GetComponent<Camera>();
        scaleCamera = GetComponent<ScaleCamera>();
        cam.depthTextureMode |= DepthTextureMode.Depth;
        
        // set clear flags to Skybox if there is no ScaleSpace (and therefore no ScaleSpace camera behind this one)
        cam.clearFlags = (ScaleSpace.instance && !scaleCamera) ? CameraClearFlags.Depth : CameraClearFlags.Skybox;
    }
    void OnDisable() {
        if (atmoMat) DestroyImmediate(atmoMat);
    }

    void OnLevelWasLoaded(int level) {
        // set clear flags to Skybox if there is no ScaleSpace (and therefore no ScaleSpace camera behind this one)
        cam.clearFlags = (ScaleSpace.instance && !scaleCamera) ? CameraClearFlags.Depth : CameraClearFlags.Skybox;
    }
    
    void Update() {
        if (renderWireframe) {
            Shader.SetGlobalFloat("_WireThickness", wireframeThickness);
            Shader.SetGlobalColor("_WireColor", new Color(0f, 1f, 0f, 1f));
            Shader.SetGlobalColor("_BaseColor", new Color(0f, 1f, 0f, 1f));
        }

        if (renderWireframe && !wireframe) {
            cam.SetReplacementShader(Shader.Find("Unlit/Wireframe"), null);
            wireframe = true;
        } else if (!renderWireframe && wireframe) {
            cam.ResetReplacementShader();
            wireframe = false;
        }

        Shader.SetGlobalTexture("_NoiseTexture", noiseTex);
    }
    
    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if (renderAtmosphere && !renderWireframe && ScaleSpace.instance) {
            Vector3d cp;
            if (scaleCamera)
                cp = scaleCamera.scalePos;
            else
                cp = (Vector3d)transform.position + ScaleSpace.instance.origin;
            double d2;
            Planet planet = ScaleSpace.instance.GetSOI(cp, out d2);

            if (planet && planet.hasAtmosphere) {
                Profiler.BeginSample("Post-Process Atmospheric Scattering");
                if (!atmoMat) atmoMat = new Material(Shader.Find("Hidden/PostAtmo"));

                if (scaleCamera) {
                    atmoMat.SetFloat("_Far", cam.farClipPlane);
                    atmoMat.SetFloat("_SkyScatter", 1f);
                } else {
                    atmoMat.SetFloat("_Far", (float)(cam.farClipPlane * ScaleSpace.instance.scale));
                    atmoMat.SetFloat("_SkyScatter", 0f);
                }

                atmoMat.SetMatrix("_ClipToWorld", (cam.transform.localToWorldMatrix * cam.projectionMatrix).inverse);

                atmoMat.SetVector("_CameraPos", (Vector3)((cp - planet.position) * ScaleSpace.instance.scale));
                atmoMat.SetFloat("_InnerRadius", (float)((planet.radius + planet.waterHeight) * ScaleSpace.instance.scale));
                atmoMat.SetFloat("_OuterRadius", (float)((planet.radius + planet.atmosphereHeight) * ScaleSpace.instance.scale));
                atmoMat.SetFloat("_CloudMin", (float)((planet.radius + planet.cloudHeightMin) * ScaleSpace.instance.scale));
                atmoMat.SetFloat("_CloudMax", (float)((planet.radius + planet.cloudHeightMax) * ScaleSpace.instance.scale));

                atmoMat.SetVector("_SunDir", sunLight.transform.forward);
                atmoMat.SetFloat("_SunPower", planet.sun * sunLight.intensity);
                atmoMat.SetFloat("_Hr", planet.reyleighScaleDepth);
                atmoMat.SetFloat("_Hm", planet.mieScaleDepth);
                atmoMat.SetColor("_CloudColor", planet.hasClouds ? planet.cloudColor : Color.clear);
                atmoMat.SetFloat("_CloudScale", planet.cloudScale);
                atmoMat.SetFloat("_CloudScroll", planet.cloudScroll);
                atmoMat.SetFloat("_CloudSparse", planet.cloudSparse);

                Graphics.Blit(source, destination, atmoMat);
                Profiler.EndSample();
            } else
                Graphics.Blit(source, destination);
        } else
            Graphics.Blit(source, destination);
    }
}
