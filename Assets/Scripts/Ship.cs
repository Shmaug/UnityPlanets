using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Gravity))]
[RequireComponent(typeof(Animation))]
public class Ship : MonoBehaviour {
    public MeshCollider shipCollider;
    public bool landed = false;
    public float maxPlanetarySpeed = 300f;
    public float maxSpaceSpeed = 1000f;
    public float warpSpeed = 1e6f;
    public float landingSpeed = 5f;
    public float landingDistance = 30f;
    public float rotSpeed = 30f;
    
    [Space]
    public Transform cameraCockpit;
    public LaserGun[] guns;

    [Space]
    public UnityEngine.UI.Text warpText;
    public UnityEngine.UI.Text speedText;
    public UnityEngine.UI.Text altitudeText;
    public UnityEngine.UI.Text radarAltText;
    public UnityEngine.UI.Text landText;
    public UnityEngine.UI.Text pressureText;
    public RectTransform landProgressBar;
    public UnityEngine.UI.RawImage atmoSliderBar;
    public RectTransform atmoSliderIcon;

    [Space]
    public AudioClip midHumStart;
    public AudioClip midHumLoop;
    public AudioClip midHumEnd;

    [System.NonSerialized]
    public Vector3 throttle;
    [System.NonSerialized]
    public Vector3 pitchYawRoll;

    [System.NonSerialized]
    public bool firing;
    [System.NonSerialized]
    public bool engageWarp = false;
    public bool canLand { get; private set; }
    public Vector3 landPos { get; private set; }
    public Vector3 landUp { get; private set; }
    public bool landing { get; private set; }
    public float landProgress { get; private set; } = 0f;

    public bool canWarp { get; private set; }
    public bool warping { get; private set; }
    public float warpEngageTimer { get; private set; } = 5f;

    Animation anim;
    [System.NonSerialized]
    public Rigidbody rbody;
    Gravity gravity;
    
    void Start() {
        anim = GetComponent<Animation>();
        rbody = GetComponent<Rigidbody>();
        gravity = GetComponent<Gravity>();

        if (ScaleSpace.instance) ScaleSpace.instance.RegisterFloatingOrigin(transform);

        //anim["ship_geardown"].layer = 0;
        //anim["ship_gearup"].layer = 0;
        //anim["ship_cockpitopen"].layer = 1;
        //anim["ship_cockpitclose"].layer = 1;
    }

    void OnLevelWasLoaded(int level) {
        if (ScaleSpace.instance) ScaleSpace.instance.RegisterFloatingOrigin(transform);
    }

    void FixedUpdate() {
        if (!landed && !landing) {
            float spd;
            if (warping) {
                spd = warpSpeed;
                rbody.velocity = transform.forward * warpSpeed;
            } else {
                if (gravity.planet) {
                    double alt = gravity.distToPlanet - (gravity.planet.radius + gravity.planet.waterHeight);
                    double d = gravity.planet.AtmosphereDensity(gravity.distToPlanet);
                    double h = 1f - Mathd.Clamp01(alt / gravity.planet.atmosphereHeight);
                    d = Mathd.Clamp01(d + h * h);
                    spd = Mathf.Lerp(maxSpaceSpeed, maxPlanetarySpeed, (float)d);
                } else {
                    spd = maxSpaceSpeed;
                }

                Vector3 target = transform.rotation * throttle * spd;
                rbody.AddForce((target - rbody.velocity) * .95f, ForceMode.Acceleration);
                rbody.AddRelativeTorque(pitchYawRoll, ForceMode.Acceleration);
            }
        }
    }

    void Update() {
        canWarp = !gravity.planet || gravity.distToPlanet > gravity.planet.radius + gravity.planet.atmosphereHeight * 1.5;

        if (engageWarp && !warping && canWarp) {
            warpEngageTimer -= Time.deltaTime;
            if (warpEngageTimer <= 0f) {
                warping = true;
                rbody.velocity = rbody.velocity.normalized * warpSpeed;
            }
        } else
            warpEngageTimer = 5f;

        // weapons
        if (landed) firing = false;
        for (int i = 0; i < guns.Length; i++)
            guns[i].firing = firing;

        canLand = false;
        if (gravity.planet) {
            // landing
            RaycastHit rh;
            if (!landing && !landed && Physics.Raycast(transform.position, (-gravity.up + .5f * rbody.velocity).normalized, out rh, landingDistance)) {
                if (Vector3.Dot(rh.normal, gravity.up) > .9f) {
                    canLand = true;
                    landPos = rh.point;
                    landUp = rh.normal;
                }
            }
        }

        UpdateUI();
    }

