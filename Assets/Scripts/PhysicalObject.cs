using UnityEngine;

public static class Meteorology
{
    public static Vector3 WindVelocity;
    public static float AirDensity;

    static Meteorology()
    {
        WindVelocity = Vector3.zero;
        AirDensity = 1;
    }
}

[RequireComponent(typeof(Rigidbody))]
public class PhysicalObject : MonoBehaviour
{
    public bool showSurfacesDuringPlaymode;

    private new Rigidbody rigidbody;

    public Vector3 FacingAirflow {
        get
        {
            return -rigidbody.velocity + Meteorology.WindVelocity;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
