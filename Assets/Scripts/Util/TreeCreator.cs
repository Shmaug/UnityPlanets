using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(TreeCreator))]
public class TreeCreatorEditor : Editor {
    int groupIndex = 0;

    void InspectBranchGroup(TreeCreator tc, TreeCreator.BranchGroup group) {
        EditorGUI.BeginChangeCheck();

        int frequency = EditorGUILayout.IntField("Frequency", group.frequency);
        int sides = EditorGUILayout.IntSlider("Sides", group.sides, 3, 12);
        int steps = EditorGUILayout.IntSlider("Steps", group.steps, 2, 16);
        int spread = EditorGUILayout.IntSlider("Branch Spread", group.branchSpread, 0, 6);
        float taperBias = EditorGUILayout.Slider("Taper Bias", group.taperBias, .01f, 1f);
        float kinky = EditorGUILayout.FloatField("Kinky", group.kinky);
        float lengthScale = EditorGUILayout.Slider("Length Scale", group.lengthScale, 0f, 2f);
        float radiusScale = EditorGUILayout.Slider("Radius Scale", group.radiusScale, 0f, 2f);
        int leaves = EditorGUILayout.IntField("Leaves", group.leaves);
        float leafSize = EditorGUILayout.FloatField("Leaf Size", group.leafSize);
        float branchHeight = EditorGUILayout.Slider("Branch Height", group.branchHeight, 0f, 1f);
        float heightBias = EditorGUILayout.FloatField("Height Bias", group.heightBias);
        float forward = EditorGUILayout.Slider("Forward", group.forward, 0f, 1f);
        float seekSun = EditorGUILayout.Slider("Seek Sun", group.seekSun, 0f, 1f);

        if (EditorGUI.EndChangeCheck()) {
            Undo.RecordObject(target, "Edit branch group");
            group.frequency = frequency;
            group.sides = sides;
            group.steps = steps;
            group.branchSpread = spread;
            group.taperBias = taperBias;
            group.kinky = kinky;
            group.lengthScale = lengthScale;
            group.radiusScale = radiusScale;
            group.leaves = leaves;
            group.leafSize = leafSize;
            group.branchHeight = branchHeight;
            group.heightBias = heightBias;
            group.forward = forward;
            group.seekSun = seekSun;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(group.index == 0);
        if (GUILayout.Button("Remove")) {
            Undo.RecordObject(target, "Delete branch group");
            tc.groups.RemoveAt(group.index);
        }
        EditorGUI.EndDisabledGroup();
        if (GUILayout.Button("Add Next")) {
            Undo.RecordObject(target, "Add branch group");
            tc.groups.Insert(group.index + 1, new TreeCreator.BranchGroup() { index = group.index + 1 });
        }
        EditorGUILayout.EndHorizontal();
    }

    void GenerateBillboard(GameObject go, Mesh mesh, Material[] mats, Texture2D albedo, Texture2D normal) {
        Material blitMat = new Material(Shader.Find("Hidden/BlitRT"));

        int layer = go.layer;
        go.layer = 29;

        Camera cam = new GameObject().AddComponent<Camera>();
        cam.cullingMask = 1 << 29;
        cam.allowMSAA = false;
        cam.orthographic = true;
        
        float s = Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.z) * Mathf.Sqrt(2f);
        cam.aspect = 1f;
        cam.orthographicSize = s * .5f;
        cam.nearClipPlane = -s * .5f;
        cam.farClipPlane = s * .5f;
        cam.transform.rotation = go.transform.rotation;
        cam.transform.position = go.transform.TransformPoint(mesh.bounds.center);
        cam.renderingPath = RenderingPath.Forward;
        cam.depthTextureMode = DepthTextureMode.DepthNormals;
        cam.clearFlags = CameraClearFlags.Color;
        cam.backgroundColor = Color.clear;
        
        RenderTexture srcRT = RenderTexture.GetTemporary(albedo.width, albedo.height, 16, RenderTextureFormat.ARGB32);
        RenderTexture dstRT = RenderTexture.GetTemporary(albedo.width, albedo.height, 16, RenderTextureFormat.ARGB32);
        srcRT.antiAliasing = dstRT.antiAliasing = 1;

        cam.targetTexture = srcRT;
        
        cam.Render();
        
        RenderTexture.active = srcRT;
        albedo.ReadPixels(new Rect(0, 0, srcRT.width, srcRT.height), 0, 0);
        albedo.Apply();

        blitMat.SetMatrix("_InvView", cam.cameraToWorldMatrix);
        Graphics.Blit(srcRT, dstRT, blitMat);

        RenderTexture.active = dstRT;
        normal.ReadPixels(new Rect(0, 0, dstRT.width, dstRT.height), 0, 0);
        normal.Apply();

        RenderTexture.active = null;
        cam.targetTexture = null;

        RenderTexture.ReleaseTemporary(srcRT);
        RenderTexture.ReleaseTemporary(dstRT);
        DestroyImmediate(cam.gameObject);
        DestroyImmediate(blitMat);
        
        go.layer = layer;
    }

