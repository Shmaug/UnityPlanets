using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

public class QuadNode {
    public struct NodeUpdateJob {
        public enum RenderFlags : int {
            None = 0,
            Surface = 1,
            Grass = 2
        }

        public QuadNode node;
        public RenderFlags flags;

        public NodeUpdateJob(QuadNode n, RenderFlags f) {
            node = n; flags = f;
        }
    }

    public const int GridSize = 16; // at 16, meshes have >900 vertex attributes and Unity doesn't batch them.

    public enum SurfaceType {
        Ground, Water
    }
    public struct TreeInstance {
        public GameObject prefab;
        public CapsuleCollider collider;
        public Mesh mesh;
        public Material[] materials;

        public Matrix4x4[] instances;
        public Matrix4x4[] tmpinstances;
    }

    public Planet planet;
    public double size;
    public double arcSize;
    public QuadNode parent;
    public QuadNode[] children = new QuadNode[4];
    public bool isSplit { get; private set; }

    public SurfaceType type;
    
    public Vector3d cubePos;
    /// <summary>
    /// Relative to planet
    /// </summary>
    public Vector3d meshPos;
    public Vector3d radialPos;
    public Quaterniond rotation;
    public int siblingIndex;
    public int lod;

    public double vertexResolution;

    public Material renderMaterial;

    public Mesh mesh { get; private set; }
    public Mesh grassmesh { get; private set; }
    public MeshCollider mc { get; private set; }
    Matrix4x4 node2planet;
    
    public bool isDestroyed { get; private set; } = false;

    bool immediateChildrenGenerated {
        get {
            return isSplit && children[0].generated && children[1].generated && children[2].generated && children[3].generated;
        }
    }
    bool immediateChildrenSplit {
        get {
            return isSplit &&
                children[0].isSplit && children[1].isSplit && children[2].isSplit && children[3].isSplit;
        }
    }

    public TreeInstance[] trees;

    /// <summary>
    /// Relative to planet
    /// </summary>
    List<Vector3d> vertexSamples;

    double radius;

    bool generated = false;
    Vector3[] vertices;
    Vector3[] normals;
    Vector4[] tangents;
    Vector4[] texcoord0;
    Color[] weights;
    int setIndex = -1;

    List<Vector3> gverts;
    List<Vector3> gnorms;
    List<Vector4> guvs;
    int[] grassindices;

    bool hasVisibleVertices;

    Matrix4x4 trs;
    
    public QuadNode(Planet planet, SurfaceType type, int siblingIndex, double size, int lod, QuadNode parent, Vector3d cubePos, Quaterniond rotation) {
        this.planet = planet;
        this.parent = parent;
        this.type = type;
        this.siblingIndex = siblingIndex;
        this.size = size;
        this.lod = lod;
        this.cubePos = cubePos;
        this.rotation = rotation;

        arcSize = planet.ArcLength(size);
        vertexResolution = GridSize / size;
        meshPos = cubePos.normalized;
        meshPos *= GetHeight(meshPos);
        radialPos = cubePos.normalized * planet.radius;

        radius = size * Mathd.Sqrt(2.0);
        
        switch (type) {
            case SurfaceType.Ground:
                renderMaterial = planet.surfaceMaterial;
                break;
            case SurfaceType.Water:
                renderMaterial = planet.waterMaterial;
                radius += 4;
                break;
        }
        
        lock (planet.gameObjectQueue)
            planet.gameObjectQueue.Add(this);

        // vertex samples
        vertexSamples = new List<Vector3d>();
        Vector3d p;
        double scale = size / GridSize;
        Vector3d offset = new Vector3d(GridSize * .5, 0, GridSize * .5);
        for (int x = 0; x <= GridSize; x += GridSize / 2)
            for (int z = 0; z <= GridSize; z += GridSize / 2) {
                p = Vector3d.Normalize(cubePos + rotation * (scale * (new Vector3d(x, 0, z) - offset)));
                p = p * GetHeight(p);
                vertexSamples.Add(p);
            }

        node2planet = Matrix4x4.TRS(
            (Vector3)(meshPos * ScaleSpace.instance.scale),
            Quaternion.identity,
            Vector3.one * (float)(size * ScaleSpace.instance.scale));

        generated = false;
        QuadNodeGenerator.instance.QueueNodeGeneration(this);
    }
    void Destroy() {
        if (isSplit) {
            children[0].Destroy();
            children[1].Destroy();
            children[2].Destroy();
            children[3].Destroy();
        }

        lock (planet.disposeQueue)
            planet.disposeQueue.Add(this);

        isDestroyed = true;
    }

