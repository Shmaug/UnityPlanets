using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class VolumeLight : MonoBehaviour {
    new Light light;

    void Start() {
        light = GetComponent<Light>();
    }
}
