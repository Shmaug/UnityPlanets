using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FreeCam : MonoBehaviour {
    public float moveSpeed = 5.0f;
    public float lookSensitivity = 1.5f;

    Vector3 euler;

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
        transform.rotation = Quaternion.Euler(euler);

        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
            move += Vector3.forward;
        if (Input.GetKey(KeyCode.S))
            move += Vector3.back;
        if (Input.GetKey(KeyCode.D))
            move += Vector3.right;
        if (Input.GetKey(KeyCode.A))
            move += Vector3.left;
        transform.position += (transform.rotation * move.normalized) * moveSpeed * Time.deltaTime;
    }
}
