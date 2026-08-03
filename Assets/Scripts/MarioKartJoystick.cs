using UnityEngine;

public class MarioKartJoystick : MonoBehaviour
{
    [Header("Joystick Reference")]
    public Joystick joystick;

    [Header("Car Control")]
    [SerializeField] private float maxSteeringAngle = 30f;
    [SerializeField] private float accelerationForce = 10f;
    [SerializeField] private float maxSpeed = 50f;
    [SerializeField] private float driftFactor = 0.95f;
    [SerializeField] private float deadZone = 0.2f;

    private Rigidbody carRigidbody;
    private bool isDrifting = false;

    private void Awake()
    {
        carRigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        ControlCar();
    }

    private void ControlCar()
    {
        float h = joystick.Horizontal;
        float v = 1;
        Vector2 input = new Vector2(h, v);

        // Rotation (steering)
        float steering = h * maxSteeringAngle;
        Quaternion deltaRotation = Quaternion.Euler(0f, steering * Time.deltaTime, 0f);
        carRigidbody.MoveRotation(carRigidbody.rotation * deltaRotation);

        // Drift si le joueur pousse beaucoup en lat�ral
        isDrifting = input.magnitude > deadZone && Mathf.Abs(h) > 0.7f;

        // Acc�l�ration
        if (input.magnitude > deadZone)
        {
            Vector3 force = transform.forward * accelerationForce * v;

            if (isDrifting)
            {
                carRigidbody.AddForce(force * driftFactor);
                Debug.Log("Drifting!");
            }
            else
            {
                carRigidbody.AddForce(force);
            }

            // Limite de vitesse
            if (carRigidbody.linearVelocity.magnitude > maxSpeed)
            {
                carRigidbody.linearVelocity = carRigidbody.linearVelocity.normalized * maxSpeed;
            }
        }
    }
}
