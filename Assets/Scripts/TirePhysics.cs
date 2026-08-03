using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TirePhysics : MonoBehaviour
{
    public Joystick joystick;
    public Rigidbody carRigidbody;
    private Transform tireTransform;
    public Transform carTransform, wheelVisual;
    public float suspensionRestDist, springStrength, springDamper, tireMass, tireGripFactor, carTopSpeed;
    public float maxSteerAngle;
    public bool isFrontWheel = false;
    public float steerInput, accelInput;
    public AnimationCurve powerCurve;
    public LayerMask groundLayer;
    private float currentSteerAngle = 0f;


    [Header("Drift Settings")]
    public bool drifting = false;
    private int driftDirection;
    private float driftPower;
    private int driftMode;
    private bool first, second, third;
    public Transform wheelParticles;
    public Transform flashParticles;
    public Color[] turboColors;

    [Header("Particles")]
    public List<ParticleSystem> primaryParticles = new List<ParticleSystem>();
    public List<ParticleSystem> secondaryParticles = new List<ParticleSystem>();

    void Start()
    {
        tireTransform = gameObject.GetComponent<Transform>();

        /*for (int i = 0; i < wheelParticles.GetChild(0).childCount; i++)
        {
            primaryParticles.Add(wheelParticles.GetChild(0).GetChild(i).GetComponent<ParticleSystem>());
        }

        for (int i = 0; i < wheelParticles.GetChild(1).childCount; i++)
        {
            primaryParticles.Add(wheelParticles.GetChild(1).GetChild(i).GetComponent<ParticleSystem>());
        }

        foreach (ParticleSystem p in flashParticles.GetComponentsInChildren<ParticleSystem>())
        {
            secondaryParticles.Add(p);
        }*/
    }

    void FixedUpdate()
    {
        accelInput =Input.GetAxis("Vertical");
        steerInput = GetSteerInput();


        /*bool shouldDrift = Mathf.Abs(steerInput) > 0.2f && carRigidbody.velocity.magnitude > 1f;

        if (shouldDrift && !drifting)
        {
            drifting = true;
            driftDirection = steerInput > 0 ? 1 : -1;
            foreach (ParticleSystem p in primaryParticles)
            {
                var main = p.main;
                main.startColor = Color.clear;
                p.Play();
            }
        }

        if (!shouldDrift && drifting)
        {
            Boost();
        }

        if (drifting)
        {
            float control = driftDirection == 1 ? Mathf.InverseLerp(-1, 1, steerInput) * 2 : Mathf.InverseLerp(-1, 1, steerInput) * -2 + 2;
            float powerControl = driftDirection == 1 ? Mathf.InverseLerp(-1, 1, steerInput) * 0.8f + 0.2f : 1 - (Mathf.InverseLerp(-1, 1, steerInput) * 0.8f + 0.2f);
            driftPower += 1f;
            HandleDriftColors();
        }
        /*if (Mathf.Abs(steerInput) > 0.5f)
        {
            float resistanceFactor = Mathf.Abs(steerInput) * 0.2f; // Ajuste l'intensit�
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(carRigidbody.velocity, Vector3.up);
            Vector3 resistance = -horizontalVelocity * resistanceFactor;
            carRigidbody.AddForce(resistance, ForceMode.Acceleration);
        }*/


        Vector3 accel = Acceleration();
        Vector3 suspension = Suspension();
        Vector3 steer = Steering();
        //WheelVisual();
        //AntiRoll();
        Debug.DrawRay(tireTransform.position, suspension, Color.red);
        Debug.DrawRay(tireTransform.position, accel + suspension + steer, Color.yellow);
    }

    void Boost()
    {
        drifting = false;
        if (driftMode > 0)
        {
            //carRigidbody.AddForce(carTransform.forward * (driftMode * 100f), ForceMode.Impulse);
        }
        driftPower = 0;
        driftMode = 0;
        first = second = third = false;
        foreach (ParticleSystem p in primaryParticles)
        {
            var main = p.main;
            main.startColor = Color.clear;
            p.Stop();
        }
    }

    void HandleDriftColors()
    {
        Color c = Color.clear;
        if (driftPower > 50 && driftPower < 100 && !first)
        {
            first = true;
            driftMode = 1;
            c = turboColors[0];
        }
        if (driftPower > 100 && driftPower < 150 && !second)
        {
            second = true;
            driftMode = 2;
            c = turboColors[1];
        }
        if (driftPower > 150 && !third)
        {
            third = true;
            driftMode = 3;
            c = turboColors[2];
        }

        foreach (ParticleSystem p in primaryParticles)
        {
            var main = p.main;
            main.startColor = c;
        }
    }

    private float GetSteerInput()
    {
        float steer = Input.GetAxis("Horizontal"); // Clavier / manette / gyroscope

        if (Input.GetMouseButton(0))
        {
            float x = Input.mousePosition.x;

            if (x < Screen.width / 2f)
                steer = -1f;
            else if (x > Screen.width / 2f)
                steer = 1f;
        }
        /*if (Input.GetMouseButton(0))

            steer = joystick.Horizontal;*/

        return steer;
    }


    /*void WheelVisual()
    {
        if (wheelVisual != null)
        {
            RaycastHit hit;
            Vector3 startPos = tireTransform.position;
            Vector3 endPos = tireTransform.position - tireTransform.up * suspensionRestDist;

            if (Physics.Raycast(startPos, -tireTransform.up, out hit, suspensionRestDist, groundLayer))
            {
                wheelVisual.position = hit.point + (tireTransform.up * 0.3f);
            }
            else
            {
                wheelVisual.position = endPos;
            }

            float wheelRadius = wheelVisual.localScale.y / 2;
            float rotationSpeed = carRigidbody.velocity.magnitude / (2 * Mathf.PI * wheelRadius) * Mathf.Rad2Deg;
            float direction = Vector3.Dot(carRigidbody.velocity, transform.forward) >= 0 ? 1f : -1f;
            wheelVisual.Rotate(Vector3.right, direction * rotationSpeed * 2 * Time.deltaTime);
        }
    }*/

    Vector3 Acceleration()
    {
        RaycastHit tireRay;
        Vector3 accelDir = tireTransform.forward;
        if (Physics.Raycast(tireTransform.position, -tireTransform.up, out tireRay, suspensionRestDist, groundLayer) && !isFrontWheel)
        {
            float carSpeed = Vector3.Dot(carTransform.forward, carRigidbody.linearVelocity);
            float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / carTopSpeed);

            /*float turnFactor = Mathf.Lerp(1f, 0.7f, Mathf.Abs(steerInput)); // R�duit la puissance jusqu�� 70% en virage max
            float availableTorque = powerCurve.Evaluate(normalizedSpeed) * accelInput * turnFactor;*/

            float availableTorque = /*powerCurve.Evaluate(normalizedSpeed) **/ accelInput;
            carRigidbody.AddForceAtPosition(accelDir * availableTorque, tireTransform.position, ForceMode.Impulse);
            return accelDir * availableTorque;
        }
        return Vector3.zero;
    }

    Vector3 Suspension()
    {
        RaycastHit tireRay;
        Vector3 springDir = tireTransform.up;
        if (Physics.Raycast(tireTransform.position, -springDir, out tireRay, suspensionRestDist, groundLayer))
        {
            Vector3 tireWorldVel = carRigidbody.GetPointVelocity(tireTransform.position);
            float offset = suspensionRestDist - tireRay.distance;
            float vel = Vector3.Dot(springDir, tireWorldVel);
            float force = (offset * springStrength) - (vel * springDamper);
            carRigidbody.AddForceAtPosition(springDir * force, tireTransform.position);
            return springDir * force;
        }
        return Vector3.zero;
    }

    Vector3 Steering()
    {
        /*if (isFrontWheel)
        {
            // Applique directement la rotation au ch�ssis (comme votre exemple)
            float steerForce = steerInput * maxSteerAngle;
            carRigidbody.transform.Rotate(
                Vector3.up * steerForce * Time.fixedDeltaTime,
                Space.Self
            );
        }
        return Vector3.zero;*/
        if (isFrontWheel)
        {
            float steerAngle = maxSteerAngle * steerInput;
            tireTransform.localRotation = Quaternion.Euler(0f, steerAngle, 0f);
        }

        if (Physics.Raycast(tireTransform.position, -tireTransform.up, out RaycastHit hit, suspensionRestDist, groundLayer))
        {
            Vector3 lateralDir = tireTransform.right;
            Vector3 wheelVel = carRigidbody.GetPointVelocity(tireTransform.position);
            float latVel = Vector3.Dot(lateralDir, wheelVel);
            float desiredVelChange = -latVel * tireGripFactor;
            float desiredAccel = desiredVelChange / Time.fixedDeltaTime;
            carRigidbody.AddForceAtPosition(lateralDir * tireMass * desiredAccel, tireTransform.position);
            return lateralDir * tireMass * desiredAccel;
        }
        return Vector3.zero ;
    }
}

