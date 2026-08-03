using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CarMovementOffline : MonoBehaviour
{
    [Header("Car Settings")]
    public float speed = 50f;
    public float tempSpeed;
    public float drag = 0.95f;
    public float angleSpeed = 5f;
    public float traction = 1f;

    [Header("Steering")]
    public float currentTurnAngle = 0f;
    public float maxTurnAngle = 5f;
    public float turnAcceleration = 10f;

    [Header("Components")]
    public Rigidbody rb;

    // Variables locales
    private Vector3 moveForce;
    private float steerInput, accelInput;

    // Pour l'interpolation smooth
    private float lerpRate = 15f;

    void Start()
    {

    }


    void FixedUpdate()
    {

            HandleInput();
            HandleMovement();
    }

    void HandleInput()
    {
        accelInput = 1/*Input.GetAxis("Vertical")*/;
        steerInput = GetSteerInput();
    }

    void HandleMovement()
    {
        // Logique de mouvement (identique à votre code original)
        moveForce += transform.forward * speed * accelInput * Time.fixedDeltaTime;
        transform.position += moveForce * Time.fixedDeltaTime;
        moveForce *= drag;
        moveForce = Vector3.ClampMagnitude(moveForce, speed / 3);

        moveForce = Vector3.Lerp(moveForce.normalized, transform.forward, traction * Time.fixedDeltaTime) * moveForce.magnitude;

        if (steerInput != 0)
        {
            if (steerInput < 0)
            {
                MoveLeft();
            }
            else
            {
                MoveRight();
            }
        }

    }


    private float GetSteerInput()
    {
        float steer = Input.GetAxis("Horizontal");

        // Support tactile/mobile
        if (Input.GetMouseButton(0))
        {
            float x = Input.mousePosition.x;
            if (x < Screen.width / 2f)
                steer = -1f;
            else if (x > Screen.width / 2f)
                steer = 1f;
        }

        return steer;
    }

    public void MoveLeft()
    {
        transform.Rotate(-Vector3.up * moveForce.magnitude * angleSpeed * Time.fixedDeltaTime);
    }

    public void MoveRight()
    {
        transform.Rotate(Vector3.up * moveForce.magnitude * angleSpeed * Time.fixedDeltaTime);
    }
}