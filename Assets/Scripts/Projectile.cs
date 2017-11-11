using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour {
    public float damage = 25f;
    public float lifetime = 5f;
    public float stickTime = 0;
    public GameObject spawnOnHit = null;

    Collider col;
    Rigidbody rbody;

	void Start () {
        Destroy(gameObject, lifetime);
        rbody = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
	}

    void OnCollisionEnter(Collision collision) {
        if (collision.gameObject.GetComponentInParent<Projectile>()) return;

        if (spawnOnHit) {
            GameObject s = Instantiate(spawnOnHit);
            s.transform.position = transform.position;
            s.transform.rotation = transform.rotation;
            Rigidbody r = s.GetComponent<Rigidbody>();
            if (r)
                r.velocity = rbody.velocity;
        }

        if (stickTime > 0) {
            transform.parent = collision.rigidbody.transform;
            Destroy(rbody);
            Destroy(col);
            Destroy(this);
            Destroy(gameObject, stickTime);
        } else
            Destroy(gameObject);
    }
}
