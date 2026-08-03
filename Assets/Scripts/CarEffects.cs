using System.Collections;
using TMPro;
using UnityEngine;

public class CarEffects : MonoBehaviour
{
    public ParticleSystem[] smoke;
    private bool smokeFlag = false, playSmoke;
    private Rigidbody rb;
    public TextMeshProUGUI text;
    private float carSpeed, carSpeedSideways;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        text.text = $"{rb.linearVelocity.magnitude:F2} / {Mathf.Abs(carSpeedSideways):F2}";
    }

    private void FixedUpdate()
    {
        carSpeed = Vector3.Dot(rb.transform.forward, rb.linearVelocity);
        carSpeedSideways = Vector3.Dot(rb.transform.right, rb.linearVelocity);

        // V�rifier si la voiture glisse lat�ralement
        playSmoke = Mathf.Abs(carSpeedSideways) > 0.3f; // Activation d�s qu'on d�passe 0.5

        if (playSmoke && !smokeFlag)
        {
            StartSmoke();
        }
        else if (!playSmoke && smokeFlag)
        {
            StopSmoke();
        }

        if (smokeFlag)
        {
            for (int i = 0; i < smoke.Length; i++)
            {
                var emission = smoke[i].emission;
                emission.rateOverTime = (rb.linearVelocity.magnitude >= 3) ? 20 : 1; // Invers� pour plus de logique
            }
        }
    }

    public void StartSmoke()
    {
        if (smokeFlag) return;

        foreach (var s in smoke)
        {
            var emission = s.emission;
            emission.rateOverTime = (rb.linearVelocity.magnitude >= 3) ? 20 : 1;
            s.Play();
        }
        smokeFlag = true;
    }

    public void StopSmoke()
    {
        if (!smokeFlag) return;

        foreach (var s in smoke)
        {
            s.Stop();
        }
        smokeFlag = false;
    }
}
