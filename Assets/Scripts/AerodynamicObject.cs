using UnityEngine;

/// <summary>
/// Meteorological info which affect flight
/// </summary>
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
public class AerodynamicObject : MonoBehaviour
{
    // ========== INSPECTOR ========== //

    public bool displaySurfacesDuringPlaymode;
    [Space] public float defaultForce;

    // ========== HIDDEN ========== //

    private new Rigidbody rigidbody;

    public Vector3 velocity 
    {
        get 
        { 
            return rigidbody.velocity; 
        } 
        private set { rigidbody.velocity = value; }
    }

    public float SpeedPercent 
    { 
        get
        {
            return velocity.magnitude / 40;
        } 
    }

    public Vector3 FacingAirflow 
    {
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

    public void AddForceAtPosition(Vector3 force, Vector3 position, ForceMode mode)
    {
        rigidbody.AddForceAtPosition(force, position, mode);

        Debug.DrawLine(position, position + force * 500);
    }

    public void AddForceAtPosition(Vector3 force, Vector3 position)
    {
        AddForceAtPosition(force, position, ForceMode.Force);
    }

    public void SetVelocity(Vector3 velocity)
    {
        rigidbody.velocity = velocity;
    }
}