    public override void OnInspectorGUI() {
        TreeCreator tc = target as TreeCreator;
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("seed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("height"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("radius"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("textureScaleX"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("textureScaleY"));
        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Branch Group " + groupIndex, EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(groupIndex <= 0);
        if (GUILayout.Button("<", EditorStyles.miniButton))
            groupIndex--;
        EditorGUI.EndDisabledGroup();
        EditorGUI.BeginDisabledGroup(groupIndex >= tc.groups.Count - 1);
        if (GUILayout.Button(">", EditorStyles.miniButton))
            groupIndex++;
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        groupIndex = EditorGUILayout.IntSlider(groupIndex, 0, tc.groups.Count - 1);
        InspectBranchGroup(tc, tc.groups[groupIndex]);

        if (EditorGUI.EndChangeCheck())
            tc.CreateMesh();
        
        EditorGUILayout.Space();

        if (GUILayout.Button("Create Billboards")) {
            if (!tc.billboardAlbedo) {
                tc.billboardAlbedo = new Texture2D(1024, 1024, TextureFormat.ARGB32, false);
                tc.billboardAlbedo.name = tc.gameObject.name + "AlbedoBillboard";
            }
            if (!tc.billboardNormal) {
                tc.billboardNormal = new Texture2D(1024, 1024, TextureFormat.ARGB32, false);
                tc.billboardNormal.name = tc.gameObject.name + "NormalBillboard";
            }

            GenerateBillboard(tc.gameObject, tc.GetComponent<MeshFilter>().sharedMesh, tc.GetComponent<MeshRenderer>().sharedMaterials, tc.billboardAlbedo, tc.billboardNormal);


            if (!AssetDatabase.Contains(tc.billboardAlbedo))
                AssetDatabase.CreateAsset(tc.billboardAlbedo, "Assets/Trees/" + tc.billboardAlbedo.name + ".asset");
            else
                EditorUtility.SetDirty(tc.billboardAlbedo);

            if (!AssetDatabase.Contains(tc.billboardNormal))
                AssetDatabase.CreateAsset(tc.billboardNormal, "Assets/Trees/" + tc.billboardNormal.name + ".asset");
            else
                EditorUtility.SetDirty(tc.billboardNormal);
        }
    }
}
#endif

public class TreeCreator : MonoBehaviour {
    [System.Serializable]
    public class BranchGroup {
        public int frequency = 1;

        [Range(3, 12)]
        public int sides = 6;
        [Range(2, 16)]
        public int steps = 6;
        [Range(1, 6)]
        public int branchSpread = 3;

        [Range(0.01f, 1f)]
        public float taperBias = .8f;
        public float kinky = 1f;
        [Range(0f, 2f)]
        public float lengthScale = .5f;
        [Range(0f, 1f)]
        public float radiusScale = .5f;
        public int leaves = 50;
        public float leafSize = 1f;
        [Range(0f, 1f)]
        public float branchHeight = .2f;
        public float heightBias = 2;
        [Range(0f, 1f)]
        public float forward = .25f;
        public float seekSun = 1f;

        public int index = 0;
    }
    class Branch {
        public float radius;
        public float length;
        public int leaves = 0;
        public BranchGroup group;
        public Vector3[] points;
        public Quaternion[] orientations;
        
