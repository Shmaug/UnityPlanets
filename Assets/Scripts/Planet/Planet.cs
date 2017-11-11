using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Noise;
using System.Threading;
using System.Linq;
using UnityEngine.Profiling;

public class Planet : MonoBehaviour, ISerializationCallbackReceiver {
    [HideInInspector]
    public bool atmosphereFoldout, cloudFoldout, surfaceFoldout, waterFoldout, treeFoldout, grassFoldout;

    public int seed = 12345;

    // SURFACE //

    [Tooltip("Radius (m)")]
    public double radius = 600000; // 1/10th earth's radius
    public double terrainHeight = 4500;
    public Material surfaceMaterial;
    [Tooltip("Max vertices per meter")]
    public double maximumVertexResolution = 2f;
    // TREES //
    public bool hasTrees = true;
    public GameObject[] treePrefabs;
    [Tooltip("Min vertex resolution for trees to be generated")]
    public double treeVertexResolution = .1f;
    public bool hasGrass = true;
    public Material grassMaterial;
    public float grassDensity = 3f;
    public double grassVertexResolution = .25f;

    // WATER //

    public bool hasWater;
    public double waterHeight;
    public Material waterMaterial;

    // ATMOSPHERE //

    public bool hasAtmosphere;
    public double atmosphereHeight = 8000;
    [Tooltip("Sun brightness")]
    public float sun = 20f;
    [Range(0, 1)]
    public float reyleighScaleDepth = .1332333f;
    [Range(0, 1)]
    public float mieScaleDepth = .02f;

    public bool hasClouds;
    public Color cloudColor = Color.white;
    public float cloudScale = .1f;
    public float cloudScroll = 1f;
    [Range(0f, 2f)]
    public float cloudSparse = 1f;
    public double cloudHeightMax = 3600;
    public double cloudHeightMin = 3000;

    // PHYSICS //

    [Space]

    public Vector3d position;
    public Quaterniond rotation = Quaterniond.identity;
    public double mass = 5.972e24; // earth's mass
    [Tooltip("Min vertices per meter for colliders")]
    public double minPhysicsVertexResolution = .1f;

    public double SoI { get; private set; }

    // COLORS //

    [Space]
    public Gradient heightWeightGradient;
    public Gradient heightColorGradient;

    [HideInInspector]
    public Vector2 editorNoiseOutputPos = new Vector2(600, 0);

    [HideInInspector]
    [SerializeField]
    byte[] noiseData;

    [System.NonSerialized]
    public int heightOutput = 0;
    [System.NonSerialized]
    public int tempOutput = 0;
    [System.NonSerialized]
    public int humidOutput = 0;
    [System.NonSerialized]
    public NoiseModule[] noiseModules = new NoiseModule[1] { new Simplex() { index = 0 } };

    public NoiseModule heightNoiseModule { get { return noiseModules[heightOutput]; } }
    public NoiseModule tempNoiseModule { get { return noiseModules[tempOutput]; } }
    public NoiseModule humidNoiseModule { get { return noiseModules[humidOutput]; } }
    public NoiseModule simplex;

    [System.NonSerialized]
    public QuadNode[] groundNodes;
    [System.NonSerialized]
    public QuadNode[] waterNodes;
    List<QuadNode.NodeUpdateJob> visible = new List<QuadNode.NodeUpdateJob>();

    Thread lodThread;
    bool lodloop;
    bool started = false;

    public List<QuadNode> disposeQueue = new List<QuadNode>();
    public List<QuadNode> gameObjectQueue = new List<QuadNode>();

    public MaterialPropertyBlock propertyBlock { get; private set; }

    public void OnBeforeSerialize() {
        using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
        using (System.IO.BinaryWriter bw = new System.IO.BinaryWriter(ms)) {
            bw.Write(heightOutput);
            bw.Write(noiseModules.Length);
            foreach (NoiseModule nm in noiseModules)
                nm.Serialize(bw);
            noiseData = ms.ToArray();
        }
    }
    public void OnAfterDeserialize() {
        if (noiseData == null || noiseData.Length == 0) {
            heightOutput = 0;
            noiseModules = new NoiseModule[1] { new Simplex() { planet = this } };
            return;
        }

        using (System.IO.MemoryStream ms = new System.IO.MemoryStream(noiseData))
        using (System.IO.BinaryReader br = new System.IO.BinaryReader(ms)) {
            heightOutput = br.ReadInt32();
            int c = br.ReadInt32();
            noiseModules = new NoiseModule[c];
            for (int i = 0; i < c; i++) {
                noiseModules[i] = NoiseModule.DeserializeModule(br);
                noiseModules[i].planet = this;
                noiseModules[i].index = i;
            }
        }
    }
    
