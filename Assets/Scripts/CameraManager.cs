using UnityEngine;
using UnityEngine.InputSystem;

public static class CameraInput
{
    // Range: [-1,1]
    public static float X;
    public static float Y;
}

public class CameraManager : MonoBehaviour
{
    [SerializeField, Tooltip("Object to follow")] private PlayerController target;
    [Header("Manual Camera")]
    [SerializeField, Min(0)] private float maxX;
    [SerializeField] private float sensX;
    [Space]
    [SerializeField, Min(0)] private float maxY;
    [SerializeField] private float sensY;
    [Header("Auto Camera")]
    [SerializeField, Min(0)] private float returnSlowness;
    [SerializeField, Range(0.1f, 1)] private float smoothness;
    [Space]
    [SerializeField] private float minSeparation;
    [SerializeField] private float maxSeparation;

    private Vector3 positionOffset;
    private Transform cameras;

    private float yawOffset;
    private float pitchOffset;

    void Start()
    {
        cameras = transform.GetChild(0);

        positionOffset = transform.position - target.transform.position;
        yawOffset = 0;
        pitchOffset = 0;
    }

    // Camera updates on fixed update due to the fact that the plane is moved by physics
    void FixedUpdate()
    {
        // Extra yaw rotation applied by the player
        yawOffset += Time.deltaTime * sensX * CameraInput.X;
        yawOffset = Mathf.Clamp(yawOffset, -maxX, maxX);

        // Extra pitch rotation applied by the player
        pitchOffset += Time.deltaTime * sensY * CameraInput.Y;
        pitchOffset = Mathf.Clamp(pitchOffset, -maxY, maxY);

        // When moving, slowy returns to the default rotation
        float lerp = target.physics.velocity.magnitude / returnSlowness;

        if (CameraInput.X == 0)
            yawOffset = Mathf.Lerp(yawOffset, 0, Time.deltaTime * lerp);

        if (CameraInput.Y == 0)
            pitchOffset = Mathf.Lerp(pitchOffset, 0, Time.deltaTime * lerp);

        // Copies the target position
        transform.position = target.transform.position + positionOffset;

        // Smoothly changes it rotation
        Quaternion newRotation = Quaternion.Euler(
            target.transform.localEulerAngles.x + pitchOffset,
            target.transform.localEulerAngles.y + yawOffset,
            target.transform.eulerAngles.z
        );

        transform.localRotation = Quaternion.Lerp(transform.localRotation, newRotation, Time.deltaTime / smoothness);

        // The more speed the plane has, the more distance the camera will have
        float separation = minSeparation + target.physics.SpeedPercent * (maxSeparation - minSeparation);
        cameras.localPosition = new(cameras.localPosition.x, cameras.localPosition.y, separation);
    }

    public void OnCameraRotate(InputAction.CallbackContext context)
    {
        Vector2 direction = context.ReadValue<Vector2>();

        CameraInput.X = direction.x;
        CameraInput.Y = -direction.y;
    }
}