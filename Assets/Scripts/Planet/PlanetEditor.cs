#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Noise;

public class PlanetNoiseEditorWindow : EditorWindow {
    const int moduleWidth = 200;

    Vector2 camPos;
    Planet planet;

    Color inspectorColor;

    NoiseModule add;
    int connectFrom = -1;
    int dragging = -1;
    int hover = -1, selected = -1;
    Vector2 moveOffset;

    byte curDisplayGroup = 1;

    Texture2D preview;

    bool[] buttons = new bool[3];

    bool buttonClick = false;

    List<Vector3> lines = new List<Vector3>();
    Vector2 cameraOffset;

    public PlanetNoiseEditorWindow() {
        wantsMouseMove = true;
        titleContent = new GUIContent("Noise Editor");
    }

    void OnDisable() {
        if (preview) DestroyImmediate(preview);
    }

    public void SetPlanet(Planet planet) {
        this.planet = planet;
    }

    void GenerateMap(NoiseModule nm, bool color = true) {
        if (!preview)
            preview = new Texture2D(1024, 512);

        Color[] colors = new Color[preview.width * preview.height];

        double dx, dy, dz;
        double h;
        double lat, lon;

        double latdelta = Mathd.PiTimes2 * 1d / preview.width;
        double londelta = Mathd.Pi * 1d / preview.height;

        int c = preview.height - 1;

        lon = -Mathd.Pi;
        for (int x = 0; x < preview.width; x++) {
            lon += londelta;
            lat = -Mathd.PiOver2;
            for (int y = 0; y < preview.height; y++) {
                dx = Mathd.Cos(lat) * Mathd.Cos(lon);
                dy = Mathd.Sin(lat);
                dz = Mathd.Cos(lat) * Mathd.Sin(lon);
                h = nm.Get(dx, dy, dz) * .5 + .5;

                if (color) {
                    if (planet.hasWater && h < planet.waterHeight / planet.terrainHeight)
                        colors[x + (c - y) * preview.width] = Color.blue;
                    else
                        colors[x + (c - y) * preview.width] = planet.heightColorGradient.Evaluate((float)h);
                } else
                    colors[x + (c - y) * preview.width] = Color.white * (float)h;

                lat += latdelta;
            }
        }

        preview.SetPixels(colors);
        preview.Apply();
    }

    Rect Box(Vector2 pos, int lines, NoiseModule nm) {
        pos += cameraOffset;

        pos.x = (int)(pos.x + .5f);
        pos.y = (int)(pos.y + .5f);

        Rect r = new Rect(pos.x, pos.y, moduleWidth, (int)((EditorGUIUtility.singleLineHeight + 2) * (lines + 1) + .5f));
        r.x -= (int)(r.width * .5f + .5f);
        r.y -= (int)(r.height * .5f + .5f);

        Color c = Color.black;
        if (r.Contains(Event.current.mousePosition) && nm != null && nm != add) {
            hover = nm.index;
            c = Color.yellow;
        } else if (nm != null && selected == nm.index)
            c = new Color(1f, .6f, 0f);

        EditorGUI.DrawRect(new Rect(r.x - 3, r.y + 3, r.width + 4, r.height), new Color(0f, 0f, 0f, .25f)); // shadow

        EditorGUI.DrawRect(new Rect(r.x - 1, r.y - 1, r.width + 2, r.height + 2), c); // outline
        EditorGUI.DrawRect(r, inspectorColor); // background
        EditorGUI.DrawRect(new Rect(r.x, r.y + (int)(EditorGUIUtility.singleLineHeight * 1.25f + .5f), (int)(r.width * .45f + .5f), 1), Color.black); // title underline

        return r;
    }
    void DrawLine(Vector2 p1, Vector2 p2) {
        float f = (p2 - p1).magnitude * .5f;
        lines.Add(p1);
        lines.Add(p2);
        lines.Add(p1 + Vector2.right * f);
        lines.Add(p2 + Vector2.left * f);
    }
    void ConnectionInputButton(System.Action<int> connect, Vector2 center, System.Action delete) {
        GUILayout.BeginArea(new Rect((int)(center.x + .5f) - 10, (int)(center.y + .5f) - 8, 20, 20));
        if (connectFrom != -1) {
            if (connectFrom != -1 && GUILayout.Button(">", EditorStyles.miniButton)) {
                connect(connectFrom);
                connectFrom = -1;
                buttonClick = true;
            }
        } else if (delete != null && GUILayout.Button("x", EditorStyles.miniButton))
            delete();

        GUILayout.EndArea();
    }
    void ConnectionOutputButton(NoiseModule output, Vector2 center) {
        GUILayout.BeginArea(new Rect((int)(center.x + .5f) - 10, (int)(center.y + .5f) - 8, 20, 20));
        if (GUILayout.Button(">", EditorStyles.miniButton)) {
            connectFrom = output.index;
            buttonClick = true;
        }

        GUILayout.EndArea();
    }