        public List<Branch> children = new List<Branch>();
    }

    public int seed = 123456;
    public float height = 10f;
    public float radius = .5f;
    public float textureScaleX = 1f;
    public float textureScaleY = 2f;
    public List<BranchGroup> groups = new List<BranchGroup>() { new BranchGroup() };

    public Texture2D billboardAlbedo;
    public Texture2D billboardNormal;

    void OnValidate() {
        CreateMesh();
    }

    void CreateBranchMesh(Branch branch, List<Vector3> vertices, List<Vector3> normals, List<Vector4> uvs, List<int> indices, List<Vector3> leaves, List<Vector3> leafNormals, List<Vector4> leafuvs) {
        BranchGroup group = branch.group;
        int bi;
        float rad = branch.radius;
        float texy = 1f;
        Vector3 pos;
        Quaternion q;
        for (int i = 0; i < branch.points.Length; i++) {
            pos = branch.points[i];
            q = branch.orientations[i];

            texy = textureScaleY * textureScaleX * branch.length / rad;
            texy *= i / (branch.points.Length - 1f);

            rad = branch.radius * Mathf.Pow(1f - i / (branch.points.Length - 1f), group.taperBias);
            bi = vertices.Count;
            for (int j = 0; j <= group.sides; j++) {
                Vector3 n = q * Quaternion.Euler(0, 0, 360f * j / group.sides) * Vector3.up;

                vertices.Add(pos + n * rad);
                normals.Add(n);
                uvs.Add(new Vector2(textureScaleX * j / group.sides, texy));
                
                if (i < branch.points.Length - 1 && j < group.sides) {
                    indices.Add(bi + j);
                    indices.Add(bi + j + 1);
                    indices.Add(bi + j + group.sides + 1);

                    indices.Add(bi + j + 1);
                    indices.Add(bi + j + group.sides + 2);
                    indices.Add(bi + j + group.sides + 1);
                }
            }
        }

        for (int i = 0; i < branch.leaves; i++) {
            float t = Random.value;

            float p = Random.value;
            float jf = p * (branch.points.Length - 1);
            int ji = (int)jf;
            float jt = jf - ji;
            int ji2 = Mathf.Min(branch.points.Length - 1, ji + 1);
            pos = Vector3.Lerp(branch.points[ji], branch.points[ji2], jt);
            Quaternion orientation = Quaternion.Lerp(branch.orientations[ji], branch.orientations[ji2], jt);
            rad = branch.radius * Mathf.Pow(1f - p, group.taperBias);
            orientation *= Quaternion.Euler(0, 0, Random.Range(-180f, 180f));
            Vector3 normal = orientation * Quaternion.Euler(Random.Range(-45f, 45f), Random.Range(-45f, 45f), 0f) * Vector3.up;

            leaves.Add(pos + normal * rad);
            leafNormals.Add(normal);
            // x: rotation
            // y: color
            // z: size
            // w: texture
            leafuvs.Add(new Vector4(Random.value, Random.value, Random.Range(.5f, 1f) * group.leafSize, Random.value));
        }

        foreach (Branch b in branch.children)
            CreateBranchMesh(b, vertices, normals, uvs, indices, leaves, leafNormals, leafuvs);
    }

