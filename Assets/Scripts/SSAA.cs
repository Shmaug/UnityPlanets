using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class SSAA : MonoBehaviour {
    [Range(.5f, 5f)]
    public float resolutionScale = 1f;
    public Camera backCamera;
    
    RenderTexture rt;
    Camera cam;

    void Start() {
        cam = GetComponent<Camera>();
    }

    void OnDisable() {
        cam.targetTexture = null;
        DestroyImmediate(rt);
    }

    void Update() {
        int width = (int)(Screen.width * resolutionScale);
        int height = (int)(Screen.height * resolutionScale);

        if (rt == null || rt.width != width || rt.height != height) {
            if (rt) {
                cam.targetTexture = null;
                rt.Release();
                if (RenderTexture.active == rt) RenderTexture.active = null;
                DestroyImmediate(rt);
            }

            rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) {
                useMipMap = false,
                autoGenerateMips = false,
                filterMode = FilterMode.Bilinear
            };
            cam.targetTexture = rt;
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if (backCamera) Graphics.Blit(backCamera.targetTexture, source);
        Graphics.Blit(source, destination);
    }
}
