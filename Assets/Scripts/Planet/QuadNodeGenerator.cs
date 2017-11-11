using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(QuadNodeGenerator))]
public class QuadNodeGeneratorEditor : Editor {
    public override bool RequiresConstantRepaint() {
        return true;
    }

    public override void OnInspectorGUI() {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("threadCount"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxPixelError"));
        serializedObject.ApplyModifiedProperties();

        if (Application.isPlaying) {
            QuadNodeGenerator qng = target as QuadNodeGenerator;
            EditorGUILayout.LabelField(qng.queueCount + " queued nodes");

            double avgsfc, minsfc, maxsfc;
            double avgtree, mintree, maxtree;
            double avggrass, mingrass, maxgrass;

            avgsfc = minsfc = maxsfc =
            avgtree = mintree = maxtree =
            avggrass = mingrass = maxgrass = -1;

            lock (QuadNode.sfctimes) {
                if (QuadNode.sfctimes.Count > 0) {
                    avgsfc = 0;
                    minsfc = QuadNode.sfctimes[0];
                    maxsfc = QuadNode.sfctimes[0];
                    QuadNode.sfctimes.ForEach(l => {
                        avgsfc += l;
                        minsfc = Mathd.Min(l, minsfc);
                        maxsfc = Mathd.Max(l, maxsfc);
                    });
                    avgsfc /= QuadNode.sfctimes.Count;
                }
            }
            lock (QuadNode.treetimes) {
                if (QuadNode.treetimes.Count > 0) {
                    avgtree = 0;
                    mintree = QuadNode.treetimes[0];
                    maxtree = QuadNode.treetimes[0];
                    QuadNode.treetimes.ForEach(l => {
                        avgtree += l;
                        mintree = Mathd.Min(l, mintree);
                        maxtree = Mathd.Max(l, maxtree);
                    });
                    avgtree /= QuadNode.treetimes.Count;
                }
            }
            lock (QuadNode.grasstimes) {
                if (QuadNode.grasstimes.Count > 0) {
                    avggrass = 0;
                    mingrass = QuadNode.grasstimes[0];
                    maxgrass = QuadNode.grasstimes[0];
                    QuadNode.grasstimes.ForEach(l => {
                        avggrass += l;
                        mingrass = Mathd.Min(l, mingrass);
                        maxgrass = Mathd.Max(l, maxgrass);
                    });
                    avggrass /= QuadNode.grasstimes.Count;
                }
            }

            float w = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 60f;

            EditorGUILayout.LabelField("Surface avg/min/max", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(avgsfc.ToString("f3"));
            EditorGUILayout.LabelField(minsfc.ToString("f3"));
            EditorGUILayout.LabelField(maxsfc.ToString("f3"));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Trees avg/min/max", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(avgtree.ToString("f3"));
            EditorGUILayout.LabelField(mintree.ToString("f3"));
            EditorGUILayout.LabelField(maxtree.ToString("f3"));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Grass avg/min/max", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(avggrass.ToString("f3"));
            EditorGUILayout.LabelField(mingrass.ToString("f3"));
            EditorGUILayout.LabelField(maxgrass.ToString("f3"));
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = w;
        }
    }
}
#endif

public class QuadNodeGenerator : MonoBehaviour {
    public static QuadNodeGenerator instance;

    public int threadCount = 1;
    public float maxPixelError = -100f;

    public int queueCount { get; private set; }

    Stack<QuadNode> nodes = new Stack<QuadNode>();
    List<Thread> threads = new List<Thread>();
    
    bool active = false;

    void Awake() {
        instance = this;
    }

    void OnEnable() {
        active = true;
        
        Thread t;
        for (int i = 0; i < threadCount; i++) {
            t = new Thread(GenLoop) {
                Priority = System.Threading.ThreadPriority.AboveNormal
            };
            t.Start();
            threads.Add(t);
        }
    }
    void OnDisable() {
        active = false;
        foreach (Thread t in threads)
            t.Join();
        threads.Clear();
    }
    
    public void QueueNodeGeneration(QuadNode node) {
        lock (nodes) {
            nodes.Push(node);
            queueCount = nodes.Count;
        }
    }

    public void DequeueNode(QuadNode node) {

    }

    void GenLoop() {
        QuadNode node;
        while (active) {
            node = null;
            lock (nodes)
                if (nodes.Count > 0) {
                    node = nodes.Pop();
                    queueCount = nodes.Count;
                }

            if (node != null && !node.isDestroyed) node.Generate();
        }
    }
}