    void BuildBranch(Branch branch, Vector3 start, Quaternion rotation) {
        Vector2 k = Vector2.zero;
        Quaternion q = rotation;
        Vector3 pos = start;
        branch.points = new Vector3[branch.group.steps];
        branch.orientations = new Quaternion[branch.group.steps];
        for (int i = 0; i < branch.group.steps; i++) {
            branch.points[i] = pos;
            branch.orientations[i] = q;

            k = Random.insideUnitCircle * branch.group.kinky;
            q *= Quaternion.Euler(k.x, k.y, 0f);
            q = Quaternion.Lerp(q, Quaternion.LookRotation(Vector3.up, q * Vector3.up), branch.group.seekSun * (pos.x * pos.x + pos.z * pos.z));
            pos += q * new Vector3(0f, 0f, branch.length / (branch.group.steps - 1f));
        }
    }
    Branch CreateBranch(Branch parent, BranchGroup group, float p, float rot) {
        float r = radius;
        float l = height;
        Vector3 pos = Vector3.zero;
        Quaternion orientation = Quaternion.Euler(-90f, 0f, 0f);

        if (parent != null) {
            r = parent.radius * group.radiusScale;
            l = parent.length * group.lengthScale;
            
            float jf = p * (parent.points.Length - 1);
            int ji = (int)jf;
            float jt = jf - ji;
            int ji2 = Mathf.Min(parent.points.Length - 1, ji + 1);
            float taper = Mathf.Pow(1f - p, group.taperBias);

            r *= taper;
            l *= Mathf.Max(taper, .5f) * Random.Range(.8f, 1.2f);

            pos = Vector3.Lerp(parent.points[ji], parent.points[ji2], jt);
            orientation = Quaternion.Lerp(parent.orientations[ji], parent.orientations[ji2], jt);
            orientation *= Quaternion.Euler(0, 0, rot) * Quaternion.Euler(-90f * (1f - group.forward), 0, 0);
        }

        Branch branch = new Branch() {
            radius = r,
            length = l,
            leaves = group.leaves,
            group = group,
        };
        
        BuildBranch(branch, pos, Quaternion.LookRotation(orientation * Vector3.forward));

        if (parent != null) parent.children.Add(branch);

        return branch;
    }

    Branch[] CreateBranchGroup(Branch parent, int groupIndex) {
        BranchGroup group = groups[groupIndex];

        List<float> ptmp = new List<float>();
        float[] rotations = new float[group.frequency];
        for (int i = 0; i < group.frequency; i++) {
            ptmp.Add(Mathf.Clamp01(group.branchHeight + (1f - group.branchHeight) * (1f - Mathf.Pow(Random.value, group.heightBias))));
            rotations[i] = (i == 0 ? 0f : rotations[i - 1]) + Random.Range(90f, 270f);
        }
        ptmp.Sort();
        float[] positions = ptmp.ToArray();

        for (int i = 0; i < group.branchSpread; i++) {
            float[] tmp = new float[positions.Length];
            for (int j = 0; j < positions.Length; j++) {
                float prev = j == 0 ? group.branchHeight : positions[j - 1];
                float next = j == positions.Length - 1 ? 1f : positions[j + 1];
                tmp[j] = (prev + next) * .5f;
            }
            positions = tmp;
        }
        
        Branch[] branches = new Branch[group.frequency];
        for (int i = 0; i < group.frequency; i++) {
            branches[i] = CreateBranch(parent, group, positions[i], rotations[i]);

            if (groupIndex + 1 < groups.Count)
                CreateBranchGroup(branches[i], groupIndex + 1);
        }
        return branches;
    }

    public void CreateMesh() {
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (!mf) mf = gameObject.AddComponent<MeshFilter>();
        if (!mr) mr = gameObject.AddComponent<MeshRenderer>();

        Mesh mesh = mf.sharedMesh;
        if (!mesh) {
            mesh = new Mesh() { name = gameObject.name };
            mf.mesh = mesh;
        } else
            mesh.Clear();

        Random.State initial = Random.state;
        Random.InitState(seed);
        
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector4> uvs = new List<Vector4>();
        List<int> indices = new List<int>();
        List<Vector3> leaves = new List<Vector3>();
        List<Vector3> leafNormals = new List<Vector3>();
        List<Vector4> leafuvs = new List<Vector4>();
        
        Branch[] branches = CreateBranchGroup(null, 0);
        foreach (Branch b in branches)
            CreateBranchMesh(b, vertices, normals, uvs, indices, leaves, leafNormals, leafuvs);

        int[] leafinds = new int[leaves.Count];
        for (int i = 0; i < leafinds.Length; i++)
            leafinds[i] = vertices.Count + i;
        vertices.AddRange(leaves);
        normals.AddRange(leafNormals);
        uvs.AddRange(leafuvs);

        if (leafinds.Length > 0)
            mesh.subMeshCount = 2;

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
        mesh.RecalculateTangents();

        if (leafinds.Length > 0)
            mesh.SetIndices(leafinds, MeshTopology.Points, 1);

        mesh.RecalculateBounds();

        Random.state = initial;
    }
}