    // thread safe interop
    public void Dispose() {
        if (mc) Object.Destroy(mc.gameObject);
        if (mesh) Object.Destroy(mesh);
    }
    public void Create() {
        if (vertexResolution > planet.minPhysicsVertexResolution && type == SurfaceType.Ground) {
            GameObject obj = new GameObject("QuadNode");
            obj.transform.parent = planet.transform;
            mc = obj.AddComponent<MeshCollider>();
            obj.transform.localScale = Vector3.one * (float)size;
        }
    }
    
    public bool IsAboveHorizon(Vector3d camera, double dist) {
        return true;

        Vector3d planetToCam = (camera - planet.position) / dist;
        double horizonAngle = Mathd.Acos((planet.radius * .5) / dist);

        double meshAngle;

        foreach (Vector3d v in vertexSamples) {
            meshAngle = Mathd.Acos(Vector3d.Dot(planetToCam, v.normalized));
            if (horizonAngle > meshAngle)
                return true;
        }

        return false;
    }

    public void Update(Vector3d camRadialPos, double camDist, ref List<NodeUpdateJob> render, bool parentdraw = false) {
        NodeUpdateJob.RenderFlags flags = NodeUpdateJob.RenderFlags.None;

        if (grassindices != null)
            flags |= NodeUpdateJob.RenderFlags.Grass;

        if (isSplit) {
            if (!immediateChildrenGenerated && !parentdraw) {
                if (generated && hasVisibleVertices) {
                    flags |= NodeUpdateJob.RenderFlags.Surface;
                    parentdraw = true;
                }
            }

            children[0].Update(camRadialPos, camDist, ref render, parentdraw);
            children[1].Update(camRadialPos, camDist, ref render, parentdraw);
            children[2].Update(camRadialPos, camDist, ref render, parentdraw);
            children[3].Update(camRadialPos, camDist, ref render, parentdraw);
            
            if (!immediateChildrenSplit)
                DynamicSplit(camRadialPos);

        } else {
            DynamicSplit(camRadialPos);
            if ((parent == null || parent.immediateChildrenGenerated) && !parentdraw && generated && hasVisibleVertices)
                flags |= NodeUpdateJob.RenderFlags.Surface;
        }

        if (flags != NodeUpdateJob.RenderFlags.None && IsAboveHorizon(ScaleSpace.instance.cameraPosition, camDist))
            render.Add(new NodeUpdateJob(this, flags));
    }
    
