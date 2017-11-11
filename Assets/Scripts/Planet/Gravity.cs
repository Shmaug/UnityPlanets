using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gravity : MonoBehaviour {
    public const double G = 6.674e-11; // m^3/(kg*s^2)

    public bool applyGravity = true;

    public Vector3d scaledPos { get; private set; }
    public Planet planet { get; private set; }
    public Vector3 up { get; private set; }
    public Vector3 gravity { get; private set; }
    public double distToPlanet { get; private set; }
    
    Rigidbody rbody;
    
	void Start () {
        rbody = GetComponent<Rigidbody>();
	}
	
	void FixedUpdate() {
        if (!ScaleSpace.instance) {
            planet = null;
            gravity = new Vector3(0f, -9.81f, 0f);
            up = Vector3.up;
            if (rbody && !rbody.isKinematic && applyGravity)
                rbody.AddForce(gravity);
            return;
        }

        scaledPos = (Vector3d)transform.position + ScaleSpace.instance.origin;
        double d2;
        planet = ScaleSpace.instance.GetSOI(scaledPos, out d2);

        up = gravity = Vector3.zero;
        if (planet) {
            distToPlanet = Mathd.Sqrt(d2);
            up = (Vector3)(scaledPos - planet.position).normalized;
            gravity = -up * (float)(G * (rbody ? rbody.mass : 1f) * planet.mass / d2);

            if (rbody && !rbody.isKinematic && applyGravity)
                rbody.AddForce(gravity);
        }
	}
}
