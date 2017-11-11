using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Gravity))]
public class Player : MonoBehaviour {
    public float walkSpeed = 1.4f;
    public float runSpeed = 4.2f;
    public float flySpeed = 10f;
    public float flyBoostSpeed = 100f;
    public float lookSensitivity = 1.5f;
    [Range(0,1)]
    public float stiffness = .25f;
    [Tooltip("newtons")]
    public float jumpForce = 2000f;
    public Ship vehicle;

    [Space]

    public Camera eye;
    public float vacLPFfreq = 300f;
    public float atmLPFFreq = 22000f;
    
    Vector3 euler;
    AudioLowPassFilter earLPF;

    Vector3 move;
    bool jump, run;

    Transform parent;
    CapsuleCollider capsule;
    Gravity gravity;
    Rigidbody rbody;
    
    void Start() {
        rbody = GetComponent<Rigidbody>();
        gravity = GetComponent<Gravity>();
        capsule = GetComponent<CapsuleCollider>();

        earLPF = eye.GetComponent<AudioLowPassFilter>();

        if (ScaleSpace.instance) ScaleSpace.instance.RegisterFloatingOrigin(transform);

        if (vehicle)
            EnterVehicle(vehicle);
    }

    void OnLevelWasLoaded(int level) {
        if (ScaleSpace.instance) ScaleSpace.instance.RegisterFloatingOrigin(transform);
    }

    void EnterVehicle(Ship v) {
        vehicle = v;
        rbody.isKinematic = true;
        capsule.enabled = false;
        parent = transform.parent;
        transform.parent = vehicle.transform;
        transform.localPosition = vehicle.cameraCockpit.localPosition - eye.transform.localPosition;
        transform.localRotation = Quaternion.identity;

        ScaleSpace.instance.UnregisterFloatingOrigin(transform);
    }
    void ExitVehicle() {
        rbody.velocity = vehicle.rbody.velocity;
        transform.parent = parent;
        transform.position = vehicle.transform.position + vehicle.transform.up * 5f;
        capsule.enabled = true;
        rbody.isKinematic = false;
        vehicle.firing = false;
        vehicle = null;

        ScaleSpace.instance.RegisterFloatingOrigin(transform);
    }
    
    void FixedUpdate() {
        if (!vehicle) {
            Vector3 delta = stiffness * (move - rbody.velocity);

            if (gravity.up != Vector3.zero) {
                // dont apply a force upwards
                delta -= Vector3.Project(delta, gravity.up);
                gravity.applyGravity = true;
            } else
                gravity.applyGravity = false;

            if (jump) {
                rbody.AddForce(gravity.up * jumpForce);
                jump = false;
            }

            rbody.AddForce(delta, ForceMode.VelocityChange);
        }
    }
    void Update() {
        if (Input.GetKeyDown(KeyCode.C))
            if (Cursor.lockState == CursorLockMode.Locked)
                Cursor.lockState = CursorLockMode.None;
            else
                Cursor.lockState = CursorLockMode.Locked;

        if (Cursor.lockState == CursorLockMode.Locked) {
            euler.x -= Input.GetAxis("Mouse Y") * lookSensitivity;
            euler.y += Input.GetAxis("Mouse X") * lookSensitivity;
        }

        if (gravity.planet) {
            float pressure = (float)gravity.planet.AtmosphereDensity(gravity.distToPlanet);
            earLPF.cutoffFrequency = Mathf.Lerp(vacLPFfreq, atmLPFFreq, 1f - Mathf.Clamp01(1f - pressure * pressure * pressure * pressure));
        } else
            earLPF.cutoffFrequency = vacLPFfreq;

        if (vehicle)
            MoveVehicle();
        else
            MoveOnFoot();

        if (Input.GetKeyDown(KeyCode.E)) Interact();
    }
    
    void Interact() {
        if (vehicle) {
            if (vehicle.landed)
                ExitVehicle();
            else
                vehicle.Land();
        } else {
            RaycastHit rh;
            if (Physics.Raycast(eye.transform.position, eye.transform.forward, out rh, 2f)){
                Ship ship = rh.transform.GetComponentInParent<Ship>();
                if (ship)
                    EnterVehicle(ship);
            }
        }
    }

    void MoveVehicle() {
        eye.transform.localRotation = Quaternion.RotateTowards(eye.transform.localRotation,
            Quaternion.identity,
            360f * Time.deltaTime);

        move = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
            move += Vector3.forward;
        if (Input.GetKey(KeyCode.S))
            move += Vector3.back;
        if (Input.GetKey(KeyCode.D))
            move += Vector3.right;
        if (Input.GetKey(KeyCode.A))
            move += Vector3.left;

        if (Input.GetKey(KeyCode.Space)) {
            if (vehicle.landed)
                vehicle.TakeOff();

            move += Vector3.up;
        }
        if (Input.GetKey(KeyCode.LeftControl))
            move += Vector3.down;

        vehicle.throttle = new Vector3(0, move.y, move.z);
        vehicle.pitchYawRoll = new Vector3(-euler.x, move.x, -euler.y);
        euler = Vector3.zero;

        vehicle.firing = Input.GetMouseButton(0);
        if (!vehicle.warping) {
            vehicle.engageWarp = Input.GetKey(KeyCode.LeftShift);
        } else if (Input.GetKey(KeyCode.LeftShift))
                vehicle.ExitWarp();
    }
    void MoveOnFoot() {
        // rotate camera & body
        if (!ScaleSpace.instance || gravity.planet) {
            euler.x = Mathf.Clamp(euler.x, -90, 90);
            euler.z = 0;

            transform.rotation = Quaternion.FromToRotation(Vector3.up, gravity.up) * Quaternion.Euler(0, euler.y, 0);
            eye.transform.localRotation = Quaternion.Euler(euler.x, 0, 0);
        } else {
            transform.rotation = Quaternion.Euler(euler);
            eye.transform.localRotation = Quaternion.identity;
        }

        move = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
            move += Vector3.forward;
        if (Input.GetKey(KeyCode.S))
            move += Vector3.back;
        if (Input.GetKey(KeyCode.D))
            move += Vector3.right;
        if (Input.GetKey(KeyCode.A))
            move += Vector3.left;

        jump = Input.GetKeyDown(KeyCode.Space);
        run = Input.GetKey(KeyCode.LeftShift);

        move = (transform.rotation * move).normalized;
        move *= run ? runSpeed : walkSpeed;
    }
}
