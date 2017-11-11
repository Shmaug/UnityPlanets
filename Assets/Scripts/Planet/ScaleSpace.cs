using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleSpace : MonoBehaviour {
    public static ScaleSpace instance;

    public Camera mainCamera;
    public float cameraPerspectiveScale; // for lod splitting functions

    public double scale = .0001;
    public Vector3d origin = Vector3d.zero;
    public float maxOriginDistance = 6000;

    public int layer { get; private set; }

    public Vector3d cameraPosition;

    List<Planet> planets = new List<Planet>();
    List<Transform> moveWithOrigin = new List<Transform>();

    void Awake() {
        instance = this;

        layer = LayerMask.NameToLayer("ScaledSpace");
    }

    void Start() {
        planets.Clear();
        planets.AddRange(FindObjectsOfType<Planet>());
    }

    void Update() {
        instance = this;
        if (mainCamera.transform.position.magnitude > maxOriginDistance)
            MoveOrigin((Vector3d)mainCamera.transform.position);

        float hfov = Mathf.Rad2Deg * 2f * Mathf.Atan(Mathf.Tan(mainCamera.fieldOfView * Mathf.Deg2Rad * .5f) * mainCamera.aspect);
        cameraPerspectiveScale = Screen.width / (2f * Mathf.Tan(hfov * .5f));
    }

    void LateUpdate() {
        cameraPosition = (Vector3d)Camera.main.transform.position + origin;
    }

    public Planet GetSOI(Vector3d pos, out double distsqr) {
        Planet soi = null;
        distsqr = 0;
        double d;
        foreach (Planet p in planets) {
            d = (pos - p.position).sqrMagnitude;
            if (d < p.SoI * p.SoI && (d < distsqr || soi == null)) {
                soi = p;
                distsqr = d;
            }
        }

        return soi;
    }

    public void MoveOrigin(Vector3d delta) {
        Vector3 deltaf = (Vector3)delta;

        foreach (Transform t in moveWithOrigin) {
            t.position -= deltaf;
            t.SendMessage("OnOriginChange", deltaf, SendMessageOptions.DontRequireReceiver);
        }

        origin += delta;
    }

    public void RegisterFloatingOrigin(Transform t) {
        moveWithOrigin.Add(t);
    }
    public void UnregisterFloatingOrigin(Transform t) {
        moveWithOrigin.Remove(t);
    }

    static string[] distUnits = { "m", "km", "Mm", "Gm", "ls", "ld", "ly" };
    static string[] spdUnits  = { "m/s", "km/s", "c" };
    const double LightSpeed = 299792458d;
    const double LightDay = LightSpeed * (24d * 60d * 60d);
    const double LightYear = 365d * LightDay;
    const double InvLightSpeed = 1d / LightSpeed;
    const double InvLightDay = 1d / LightDay;
    const double InvLightYear = 1d / LightYear;
    
    public static string DisplaySpeedMeasure(double measure, int decimals = 2) {
        double m = Mathd.Abs(measure);
        int u = 0;

        if (m < 1e3d) {
            u = 0; // m
        } else if (m < 1e6d) {
            measure *= 1e-3d;
            u = 1; // km
        } else {
            measure *= InvLightSpeed;
            u = 2; // c
        }

        return measure.ToString("f" + decimals) + spdUnits[u];
    }
    public static string DisplayDistanceMeasure(double measure, int decimals = 2) {
        double m = Mathd.Abs(measure);
        int u = 0;

        if (m < 1e3d) {
            u = 0;
        } else if (m < 1e6d) {
            measure *= 1e-3d;
            u = 1;
        } else if (m < 1e9d) {
            measure *= 1e-6d;
            u = 2;
        } else if (m < LightSpeed) {
            measure *= 1e-9d;
            u = 3;
        } else if (m < LightDay) {
            measure *= InvLightSpeed;
            u = 4;
        } else if (m < LightYear) {
            measure *= InvLightDay;
            u = 5;
        } else {
            measure *= InvLightYear;
            u = 6;
        }

        return measure.ToString("f" + decimals) + distUnits[u];
    }
}