    public void Draw(NodeUpdateJob.RenderFlags flags) {
        if (!mesh)
            SetMeshData();

        if (!mesh) return;
        
        if (setIndex != -1) {
            Profiler.BeginSample("Set Indices");
            mesh.SetIndices(TriangleCache.IndexCache[setIndex], MeshTopology.Triangles, 0);
            setIndex = -1;
            Profiler.EndSample();
        }
        
        Profiler.BeginSample("Node Calculations");
        Vector3d pos = planet.position + planet.rotation * meshPos;
        double dist = Vector3d.Dot((Vector3d)ScaleSpace.instance.mainCamera.transform.forward, pos - ScaleSpace.instance.cameraPosition);
        Profiler.EndSample();

        Profiler.BeginSample("Node Collider Position");
        if (mc) {
            mc.transform.position = (Vector3)(pos - ScaleSpace.instance.origin);
            mc.transform.rotation = (Quaternion)planet.rotation;
        }
        Profiler.EndSample();

        Profiler.BeginSample("Node Property Set");
        planet.propertyBlock.SetMatrix("_NodeToPlanet", node2planet);
        Profiler.EndSample();

        Profiler.BeginSample("World Space Draw");
        if (dist < ScaleSpace.instance.mainCamera.farClipPlane + radius) {
            // world space mesh
            trs = Matrix4x4.TRS(
                (Vector3)(pos - ScaleSpace.instance.origin),
                (Quaternion)planet.rotation,
                Vector3.one * (float)size);
            
            if ((flags & NodeUpdateJob.RenderFlags.Surface) > 0)
                Graphics.DrawMesh(mesh, trs, renderMaterial, 1, null, 0, planet.propertyBlock);

            // grass
            trs = Matrix4x4.TRS(
                (Vector3)(pos - ScaleSpace.instance.origin),
                (Quaternion)planet.rotation,
                Vector3.one);
            if ((flags & NodeUpdateJob.RenderFlags.Grass) > 0 && planet.hasGrass && grassindices != null)
                Graphics.DrawMesh(grassmesh, trs, planet.grassMaterial, 1, null, 0, planet.propertyBlock);
        }
        Profiler.EndSample();

        if ((flags & NodeUpdateJob.RenderFlags.Surface) > 0) {
            Profiler.BeginSample("Scale Space Draw");
            if (dist > ScaleSpace.instance.mainCamera.farClipPlane - radius) {
                // scaled space mesh
                trs = Matrix4x4.TRS(
                    (Vector3)(pos * ScaleSpace.instance.scale),
                    (Quaternion)planet.rotation,
                    Vector3.one * (float)(size * ScaleSpace.instance.scale));
                Graphics.DrawMesh(mesh, trs, renderMaterial, ScaleSpace.instance.layer, null, 0, planet.propertyBlock);
            }
            Profiler.EndSample();
        }
    }

    #region splitting
    public void DynamicSplit(Vector3d camRadialPos) {
        bool shouldSplit = false;
        
        if (vertexResolution < planet.maximumVertexResolution) { // dont split past 1 lod past max vertex resolution
            double lindist;
            Vector3d p = ClosestVertex(ScaleSpace.instance.cameraPosition, out lindist);

            double error = (size * .5 / lindist) * ScaleSpace.instance.cameraPerspectiveScale;

            shouldSplit = IsNeighborLODTooMuch() || error >= QuadNodeGenerator.instance.maxPixelError;
        }
        
        if (isSplit) {
            if (!shouldSplit)
                Join();
            else
                for (int i = 0; i < 4; i++) children[i].DynamicSplit(camRadialPos);
        } else {
            if (shouldSplit)
                Split();
        }
    }
    
    public void Split() {
        if (isSplit) return;
        isSplit = true;

        double s = size * .5;

        //  | 0 | 1 |
        //  | 2 | 3 |

        Vector3d rght = rotation * Vector3d.right;
        Vector3d fwd = rotation * Vector3d.forward;
        
        children = new QuadNode[4];
        children[0] = new QuadNode(planet, type, 0, s, lod + 1, this, cubePos + s * .5 * (-rght + fwd), rotation);
        children[1] = new QuadNode(planet, type, 1, s, lod + 1, this, cubePos + s * .5 * (rght + fwd), rotation);
        children[2] = new QuadNode(planet, type, 2, s, lod + 1, this, cubePos + s * .5 * (-rght + -fwd), rotation);
        children[3] = new QuadNode(planet, type, 3, s, lod + 1, this, cubePos + s * .5 * (rght + -fwd), rotation);
        
        UpdateNeighborIndicies();
    }
    public void Join() {
        if (!isSplit) return;
        isSplit = false;
        
        children[0].Destroy();
        children[1].Destroy();
        children[2].Destroy();
        children[3].Destroy();
        children = new QuadNode[4];
        
        GetIndicies();
        UpdateNeighborIndicies();
    }
    #endregion

    #region neighbor and index calculation
    public void GetIndicies(bool recurse = true) {
        if (recurse && isSplit) {
            children[0].GetIndicies();
            children[1].GetIndicies();
            children[2].GetIndicies();
            children[3].GetIndicies();
        }
        if (!mesh) return;

        QuadNode l = GetLeft();
        QuadNode r = GetRight();
        QuadNode u = GetUp();
        QuadNode d = GetDown();
        
        int index = 0;
        if (l != null && l.lod < lod) index |= 1;
        if (u != null && u.lod < lod) index |= 2;
        if (r != null && r.lod < lod) index |= 4;
        if (d != null && d.lod < lod) index |= 8;

        setIndex = index;
    }

