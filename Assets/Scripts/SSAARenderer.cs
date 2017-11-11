using UnityEngine;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class SSAARenderer : MonoBehaviour {
    public Camera mainCamera;
    
    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if (mainCamera) Graphics.Blit(mainCamera.targetTexture, source);
        Graphics.Blit(source, destination);
    }
}
