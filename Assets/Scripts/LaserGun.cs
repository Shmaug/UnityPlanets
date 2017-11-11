using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserGun : MonoBehaviour {
    public Transform laserSpawn;
    public GameObject laser;
    [Tooltip("Shots per second")]
    public float fireRate = 10f;
    public float laserSpeed = 500f;
    public float laserSpread = 1f;
    [Space]
    public Light fireLight;
    public Collider parentCollider;
    public float minPitchModulation = 1f;
    public float maxPitchModulation = 1f;

    public Transform rotor;

    public bool firing = false;

    float lastShot = 0f;
    
    float lightTime = 0;
    float defaultIntensity = 0;

    float spin;
    float desiredSpin;
    
    AudioSource fireSound;
    Rigidbody rbody;
    
    void Start() {
        defaultIntensity = fireLight.intensity;
        rbody = GetComponentInParent<Rigidbody>();
        fireSound = laserSpawn.GetComponent<AudioSource>();
    }

    void Update() {
        if (firing) {
            if (Time.time - lastShot > 1f / fireRate) {
                Fire();
                desiredSpin += 120f;
            }
        }
        spin += Mathf.Min(Time.deltaTime * fireRate * 120f, desiredSpin - spin);
        rotor.localEulerAngles = new Vector3(0f, spin, 0f);

        if (spin > 360f && desiredSpin > 360f) {
            spin = spin % 360f;
            desiredSpin = desiredSpin % 360f;
        }
        
        if (lightTime > 0) {
            lightTime -= Time.deltaTime;
            fireLight.intensity = defaultIntensity * lightTime / .25f;
            fireLight.enabled = true;
        } else {
            fireLight.intensity = 0;
            fireLight.enabled = false;
        }
    }

    void Fire() {
        lightTime = .25f;
        lastShot = Time.time;

        GameObject p = Instantiate(laser);
        p.transform.position = laserSpawn.position;
        Vector2 r = Random.insideUnitCircle * laserSpread;
        p.transform.rotation = laserSpawn.rotation * Quaternion.Euler(r.x, r.y, 0f);
        p.GetComponent<Rigidbody>().velocity = rbody.GetPointVelocity(laserSpawn.position) + p.transform.forward * laserSpeed;
        Physics.IgnoreCollision(p.GetComponent<Collider>(), parentCollider);

        if (fireSound) {
            AudioSource a = laserSpawn.gameObject.AddComponent<AudioSource>();

            a.clip = fireSound.clip;
            a.outputAudioMixerGroup = fireSound.outputAudioMixerGroup;
            a.mute = fireSound.mute;
            a.bypassEffects = fireSound.bypassEffects;
            a.bypassListenerEffects = fireSound.bypassListenerEffects;
            a.bypassReverbZones = fireSound.bypassReverbZones;
            a.priority = fireSound.priority;
            a.volume = fireSound.volume;
            a.pitch = fireSound.pitch * Random.Range(minPitchModulation, maxPitchModulation);
            a.panStereo = fireSound.panStereo;
            a.spatialBlend = fireSound.spatialBlend;
            a.reverbZoneMix = fireSound.reverbZoneMix;
            a.dopplerLevel = fireSound.dopplerLevel;
            a.spread = fireSound.spread;
            a.minDistance = fireSound.minDistance;
            a.maxDistance = fireSound.maxDistance;
            a.rolloffMode = fireSound.rolloffMode;

            a.Play();
            Destroy(a, fireSound.clip.length);
        }
    }
}