    QuadNode GetLeft() {
        if (parent != null) {
            QuadNode l;
            switch (siblingIndex) {
                case 0:
                    l = parent.GetLeft();
                    if (l != null) {
                        if (l.isSplit) // parent left node is split, return the adjacent child
                            return l.children[1];
                        else
                            return l; // parent left node isnt split
                    }
                    break;
                case 2:
                    l = parent.GetLeft();
                    if (l != null) {
                        if (l.isSplit) // parent left node is split, return the adjacent child
                            return l.children[3];
                        else
                            return l; // parent left node isnt split
                    }
                    break;
                case 1:
                    return parent.children[0];
                case 3:
                    return parent.children[2];
            }
        }

        return null;
    }
    QuadNode GetRight()  {
        if (parent != null && parent.isSplit) {
            QuadNode l;
            switch (siblingIndex) {
                case 1:
                    l = parent.GetRight();
                    if (l != null) {
                        if (l.isSplit) // parent right node is split, return the adjacent child
                            return l.children[0];
                        else
                            return l; // parent left node isnt split
                    }
                    break;
                case 3:
                    l = parent.GetRight();
                    if (l != null) {
                        if (l.isSplit) // parent right node is split, return the adjacent child
                            return l.children[2];
                        else
                            return l; // parent right node isnt split
                    }
                    break;
                case 0:
                    return parent.children[1];
                case 2:
                    return parent.children[3];
            }
        }

        return null;
    }
    QuadNode GetUp() {
        if (parent != null && parent.isSplit) {
            QuadNode l;
            switch (siblingIndex) {
                case 0:
                    l = parent.GetUp();
                    if (l != null) {
                        if (l.isSplit) // parent up node is split, return the adjacent child
                            return l.children[2];
                        else
                            return l; // parent up node isnt split
                    }
                    break;
                case 1:
                    l = parent.GetUp();
                    if (l != null) {
                        if (l.isSplit) // parent up node is split, return the adjacent child
                            return l.children[3];
                        else
                            return l; // parent up node isnt split
                    }
                    break;
                case 2:
                    return parent.children[0];
                case 3:
                    return parent.children[1];
            }
        }

        return null;
    }
    QuadNode GetDown() {
        if (parent != null && parent.isSplit) {
            QuadNode l;
            switch (siblingIndex) {
                case 2:
                    l = parent.GetDown();
                    if (l != null) {
                        if (l.isSplit) // parent down node is split, return the adjacent child
                            return l.children[0];
                        else
                            return l; // parent down node isnt split
                    }
                    break;
                case 3:
                    l = parent.GetDown();
                    if (l != null) {
                        if (l.isSplit) // parent down node is split, return the adjacent child
                            return l.children[1];
                        else
                            return l; // parent down node isnt split
                    }
                    break;
                case 0:
                    return parent.children[2];
                case 1:
                    return parent.children[3];
            }
        }

        return null;
    }

    bool IsNeighborLODTooMuch() {
        QuadNode l = GetLeft();
        if (l != null && l.isSplit && (l.children[1].isSplit || l.children[3].isSplit)) return true;
        QuadNode r = GetRight();
        if (r != null && r.isSplit && (r.children[0].isSplit || r.children[2].isSplit)) return true;
        QuadNode u = GetUp();
        if (u != null && u.isSplit && (u.children[2].isSplit || u.children[3].isSplit)) return true;
        QuadNode d = GetDown();
        if (d != null && d.isSplit && (d.children[0].isSplit || d.children[1].isSplit)) return true;

        return false;
    }

    void UpdateNeighborIndicies() {
        QuadNode r = GetRight();
        QuadNode d = GetDown();
        QuadNode l = GetLeft();
        QuadNode u = GetUp();

        if (r != null)
            r.GetIndicies();
        if (l != null)
            l.GetIndicies();
        if (d != null)
            d.GetIndicies();
        if (u != null)
            u.GetIndicies();
    }
    #endregion

