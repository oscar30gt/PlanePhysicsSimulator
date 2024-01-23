using TMPro;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PhysicalSurface : MonoBehaviour
{
    [SerializeField, Tooltip("Whether the surface will be a movable part of the object")] private bool dynamic;
    [SerializeField, Tooltip("Object this surface belongs to")] private PhysicalObject parent;

    [HideInInspector] public Sprite sprite;

    private readonly Color dynamicColor = Color.green;
    private readonly Color staticColor = Color.blue;

    // Displacement since last FixedUpdate() call
    private Vector3 deltaPosition;

    // Position where the surface was at the last frame
    private Vector3 lastFramePos;

    private new SpriteRenderer renderer;

    private TextMeshPro details;

    private void OnValidate()
    {
        renderer = GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.color = dynamic ? dynamicColor : staticColor;
    }

    void Start()
    {
        renderer = GetComponent<SpriteRenderer>();
        deltaPosition = Vector3.zero;
        lastFramePos = transform.position;

#if UNITY_EDITOR
        if (parent?.showSurfacesDuringPlaymode ?? false)
        {
            GameObject detailsInfo = new("Info");
            details = detailsInfo.AddComponent<TextMeshPro>();
            details.fontSize = 6;
            details.alignment = TextAlignmentOptions.Center;

            details.gameObject.layer = 20;
            details.gameObject.transform.SetParent(transform, false);
        }
    }
#endif

    // FixedUpdate used for physics-related tasks
    void FixedUpdate()
    {
        if (parent != null)
        {
            deltaPosition = transform.position - lastFramePos;
            GetNormalForce();
            lastFramePos = transform.position;
        }
    }

    /// <summary>
    /// Calculates the force performed over the surface's normal
    /// </summary>
    /// <returns> The magnitude of that force </returns>
    private float GetNormalForce()
    {
        // Calculates the relative movement, according to the wind. When there's no air, raw and real displacements are equal.
        Vector3 actualDisplacement = deltaPosition - Meteorology.WindVelocity * Time.fixedDeltaTime;
        Vector3 normal = transform.forward;

        // Angle between the facing air and the surface's normal
        float angle = Vector3.Angle(normal, -actualDisplacement);
        float incidencePercentageOverNormal = Mathf.Cos(angle * Mathf.Deg2Rad);

#if UNITY_EDITOR
        // Visually shows how much force is the surface supporting
        // (From white to red, where more reddish values mean the surface is supporting a greater force. Green means the surface is rather neutral)
        if (parent.showSurfacesDuringPlaymode)
        {
            float lerp = Mathf.Abs(incidencePercentageOverNormal);
            renderer.color = lerp <= 0.05f ? Color.clear : Color.Lerp(Color.white, Color.red, lerp);
            details.text = incidencePercentageOverNormal.ToString("0.00");
            details.gameObject.transform.LookAt(Camera.allCameras[0].transform);
            details.transform.Rotate(0, 180, 0);
        }
#endif

        // Force performed over the normal
        float normalForce = incidencePercentageOverNormal * parent.FacingAirflow.magnitude;
        return normalForce;
    }
}