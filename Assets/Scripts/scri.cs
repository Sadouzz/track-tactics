using UnityEngine;

public class scri : MonoBehaviour
{
    [Header("Car Settings")]
    public float acceleration = 1500f;
    public float maxSpeed = 20f;
    public float turnSpeed = 50f;
    public float driftFactor = 0.9f;
    public float grip = 0.85f;

    private Rigidbody rb;
    private float accelerationInput = 0f;
    private float steeringInput = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        accelerationInput = Input.GetAxis("Vertical");
        steeringInput = Input.GetAxis("Horizontal");
    }

    void FixedUpdate()
    {
        ApplyEngineForce();
        ApplySteering();
        ApplyGrip();
    }

    void ApplyEngineForce()
    {
        if (accelerationInput != 0)
        {
            Vector3 force = transform.forward * acceleration * accelerationInput;
            rb.AddForce(force, ForceMode.Acceleration);
        }

        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxSpeed);
    }

    void ApplySteering()
    {
        float speedFactor = rb.linearVelocity.magnitude / maxSpeed;
        float turnAmount = turnSpeed * steeringInput * speedFactor * Time.deltaTime;
        transform.Rotate(0, turnAmount, 0);
    }

    void ApplyGrip()
    {
        Vector3 forwardVelocity = transform.forward * Vector3.Dot(rb.linearVelocity, transform.forward);
        Vector3 sidewaysVelocity = transform.right * Vector3.Dot(rb.linearVelocity, transform.right);
        rb.linearVelocity = forwardVelocity + sidewaysVelocity * grip;
    }
}