    void DrawValue(NoiseValue module) {
        Rect r = Box(module.editorPos, module.type == NoiseValueType.Number ? 3 : 2, module);

        EditorGUI.BeginDisabledGroup(add == module);
        GUILayout.BeginArea(new Rect(r.x + 16, r.y + 5, r.width - 32, r.height - 10));

        EditorGUILayout.LabelField(module.type.ToString(), EditorStyles.boldLabel);
        module.type = (NoiseValueType)EditorGUILayout.EnumPopup("Value Type", module.type);
        if (module.type == NoiseValueType.Number)
            module.value = EditorGUILayout.DoubleField("Value", module.value);

        GUILayout.EndArea();

        ConnectionOutputButton(module, new Vector3(r.xMax, r.center.y));
        EditorGUI.EndDisabledGroup();
    }
    void DrawSimplex(Simplex module) {
        Rect r = Box(module.editorPos, 5, module);

        EditorGUI.BeginDisabledGroup(add == module);
        GUILayout.BeginArea(new Rect(r.x + 16, r.y + 5, r.width - 32, r.height - 10));

        EditorGUILayout.LabelField("Simplex", EditorStyles.boldLabel);
        int seed = EditorGUILayout.DelayedIntField("Seed", module.seed);
        if (seed != module.seed) module.seed = seed;
        EditorGUILayout.LabelField("Domain Warp");
        module.offset = EditorGUILayout.DoubleField("Offset", module.offset);
        module.scale = EditorGUILayout.DoubleField("Scale", module.scale);

        GUILayout.EndArea();

        Vector2 pos = new Vector2(r.x, (int)(r.y + 5 + EditorGUIUtility.singleLineHeight * .5f + (EditorGUIUtility.singleLineHeight + 2f) * 2f + .5f));

        // domain warp connection
        if (module.warpModule != -1)
            DrawLine(planet.noiseModules[module.warpModule].editorPos + cameraOffset + new Vector2(moduleWidth * .5f, 0), pos);
        ConnectionInputButton(
            (int nm) => { module.warpModule = nm; }, pos,
            () => { module.warpModule = -1; });

        ConnectionOutputButton(module, new Vector3(r.xMax, r.center.y));
        EditorGUI.EndDisabledGroup();
    }
    void DrawFractal(Fractal module) {
        Rect r = Box(module.editorPos, 9, module);

        EditorGUI.BeginDisabledGroup(add == module);
        GUILayout.BeginArea(new Rect(r.x + 16, r.y + 5, r.width - 32, r.height - 10));

        EditorGUILayout.LabelField("Fractal", EditorStyles.boldLabel);
        int seed = EditorGUILayout.DelayedIntField("Seed", module.seed);
        if (seed != module.seed) module.seed = seed;
        EditorGUILayout.LabelField("Domain Warp");
        module.offset = EditorGUILayout.DoubleField("Offset", module.offset);
        module.scale = EditorGUILayout.DoubleField("Scale", module.scale);

        FractalType type = (FractalType)EditorGUILayout.EnumPopup("Fractal Type", module.type);
        if (type != module.type) module.type = type;
        int octaves = EditorGUILayout.DelayedIntField("Octaves", module.octaves);
        if (octaves != module.octaves) module.octaves = octaves;
        module.frequency = EditorGUILayout.DoubleField("Frequency", module.frequency);
        module.lacunarity = EditorGUILayout.DoubleField("Lacunarity", module.lacunarity);

        GUILayout.EndArea();

        Vector2 pos = new Vector2(r.x, (int)(r.y + 5 + EditorGUIUtility.singleLineHeight * .5f + (EditorGUIUtility.singleLineHeight + 2f) * 2f + .5f));

        // domain warp connection
        if (module.warpModule != -1)
            DrawLine(planet.noiseModules[module.warpModule].editorPos + cameraOffset + new Vector2(moduleWidth * .5f, 0), pos);
        ConnectionInputButton(
            (int nm) => { module.warpModule = nm; }, pos,
            () => { module.warpModule = -1; });

        ConnectionOutputButton(module, new Vector3(r.xMax, r.center.y));
        EditorGUI.EndDisabledGroup();
    }
    void DrawBlend(NoiseBlend module) {
        Rect r = Box(module.editorPos, 5, module);

        // main box
        EditorGUI.BeginDisabledGroup(add == module);
        GUILayout.BeginArea(new Rect(r.x + 16, r.y + 5, r.width - 32, r.height - 10));

        EditorGUILayout.LabelField("Blend", EditorStyles.boldLabel);
        module.type = (NoiseBlendType)EditorGUILayout.EnumPopup("Type", module.type);
        EditorGUILayout.LabelField("Control", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Module 1", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Module 2", EditorStyles.boldLabel);

        GUILayout.EndArea();

        EditorGUI.EndDisabledGroup();

        // connection buttons
        ConnectionOutputButton(module, new Vector3(r.xMax, r.center.y)); // module output

        Vector2 pos = new Vector2(r.x, (int)(r.y + 5 + EditorGUIUtility.singleLineHeight * .5f + (EditorGUIUtility.singleLineHeight + 2f) * 2f + .5f));

        // control
        ConnectionInputButton(
            (int nm) => { module.control = nm; }, pos,
            () => { module.control = -1; });
        if (module.control != -1)
            DrawLine(planet.noiseModules[module.control].editorPos + cameraOffset + new Vector2(moduleWidth * .5f, 0f), pos);
        pos.y += EditorGUIUtility.singleLineHeight + 2f;
        pos.y = (int)(pos.y + .5f);

        // module1
        ConnectionInputButton(
            (int nm) => { module.module1 = nm; }, pos,
            () => { module.module1 = -1; });
        if (module.module1 != -1)
            DrawLine(planet.noiseModules[module.module1].editorPos + cameraOffset + new Vector2(moduleWidth * .5f, 0f), pos);
        pos.y += EditorGUIUtility.singleLineHeight + 2f;
        pos.y = (int)(pos.y + .5f);

        // module2
        ConnectionInputButton(
            (int nm) => { module.module2 = nm; }, pos,
            () => { module.module2 = -1; });
        if (module.module2 != -1)
            DrawLine(planet.noiseModules[module.module2].editorPos + cameraOffset + new Vector2(moduleWidth * .5f, 0f), pos);
    }
    void DrawMath(NoiseMath module) {
        Rect r = Box(module.editorPos, 2 + module.usedModules + (module.canAcceptMore ? 1 : 0), module);

        EditorGUI.BeginDisabledGroup(add == module);
        GUILayout.BeginArea(new Rect(r.x + 16, r.y + 5, r.width - 32, r.height - 10));

        EditorGUILayout.LabelField(module.operation.ToString(), EditorStyles.boldLabel);
        module.operation = (NoiseOperation)EditorGUILayout.EnumPopup("Type", module.operation);
        for (int i = 0; i < module.usedModules; i++)
            EditorGUILayout.LabelField("Module " + i, EditorStyles.boldLabel);
        if (module.canAcceptMore)
            EditorGUILayout.LabelField("Add Module", EditorStyles.boldLabel);

        GUILayout.EndArea();

        EditorGUI.EndDisabledGroup();

        // connection buttons
        ConnectionOutputButton(module, new Vector3(r.xMax, r.center.y));

        float h = EditorGUIUtility.singleLineHeight + 2f;

        Vector2 pos = new Vector2(r.x, r.y + 5 + EditorGUIUtility.singleLineHeight * .5f + 2 * h);
        for (int i = 0; i < module.usedModules; i++) {
            if (i < module.modules.Count && module.modules[i] != -1) {
                DrawLine(planet.noiseModules[module.modules[i]].editorPos + cameraOffset + new Vector2(moduleWidth * .5f, 0f), pos);
                ConnectionInputButton(
                    (int nm) => { module.modules[i] = nm; }, pos,
                    () => { module.modules.RemoveAt(i); i--; });
            }

            pos.y += h;
        }

        // new module button
        if (module.canAcceptMore)
            ConnectionInputButton((int nm) => { module.modules.Add(nm); }, pos, null);
    }

