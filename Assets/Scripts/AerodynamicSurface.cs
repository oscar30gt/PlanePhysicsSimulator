using System;
using TMPro;
using UnityEngine;

[Serializable]
public struct BooleanVector3
{
    public bool x;
    public bool y;
    public bool z;

    public readonly Vector3 vector
    {
        get
        {
            return new(x ? 1 : 0, y ? 1 : 0, z ? 1 : 0);
        }
    }

    public readonly Vector3 inverseVector
    {
        get
        {
            return new(x ? 0 : 1, y ? 0 : 1, z ? 0 : 1);
        }
    }
}

/// <summary>
/// Physical surfaces are zones which are affected by t
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class AerodynamicSurface : MonoBehaviour
{
    #region Variables

    [SerializeField, Tooltip("Whether the surface will be a movable part of the object")] private bool dynamic;
    [SerializeField, Tooltip("Object this surface belongs to")] private AerodynamicObject parent;
    [SerializeField, Tooltip("A bigger area will produce larger forces. Despite this, you can manually modify the force produced over the object")] private float forceMultiplier = 1;
    [SerializeField, Tooltip("Local axes the surface will not affect")] private BooleanVector3 clampAxes;

    [HideInInspector] public Sprite sprite;

    // Displacement since last FixedUpdate() call
    private Vector3 deltaPosition;

    // Position where the surface was at the last FixedUpdate() call
    private Vector3 lastFramePos;

    // Total surface area. The larger the area, the more force it receives.
    private float area;

    private SpriteRenderer surfaceRenderer;
    private TextMeshPro details;

    #endregion

    #region UnityEventSystem
    
    private void Reset()
    {
        surfaceRenderer = GetComponent<SpriteRenderer>();
        surfaceRenderer.sprite = sprite;
        surfaceRenderer.drawMode = SpriteDrawMode.Tiled;
    }

    private void OnValidate()
    {
        surfaceRenderer = GetComponent<SpriteRenderer>();
        surfaceRenderer.color = dynamic ? Color.cyan : Color.blue;
    }

    private void OnEnable()
    {
        lastFramePos = transform.position;
    }

    void Start()
    {
        surfaceRenderer = GetComponent<SpriteRenderer>();
        deltaPosition = Vector3.zero;
        lastFramePos = transform.position;

        area = surfaceRenderer.size.x * surfaceRenderer.size.y;

#if UNITY_EDITOR
        if (parent?.displaySurfacesDuringPlaymode ?? false)
        {
            GameObject detailsInfo = new("Info");
            details = detailsInfo.AddComponent<TextMeshPro>();
            details.fontSize = 6;
            details.alignment = TextAlignmentOptions.Center;

            details.gameObject.layer = 20;
            details.gameObject.transform.SetParent(transform, false);
        }
#endif
    }

    // FixedUpdate is used for physics-related tasks
    void FixedUpdate()
    {
        // The surface must belong to a parent, it can't work itself
        if (parent == null) return;
        
        deltaPosition = transform.position - lastFramePos;

        float force = GetNormalForce();
        ApplyForceOverObject(force);
        
        lastFramePos = transform.position;
    }

    private void OnDisable()
    {
        surfaceRenderer.color = Color.clear;
    }

    #endregion

    /// <summary>
    /// Calculates the force the air performs over the surface's normal
    /// </summary>
    /// <returns> The magnitude of that force </returns>
    private float GetNormalForce()
    {
        // Calculates the relative movement, according to the wind. When there's no wind, raw and real displacements are equal.
        Vector3 actualDisplacement = deltaPosition - Meteorology.WindVelocity * Time.fixedDeltaTime;
        Vector3 normal = transform.forward;

        // Angle between the facing air and the surface's normal
        float angle = Vector3.Angle(normal, -actualDisplacement);
        float incidencePercentageOverNormal = Mathf.Cos(angle * Mathf.Deg2Rad);  // [0, 1] range

#if UNITY_EDITOR
        // Visually shows how much force is the surface supporting
        // (From white to red, where more reddish values mean the surface is supporting a greater force)
        if (parent.displaySurfacesDuringPlaymode)
        {
            float lerp = Mathf.Abs(incidencePercentageOverNormal);
            surfaceRenderer.color = lerp <= 0.05f ? Color.clear : Color.Lerp(Color.white, Color.red, lerp);

            details.text = incidencePercentageOverNormal.ToString("0.00");
            details.gameObject.transform.LookAt(Camera.allCameras[0].transform);
            details.transform.Rotate(0, 180, 0);
        }
        else
        {
            surfaceRenderer.color = Color.clear;
        }
#else
            surfaceRenderer.color = Color.clear;
#endif
        /*
         * Force performed over the normal
         *
         * F = cos(a) * A * v^2
         *
         * F : Force over the normal
         * a : Air's incidence angle
         * A : Surface's area
         * v : Speed of the surface
         */
        float normalForce = incidencePercentageOverNormal * area * Mathf.Pow(actualDisplacement.magnitude, 2);
        return normalForce;
    }


    /// <summary>
    /// Adds a torque to the object from the surface's position, following it normal's direction.
    /// </summary>
    /// <param name="forceMagnitude"> Magnitude of the force to apply over the normal</param>
    private void ApplyForceOverObject(float forceMagnitude)
    {
        Vector3 forceDirection = transform.forward;

        Vector3 objectForce = parent.transform.InverseTransformVector(forceDirection);
        objectForce = new(
            clampAxes.inverseVector.x * objectForce.x,
            clampAxes.inverseVector.y * objectForce.y, 
            clampAxes.inverseVector.z * objectForce.z
            );

        forceDirection = parent.transform.TransformVector(objectForce);
        Vector3 totalForce = parent.defaultForce * forceMultiplier * forceMagnitude * forceDirection;

        parent.AddForceAtPosition(totalForce, transform.position, ForceMode.VelocityChange);
    }
}