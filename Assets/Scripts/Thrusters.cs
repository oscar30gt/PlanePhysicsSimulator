using UnityEngine;

public class Thrusters : MonoBehaviour
{
    [SerializeField] private float maxPower;
    [SerializeField] private float maxOpening;
    [Space]
    [SerializeField] private ParticleSystem[] particles;
    [SerializeField] private Transform top;
    [SerializeField] private Transform bottom;

    public void SetPower(float percentage)
    {
        percentage = Mathf.Clamp01(percentage);

        // Sets the power (Initial speed) of each thruster
        foreach (ParticleSystem thruster in particles)
        {
            ParticleSystem.EmissionModule main = thruster.emission;
            main.rateOverTime = percentage * maxPower;

            if (percentage == 0)
            {
                thruster.Stop();
                continue;
            }

            thruster.Play();
        }

        float angle = maxOpening * percentage;
        top.localEulerAngles = new Vector3(angle, top.localEulerAngles.y, top.localEulerAngles.z);
        bottom.localEulerAngles = new Vector3(-angle, top.localEulerAngles.y, top.localEulerAngles.z);
    }
}