    void DrawNoiseModule(NoiseModule module) {
        if (module.displayGroup != curDisplayGroup) return;

        if (module is Simplex)
            DrawSimplex(module as Simplex);
        else if (module is Fractal)
            DrawFractal(module as Fractal);
        else if (module is NoiseBlend)
            DrawBlend(module as NoiseBlend);
        else if (module is NoiseMath)
            DrawMath(module as NoiseMath);
        else if (module is NoiseValue)
            DrawValue(module as NoiseValue);
    }

    void RemoveModule(int index) {
        Undo.RecordObject(planet, "Remove module");

        NoiseModule[] tmp = new NoiseModule[planet.noiseModules.Length - 1];

        for (int i = 0; i < planet.noiseModules.Length; i++) {
            int j = i;
            if (i > index) {
                j = i - 1;
                tmp[j] = planet.noiseModules[i];
                tmp[j].index--;
            } else if (i < index)
                tmp[j] = planet.noiseModules[i];
            else continue;

            if (tmp[j] is NoiseBlend) {
                NoiseBlend m = tmp[j] as NoiseBlend;

                if (m.control > index) m.control--;
                else if (m.control == index) m.control = -1;

                if (m.module1 > index) m.module1--;
                else if (m.control == index) m.control = -1;

                if (m.control > index) m.control--;
                else if (m.module2 == index) m.module2 = -1;

            } else if (tmp[j] is NoiseMath) {
                NoiseMath m = tmp[j] as NoiseMath;

                for (int k = 0; k < m.modules.Count; k++)
                    if (m.modules[k] > index) m.modules[k]--;
                    else if (m.modules[k] == index) {
                        m.modules.RemoveAt(k);
                        k--;
                    }

            } else if (tmp[j] is Fractal) {
                Fractal m = tmp[j] as Fractal;
                if (m.warpModule > index) m.warpModule--;
                else if (m.warpModule == index) m.warpModule = -1;

            } else if (tmp[j] is Simplex) {
                Simplex m = tmp[j] as Simplex;
                if (m.warpModule > index) m.warpModule--;
                else if (m.warpModule == index) m.warpModule = -1;
            }
        }

        if (planet.heightOutput == index)
            planet.heightOutput = 0;
        else if (planet.heightOutput > index)
            planet.heightOutput--;

        planet.noiseModules = tmp;
    }

