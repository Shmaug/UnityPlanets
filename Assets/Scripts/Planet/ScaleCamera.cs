using UnityEngine;
using UnityEngine.PostProcessing;

[RequireComponent(typeof(Camera))]
public class ScaleCamera : MonoBehaviour {
    public Camera targetCamera;
    public double unscaledFarClipPlane = 1e6;
    public double nearClipOffset = .99;

    public Vector3d scalePos { get; private set; }

    SSAA ssaa;
    SSAA targssaa;
    Camera cam;
    PostEffects targpostfx;
    PostEffects postfx;

    void Start() {
        cam = GetComponent<Camera>();
        ssaa = GetComponent<SSAA>();
        postfx = GetComponent<PostEffects>();

        targssaa = targetCamera.GetComponent<SSAA>();
        targpostfx = targetCamera.GetComponent<PostEffects>();
    }

    void LateUpdate() {
        scalePos = (Vector3d)targetCamera.transform.position + ScaleSpace.instance.origin;
        transform.localPosition = (Vector3)(scalePos * ScaleSpace.instance.scale);
        transform.localRotation = targetCamera.transform.rotation;

        if (ssaa) ssaa.resolutionScale = targssaa.resolutionScale;

        postfx.wireframeThickness = targpostfx.wireframeThickness;
        postfx.renderAtmosphere = targpostfx.renderAtmosphere;
        postfx.renderWireframe = targpostfx.renderWireframe;
        postfx.noiseTex = targpostfx.noiseTex;
        postfx.noiseTex = targpostfx.noiseTex;

        cam.nearClipPlane = (float)(targetCamera.farClipPlane * ScaleSpace.instance.scale * nearClipOffset);
        cam.farClipPlane = (float)(unscaledFarClipPlane * ScaleSpace.instance.scale);
        cam.fieldOfView = targetCamera.fieldOfView;
        cam.allowHDR = targetCamera.allowHDR;
        cam.allowMSAA = targetCamera.allowMSAA;
        cam.targetDisplay = targetCamera.targetDisplay;
        cam.renderingPath = targetCamera.renderingPath;
        cam.stereoTargetEye = targetCamera.stereoTargetEye;
        cam.targetTexture = targetCamera.targetTexture;
        cam.aspect = targetCamera.aspect;
    }

    void OnPreRender() {
        Shader.SetGlobalFloat("_ScaleSpace", 1f);
    }
    void OnPostRender() {
        Shader.SetGlobalFloat("_ScaleSpace", 0f);
    }
}
