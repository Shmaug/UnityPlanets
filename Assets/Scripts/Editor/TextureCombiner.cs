using UnityEngine;
using UnityEditor;

public class TextureCombiner : ScriptableWizard {
    public enum Channel {
        R = 1,
        G = 2,
        B = 4,
        A = 8
    }

    Texture2D tex0;
    bool tex0invert;
    Channel tex0r = Channel.R;
    Channel tex0g = Channel.G;
    Channel tex0b = Channel.B;
    Channel tex0a;

    Texture2D tex1;
    bool tex1invert;
    Channel tex1r = Channel.A;
    Channel tex1g;
    Channel tex1b;
    Channel tex1a;

    [MenuItem("Assets/Combine Textures")]
    static void CreateWizard() {
        DisplayWizard<TextureCombiner>("Combine Textures");
    }
    
    void OnGUI() {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical();
        tex0 = (Texture2D)EditorGUILayout.ObjectField(tex0, typeof(Texture2D), false);
        tex0invert = EditorGUILayout.Toggle("Invert", tex0invert);
        tex0r = (Channel)EditorGUILayout.EnumMaskPopup("R channel map", tex0r);
        tex0g = (Channel)EditorGUILayout.EnumMaskPopup("G channel map", tex0g);
        tex0b = (Channel)EditorGUILayout.EnumMaskPopup("B channel map", tex0b);
        tex0a = (Channel)EditorGUILayout.EnumMaskPopup("A channel map", tex0a);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical();
        tex1 = (Texture2D)EditorGUILayout.ObjectField(tex1, typeof(Texture2D), false);
        tex1invert = EditorGUILayout.Toggle("Invert", tex1invert);
        tex1r = (Channel)EditorGUILayout.EnumMaskPopup("R channel map", tex1r);
        tex1g = (Channel)EditorGUILayout.EnumMaskPopup("G channel map", tex1g);
        tex1b = (Channel)EditorGUILayout.EnumMaskPopup("B channel map", tex1b);
        tex1a = (Channel)EditorGUILayout.EnumMaskPopup("A channel map", tex1a);
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (GUILayout.Button("Create")) {
            Create();
            Close();
        }
    }

    void Create() {
        RenderTexture src = RenderTexture.GetTemporary(tex0.width, tex0.height, 0, RenderTextureFormat.ARGB32);
        RenderTexture dst = RenderTexture.GetTemporary(tex0.width, tex0.height, 0, RenderTextureFormat.ARGB32);
        
        Material mat = new Material(Shader.Find("Hidden/TextureCombine"));
        mat.SetTexture("_Tex0", tex0);
        mat.SetInt("_Tex0Invert", tex0invert ? 1 : 0);
        mat.SetInt("_Tex0r", (int)tex0r);
        mat.SetInt("_Tex0g", (int)tex0g);
        mat.SetInt("_Tex0b", (int)tex0b);
        mat.SetInt("_Tex0a", (int)tex0a);

        mat.SetTexture("_Tex1", tex1);
        mat.SetInt("_Tex1Invert", tex1invert ? 1 : 0);
        mat.SetInt("_Tex1r", (int)tex1r);
        mat.SetInt("_Tex1g", (int)tex1g);
        mat.SetInt("_Tex1b", (int)tex1b);
        mat.SetInt("_Tex1a", (int)tex1a);

        Graphics.Blit(src, dst, mat);
        
        RenderTexture.active = dst;
        Texture2D result = new Texture2D(dst.width, dst.height, TextureFormat.ARGB32, false);
        result.ReadPixels(new Rect(0, 0, dst.width, dst.height), 0, 0);
        RenderTexture.active = null;

        string dir = AssetDatabase.GetAssetPath(tex0);
        dir = System.IO.Directory.GetParent(dir).FullName;

        System.IO.File.WriteAllBytes(dir + "/result.png", result.EncodeToPNG());

        DestroyImmediate(mat);
        DestroyImmediate(result);

        RenderTexture.ReleaseTemporary(src);
        RenderTexture.ReleaseTemporary(dst);
    }
}