    void OnGUI() {
        inspectorColor = EditorGUIUtility.isProSkin ? new Color32(56, 56, 56, 255) : new Color32(194, 194, 194, 255);
        buttonClick = false;

        if (planet) {
            EditorGUIUtility.labelWidth = 100;

            cameraOffset = -(camPos - new Vector2(position.width, position.height) * .5f);
            Vector2 mousePos = Event.current.mousePosition - cameraOffset - new Vector2(moduleWidth, 0);
            bool dirty = false;

            #region setup
            hover = -1;

            // draw background
            EditorGUI.DrawRect(new Rect(moduleWidth, 0, position.width - moduleWidth, position.height), Color.gray);
            if (preview) {
                float h = moduleWidth * ((float)preview.width / preview.height);
                EditorGUI.DrawPreviewTexture(new Rect(moduleWidth, position.height * .5f - h * .5f, position.width - moduleWidth, h), preview);
            }
            #endregion

            EditorGUI.BeginChangeCheck();
            GUILayout.BeginArea(new Rect(moduleWidth, 0, position.width - moduleWidth, position.height));

            #region draw lines
            // draw lines from last frame
            Handles.BeginGUI();

            // shadow
            var n = lines.GetEnumerator();
            Vector3 a, b, at, bt;
            Vector3 o = new Vector3(-1, 2, 0);
            while (n.MoveNext()) {
                a = n.Current; n.MoveNext();
                b = n.Current; n.MoveNext();
                at = n.Current; n.MoveNext();
                bt = n.Current;
                Handles.DrawBezier(a + o, b + o, at + o, bt + o, new Color(0f, 0f, 0f, .25f), null, 7f);
            }

            n = lines.GetEnumerator();
            while (n.MoveNext()) {
                a = n.Current; n.MoveNext();
                b = n.Current; n.MoveNext();
                at = n.Current; n.MoveNext();
                bt = n.Current;
                Handles.DrawBezier(a, b, at, bt, new Color(1f, .6f, .3f, 1f), null, 5f);
            }

            Handles.EndGUI();
            lines.Clear();
            #endregion

            #region output box
            Rect endNode = Box(planet.editorNoiseOutputPos, curDisplayGroup == 0 ? 2 : 3, null);

            GUILayout.BeginArea(new Rect(endNode.x + 5f, endNode.y + 5f, endNode.width - 10f, endNode.height - 10f));
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            if (curDisplayGroup == 0)
                EditorGUILayout.LabelField("Planet Height");
            else if (curDisplayGroup == 1) {
                EditorGUILayout.LabelField("Planet Temperature");
                EditorGUILayout.LabelField("Planet Humidity");
            }
            GUILayout.EndArea();

            // output box connection
            if (curDisplayGroup == 0) {
                ConnectionInputButton((int nm) => {
                    planet.heightOutput = nm;
                }, new Vector2(endNode.x, endNode.y + EditorGUIUtility.singleLineHeight * 2f), null);
            } else if (curDisplayGroup == 1) {
                ConnectionInputButton((int nm) => {
                    planet.tempOutput = nm;
                }, new Vector2(endNode.x, endNode.y + EditorGUIUtility.singleLineHeight * 2f), null);
                ConnectionInputButton((int nm) => {
                    planet.humidOutput = nm;
                }, new Vector2(endNode.x, endNode.y + EditorGUIUtility.singleLineHeight * 3f), null);
            }
            #endregion
            #region module draw
            foreach (NoiseModule nm in planet.noiseModules)
                DrawNoiseModule(nm);

            if (add != null) {
                add.editorPos = mousePos;
                int tmp = hover;
                DrawNoiseModule(add);
                hover = tmp;
            }
            #endregion
            #region special lines
            // output connecton line
            if (curDisplayGroup == 0) {
                if (planet.heightOutput != -1)
                    DrawLine(
                        planet.noiseModules[planet.heightOutput].editorPos + cameraOffset + new Vector2(moduleWidth * .5f, 0),
                        new Vector2(endNode.x, endNode.y + EditorGUIUtility.singleLineHeight * 2f));
            } else if (curDisplayGroup == 1) {
                if (planet.tempOutput != -1)
                    DrawLine(
                        planet.noiseModules[planet.tempOutput].editorPos + cameraOffset + new Vector2(moduleWidth * .5f, 0),
                        new Vector2(endNode.x, endNode.y + EditorGUIUtility.singleLineHeight * 2f));
                if (planet.humidOutput != -1)
                    DrawLine(
                        planet.noiseModules[planet.humidOutput].editorPos + cameraOffset + new Vector2(moduleWidth * .5f, 0),
                        new Vector2(endNode.x, endNode.y + EditorGUIUtility.singleLineHeight * 3f));
            }
            // active connection line
            if (connectFrom != -1)
                DrawLine(
                    planet.noiseModules[connectFrom].editorPos + new Vector2(moduleWidth * .5f, 0) + cameraOffset,
                    Event.current.mousePosition);
            #endregion

            GUILayout.EndArea();

            #region side panel
            GUILayout.BeginArea(new Rect(0, 0, moduleWidth, position.height));

            EditorGUILayout.Space();

            if (GUILayout.Button("Simplex")) {
                add = new Simplex() { planet = planet, displayGroup = curDisplayGroup };
                buttonClick = true;
            }
            if (GUILayout.Button("Fractal")) {
                add = new Fractal() { planet = planet, displayGroup = curDisplayGroup };
                buttonClick = true;
            }
            if (GUILayout.Button("Math")) {
                add = new NoiseMath() { planet = planet, displayGroup = curDisplayGroup };
                buttonClick = true;
            }
            if (GUILayout.Button("Blend")) {
                add = new NoiseBlend() { planet = planet, displayGroup = curDisplayGroup };
                buttonClick = true;
            }
            if (GUILayout.Button("Value")) {
                add = new NoiseValue() { planet = planet, displayGroup = curDisplayGroup };
                buttonClick = true;
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Height")) {
                curDisplayGroup = 0;
                buttonClick = true;
            }
            if (GUILayout.Button("Temp/Humid")) {
                curDisplayGroup = 1;
                buttonClick = true;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview Color"))
                GenerateMap(selected == -1 ? planet.tempNoiseModule : planet.noiseModules[selected]);
            if (GUILayout.Button("Preview Gray"))
                GenerateMap(selected == -1 ? planet.tempNoiseModule : planet.noiseModules[selected], false);
            EditorGUILayout.EndHorizontal();


            GUILayout.Space(100);

            if (GUILayout.Button("Reset")) {
                Undo.RecordObject(planet, "Reset noise");

                add = null;
                hover = selected = dragging = -1;
                planet.noiseModules = new NoiseModule[1] { new Simplex() { planet = planet, index = 0 } };
                planet.heightOutput = 0;
                planet.editorNoiseOutputPos = new Vector2(600, 0);
                buttonClick = true;
                dirty = true;
            }

            GUILayout.EndArea();

            EditorGUI.DrawRect(new Rect(moduleWidth, 0, 2, position.height), new Color(0f, 0f, 0f, .25f));
            #endregion

            if (EditorGUI.EndChangeCheck())
                dirty = true;

            #region event handling
            switch (Event.current.type) {
                case EventType.KeyDown:
                    switch (Event.current.keyCode) {
                        case KeyCode.Delete:
                            if (selected != -1) {
                                RemoveModule(selected);
                                selected = -1;
                                dirty = true;
                            }
                            break;
                        case KeyCode.O:
                            planet.editorNoiseOutputPos = mousePos;
                            dirty = true;
                            break;
                    }
                    Repaint();
                    break;

                case EventType.MouseDown:
                    if (Event.current.button == 0) {
                        if (!buttonClick) {
                            if (Event.current.mousePosition.x > moduleWidth)
                                connectFrom = selected = -1;

                            if (add != null) {
                                if (Event.current.mousePosition.x > moduleWidth) {
                                    Undo.RecordObject(planet, "Add module");
                                    add.index = planet.noiseModules.Length;
                                    NoiseModule[] tmp = new NoiseModule[planet.noiseModules.Length + 1];
                                    System.Array.Copy(planet.noiseModules, tmp, planet.noiseModules.Length);
                                    tmp[tmp.Length - 1] = add;
                                    planet.noiseModules = tmp;
                                    dirty = true;
                                }
                                add = null;
                            } else if (hover != -1 && Event.current.mousePosition.x > moduleWidth) {
                                selected = hover;
                                dragging = hover;
                                moveOffset = planet.noiseModules[hover].editorPos - mousePos;
                            }

                            if (hover == -1)
                                GUI.FocusControl(null);
                        }
                    } else if (Event.current.button == 1) {
                        add = null;
                        connectFrom = -1;
                    }

                    buttons[Event.current.button] = true;
                    Repaint();
                    break;
                case EventType.MouseUp:
                    if (Event.current.button == 0)
                        dragging = -1;

                    buttons[Event.current.button] = false;
                    Repaint();
                    break;
                case EventType.MouseDrag:
                    if (Event.current.button == 2)
                        camPos -= Event.current.delta;
                    else if (Event.current.button == 0) {
                        if (dragging != -1) {
                            planet.noiseModules[dragging].editorPos = mousePos + moveOffset;
                            dirty = true;
                        }
                    }
                    Repaint();
                    break;
                case EventType.MouseMove:
                    Repaint();
                    break;
            }
            #endregion

            if (dirty)
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        }
    }
}

[CustomEditor(typeof(Planet))]
public class PlanetEditor : Editor {
    public override void OnInspectorGUI() {
        Planet planet = target as Planet;

        planet.surfaceFoldout = EditorGUILayout.Foldout(planet.surfaceFoldout, new GUIContent("Surface"));
        if (planet.surfaceFoldout) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("radius"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("terrainHeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("surfaceMaterial"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumVertexResolution"));
            planet.treeFoldout = EditorGUILayout.Foldout(planet.treeFoldout, new GUIContent("Trees"));
            if (planet.treeFoldout) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hasTrees"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("treeVertexResolution"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("treePrefabs"), true);
                EditorGUI.indentLevel--;
            }
            planet.grassFoldout = EditorGUILayout.Foldout(planet.grassFoldout, new GUIContent("Grass"));
            if (planet.grassFoldout) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hasGrass"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("grassDensity"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("grassVertexResolution"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("grassMaterial"));
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
        }