    double GetHeight(Vector3d vec) {
        double n; bool vis = true;
        return GetHeight(vec, out n, ref vis);
    }
    double GetHeight(Vector3d vec, out double n, ref bool visible) {
        n = 0;
        switch (type) {
            case SurfaceType.Ground:
                visible = true;
                return planet.GetHeight(vec, out n);
            case SurfaceType.Water:
                if (!visible)
                    visible = planet.GetHeight(vec, out n) < planet.radius + planet.waterHeight;
                n = 0;
                return planet.radius + planet.waterHeight;
        }
        return planet.radius;
    }

    public static List<double> sfctimes = new List<double>();
    public static List<double> treetimes = new List<double>();
    public static List<double> grasstimes = new List<double>();

    Vector3 bilinear(Vector3 f1, Vector3 f2, Vector3 f3, Vector3 f4, float xr, float zr) {
        Vector3 f12 = f1 + (f2 - f1) * xr;
        return f12 + ((f3 + (f4 - f3) * xr) - f12) * zr;
    }

    public void Generate() {
        int s = GridSize + 1;
        double scale = size / GridSize;
        double invSize = 1.0 / size;

        Vector3d p1, p2, p3, tmp, tan;
        double n;
        Vector3d offset = new Vector3d(GridSize * .5, 0, GridSize * .5);

        vertices = new Vector3[s * s];
        normals = new Vector3[s * s];
        tangents = new Vector4[s * s];
        texcoord0 = new Vector4[s * s];
        weights = new Color[s * s];

        Vector3d?[,] vertexCache = new Vector3d?[s, s];
        double?[,] noiseCache = new double?[s, s];

        bool outwardNormals = type != SurfaceType.Ground;
        hasVisibleVertices = false;

        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        int j = 0;
        // surface mesh
        for (int x = 0; x < s; x++)
            for (int z = 0; z < s; z++) {
                // get normalized cube space vertex (vertices -.5 to .5)
                if (vertexCache[x, z].HasValue) {
                    p1 = vertexCache[x, z].Value;
                    n = noiseCache[x, z].Value;
                } else {
                    p1 = Vector3d.Normalize(cubePos + rotation * (scale * (new Vector3d(x, 0, z) - offset)));
                    p1 = p1 * GetHeight(p1, out n, ref hasVisibleVertices) - meshPos;
                    vertexCache[x, z] = p1;
                    noiseCache[x, z] = n;
                }

                if (z + 1 < s && vertexCache[x, z + 1].HasValue)
                    p2 = vertexCache[x, z + 1].Value;
                else {
                    p2 = Vector3d.Normalize(cubePos + rotation * (scale * (new Vector3d(x, 0, z + 1) - offset)));
                    p2 = p2 * GetHeight(p2, out n, ref hasVisibleVertices) - meshPos;
                    if (z + 1 < s) {
                        vertexCache[x, z + 1] = p2;
                        noiseCache[x, z + 1] = n;
                    }
                }

                if (x + 1 < s && vertexCache[x + 1, z].HasValue)
                    p3 = vertexCache[x + 1, z].Value;
                else {
                    p3 = Vector3d.Normalize(cubePos + rotation * (scale * (new Vector3d(x + 1, 0, z) - offset)));
                    p3 = p3 * GetHeight(p3, out n, ref hasVisibleVertices) - meshPos;
                    if (x + 1 < s) {
                        vertexCache[x + 1, z] = p3;
                        noiseCache[x + 1, z] = n;
                    }
                }
                
                tmp = (p1 + meshPos).normalized;
                tan = Vector3d.Cross(planet.rotation * Vector3d.up, tmp).normalized;

                vertices[j] =(Vector3)(p1 * invSize);
                normals[j] = outwardNormals ? (Vector3)tmp : Vector3.Cross((Vector3)Vector3d.Normalize(p2 - p1), (Vector3)Vector3d.Normalize(p3 - p1));
                texcoord0[j] = new Vector4((float)tmp.x, (float)tmp.y, (float)tmp.z, (float)size);
                tangents[j] = (Vector3)tan;
                weights[j] = planet.heightWeightGradient.Evaluate((float)n);
                j++;
            }

        sw.Stop();
        lock (sfctimes) sfctimes.Add(sw.Elapsed.TotalMilliseconds);
        sw.Reset();

        // grass, trees, surface objects
        if (type == SurfaceType.Ground) {
            // trees
            if (planet.hasTrees &&
                vertexResolution >= planet.treeVertexResolution && vertexResolution * .5 < planet.treeVertexResolution) {
                sw.Start();
                trees = new TreeInstance[1];
                System.Random rand = new System.Random(planet.seed ^ (meshPos.GetHashCode()));

                GameObject tree = planet.treePrefabs[0];
                bool q = false;

                double h;
                float x, z;
                List<Matrix4x4> m = new List<Matrix4x4>();
                for (int i = 0; i < .01 * (size * size); i++) {
                    x = (float)rand.NextDouble() * GridSize;
                    z = (float)rand.NextDouble() * GridSize;
                    p1 = (cubePos + rotation * (scale * (new Vector3d(x, 0, z) - offset)));
                    p1 = p1.normalized;
                    h = GetHeight(p1, out n, ref q);
                    if (h > planet.radius + planet.waterHeight) {
                        p2 = p1;
                        p1 = p1 * h - meshPos;
                        m.Add(Matrix4x4.TRS(
                            (Vector3)p1, Quaternion.FromToRotation(Vector3.up, (Vector3)p2), Vector3.one
                            ));
                    }
                }

                TreeInstance t = new TreeInstance() {
                    instances = m.ToArray(),
                    tmpinstances = new Matrix4x4[m.Count],
                    prefab = tree
                };
                trees[0] = t;

                sw.Stop();
                lock (treetimes) treetimes.Add(sw.Elapsed.TotalMilliseconds);
                sw.Reset();
            }

            // grass
            // TODO: grass generation is atrociously slow
            if (planet.hasGrass &&
               vertexResolution >= planet.grassVertexResolution && vertexResolution * .5 < planet.grassVertexResolution) {
                sw.Start();

                System.Random rand = new System.Random(~(planet.seed ^ (meshPos.GetHashCode())));

                int count = (int)(planet.grassDensity * size * size);
                List<int> ginds = new List<int>(count);
                gverts = new List<Vector3>(count);
                gnorms = new List<Vector3>(count);
                guvs = new List<Vector4>(count);
                double h2 = (planet.radius + planet.waterHeight) * (planet.radius + planet.waterHeight);
                float x, z, xr, zr;
                Vector3d pos;
                for (int i = 0; i < count; i++) {
                    x = (float)(rand.NextDouble() * GridSize);
                    z = (float)(rand.NextDouble() * GridSize);

                    xr = x - (int)x;
                    zr = z - (int)z;

                    // interpolate height from mesh
                    pos = (Vector3d)bilinear(
                        vertices[(int)x * s + (int)z],
                        vertices[(int)x * s + (int)z + 1],
                        vertices[(int)(x + 1) * s + (int)z],
                        vertices[(int)(x + 1) * s + (int)z + 1],
                        xr, zr) * size;

                    double h = ((pos + meshPos).magnitude - planet.radius) / planet.terrainHeight;
                    if (planet.heightWeightGradient.Evaluate((float)h).g > .5f) {
                        gverts.Add((Vector3)pos);
                        gnorms.Add(bilinear(
                        normals[(int)x * s + (int)z],
                        normals[(int)x * s + (int)z + 1],
                        normals[(int)(x + 1) * s + (int)z],
                        normals[(int)(x + 1) * s + (int)z + 1],
                        xr, zr).normalized);
                        guvs.Add(new Vector4((float)rand.NextDouble(), 0f, 1f, 0));
                        ginds.Add(gverts.Count - 1);
                    }
                }

                grassindices = ginds.ToArray();

                sw.Stop();
                lock (grasstimes) grasstimes.Add(sw.Elapsed.TotalMilliseconds);
                sw.Reset();
            }
        }

        generated = true;
    }
    void SetMeshData() {
        if (!generated) return;

        Profiler.BeginSample("Mesh Set Data");
        if (!mesh) {
            mesh = new Mesh() { hideFlags = HideFlags.DontSave };
        } else
            mesh.Clear();

        mesh.SetVertices(vertices.ToList());
        mesh.SetNormals(normals.ToList());
        mesh.SetTangents(tangents.ToList());
        mesh.SetUVs(0, texcoord0.ToList());
        mesh.SetColors(weights.ToList());
        mesh.RecalculateBounds();

        // since water moves, expand bounds
        if (type == SurfaceType.Water)
            mesh.bounds.Expand(2f);

        if (grassindices != null) {
            if (!grassmesh)
                grassmesh = new Mesh() { hideFlags = HideFlags.HideAndDontSave };
            else
                grassmesh.Clear();
            grassmesh.SetVertices(gverts);
            grassmesh.SetNormals(gnorms);
            grassmesh.SetUVs(0, guvs);
            grassmesh.SetIndices(grassindices, MeshTopology.Points, 0);
            grassmesh.RecalculateBounds();
            grassmesh.bounds.Expand(planet.grassMaterial.GetFloat("_Size"));
        }

        Profiler.EndSample();
        
        Profiler.BeginSample("Mesh Collider Set");
        if (mc) {
            mesh.SetIndices(TriangleCache.IndexCache[0], MeshTopology.Triangles, 0);
            mc.sharedMesh = mesh;
        }
        Profiler.EndSample();

        GetIndicies(false);
    }