    void InitPropertyBlock() {
        Vector3 wind3 = new Vector3(1f, .3f, -.1f).normalized;

        propertyBlock = new MaterialPropertyBlock();
        propertyBlock.SetVector("_Wind", new Vector4(wind3.x, wind3.y, wind3.z, .2f));
    }

    void Start() {
        SoI = Mathd.Sqrt(Gravity.G * mass / .0001);

        double s = 1.41421356237 * radius;
        
        Vector3d[] v = new Vector3d[] {
            Vector3d.up,
            Vector3d.down,
            Vector3d.left,
            Vector3d.right,
            Vector3d.forward,
            Vector3d.back
        };
        Quaterniond[] q = new Quaterniond[] {
            Quaterniond.Euler(0, 0, 0),
            Quaterniond.Euler(180, 0, 0),
            Quaterniond.Euler(0, 0, 90),
            Quaterniond.Euler(0, 0, -90),
            Quaterniond.Euler(90, 0, 0),
            Quaterniond.Euler(-90, 0, 0)
        };
        
        groundNodes = new QuadNode[6];
        if (hasWater) waterNodes = new QuadNode[6];

        for (int i = 0; i < 6; i++) {
            groundNodes[i] = new QuadNode(this, QuadNode.SurfaceType.Ground, 0, s, 0, null, s * .5 * v[i], q[i]);
            if (hasWater)
                waterNodes[i] = new QuadNode(this, QuadNode.SurfaceType.Water, 0, s, 0, null, s * .5 * v[i], q[i]);
        }

        simplex = new Simplex() {
            seed = seed
        };

        InitPropertyBlock();

        started = true;
    }
    
    void OnEnable() {
        lodThread = new Thread(UpdateNodes) {
            Priority = System.Threading.ThreadPriority.Highest
        };

        lodloop = true;
        lodThread.Start();
    }
    void OnDisable() {
        lodloop = false;
        lodThread.Join();
    }

    void Update() {
        transform.rotation = (Quaternion)rotation;
        
        Profiler.BeginSample("Node Update");
        propertyBlock.SetVector("_PlanetSpaceCameraPos", (Vector3)((ScaleSpace.instance.cameraPosition - position) * ScaleSpace.instance.scale));
        lock (visible)
            foreach (QuadNode.NodeUpdateJob q in visible)
                q.node.Draw(q.flags);
        Profiler.EndSample();

        Profiler.BeginSample("Node Initialization");
        lock (gameObjectQueue) {
            foreach (QuadNode n in gameObjectQueue)
                n.Create();
            gameObjectQueue.Clear();
        }
        Profiler.EndSample();

        Profiler.BeginSample("Node Disposal");
        lock (disposeQueue) {
            foreach (QuadNode n in disposeQueue)
                n.Dispose();
            disposeQueue.Clear();
        }
        Profiler.EndSample();
    }
    
    void UpdateNodes() {
        List<QuadNode.NodeUpdateJob> tmp = new List<QuadNode.NodeUpdateJob>();

        Vector3d  planet2cam;
        double d;
        int i;
        while (lodloop) {
            if (!started) continue;

            tmp.Clear();

            planet2cam = ScaleSpace.instance.cameraPosition - position;
            d = planet2cam.magnitude;
            planet2cam = (planet2cam / d) * radius;

            for (i = 0; i < 6; i++) {
                if (groundNodes != null)
                    groundNodes[i].Update(planet2cam, d, ref tmp);
                if (hasWater && waterNodes != null)
                    waterNodes[i].Update(planet2cam, d, ref tmp);
            }

            lock (visible) {
                visible.Clear();
                visible.AddRange(tmp);
            }
        }
    }
    
    public double GetHeight(Vector3d vec) {
        return radius + (heightNoiseModule.Get(vec.x, vec.y, vec.z) * .5 + .5) * terrainHeight;
    }
    public double GetHeight(Vector3d vec, out double noise) {
        noise = heightNoiseModule.Get(vec.x, vec.y, vec.z) * .5 + .5;
        return radius + noise * terrainHeight;
    }

    public double ArcLength(double distance) {
        double angle = 2 * Mathd.Asin(distance / 2 / radius);
        return radius * angle;
    }

    public double AtmosphereDensity(double distance) {
        if (!hasAtmosphere) return 0;

        double h = Mathd.Clamp01((distance - (radius + waterHeight)) / atmosphereHeight);
        return .5 * (Mathd.Exp(-h / reyleighScaleDepth) + Mathd.Exp(-h / mieScaleDepth));
    }
}