        planet.waterFoldout = EditorGUILayout.Foldout(planet.waterFoldout, new GUIContent("Water"));
        if (planet.waterFoldout) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hasWater"));
            if (planet.hasWater) {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("waterHeight"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("waterMaterial"));
            }
            EditorGUI.indentLevel--;
        }

        planet.atmosphereFoldout = EditorGUILayout.Foldout(planet.atmosphereFoldout, new GUIContent("Atmosphere"));
        if (planet.atmosphereFoldout) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hasAtmosphere"));
            if (planet.hasAtmosphere) {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("atmosphereHeight"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("sun"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("reyleighScaleDepth"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("mieScaleDepth"));

                planet.cloudFoldout = EditorGUILayout.Foldout(planet.cloudFoldout, new GUIContent("Clouds"));
                if (planet.cloudFoldout) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("hasClouds"));
                    if (planet.hasClouds) {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudColor"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudScale"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudScroll"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudSparse"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudHeightMin"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("cloudHeightMax"));
                    }
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("position"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rotation"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("mass"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("minPhysicsVertexResolution"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("heightWeightGradient"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("heightColorGradient"));

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();

        if (GUILayout.Button("Edit Noise")) {
            PlanetNoiseEditorWindow window = EditorWindow.GetWindow<PlanetNoiseEditorWindow>();
            window.SetPlanet(planet);
        }
    }
}
#endif