    /// <summary>
    /// Closest vertex, in scaled space
    /// </summary>
    Vector3d ClosestVertex(Vector3d pos, out double dist) {
        Vector3d localPos = planet.rotation.inverse * (pos - planet.position);
        Vector3d p = Vector3d.zero;
        dist = -1;
        double d;
        foreach (Vector3d v in vertexSamples) {
            d = (localPos - v).sqrMagnitude;
            if (dist < 0 || d < dist) {
                dist = d;
                p = v;
            }
        }

        dist = Mathd.Sqrt(dist);
        return (planet.rotation * p) + planet.position;
    }
}

static class TriangleCache {
    static readonly int GridSize = QuadNode.GridSize;

    public static int[][] IndexCache;

    static int[] MakeIndicies(int index) {
        bool fanLeft = (index & 1) >= 1;
        bool fanUp = (index & 2) >= 1;
        bool fanRight = (index & 4) >= 1;
        bool fanDown = (index & 8) >= 1;

        List<int> inds = new List<int>();
        int s = GridSize + 1;
        int i0, i1, i2, i3, i4, i5, i6, i7, i8;
        for (int x = 0; x < s - 2; x += 2) {
            for (int z = 0; z < s - 2; z += 2) {
                i0 = (x + 0) * s + z;
                i1 = (x + 1) * s + z;
                i2 = (x + 2) * s + z;
                
                i3 = (x + 0) * s + z + 1;
                i4 = (x + 1) * s + z + 1;
                i5 = (x + 2) * s + z + 1;
                
                i6 = (x + 0) * s + z + 2;
                i7 = (x + 1) * s + z + 2;
                i8 = (x + 2) * s + z + 2;

                if (fanUp && z == s - 3) {
                    if (fanRight && x == s - 3) {
                        #region Fan right/up
                        //    i6 --- i7 --- i8
                        //    |  \        /  |
                        //    |    \    /    |
                        // z+ i3 --- i4     i5
                        //    |  \    | \    |
                        //    |    \  |   \  |
                        //    i0 --- i1 --- i2
                        //           x+
                        inds.AddRange(new int[] {
                                i6, i8, i4,
                                i8, i2, i4,
                                i6, i4, i3,
                                i3, i4, i1,
                                i3, i1, i0,
                                i4, i2, i1
                            });
                        #endregion
                    } else if (fanLeft && x == 0) {
                        #region Fan left/up
                        //    i6 --- i7 --- i8
                        //    |  \        /  |
                        //    |    \    /    |
                        // z+ i3     i4 --- i5
                        //    |    /  | \    |
                        //    |  /    |   \  |
                        //    i0 --- i1 --- i2
                        //           x+
                        inds.AddRange(new int[] {
                                i6, i8, i4,
                                i6, i4, i0,
                                i8, i5, i4,
                                i4, i5, i2,
                                i4, i2, i1,
                                i4, i1, i0
                            });
                        #endregion
                    } else {
                        #region Fan up
                        //    i6 --- i7 --- i8
                        //    |  \        /  |
                        //    |    \    /    |
                        // z+ i3 --- i4 --- i5
                        //    |  \    | \    |
                        //    |    \  |   \  |
                        //    i0 --- i1 --- i2
                        //           x+
                        inds.AddRange(new int[] {
                                i6, i4, i3,
                                i6, i8, i4,
                                i4, i8, i5,

                                i3, i4, i1,
                                i3, i1, i0,
                                i4, i5, i2,
                                i4, i2, i1
                            });
                        #endregion
                    }
                } else if (fanDown && z == 0) {
                    if (fanRight && x == s - 3) {
                        #region Fan right/down
                        //    i6 --- i7 --- i8
                        //    |  \    |   /  |
                        //    |    \  | /    |
                        // z+ i3 --- i4     i5
                        //    |    /    \    |
                        //    |  /        \  |
                        //    i0 --- i1 --- i2
                        //           x+
                        inds.AddRange(new int[] {
                                i6, i7, i4,
                                i6, i4, i3,
                                i3, i4, i0,
                                i0, i4, i2,
                                i7, i8 ,i4,
                                i4, i8, i2
                            });
                        #endregion
                    } else if (fanLeft && x == 0) {
                        #region Fan left/down
                        //    i6 --- i7 --- i8
                        //    |  \    | \    |
                        //    |    \  |   \  |
                        // z+ i3     i4 --- i5
                        //    |    /    \    |
                        //    |  /        \  |
                        //    i0 --- i1 --- i2
                        //           x+
                        inds.AddRange(new int[] {
                                i6, i7, i4,
                                i7, i8, i5,
                                i7, i5, i4,
                                i4, i5, i2,
                                i6, i4, i0,
                                i0, i4, i2
                            });
                        #endregion
                    } else {
                        #region Fan down
                        //    i6 --- i7 --- i8
                        //    |  \    | \    |
                        //    |    \  |   \  |
                        // z+ i3 --- i4 --- i5
                        //    |    /    \    |
                        //    |  /        \  |
                        //    i0 --- i1 --- i2
                        //           x+
                        inds.AddRange(new int[] {
                                i6, i7, i4,
                                i6, i4, i3,
                                i7, i8, i5,
                                i7, i5, i4,

                                i3, i4, i0,
                                i0, i4, i2,
                                i4, i5, i2
                            });
                        #endregion
                    }
                } else if (fanRight && x == s - 3) {
                    #region Fan right
                    //    i6 --- i7 --- i8
                    //    |  \    |   /  |
                    //    |    \  | /    |
                    // z+ i3 --- i4     i5
                    //    |  \    | \    |
                    //    |    \  |   \  |
                    //    i0 --- i1 --- i2
                    //           x+
                    inds.AddRange(new int[] {
                            i6, i7, i4,
                            i6, i4, i3,
                            i3, i4, i1,
                            i3, i1, i0,

                            i7, i8, i4,
                            i8, i2, i4,
                            i4, i2, i1
                        });
                    #endregion
                } else if (fanLeft && x == 0) {
                    #region Fan left
                    //    i6 --- i7 --- i8
                    //    |  \    | \    |
                    //    |    \  |   \  |
                    // z+ i3     i4 --- i5
                    //    |    /  | \    |
                    //    |  /    |   \  |
                    //    i0 --- i1 --- i2
                    //           x+
                    inds.AddRange(new int[] {
                            i6, i7, i4,
                            i6, i4, i0,
                            i7, i8, i5,
                            i7, i5, i4,
                            i4, i5, i2,
                            i4, i2, i1,
                            i4, i1, i0
                        });
                    #endregion
                } else {
                    #region No fan
                    //    i6 --- i7 --- i8
                    //    |  \    | \    |
                    //    |    \  |   \  |
                    // z+ i3 --- i4 --- i5
                    //    |  \    | \    |
                    //    |    \  |   \  |
                    //    i0 --- i1 --- i2
                    //           x+
                    inds.AddRange(new int[] {
                            i6, i7, i4,
                            i6, i4, i3,
                            i7, i8, i5,
                            i7, i5, i4,
                            i3, i4, i1,
                            i3, i1, i0,
                            i4, i5, i2,
                            i4, i2, i1
                        });
                    #endregion
                }
            }
        }
        return inds.ToArray();
    }

    static TriangleCache() {
        IndexCache = new int[16][];
        for (int i = 0; i < IndexCache.Length; i++)
            IndexCache[i] = MakeIndicies(i);
    }
}