/*
using System.Collections.Generic;
using UnityEngine;

public class TirePhysics : MonoBehaviour
{
    [Header("R�f�rences")]
    public Rigidbody carRigidbody;
    public Transform carTransform;
    public Transform wheelVisual;
    private Transform tireTransform;

    [Header("Physique de roue")]
    public float suspensionRestDist = 0.4f;
    public float springStrength = 20000f;
    public float springDamper = 2500f;
    public float tireMass = 20f;
    public float tireGripFactor = 1f;
    public float carTopSpeed = 100f;
    public float maxSteerAngle = 25f;
    public bool isFrontWheel = false;
    public LayerMask groundLayer;

    [Header("Inputs externes")]
    [Range(-1, 1)] public float steerInput = 0f;
    [Range(0, 1)] public float accelInput = 1f;
    public AnimationCurve powerCurve;

    void Start()
    {
        tireTransform = transform;
    }

    void FixedUpdate()
    {
        accelInput = Input.GetAxis("Vertical");
        steerInput = GetSteerInput();
        ApplyAcceleration();
        ApplySuspension();
        ApplySteering();
        UpdateWheelVisual();
    }

    private float GetSteerInput()
    {
        float steer = Input.GetAxis("Horizontal"); // Clavier / manette / gyroscope

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

    void ApplyAcceleration()
    {
        if (Physics.Raycast(tireTransform.position, -tireTransform.up, out RaycastHit hit, suspensionRestDist, groundLayer))
        {
            float carSpeed = Vector3.Dot(carTransform.forward, carRigidbody.velocity);
            float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / carTopSpeed);
            float availableTorque = powerCurve.Evaluate(normalizedSpeed) * accelInput;
            Vector3 force = tireTransform.forward * availableTorque;
            carRigidbody.AddForceAtPosition(force, tireTransform.position);
        }
    }

    void ApplySuspension()
    {
        if (Physics.Raycast(tireTransform.position, -tireTransform.up, out RaycastHit hit, suspensionRestDist, groundLayer))
        {
            Vector3 springDir = tireTransform.up;
            Vector3 wheelVel = carRigidbody.GetPointVelocity(tireTransform.position);
            float offset = suspensionRestDist - hit.distance;
            float vel = Vector3.Dot(springDir, wheelVel);
            float force = (offset * springStrength) - (vel * springDamper);
            carRigidbody.AddForceAtPosition(springDir * force, tireTransform.position);
        }
    }

    void ApplySteering()
    {
        if (isFrontWheel)
        {
            float steerAngle = maxSteerAngle * steerInput;
            tireTransform.localRotation = Quaternion.Euler(0f, steerAngle, 0f);
        }

        if (Physics.Raycast(tireTransform.position, -tireTransform.up, out RaycastHit hit, suspensionRestDist, groundLayer))
        {
            Vector3 lateralDir = tireTransform.right;
            Vector3 wheelVel = carRigidbody.GetPointVelocity(tireTransform.position);
            float latVel = Vector3.Dot(lateralDir, wheelVel);
            float desiredVelChange = -latVel * tireGripFactor;
            float desiredAccel = desiredVelChange / Time.fixedDeltaTime;
            carRigidbody.AddForceAtPosition(lateralDir * tireMass * desiredAccel, tireTransform.position);
        }
    }

    void UpdateWheelVisual()
    {
        if (!wheelVisual) return;

        if (Physics.Raycast(tireTransform.position, -tireTransform.up, out RaycastHit hit, suspensionRestDist, groundLayer))
        {
            wheelVisual.position = hit.point + tireTransform.up * 0.3f;
        }
        else
        {
            wheelVisual.position = tireTransform.position - tireTransform.up * suspensionRestDist;
        }

        float wheelRadius = wheelVisual.localScale.y / 2f;
        float rotSpeed = carRigidbody.velocity.magnitude / (2 * Mathf.PI * wheelRadius) * Mathf.Rad2Deg;
        float direction = Vector3.Dot(carRigidbody.velocity, carTransform.forward) >= 0 ? 1f : -1f;
        wheelVisual.Rotate(Vector3.right, direction * rotSpeed * 2 * Time.deltaTime);
    }
}
*/