    void UpdateUI() {
        if (gravity.planet) {
            double alt = gravity.distToPlanet - (gravity.planet.radius + gravity.planet.waterHeight);
            altitudeText.text = ScaleSpace.DisplayDistanceMeasure(alt);
            radarAltText.text = ScaleSpace.DisplayDistanceMeasure(gravity.distToPlanet - gravity.planet.GetHeight(gravity.planet.rotation.inverse * (Vector3d)gravity.up));

            // atmosphere UI
            if (gravity.planet.hasAtmosphere) {
                atmoSliderBar.gameObject.SetActive(true);
                atmoSliderIcon.anchoredPosition = new Vector2(
                    (float)Mathd.Clamp01(alt / gravity.planet.atmosphereHeight) * atmoSliderBar.rectTransform.rect.width,
                    0);
                atmoSliderBar.materialForRendering.SetFloat("_Hr", gravity.planet.reyleighScaleDepth);
                atmoSliderBar.materialForRendering.SetFloat("_Hm", gravity.planet.mieScaleDepth);
                double pressure = gravity.planet.AtmosphereDensity(gravity.distToPlanet);
                pressureText.text = pressure.ToString("f4") + " atm";
            } else {
                pressureText.text = "------ atm";
                atmoSliderBar.gameObject.SetActive(false);
            }
        } else {
            altitudeText.text = "------ m";
            radarAltText.text = "------ m";
            pressureText.text = "------ atm";
            atmoSliderBar.gameObject.SetActive(false);
        }
        landText.color = landing ? new Color(1f, .5f, 0f) : canLand ? Color.green : Color.white * .65f;
        landProgressBar.sizeDelta = new Vector2((landProgressBar.parent as RectTransform).rect.width * landProgress, landProgressBar.sizeDelta.y);

        speedText.text = ScaleSpace.DisplaySpeedMeasure(rbody.velocity.magnitude);

        if (warping) {
            warpText.text = "WARPING";
            warpText.color = Color.white;
        } else {
            if (canWarp) {
                if (engageWarp) {
                    warpText.text = "WARP: " + (int)(warpEngageTimer + .5f);
                    warpText.color = Color.white;
                } else {
                    warpText.color = Color.green;
                    warpText.text = "WARP AVAILABLE";
                }
            } else {
                warpText.color = new Color(.9f, 0f, 0f);
                warpText.text = "WARP UNAVAILABLE";
            }
        }
    }

    public void ExitWarp() {
        warping = false;
        rbody.velocity = transform.forward * maxSpaceSpeed;
    }

    public void TakeOff() {
        if (!landed) return;

        landed = false;
        shipCollider.convex = true;
        rbody.isKinematic = false;
        //anim.Play("ship_gearup", PlayMode.StopSameLayer);
    }
    public void Land() {
        if (landing || !canLand) return;
        landing = true;
        //anim.Play("ship_geardown", PlayMode.StopSameLayer);
        StartCoroutine(LandSequence());
    }

    IEnumerator LandSequence() {
        rbody.isKinematic = true;
        shipCollider.convex = false;

        Vector3 toTarget;
        float dist;
        float startDist = (transform.position - landPos).magnitude;
        Quaternion startRot = transform.rotation;
        Quaternion targRot = Quaternion.FromToRotation(transform.up, landUp) * transform.rotation;
        while (true) {
            toTarget = landPos - transform.position;
            dist = toTarget.magnitude;
            if (dist < .2f) break;
            toTarget /= dist;
            
            landProgress = 1f - dist / startDist;

            transform.rotation = Quaternion.Lerp(startRot, targRot, landProgress);
            transform.position += toTarget * Mathf.Min(dist, Time.deltaTime * landingSpeed);

            yield return new WaitForFixedUpdate();
        }
        landProgress = 0;

        transform.position = landPos;
        transform.rotation = targRot;
        rbody.velocity = Vector3.zero;
        rbody.angularVelocity = Vector3.zero;

        landing = false;
        landed = true;
    }

    void OnOriginChange(Vector3 delta) {
        landPos -= delta;
    }
}
