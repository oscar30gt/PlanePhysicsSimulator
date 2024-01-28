using UnityEngine;

public class Thruster : MonoBehaviour
{
    [Header("Power")]
    [SerializeField] private float thrustForce;
    [SerializeField] private float maxOpening;

    [Header("Vectorial Thrust")] 
    [SerializeField] private float maxAngle;
    [SerializeField] private AerodynamicObject parent;

    [Header("Visuals")]
    [SerializeField] private float maxParticles;
    [SerializeField] private Transform particlesParent;
    [SerializeField] private ParticleSystem[] particles;
    [Space]
    [SerializeField] private Transform topCover;
    [SerializeField] private Transform bottomCover;

    private float Power;
    private float Angle;

    void FixedUpdate()
    {
        parent.AddForceAtPosition(Power * thrustForce * particlesParent.forward, particlesParent.position, ForceMode.Acceleration);
    }

    /// <summary>
    /// Updates the opening of the thruster and the angle of vectorial thrust
    /// </summary>
    public void SetAngle() { SetAngle(Angle); }
    public void SetAngle(float angle)
    {
        Angle = Mathf.Clamp(angle, -1, 1);
        Angle *= maxAngle;
        
        particlesParent.localEulerAngles = new(angle, 0, 0);

        float opening = maxOpening * Power;
        topCover.localEulerAngles = new(Angle - opening, topCover.localEulerAngles.y, topCover.localEulerAngles.z);
        bottomCover.localEulerAngles = new(Angle + opening, topCover.localEulerAngles.y, topCover.localEulerAngles.z);
    }

    /// <summary>
    /// Updates the power of the thruster
    /// </summary>
    /// <param name="power"> From 0 to 1, the power of the thruster </param>
    public void SetPower(float power)
    {
        Power = Mathf.Clamp01(power);

        // Sets the power (Initial speed) of each thruster
        foreach (ParticleSystem thruster in particles)
        {
            ParticleSystem.EmissionModule main = thruster.emission;
            main.rateOverTime = Power * maxParticles;

            if (Power == 0)
            {
                thruster.Stop();
                continue;
            }

            thruster.Play();
        }

        SetAngle();
    }
}